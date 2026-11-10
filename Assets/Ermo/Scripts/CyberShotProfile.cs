using System;
using UnityEngine;

namespace Ermo
{
    public enum CyberShotPreset { Fine, Standard, Dreamcore }

    [CreateAssetMenu(menuName = "Ermo/Cyber-shot Camera Profile")]
    public sealed class CyberShotProfile : ScriptableObject
    {
        public const float GameplayCaptureFps = 30;
        public static CyberShotProfile CreateGameplayProfile()
        {
            var source = Resources.Load<CyberShotProfile>("Ermo/Camera/ERMO Dreamcore");
            if (source == null) throw new InvalidOperationException("ERMO Dreamcore camera profile is missing.");
            var profile = Instantiate(source);
            profile.preset = CyberShotPreset.Dreamcore;
            profile.limitCaptureCadence = true;
            profile.captureFps = GameplayCaptureFps;
            return profile;
        }
        public CyberShotPreset preset = CyberShotPreset.Dreamcore;
        [Header("Capture — height 0 means native; UI is never downscaled")]
        public int captureHeight = 480;
        public bool fourThree = true;
        public bool pointUpscale;
        [Tooltip("Authoring reference only. Gameplay always captures at 30 fps with cadence enabled.")]
        public bool limitCaptureCadence;
        [Range(8, 60)] public float captureFps = 30;
        [Header("CCD and electronic processing")]
        [Range(0, .08f)] public float luminanceNoise = .014f;
        [Range(0, .04f)] public float chromaNoise = .004f;
        [Range(0, 1)] public float sharpening = .55f;
        [Range(0, 1)] public float centerSoftness = .08f;
        [Range(0, 1)] public float edgeSoftness = .26f;
        [Range(0, 2)] public float saturation = 1.18f;
        [Range(.5f, 1.5f)] public float contrast = 1.1f;
        [Range(.65f, 1.2f)] public float whiteClip = .94f;
        [Range(0, .3f)] public float bloom = .1f;
        [Header("Spatial MPEG approximation — no bitstream encoding")]
        [Range(0, 1)] public float compression = .42f;
        [Range(.001f, .06f)] public float flatQuantization = .009f;
        [Range(.001f, .08f)] public float detailQuantization = .022f;
        [Range(0, 1)] public float chromaSubsampling = .85f;
        [Range(0, .3f)] public float temporalDetailLoss = .1f;
        [Header("Optics — positive barrel coefficient, not URP's signed value")]
        [Range(0, .12f)] public float barrelDistortion = .042f;
        [Range(0, 1)] public float chromaticPixels = .25f;
        [Range(0, .2f)] public float vignette = .045f;
        [Header("Scene-metered automatic exposure")]
        public bool autoExposure = true;
        [Range(.04f, .6f)] public float middleGray = .24f;
        [Range(-3, 3)] public float exposureCompensation = .35f;
        public Vector2 exposureLimits = new Vector2(-2, 2.5f);
        [Range(.1f, 3)] public float brightAdaptation = .65f;
        [Range(.1f, 3)] public float darkAdaptation = 1.05f;
        [Range(.05f, .5f)] public float meterInterval = .12f;
        [Header("Conservative gray-world white balance")]
        public bool autoWhiteBalance = true;
        [Range(0, 1)] public float whiteBalanceStrength = .2f;
        [Range(.2f, 5)] public float whiteBalanceAdaptation = 1.8f;
        [Header("URP camera + object motion vectors")]
        public bool motionBlur = true;
        [Range(0, 1)] public float motionBlurIntensity = .6f;
        [Range(.001f, .05f)] public float shutterSeconds = 1f/60;
        [Range(.001f, .05f)] public float maximumBlur = .012f;
        [Header("Autofocus — deliberately off by default")]
        public bool enableAutofocus;
        [Min(.1f)] public float focusDistance = 12;
        [Min(.1f)] public float minimumFocusDistance = .4f;
        [Min(.1f)] public float focusAdaptationSpeed = 3;
        [Range(0, 1)] public float maximumFocusBlur = .35f;
        public LayerMask focusMask = Physics.DefaultRaycastLayers;
        [Header("Physical handheld layers")]
        public HandheldSettings handheld = new HandheldSettings();

        // Existing serialized controls remain the single runtime source for the
        // original optics. Presets copy values, never change world generation.
        public void ApplyTo(WorldSettings settings)
        {
            settings.cameraProfile = this;
            settings.renderHeight = captureHeight; settings.captureFourThree = fourThree;
            settings.pixelatedUpscale = pointUpscale; settings.godotPixelGrid = false;
            settings.foundFootage = true; settings.legacyVhs = false;
            settings.filmGrain = luminanceNoise; settings.chromatic = chromaticPixels;
            settings.lensDistortion = barrelDistortion; settings.vignette = vignette;
            settings.captureSoftness = centerSoftness; settings.captureSharpness = sharpening;
            settings.captureSaturation = saturation; settings.captureContrast = contrast;
            settings.captureBloom = bloom; settings.digitalCompression = compression;
            settings.captureExposure = exposureCompensation;
        }
    }

    [Serializable]
    public sealed class HandheldSettings
    {
        [Range(0, 2)] public float intensity = 1;
        [Range(0, 2)] public float idleMotionGain = 1;
        [Range(.05f, .6f)] public float breathingHz = .22f;
        [Range(0, .02f)] public float breathingMeters = .004f;
        [Range(0, .02f)] public float posturalMeters = .003f;
        [Range(0, .5f)] public float posturalDegrees = .12f;
        [Range(0, .1f)] public float tremorDegrees = .025f;
        [Range(1, 15)] public float tremorHz = 6;
        public Vector2 adjustmentInterval = new Vector2(12, 25);
        [Range(0, .4f)] public float adjustmentDegrees = .2f;
        [Range(0, .03f)] public float walkBobMeters = .012f;
        [Range(0, 1)] public float walkRollDegrees = .3f;
        [Range(0, 3)] public float walkMotionGain = 1;
        [Range(0, 3)] public float runMotionGain = 1;
        [Range(0, .02f)] public float mouseInertia = .003f;
        [Range(0, 1)] public float maximumInertiaDegrees = .35f;
        [Range(.03f, .4f)] public float dampingSeconds = .12f;
    }
}
