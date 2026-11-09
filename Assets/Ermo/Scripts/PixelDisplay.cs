using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace Ermo
{
    public sealed class PixelDisplay : MonoBehaviour
    {
        private Camera view;
        private WorldSettings settings;
        private RenderTexture target;
        public RenderTexture ProcessedFrame { get; private set; }
        private RawImage image;
        private Material effect;
        public Camera OutputCamera { get; private set; }
        private int lastWidth, lastHeight;
        public bool ReducedMotion { get; set; }
        public bool EffectsEnabled { get; set; } = true;
        private bool lastPixelGrid;
        private bool lastPixelatedUpscale;
        private int lastRenderHeight;
        public void Initialize(Camera camera, WorldSettings worldSettings, Shader shader, Camera outputCamera = null)
        {
            view = camera; settings = worldSettings;
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
            var canvasObject = new GameObject("Pixel screen");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100;
            var screenObject = new GameObject("Lens", typeof(RectTransform)); screenObject.transform.SetParent(canvasObject.transform, false);
            image = screenObject.AddComponent<RawImage>(); image.raycastTarget = false;
            var rect = image.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            effect = new Material(shader);
            view.allowMSAA = false; view.allowHDR = false;
            UpdateEffect();
            RenderPipelineManager.endCameraRendering += ProcessFrame;
            Resize();
        }
        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight || lastPixelGrid != settings.godotPixelGrid || lastRenderHeight != settings.renderHeight || lastPixelatedUpscale != settings.pixelatedUpscale) Resize();
            UpdateEffect();
        }
        private void UpdateEffect()
        {
            effect.SetFloat("_Distortion", ReducedMotion ? 0 : settings.lensDistortion);
            effect.SetFloat("_Strength",EffectsEnabled ? 1 : 0);
            effect.SetFloat("_Grain",settings.filmGrain);
            effect.SetFloat("_Vignette",settings.vignette);
            effect.SetFloat("_Chromatic",ReducedMotion ? 0 : settings.chromatic);
            effect.SetFloat("_Dither",settings.dither);
            effect.SetFloat("_LegacyVhs",settings.legacyVhs ? 1 : 0);
            effect.SetFloat("_TapeStrength",EffectsEnabled && !ReducedMotion ? settings.tapeStrength : 0);
            effect.SetFloat("_Saturation",settings.tapeSaturation);
            effect.SetFloat("_Contrast",settings.tapeContrast);
            effect.SetFloat("_FoundFootage",settings.foundFootage ? 1 : 0);
            effect.SetFloat("_CaptureSoftness",settings.captureSoftness);
            effect.SetFloat("_CaptureSaturation",settings.captureSaturation);
            effect.SetFloat("_CaptureContrast",settings.captureContrast);
            effect.SetFloat("_CaptureInstability",ReducedMotion ? 0 : settings.captureInstability);
            effect.SetFloat("_AnalogArtifacts",settings.analogArtifacts);
            effect.SetFloat("_AnalogMotion",ReducedMotion ? 0 : 1);
        }
        private void ProcessFrame(ScriptableRenderContext context, Camera camera)
        {
            if (camera != view || target == null || ProcessedFrame == null) return;
            var command = CommandBufferPool.Get("Ermo Godot screen optics");
            command.Blit(target,ProcessedFrame,effect);
            context.ExecuteCommandBuffer(command);
            CommandBufferPool.Release(command);
        }
        private void Resize()
        {
            lastWidth = Screen.width; lastHeight = Screen.height;
            lastPixelGrid = settings.godotPixelGrid; lastRenderHeight = settings.renderHeight;
            lastPixelatedUpscale = settings.pixelatedUpscale;
            if (target != null) { view.targetTexture = null; target.Release(); Destroy(target); }
            int height = settings.renderHeight <= 0 ? Mathf.Max(1, lastHeight) : Mathf.Min(settings.renderHeight, Mathf.Max(1, lastHeight));
            int width = Mathf.Max(1, Mathf.RoundToInt(height * lastWidth / (float)Mathf.Max(1, lastHeight)));
            if (settings.godotPixelGrid) { width = 640; height = 360; }
            var rect = image.rectTransform;
            if (settings.godotPixelGrid)
            {
                float scale = Mathf.Min(lastWidth/640f,lastHeight/360f);
                if (scale >= 1) scale = Mathf.Floor(scale);
                rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
                rect.sizeDelta = new Vector2(640,360)*scale; rect.anchoredPosition = Vector2.zero;
            }
            else { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
            var filter = settings.godotPixelGrid || settings.pixelatedUpscale ? FilterMode.Point : FilterMode.Bilinear;
            target = new RenderTexture(width, height, 24) { name = "Landscape framebuffer", filterMode = FilterMode.Bilinear, antiAliasing = 1 };
            if (ProcessedFrame != null) { ProcessedFrame.Release(); Destroy(ProcessedFrame); }
            ProcessedFrame = new RenderTexture(width,height,0) { name = "Godot optics framebuffer", filterMode = filter, antiAliasing = 1 };
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
        }
    }
}
