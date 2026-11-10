using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ermo
{
    // Metering/control only. PixelDisplay owns the existing screen/render targets.
    [DefaultExecutionOrder(100)]
    public sealed class CyberShotCamera : MonoBehaviour
    {
        public readonly CameraAutoControl Automatic = new CameraAutoControl();
        public float ExposureEV => Automatic.ExposureEV;
        public Vector3 WhiteBalance => Automatic.WhiteBalance;
        public float FocusDistance { get; private set; }
        public float FocusBlur { get; private set; }
        public float CameraMotion { get; private set; }
        public float RunningAmount { get; private set; }
        public int MeterReadbacks { get; private set; }
        public MotionBlur MotionBlur { get; private set; }
        private Camera view;
        private WorldSettings settings;
        private PixelDisplay display;
        private FirstPersonWalker walker;
        private UniversalAdditionalCameraData cameraData;
        private Volume volume;
        private VolumeProfile volumeProfile;
        private RenderTexture meter;
        private Texture2D fallbackMeter;
        private bool meterPending, disposed, previousPose, synchronousMeter;
        private float nextMeter, nextFocus, focusTarget;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private CyberShotProfile Profile => settings.cameraProfile;

        public void Initialize(Camera camera, WorldSettings config, PixelDisplay screen)
        {
            view = camera; settings = config; display = screen;
            walker = camera.GetComponentInParent<FirstPersonWalker>();
            FocusDistance = focusTarget = Profile.focusDistance;
            Automatic.Reset(config.captureExposure);
            cameraData = view.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.requiresDepthTexture = true;
            // A private volume mask avoids inheriting SampleScene's cinematic
            // effects, without editing any existing Volume or pipeline asset.
            var volumeObject = new GameObject("Cyber-shot motion vectors volume") { layer = 31 };
            volumeObject.transform.SetParent(transform,false);
            volume = volumeObject.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 100;
            volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = volumeProfile;
            cameraData.volumeLayerMask = 1 << 31; cameraData.volumeTrigger = transform;
            // Explicit neutral overrides also protect against future changes to
            // URP's default volume profile. Digital effects have one owner only.
            volumeProfile.Add<Tonemapping>(true).mode.Override(TonemappingMode.None);
            volumeProfile.Add<Bloom>(true).intensity.Override(0);
            volumeProfile.Add<FilmGrain>(true).intensity.Override(0);
            volumeProfile.Add<ChromaticAberration>(true).intensity.Override(0);
            volumeProfile.Add<LensDistortion>(true).intensity.Override(0);
            volumeProfile.Add<Vignette>(true).intensity.Override(0);
            volumeProfile.Add<DepthOfField>(true).mode.Override(DepthOfFieldMode.Off);
            var balance = volumeProfile.Add<UnityEngine.Rendering.Universal.WhiteBalance>(true);
            balance.temperature.Override(0); balance.tint.Override(0);
            var grade = volumeProfile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(0); grade.contrast.Override(0); grade.saturation.Override(0); grade.colorFilter.Override(Color.white);
            MotionBlur = volumeProfile.Add<MotionBlur>(true);
            MotionBlur.mode.Override(MotionBlurMode.CameraAndObjects);
            MotionBlur.quality.Override(MotionBlurQuality.Medium);
            view.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
            meter = new RenderTexture(16,12,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear)
            { name = "Cyber-shot luminance / white balance meter", filterMode = FilterMode.Bilinear };
            meter.Create();
        }

        private void Update()
        {
            if (settings == null) return;
            Automatic.Advance(Time.deltaTime,Profile,Profile.exposureCompensation);
            if (!Profile.enableAutofocus) { FocusBlur = 0; FocusDistance = focusTarget = Profile.focusDistance; return; }
            if (Time.unscaledTime >= nextFocus)
            {
                nextFocus = Time.unscaledTime+.15f;
                focusTarget = Physics.Raycast(view.transform.position,view.transform.forward,out var hit,80,
                    Profile.focusMask,QueryTriggerInteraction.Ignore) ? Mathf.Max(Profile.minimumFocusDistance,hit.distance) : 80;
            }
            // Diopter error, not distance error: far-to-far transitions stay sharp.
            float error = Mathf.Abs(1/Mathf.Max(FocusDistance,.1f)-1/Mathf.Max(focusTarget,.1f));
            FocusDistance = Mathf.Lerp(FocusDistance,focusTarget,1-Mathf.Exp(-Time.deltaTime*Profile.focusAdaptationSpeed));
            FocusBlur = Mathf.Min(Profile.maximumFocusBlur,error*.12f);
        }

        private void LateUpdate()
        {
            if (settings == null) return;
            float dt = Mathf.Max(Time.deltaTime,.001f);
            float runTarget = walker != null && walker.IsRunning && !walker.MenuOpen && !walker.TraversalActive && !walker.WakingUp ? 1 : 0;
            RunningAmount = Mathf.Lerp(RunningAmount,runTarget,1-Mathf.Exp(-Time.deltaTime*5));
            float angle = previousPose ? Quaternion.Angle(previousRotation,view.transform.rotation) : 0;
            float distance = previousPose ? Vector3.Distance(previousPosition,view.transform.position) : 0;
            bool cut = angle > 40 || distance > 3;
            CameraMotion = cut ? 0 : Mathf.Clamp01(angle/dt/180+distance/dt*.06f);
            previousRotation = view.transform.rotation; previousPosition = view.transform.position; previousPose = true;
            bool active = Profile.motionBlur && display.EffectsEnabled && !display.ReducedMotion && !cut;
            // URP doubles its UV motion-vector velocity. Half the shutter/frame
            // ratio maps that filter's sampling span to the requested exposure.
            MotionBlur.intensity.Override(active ? Mathf.Clamp01(Profile.motionBlurIntensity*Profile.shutterSeconds/dt*.5f) : 0);
            MotionBlur.clamp.Override(Profile.maximumBlur);
        }

        public bool ScheduleMeter(CommandBuffer command, RenderTexture source)
        {
            if (disposed || meterPending || Time.unscaledTime < nextMeter || (!Profile.autoExposure && !Profile.autoWhiteBalance)) return false;
            nextMeter = Time.unscaledTime+Profile.meterInterval;
            command.Blit(source,meter);
            if (SystemInfo.supportsAsyncGPUReadback && !synchronousMeter)
            {
                meterPending = true;
                command.RequestAsyncReadback(meter,0,OnMeter);
                return false;
            }
            return true; // Tiny synchronous fallback, only at the metering rate.
        }

        private void OnMeter(AsyncGPUReadbackRequest request)
        {
            meterPending = false;
            if (disposed || this == null) return;
            if (request.hasError) { synchronousMeter = true; return; }
            MeterReadbacks++;
            var pixels = request.GetData<Color>();
            float logSum = 0, weightSum = 0; Color sum = Color.black;
            for (int i = 0; i < pixels.Length; i++) Accumulate(pixels[i],i,ref logSum,ref weightSum,ref sum);
            Automatic.Measure(Mathf.Exp(logSum/Mathf.Max(weightSum,.001f)),sum/Mathf.Max(weightSum,.001f));
        }

        public void ReadFallbackMeter()
        {
            if (fallbackMeter == null) fallbackMeter = new Texture2D(16,12,TextureFormat.RGBAFloat,false,true);
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = meter;
                fallbackMeter.ReadPixels(new Rect(0,0,16,12),0,0); fallbackMeter.Apply(false);
                var pixels = fallbackMeter.GetPixels();
                float logSum = 0, weights = 0; Color sum = Color.black;
                for (int i = 0; i < pixels.Length; i++) Accumulate(pixels[i],i,ref logSum,ref weights,ref sum);
                if (weights > 0) Automatic.Measure(Mathf.Exp(logSum/weights),sum/weights);
                MeterReadbacks++;
            }
            finally { RenderTexture.active = previous; }
        }

        private static void Accumulate(Color color, int i, ref float logSum, ref float weights, ref Color sum)
        {
            float luma = color.r*.2126f+color.g*.7152f+color.b*.0722f;
            if (float.IsNaN(luma) || float.IsInfinity(luma)) return;
            float x = ((i%16)+.5f)/16f*2-1, y = ((i/16)+.5f)/12f*2-1;
            float weight = .5f+Mathf.Exp(-(x*x+y*y)*2);
            logSum += Mathf.Log(Mathf.Clamp(luma,.001f,16))*weight;
            sum += color*weight; weights += weight;
        }

        private void OnDestroy()
        {
            disposed = true;
            if (meter != null) { meter.Release(); Destroy(meter); }
            if (fallbackMeter != null) Destroy(fallbackMeter);
            if (volumeProfile != null)
            {
                foreach (var component in volumeProfile.components) Destroy(component);
                Destroy(volumeProfile);
            }
        }
    }

    // Deterministic adaptation math, also exercised by editor validation.
    public sealed class CameraAutoControl
    {
        public float ExposureEV { get; private set; }
        public Vector3 WhiteBalance { get; private set; } = Vector3.one;
        public float Luminance { get; private set; } = .24f;
        private Color mean = Color.gray;
        public void Reset(float compensation) { ExposureEV = compensation; WhiteBalance = Vector3.one; }
        public void Measure(float luminance, Color average)
        {
            Luminance = Mathf.Clamp(luminance,.001f,16); mean = average;
        }
        public void Advance(float dt, CyberShotProfile p, float compensation)
        {
            float target = p.autoExposure ? Mathf.Clamp(Mathf.Log(p.middleGray/Luminance,2)+compensation,
                p.exposureLimits.x,p.exposureLimits.y) : compensation;
            float seconds = target < ExposureEV ? p.brightAdaptation : p.darkAdaptation;
            ExposureEV = p.autoExposure ? Mathf.Lerp(ExposureEV,target,1-Mathf.Exp(-dt/Mathf.Max(.01f,seconds))) : target;
            float gray = Mathf.Max(.001f,(mean.r+mean.g+mean.b)/3);
            Vector3 gain = new Vector3(Mathf.Clamp(gray/Mathf.Max(mean.r,.001f),.88f,1.12f),
                Mathf.Clamp(gray/Mathf.Max(mean.g,.001f),.88f,1.12f),Mathf.Clamp(gray/Mathf.Max(mean.b,.001f),.88f,1.12f));
            gain = p.autoWhiteBalance ? Vector3.Lerp(Vector3.one,gain,p.whiteBalanceStrength) : Vector3.one;
            WhiteBalance = Vector3.Lerp(WhiteBalance,gain,1-Mathf.Exp(-dt/Mathf.Max(.01f,p.whiteBalanceAdaptation)));
        }
    }
}
