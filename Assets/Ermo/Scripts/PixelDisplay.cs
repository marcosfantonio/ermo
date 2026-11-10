using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ermo
{
    public sealed class PixelDisplay : MonoBehaviour
    {
        private Camera view;
        [SerializeField] private WorldSettings settings;
        private RenderTexture target;
        private RenderTexture sensor, history;
        private bool historyValid;
        private float nextCapture;
        private int captureFrame;
        public int CapturedFrames => captureFrame;
        private CyberShotProfile lastProfile;
        private WaterPassage waterPassage;
        public CyberShotCamera CameraSimulation { get; private set; }
        public RenderTexture ProcessedFrame { get; private set; }
        private RawImage image;
        private Material effect;
        public Camera OutputCamera { get; private set; }
        private int lastWidth, lastHeight;
        public bool ReducedMotion => false;
        public bool EffectsEnabled => true;
        private bool lastPixelGrid;
        private bool lastPixelatedUpscale;
        private bool lastFourThree;
        private bool FourThree => settings.captureFourThree && settings.foundFootage && !settings.legacyVhs && !settings.godotPixelGrid;
        private int lastRenderHeight;
        public void Initialize(Camera camera, WorldSettings worldSettings, Shader shader, Camera outputCamera = null)
        {
            view = camera; settings = worldSettings;
            waterPassage = GetComponent<WaterPassage>();
            // The eye renders offscreen. A real display camera is also necessary:
            // an overlay Canvas alone does not count as a Game View camera.
            OutputCamera = outputCamera;
            if (OutputCamera == null)
            {
                var outputObject = new GameObject("Display 1 output");
                outputObject.transform.SetParent(transform, false);
                OutputCamera = outputObject.AddComponent<Camera>();
            }
            OutputCamera.targetDisplay = 0;
            OutputCamera.cullingMask = 0;
            OutputCamera.clearFlags = CameraClearFlags.SolidColor;
            OutputCamera.backgroundColor = Color.black;
            OutputCamera.depth = view.depth + 1;
            OutputCamera.allowHDR = false;
            OutputCamera.allowMSAA = false;
            OutputCamera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            var canvasObject = new GameObject("Pixel screen");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100;
            var screenObject = new GameObject("Lens", typeof(RectTransform)); screenObject.transform.SetParent(canvasObject.transform, false);
            image = screenObject.AddComponent<RawImage>(); image.raycastTarget = false;
            var rect = image.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            effect = new Material(shader);
            view.allowMSAA = false; view.allowHDR = true;
            CameraSimulation = view.gameObject.AddComponent<CyberShotCamera>();
            CameraSimulation.Initialize(view,settings,this);
            UpdateEffect();
            RenderPipelineManager.endCameraRendering += ProcessFrame;
            Resize();
        }
        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight || lastPixelGrid != settings.godotPixelGrid || lastRenderHeight != settings.renderHeight || lastPixelatedUpscale != settings.pixelatedUpscale || lastFourThree != FourThree) Resize();
            if (lastProfile != settings.cameraProfile)
            {
                historyValid = false; nextCapture = 0;
                lastProfile = settings.cameraProfile;
            }
            UpdateEffect();
        }
        private void UpdateEffect()
        {
            var profile = settings.cameraProfile;
            bool digital = settings.foundFootage && !settings.legacyVhs;
            effect.SetFloat("_Distortion", ReducedMotion ? 0 : (digital ? profile.barrelDistortion : settings.lensDistortion));
            effect.SetFloat("_Strength",EffectsEnabled ? 1 : 0);
            effect.SetFloat("_Grain",digital ? profile.luminanceNoise : settings.filmGrain);
            effect.SetFloat("_Vignette",digital ? profile.vignette : settings.vignette);
            effect.SetFloat("_Chromatic",ReducedMotion ? 0 : (digital ? profile.chromaticPixels : settings.chromatic));
            effect.SetFloat("_Dither",settings.dither);
            effect.SetFloat("_LegacyVhs",settings.legacyVhs ? 1 : 0);
            effect.SetFloat("_TapeStrength",EffectsEnabled && !ReducedMotion ? settings.tapeStrength : 0);
            effect.SetFloat("_Saturation",settings.tapeSaturation);
            effect.SetFloat("_Contrast",settings.tapeContrast);
            effect.SetFloat("_FoundFootage",settings.foundFootage ? 1 : 0);
            effect.SetFloat("_CaptureSoftness",profile.centerSoftness);
            effect.SetFloat("_CaptureSaturation",profile.saturation);
            effect.SetFloat("_CaptureContrast",profile.contrast);
            effect.SetFloat("_CaptureExposure",CameraSimulation.ExposureEV);
            effect.SetFloat("_CaptureSharpness",profile.sharpening);
            effect.SetFloat("_CaptureBloom",profile.bloom);
            effect.SetFloat("_DigitalCompression",profile.compression);
            effect.SetVector("_WhiteBalance",CameraSimulation.WhiteBalance);
            effect.SetFloat("_ChromaNoise",profile.chromaNoise);
            effect.SetFloat("_WhiteClip",profile.whiteClip);
            effect.SetFloat("_EdgeSoftness",profile.edgeSoftness);
            effect.SetFloat("_FocusBlur",CameraSimulation.FocusBlur);
            effect.SetFloat("_FlatQuantization",profile.flatQuantization);
            effect.SetFloat("_DetailQuantization",profile.detailQuantization);
            effect.SetFloat("_ChromaSubsampling",profile.chromaSubsampling);
            effect.SetFloat("_TemporalLoss",ReducedMotion ? 0 : profile.temporalDetailLoss);
            effect.SetFloat("_CameraMotion",CameraSimulation.CameraMotion);
            effect.SetFloat("_WaterImmersion",waterPassage != null ? waterPassage.Immersion : 0);
            var waterEffects = waterPassage != null ? waterPassage.Effects : null;
            effect.SetFloat("_WaterWetness",waterEffects != null ? waterEffects.Wetness : 0);
            effect.SetFloat("_WaterEffectTime",waterEffects != null ? waterEffects.EffectTime : 0);
            effect.SetFloat("_WaterSplash",waterEffects != null ? waterEffects.Splash : 0);
            effect.SetFloat("_RunEdgeEffect",ReducedMotion ? 0 : CameraSimulation.RunningAmount);
        }
        private void ProcessFrame(ScriptableRenderContext context, Camera camera)
        {
            if (camera != view || target == null || ProcessedFrame == null) return;
            var command = CommandBufferPool.Get("Ermo Cyber-shot low-resolution capture");
            bool digital = EffectsEnabled && settings.foundFootage && !settings.legacyVhs;
            bool fallback = digital && CameraSimulation.ScheduleMeter(command,target);
            bool capture = !digital || Time.unscaledTime >= nextCapture;
            if (capture)
            {
                if (digital)
                {
                    effect.SetFloat("_CaptureFrame",captureFrame++);
                    effect.SetFloat("_HistoryValid",historyValid ? 1 : 0);
                    effect.SetTexture("_HistoryTex",history);
                    command.Blit(target,sensor,effect,0);
                    command.Blit(sensor,ProcessedFrame,effect,1);
                    command.Blit(sensor,history);
                    historyValid = true;
                    float period = 1/CyberShotProfile.GameplayCaptureFps;
                    nextCapture += period;
                    if (nextCapture < Time.unscaledTime-period) nextCapture = Time.unscaledTime+period;
                }
                else { command.Blit(target,ProcessedFrame,effect,0); historyValid = false; }
            }
            context.ExecuteCommandBuffer(command);
            CommandBufferPool.Release(command);
            if (fallback) CameraSimulation.ReadFallbackMeter();
        }
        private void Resize()
        {
            lastWidth = Screen.width; lastHeight = Screen.height;
            lastPixelGrid = settings.godotPixelGrid; lastRenderHeight = settings.renderHeight;
            lastPixelatedUpscale = settings.pixelatedUpscale;
            lastFourThree = FourThree;
            if (target != null) { view.targetTexture = null; target.Release(); Destroy(target); }
            int height = settings.renderHeight <= 0 ? Mathf.Max(1, lastHeight) : Mathf.Max(1,settings.renderHeight);
            int width = Mathf.Max(1, Mathf.RoundToInt(height * lastWidth / (float)Mathf.Max(1, lastHeight)));
            if (FourThree) width = Mathf.Max(1,Mathf.RoundToInt(height*4f/3f));
            if (settings.godotPixelGrid) { width = 640; height = 360; }
            var rect = image.rectTransform;
            if (settings.godotPixelGrid)
            {
                float scale = Mathf.Min(lastWidth/640f,lastHeight/360f);
                if (scale >= 1) scale = Mathf.Floor(scale);
                rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
                rect.sizeDelta = new Vector2(640,360)*scale; rect.anchoredPosition = Vector2.zero;
            }
            else if (FourThree)
            {
                float scale = Mathf.Min(lastWidth/(float)width,lastHeight/(float)height);
                rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
                rect.sizeDelta = new Vector2(width,height)*scale; rect.anchoredPosition = Vector2.zero;
            }
            else { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
            var filter = settings.godotPixelGrid || settings.pixelatedUpscale ? FilterMode.Point : FilterMode.Bilinear;
            target = new RenderTexture(width,height,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear)
                { name = "Landscape HDR sensor input", filterMode = FilterMode.Bilinear, antiAliasing = 1 };
            if (ProcessedFrame != null) { ProcessedFrame.Release(); Destroy(ProcessedFrame); }
            ProcessedFrame = new RenderTexture(width,height,0) { name = "Cyber-shot compressed capture", filterMode = filter, antiAliasing = 1 };
            ReleaseSensorBuffers();
            sensor = new RenderTexture(width,height,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear)
                { name = "Cyber-shot CCD / optics", filterMode = FilterMode.Bilinear };
            history = new RenderTexture(width,height,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear)
                { name = "Cyber-shot previous sensor frame", filterMode = FilterMode.Bilinear };
            sensor.Create(); history.Create(); historyValid = false; nextCapture = 0;
            target.Create(); ProcessedFrame.Create();
            view.targetTexture = target; view.aspect = width / (float)height; image.texture = ProcessedFrame;
        }
        private void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= ProcessFrame;
            if (view != null) view.targetTexture = null;
            if (target != null) { target.Release(); Destroy(target); }
            if (ProcessedFrame != null) { ProcessedFrame.Release(); Destroy(ProcessedFrame); }
            if (effect != null) Destroy(effect);
            ReleaseSensorBuffers();
        }
        private void ReleaseSensorBuffers()
        {
            if (sensor != null) { sensor.Release(); Destroy(sensor); }
            if (history != null) { history.Release(); Destroy(history); }
        }
    }
}
