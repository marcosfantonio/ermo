using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ermo.Editor
{
    // Batch-only smoke test. Never starts automatically in a normal editor session.
    [InitializeOnLoad]
    public static class ErmoPlayModeValidation
    {
        private const string Key = "Ermo.BatchSmoke";
        static ErmoPlayModeValidation()
        {
            if (!SessionState.GetBool(Key, false)) return;
            Attach();
        }

        private static void Attach()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= CaptureError;
            Application.logMessageReceived += CaptureError;
        }

        public static void RunBatch()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("This test is for a separate batch editor only.");
            ErmoTools.Validate();
            EditorSceneManager.OpenScene("Assets/Ermo/Scenes/Ermo.unity");
            ErmoScenePreview.Refresh();
            ErmoScenePreview.Refresh();
            var preview = ErmoPreview.FindLive();
            if (preview.Count != 1 || preview[0].GetComponentsInChildren<ChunkMeshes>().Length != 9)
                throw new InvalidOperationException("Editor preview duplicated or is missing terrain.");
            ErmoScenePreview.Clear();
            Debug.Log("ERMO EDITOR PREVIEW PASSED: nine inspectable chunks, refresh without duplication, safe cleanup.");
            SessionState.SetBool(Key, true);
            SessionState.SetInt(Key + ".phase", 0);
            SessionState.SetString(Key + ".error", "");
            SessionState.SetString(Key + ".editorWarning", "");
            SessionState.SetFloat(Key + ".start", (float)EditorApplication.timeSinceStartup);
            // Also works when the project's Enter Play Mode options disable domain reload.
            Attach();
            EditorApplication.isPlaying = true;
        }

        private static void CaptureError(string condition, string trace, LogType type)
        {
            // Unity 6000.6's Search indexer can throw at batch editor startup.
            // It is editor-only, unrelated to the loaded scene; report it separately.
            if (trace.Contains("UnityEditor.Search."))
            {
                SessionState.SetString(Key + ".editorWarning", condition);
                return;
            }
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                SessionState.SetString(Key + ".error", condition + "\n" + trace);
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".start", 0) > 90)
            {
                SessionState.SetBool(Key, false);
                Debug.LogError("ERMO PLAYMODE FAILED: timeout");
                EditorApplication.Exit(1);
                return;
            }
            int phase = SessionState.GetInt(Key + ".phase", 0);
            if (phase == 1 && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                string error = SessionState.GetString(Key + ".error", "");
                SessionState.SetBool(Key, false);
                string editorWarning = SessionState.GetString(Key + ".editorWarning", "");
                if (!string.IsNullOrEmpty(editorWarning)) Debug.LogWarning("Unity Search startup issue (not a runtime error): " + editorWarning);
                if (string.IsNullOrEmpty(error)) Debug.Log("ERMO PLAYMODE PASSED: streaming, player grounding and rendered preview.");
                else Debug.LogError("ERMO PLAYMODE FAILED: " + error);
                EditorApplication.Exit(string.IsNullOrEmpty(error) ? 0 : 1);
                return;
            }
            if (phase == 2 && EditorApplication.isPlaying)
            {
                if (Time.time < SessionState.GetFloat(Key+".nativeAt",0)+.5f) return;
                try
                {
                    var screen = UnityEngine.Object.FindFirstObjectByType<PixelDisplay>();
                    if (screen.ProcessedFrame.height != Screen.height || Shader.GetGlobalFloat("_ErmoFogStyle") != 0)
                        throw new InvalidOperationException("Native-resolution / horizon-fog fallback failed.");
                    Capture(screen.ProcessedFrame,"Logs/ErmoNativePreview.png");
                    var world = UnityEngine.Object.FindFirstObjectByType<ProceduralWorld>();
                    world.Settings.godotPixelGrid = true; world.Settings.legacyVhs = true;
                    screen.EffectsEnabled = true;
                    SessionState.SetFloat(Key+".tapeAt",Time.time); SessionState.SetInt(Key+".phase",3);
                    return;
                }
                catch (Exception e) { SessionState.SetString(Key+".error",e.ToString()); }
                SessionState.SetInt(Key+".phase",1); EditorApplication.isPlaying = false;
                return;
            }
            if (phase == 3 && EditorApplication.isPlaying)
            {
                if (Time.time < SessionState.GetFloat(Key+".tapeAt",0)+.5f) return;
                try
                {
                    var screen = UnityEngine.Object.FindFirstObjectByType<PixelDisplay>();
                    if (screen.ProcessedFrame.width != 640 || screen.ProcessedFrame.height != 360)
                        throw new InvalidOperationException("Return to Godot pixel grid failed.");
                    Capture(screen.ProcessedFrame,"Logs/ErmoVhsPreview.png");
                }
                catch (Exception e) { SessionState.SetString(Key+".error",e.ToString()); }
                SessionState.SetInt(Key+".phase",1); EditorApplication.isPlaying = false;
                return;
            }
            if (phase != 0 || !EditorApplication.isPlaying || Time.time < 8) return;
            try
            {
                var world = UnityEngine.Object.FindFirstObjectByType<ProceduralWorld>();
                var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonWalker>();
                if (world == null || player == null)
                    throw new InvalidOperationException("Scene bootstrap or chunk streaming failed.");
                var audio = UnityEngine.Object.FindFirstObjectByType<RuralAudio>();
                if (audio == null || !audio.HasClips || audio.Breeze == null || !audio.Breeze.loop || audio.Footsteps == null)
                    throw new InvalidOperationException("Breeze/footsteps were not initialized.");
                int diameter = world.Settings.viewDistance * 2 + 1;
                if (world.LoadedCount < diameter * diameter) return;
                var display = UnityEngine.Object.FindFirstObjectByType<PixelDisplay>();
                if (display == null || display.OutputCamera == null || !display.OutputCamera.isActiveAndEnabled ||
                    display.OutputCamera.targetTexture != null || display.OutputCamera.targetDisplay != 0)
                    throw new InvalidOperationException("Display 1 has no active screen camera.");
                if (player.View.farClipPlane < 700) throw new InvalidOperationException("Draw distance was not increased.");
                Vector4 haze = Shader.GetGlobalVector("_ErmoHazeParams");
                if (world.Settings.fogStyle == FogStyle.HorizonOnly && (haze.x < player.View.farClipPlane * .8f || haze.y <= haze.x))
                    throw new InvalidOperationException("Fog must be confined to the end of the draw distance.");
                float expectedHeight = world.Sampler.Height(player.transform.position.x, player.transform.position.z);
                if (Mathf.Abs(player.transform.position.y - expectedHeight) > 1)
                    throw new InvalidOperationException("Player did not settle on terrain.");
                var target = player.View.targetTexture;
                if (target == null || !target.IsCreated()) throw new InvalidOperationException("Missing landscape framebuffer.");
                if (!world.Settings.godotPixelGrid && world.Settings.renderHeight == 0 && target.height != Screen.height)
                    throw new InvalidOperationException("Native resolution is not being used.");
                if (world.Settings.godotPixelGrid && (target.width != 640 || target.height != 360))
                    throw new InvalidOperationException("Godot 640x360 framebuffer mismatch.");
                if (display.ProcessedFrame == null || !display.ProcessedFrame.IsCreated())
                    throw new InvalidOperationException("Optics pass did not create an output.");
                Capture(target,"Logs/ErmoRawPreview.png");
                Capture(display.ProcessedFrame,"Logs/ErmoPreview.png");
                Debug.Log("Ermo preview: " + Path.GetFullPath("Logs/ErmoPreview.png"));
                world.Settings.godotPixelGrid = false; world.Settings.renderHeight = 0;
                world.Settings.fogStyle = FogStyle.HorizonOnly; GodotAtmosphere.Apply(world.Settings);
                display.EffectsEnabled = false;
                SessionState.SetFloat(Key+".nativeAt",Time.time); SessionState.SetInt(Key+".phase",2);
                return;
            }
            catch (Exception e) { SessionState.SetString(Key + ".error", e.ToString()); }
            SessionState.SetInt(Key + ".phase", 1);
            EditorApplication.isPlaying = false;
        }
        private static void Capture(RenderTexture target, string path)
        {
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; UnityEngine.Object.Destroy(image); }
        }
    }
}
