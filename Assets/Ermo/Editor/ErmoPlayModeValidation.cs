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
        private static WaterTowerAccess towerUnderTest;
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
            SessionState.SetInt(Key + ".framedAt", -1);
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
                    if (screen.ProcessedFrame.height != Screen.height || Shader.GetGlobalFloat("_ErmoFogStyle") != 1)
                        throw new InvalidOperationException("Native-resolution capture / permanent distance fog failed.");
                    Capture(screen.ProcessedFrame,"Logs/ErmoNativePreview.png");
                    var world = UnityEngine.Object.FindFirstObjectByType<ProceduralWorld>();
                    world.Settings.captureFourThree = true; world.Settings.pixelatedUpscale = false;
                    world.Settings.renderHeight = 240;
                    SessionState.SetInt(Key+".sizeIndex",0);
                    SessionState.SetFloat(Key+".checkAt",Time.time+.2f); SessionState.SetInt(Key+".phase",4);
                    return;
                }
                catch (Exception e) { SessionState.SetString(Key+".error",e.ToString()); }
                SessionState.SetInt(Key+".phase",1); EditorApplication.isPlaying = false;
                return;
            }
            if (phase >= 4 && phase <= 10 && EditorApplication.isPlaying)
            {
                try { TickCameraChecks(phase); }
                catch (Exception e)
                {
                    SessionState.SetString(Key+".error",e.ToString());
                    SessionState.SetInt(Key+".phase",1); EditorApplication.isPlaying = false;
                }
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
                if (world.Settings.cameraProfile.preset != CyberShotPreset.Dreamcore || !world.Settings.cameraProfile.limitCaptureCadence ||
                    world.Settings.cameraProfile.captureFps != 30 || world.Settings.fogStyle != FogStyle.GodotExponential ||
                    !world.Settings.foundFootage || world.Settings.legacyVhs || world.Settings.godotPixelGrid ||
                    display.ReducedMotion || player.ReducedMotion || !display.EffectsEnabled)
                    throw new InvalidOperationException("Gameplay must keep Dreamcore / 30 fps / digital optics / distance fog permanently active.");
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
                // Batch input/focus can leave the wake animation pointing down.
                // Capture a repeatable horizon view, not a floor-only screenshot.
                int framedAt = SessionState.GetInt(Key+".framedAt",-1);
                if (framedAt < 0)
                {
                    ValidateJump(player);
                    player.enabled = false;
                    player.Handheld.enabled = false;
                    player.Handheld.transform.localPosition = Vector3.zero;
                    player.Handheld.transform.localRotation = Quaternion.identity;
                    player.ViewOrientation.localPosition = Vector3.up*1.65f;
                    player.ViewOrientation.localRotation = Quaternion.Euler(-3,0,0);
                    SessionState.SetInt(Key+".framedAt",Time.frameCount);
                    return;
                }
                if (Time.frameCount <= framedAt+2) return;
                var target = player.View.targetTexture;
                if (target == null || !target.IsCreated()) throw new InvalidOperationException("Missing landscape framebuffer.");
                if (!world.Settings.godotPixelGrid && world.Settings.renderHeight == 0 && target.height != Screen.height)
                    throw new InvalidOperationException("Native resolution is not being used.");
                if (!world.Settings.godotPixelGrid && world.Settings.renderHeight > 0 &&
                    target.height != world.Settings.renderHeight)
                    throw new InvalidOperationException("Digital capture resolution cap was not applied.");
                if (world.Settings.captureFourThree && world.Settings.foundFootage && !world.Settings.legacyVhs && !world.Settings.godotPixelGrid &&
                    Mathf.Abs(target.width/(float)target.height-4f/3f) > .005f)
                    throw new InvalidOperationException("Digital 4:3 capture framing was not applied.");
                if (display.ProcessedFrame == null || !display.ProcessedFrame.IsCreated())
                    throw new InvalidOperationException("Optics pass did not create an output.");
                if (!world.Settings.godotPixelGrid && !world.Settings.pixelatedUpscale && display.ProcessedFrame.filterMode != FilterMode.Bilinear)
                    throw new InvalidOperationException("Digital capture must upscale with filtering, not pixel-art nearest.");
                if (display.CameraSimulation == null || display.CameraSimulation.MotionBlur.mode.value != UnityEngine.Rendering.Universal.MotionBlurMode.CameraAndObjects)
                    throw new InvalidOperationException("URP camera / object motion vectors are not configured.");
                if (float.IsNaN(display.CameraSimulation.ExposureEV) || display.CameraSimulation.Automatic.Luminance <= .001f || display.CameraSimulation.MeterReadbacks == 0)
                    throw new InvalidOperationException("Scene luminance meter did not return valid exposure data.");
                Capture(target,"Logs/ErmoRawPreview.png");
                Capture(display.ProcessedFrame,"Logs/ErmoPreview.png");
                Debug.Log("Ermo preview: " + Path.GetFullPath("Logs/ErmoPreview.png"));
                world.Settings.godotPixelGrid = false; world.Settings.renderHeight = 0;
                SessionState.SetFloat(Key+".nativeAt",Time.time); SessionState.SetInt(Key+".phase",2);
                return;
            }
            catch (Exception e) { SessionState.SetString(Key + ".error", e.ToString()); }
            SessionState.SetInt(Key + ".phase", 1);
            EditorApplication.isPlaying = false;
        }
        private static void TickCameraChecks(int phase)
        {
            if (Time.time < SessionState.GetFloat(Key+".checkAt",0)) return;
            var world = UnityEngine.Object.FindFirstObjectByType<ProceduralWorld>(FindObjectsInactive.Include);
            var screen = UnityEngine.Object.FindFirstObjectByType<PixelDisplay>();
            var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonWalker>();
            if (phase == 10)
            {
                Capture(screen.ProcessedFrame,"Logs/ErmoTowerApproachPreview.png");
                player.enabled = true;
                player.SetTraversalPose(towerUnderTest.transform.TransformPoint(new Vector3(0,13.2f,0)),75,0);
                for (int i = 0; i < 150 && !towerUnderTest.ContainsHead(player.View.transform.position); i++) player.MoveCharacter(Vector3.zero,1f/60);
                var passage = screen.GetComponent<WaterPassage>();
                if (!towerUnderTest.ContainsHead(player.View.transform.position) || !passage.BeginDive(towerUnderTest) || passage.BeginDive(towerUnderTest))
                    throw new InvalidOperationException("Open-water entry / one-shot portal guard failed.");
                SessionState.SetFloat(Key+".portalStarted",Time.time);
                SessionState.SetFloat(Key+".checkAt",Time.time+.1f); SessionState.SetInt(Key+".phase",8); return;
            }
            if (phase == 8)
            {
                var passage = screen.GetComponent<WaterPassage>();
                if (passage.State != WaterPassage.PassageState.WaterWorld)
                {
                    if (passage.State == WaterPassage.PassageState.Diving && passage.Effects.MuffleCutoff < 2000)
                    {
                        Capture(screen.ProcessedFrame,"Logs/ErmoUnderwaterPreview.png");
                        if (!passage.Effects.HasAudio || passage.Effects.Entries != 1)
                            throw new InvalidOperationException("Submersion audio or entry splash did not initialize.");
                    }
                    if (Time.time > SessionState.GetFloat(Key+".portalStarted",0)+7)
                        throw new InvalidOperationException("Water passage did not finish emerging.");
                    return;
                }
                if (passage.gameObject.scene.path != WaterWorld.ScenePath || world.gameObject.activeInHierarchy || player.TraversalActive || passage.Immersion != 0)
                    throw new InvalidOperationException("Player/capture rig scene transfer or source-world suspension failed.");
                if (passage.Effects.Exits != 1 || passage.Effects.Wetness < .5f)
                    throw new InvalidOperationException("Emergence must play one exit splash and leave a wet lens.");
                if (UnityEngine.Object.FindObjectsByType<FirstPersonWalker>(FindObjectsSortMode.None).Length != 1)
                    throw new InvalidOperationException("Water transition created a second player.");
                for (int i = 0; i < 90; i++) player.MoveCharacter(player.transform.forward*.6f,1f/60);
                if (!player.Grounded || Mathf.Abs(player.transform.position.y-WaterWorld.Instance.Arrival.y) > .2f)
                    throw new InvalidOperationException("Player did not emerge standing on the submerged pool floor.");
                SessionState.SetFloat(Key+".checkAt",Time.time+.25f); SessionState.SetInt(Key+".phase",9); return;
            }
            if (phase == 9)
            {
                Capture(screen.ProcessedFrame,"Logs/ErmoWaterWorldPreview.png");
                var effects = screen.GetComponent<WaterPassage>().Effects;
                float effectTime = effects.EffectTime;
                effects.Tick(0,true,1);
                if (effects.EffectTime != effectTime) throw new InvalidOperationException("Wet-lens animation continued while paused.");
                effects.Tick(0,false,8);
                if (effects.Wetness != 0 || effects.MuffleCutoff < 20000)
                    throw new InvalidOperationException("Wet lens did not dry / underwater muffling did not clear.");
                Debug.Log("ERMO IMMERSION PASSED: entry/exit splash audio, underwater low-pass, bubble overlay, wet lens, pause and drying.");
                Debug.Log("ERMO WATER PASSAGE PASSED: climbable ladder and rim, open reservoir, preloaded scene transfer, continuous dive/inversion/emergence, one player, walkable water destination and preserved digital capture.");
                SessionState.SetInt(Key+".phase",1); EditorApplication.isPlaying = false; return;
            }
            if (phase == 4)
            {
                int index = SessionState.GetInt(Key+".sizeIndex",0);
                int[] heights = { 240,600,480,0 };
                int expected = heights[index] == 0 ? Screen.height : heights[index];
                if (screen.ProcessedFrame.height != expected || screen.ProcessedFrame.width != Mathf.RoundToInt(expected*4f/3))
                    throw new InvalidOperationException("Capture selection failed at height "+heights[index]);
                Capture(screen.ProcessedFrame,"Logs/ErmoCapture"+heights[index]+".png");
                if (++index < heights.Length)
                {
                    world.Settings.renderHeight = heights[index]; SessionState.SetInt(Key+".sizeIndex",index);
                    SessionState.SetFloat(Key+".checkAt",Time.time+.2f); return;
                }
                world.Settings.renderHeight = 480;
                SessionState.SetInt(Key+".captureCount",screen.CapturedFrames);
                SessionState.SetInt(Key+".renderCount",Time.frameCount);
                SessionState.SetFloat(Key+".cadenceStart",Time.time);
                SessionState.SetFloat(Key+".checkAt",Time.time+1.5f); SessionState.SetInt(Key+".phase",5); return;
            }
            if (phase == 5)
            {
                float elapsed = Time.time-SessionState.GetFloat(Key+".cadenceStart",0);
                int captures = screen.CapturedFrames-SessionState.GetInt(Key+".captureCount",0);
                int rendered = Time.frameCount-SessionState.GetInt(Key+".renderCount",0);
                if (captures <= 0 || captures > Mathf.CeilToInt(elapsed*30)+2 ||
                    (rendered > elapsed*40 && Mathf.Abs(captures/elapsed-30) > 3))
                    throw new InvalidOperationException($"Capture-only cadence mismatch: {captures} captures, {rendered} game frames in {elapsed:0.00}s.");
                Debug.Log($"ERMO CADENCE PASSED: {captures} captured / {rendered} rendered frames in {elapsed:0.00}s; game logic not capped.");
                var card = GameObject.CreatePrimitive(PrimitiveType.Quad); card.name = "Cyber-shot temporary meter card";
                card.transform.SetParent(player.View.transform,false); card.transform.localPosition = Vector3.forward*.6f;
                card.transform.localScale = Vector3.one*5;
                UnityEngine.Object.Destroy(card.GetComponent<Collider>());
                var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetFloat("_Cull",0); material.SetColor("_BaseColor",new Color(.006f,.006f,.006f));
                card.GetComponent<Renderer>().sharedMaterial = material;
                SessionState.SetFloat(Key+".checkAt",Time.time+2); SessionState.SetInt(Key+".phase",6); return;
            }
            if (phase == 6)
            {
                float luminance = screen.CameraSimulation.Automatic.Luminance;
                if (luminance > .05f || screen.CameraSimulation.ExposureEV < 1)
                    throw new InvalidOperationException("Exposure did not respond to the rendered dark meter card: "+luminance);
                SessionState.SetFloat(Key+".darkExposure",screen.CameraSimulation.ExposureEV);
                SessionState.SetFloat(Key+".darkLuminance",luminance);
                GameObject.Find("Cyber-shot temporary meter card").GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",Color.white*.8f);
                SessionState.SetFloat(Key+".checkAt",Time.time+2); SessionState.SetInt(Key+".phase",7); return;
            }
            float bright = screen.CameraSimulation.Automatic.Luminance;
            float dark = SessionState.GetFloat(Key+".darkLuminance",1);
            if (bright < dark*8 || screen.CameraSimulation.ExposureEV > SessionState.GetFloat(Key+".darkExposure",0)-1)
                throw new InvalidOperationException("Scene-metered bright transition did not decrease exposure.");
            var testCard = GameObject.Find("Cyber-shot temporary meter card");
            UnityEngine.Object.Destroy(testCard.GetComponent<Renderer>().sharedMaterial); UnityEngine.Object.Destroy(testCard);
            Debug.Log($"ERMO IMAGE CONTROLS PASSED: 320x240 / 640x480 / 800x600 / native; measured luminance {dark:0.000} -> {bright:0.000}; exposure adapted to rendered light.");
            StartWaterPassageTest(player,screen);
            SessionState.SetFloat(Key+".checkAt",Time.time+.75f); SessionState.SetInt(Key+".phase",10);
        }
        private static void StartWaterPassageTest(FirstPersonWalker player, PixelDisplay screen)
        {
            if (WaterWorld.Instance == null || WaterTowerAccess.Live.Count == 0)
                throw new InvalidOperationException("Water scene was not preloaded or no generated tower has an access component.");
            var tower = WaterTowerAccess.Live[0];
            towerUnderTest = tower;
            var topWater = tower.transform.Find("Reservoir water — no collision lid").GetComponent<Renderer>().sharedMaterial;
            var lowerWater = tower.transform.Find("Water tower inverted lower water — no collision").GetComponent<Renderer>().sharedMaterial;
            if (topWater.shader != lowerWater.shader || topWater.shader != Resources.Load<Shader>("Ermo/Water/ReservoirWater") ||
                topWater.GetFloat("_WellDepth") < 6 || Mathf.Abs(topWater.GetFloat("_WellRadius")-WaterTowerAccess.InnerRadius) > .01f)
                throw new InvalidOperationException("Reservoir must use the shared water shader with a visible deep optical volume, not near-invisible alpha water.");
            var seaShader = Resources.Load<Shader>("Ermo/Water/Water");
            if (topWater.shader == seaShader || seaShader.FindPropertyIndex("_WellDepth") >= 0)
                throw new InvalidOperationException("Reservoir-only optical changes leaked into the water-world shader.");
            var passage = screen.GetComponent<WaterPassage>();
            player.enabled = true; player.MenuOpen = false;
            player.SetTraversalPose(tower.LadderPoint(1),0,0);
            Physics.SyncTransforms();
            if (!passage.AttachLadder(tower)) throw new InvalidOperationException("Generated tower ladder cannot be attached.");
            for (int i = 0; i < 900 && passage.State == WaterPassage.PassageState.Climbing; i++) passage.AdvanceClimb(1,1f/60);
            if (passage.State != WaterPassage.PassageState.Exploring || player.TraversalActive ||
                Vector3.Distance(player.transform.position,tower.TopLanding) > .2f)
                throw new InvalidOperationException("Ladder ascent / mantle is blocked by reservoir colliders.");
            for (int i = 0; i < 30; i++) player.MoveCharacter(Vector3.zero,1f/60);
            if (!player.Grounded) throw new InvalidOperationException("Reservoir rim is not physically walkable.");
            player.transform.rotation = tower.transform.rotation;
            player.SetTraversalPose(player.transform.position,50,0);
            player.enabled = false;
        }
        private static void ValidateJump(FirstPersonWalker player)
        {
            bool menu = player.MenuOpen;
            try
            {
                // Settle with the actual CharacterController and terrain collider,
                // independent of the batch window's keyboard/cursor focus.
                for (int i = 0; i < 90; i++) player.MoveCharacter(Vector3.zero,1f/60);
                if (!player.Grounded) throw new InvalidOperationException("Jump test could not ground the player.");
                player.MenuOpen = true;
                if (player.TryJump()) throw new InvalidOperationException("Player can jump while the menu is open.");
                player.MenuOpen = false;
                float start = player.transform.position.y, peak = start;
                if (!player.TryJump() || player.TryJump()) throw new InvalidOperationException("Grounded jump / repeated jump guard failed.");
                bool airborne = false;
                for (int i = 0; i < 150; i++)
                {
                    player.MoveCharacter(Vector3.zero,1f/60);
                    peak = Mathf.Max(peak,player.transform.position.y);
                    if (!player.Grounded)
                    {
                        airborne = true;
                        if (player.TryJump()) throw new InvalidOperationException("Player can double jump in mid-air.");
                    }
                }
                if (!airborne || peak-start < .5f || peak-start > 1.1f || !player.Grounded)
                    throw new InvalidOperationException("Jump height / landing collision failed.");
                Debug.Log($"ERMO JUMP PASSED: grounded takeoff, {peak-start:0.00} m rise, landing, no menu jump or double jump.");
            }
            finally { player.MenuOpen = menu; }
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
