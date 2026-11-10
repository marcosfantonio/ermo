using System;
using UnityEditor;
using UnityEngine;

namespace Ermo.Editor
{
    public static class CyberShotValidation
    {
        [MenuItem("Ermo/Validate Cyber-shot Camera")]
        public static void Validate()
        {
            var profiles = Resources.LoadAll<CyberShotProfile>("Ermo/Camera");
            Require(profiles.Length == 3,"Three camera profiles must ship with the game.");
            var config = ScriptableObject.CreateInstance<WorldSettings>();
            var root = new GameObject("Camera validation — no player input or scene edits");
            var motion = root.AddComponent<HandheldCameraMotion>();
            var gameplay = CyberShotProfile.CreateGameplayProfile();
            try
            {
                foreach (var profile in profiles)
                {
                    Require(profile.captureHeight == 480 && profile.fourThree && !profile.pointUpscale,"Preset must capture VGA / 4:3 / bilinear.");
                    Require(!profile.enableAutofocus && profile.autoExposure && profile.autoWhiteBalance,"Automatic controls / focus defaults mismatch.");
                    Require(profile.exposureLimits.x < profile.exposureLimits.y && profile.temporalDetailLoss <= .3f,"Invalid exposure / temporal limits.");
                    Require(profile.barrelDistortion > 0 && profile.chromaticPixels > 0 && profile.chromaticPixels < 1,"Subtle optics must remain active.");
                    if (profile.preset == CyberShotPreset.Standard)
                        Require(profile.limitCaptureCadence && Mathf.Abs(profile.captureFps-16.6f) < .01f && profile.compression > .5f,"Standard cadence / compression mismatch.");
                    profile.ApplyTo(config);
                    Require(config.renderHeight == 480 && config.cameraProfile == profile && !config.legacyVhs,"Preset touched wrong configuration.");
                    TestAutomatic(profile);
                }
                Require(gameplay.preset == CyberShotPreset.Dreamcore && gameplay.limitCaptureCadence && gameplay.captureFps == 30,
                    "Gameplay must use Dreamcore with mandatory 30 fps capture.");
                Require(Mathf.Abs(gameplay.handheld.idleMotionGain-1.05f) < .0001f,"Idle motion must be increased by exactly 5 percent.");
                Require(gameplay.captureHeight == 480 && gameplay.fourThree,"Gameplay must still default to 640x480.");
                Require(gameplay.handheld.walkMotionGain > 1 && gameplay.handheld.runMotionGain > gameplay.handheld.walkMotionGain,
                    "Walking and running must have increased motion gains, without changing idle motion.");
                gameplay.ApplyTo(config);
                motion.Initialize(null,config);
                float smallestY = float.MaxValue, largestY = float.MinValue, maxPosition = 0, maxRotation = 0;
                Vector3 previous = Vector3.zero;
                float largestJump = 0;
                // One complete idle minute: no input, nonzero breathing/posture,
                // bounded corrections, continuous tremor and no accumulating drift.
                for (int i = 0; i < 3600; i++)
                {
                    motion.Tick(1f/60,0,true,false,false,false,Vector2.zero);
                    var p = motion.PositionOffset;
                    smallestY = Mathf.Min(smallestY,p.y); largestY = Mathf.Max(largestY,p.y);
                    maxPosition = Mathf.Max(maxPosition,p.magnitude); maxRotation = Mathf.Max(maxRotation,motion.RotationOffset.magnitude);
                    largestJump = Mathf.Max(largestJump,Vector3.Distance(previous,p)); previous = p;
                }
                Require(largestY-smallestY > .004f,"Idle camera became a tripod.");
                Require(maxPosition < .025f && maxRotation < 1 && largestJump < .003f,"Idle motion drifted or jumped.");
                for (int i = 0; i < 180; i++) motion.Tick(1f/60,3.3f/60,true,false,false,false,new Vector2(.3f,.1f));
                Require(motion.PositionOffset.magnitude < .05f && motion.RotationOffset.magnitude < 1.5f,"Walking camera is excessive.");
                for (int i = 0; i < 180; i++) motion.Tick(1f/60,6.2f/60,true,true,false,false,Vector2.zero);
                Require(motion.PositionOffset.magnitude < .06f && motion.RotationOffset.magnitude < 1.5f,"Running camera is excessive.");
                for (int i = 0; i < 180; i++) motion.Tick(1f/60,0,true,false,true,false,Vector2.zero);
                Require(motion.PositionOffset.magnitude < .0001f && motion.RotationOffset.magnitude < .001f,"Paused motion did not settle.");
                motion.Tick(1f/60,1,true,true,false,true,Vector2.one);
                Require(motion.PositionOffset == Vector3.zero && motion.RotationOffset == Vector3.zero,"Reduced motion must zero secondary offsets.");
                var shader = Shader.Find("Ermo/Pixel Lens");
                Require(shader != null && shader.passCount >= 2,"Separate CCD and compression passes are missing.");
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    Require(message.severity != UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error,message.message);
                Debug.Log("ERMO CAMERA PASSED: fixed Dreamcore / 30 fps, +5% idle motion, exposure/AWB adaptation, 60-second idle motion, walking, pause and shader passes.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(config); UnityEngine.Object.DestroyImmediate(gameplay); }
        }

        private static void TestAutomatic(CyberShotProfile profile)
        {
            var control = new CameraAutoControl();
            control.Reset(profile.exposureCompensation);
            control.Measure(.008f,new Color(.01f,.008f,.005f));
            for (int i = 0; i < 600; i++) control.Advance(1f/60,profile,profile.exposureCompensation);
            float dark = control.ExposureEV;
            Require(dark > 1 && dark <= profile.exposureLimits.y+.001f,"Dark exposure did not increase / respect bounds.");
            control.Measure(1.5f,new Color(1.3f,1.5f,1.7f));
            control.Advance(1f/60,profile,profile.exposureCompensation);
            Require(control.ExposureEV < dark && dark-control.ExposureEV < .3f,"Bright exposure jumped instead of adapting.");
            for (int i = 0; i < 600; i++) control.Advance(1f/60,profile,profile.exposureCompensation);
            Require(control.ExposureEV < 0 && control.ExposureEV >= profile.exposureLimits.x-.001f,"Bright exposure did not settle / respect bounds.");
            Require(control.WhiteBalance.x > .9f && control.WhiteBalance.x < 1.1f && control.WhiteBalance.z > .9f && control.WhiteBalance.z < 1.1f,
                "White balance must preserve saturated scene colors.");
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Cyber-shot validation: "+message);
        }
    }
}
