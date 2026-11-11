using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ermo.Editor
{
    [InitializeOnLoad]
    public static class BalnearioValidation
    {
        private const string Key = "Ermo.BalnearioBatch";
        static BalnearioValidation()
        {
            if (SessionState.GetBool(Key,false)) EditorApplication.update += Tick;
        }
        [MenuItem("Ermo/Validate Balneario Layout")]
        public static void ValidateLayout()
        {
            bool varied = false;
            var themes = new System.Collections.Generic.HashSet<int>();
            var counts = new System.Collections.Generic.HashSet<int>();
            var baseline = new BalnearioLayout("balneario-test-0");
            for (int i = 0; i < 128; i++)
            {
                var a = new BalnearioLayout("balneario-test-"+i);
                var b = new BalnearioLayout("balneario-test-"+i);
                a.Validate(); b.Validate();
                Require(a.pools[a.slidePool].Surface-a.pools[a.slidePool].Floor >= 2.5f,"Flume runout pool is too shallow.");
                Require(Mathf.Abs(a.pools[0].depth-BalnearioLayout.PoolDepth) < .001f,"Arrival lagoon depth changed.");
                Require(a.pools.Count >= 2 && a.sites.Count > 30,"Missing arrival pools or decoration.");
                Require(a.sites.Count == b.sites.Count,"Seed changed the site count.");
                for (int p = 0; p < a.pools.Count; p++)
                    for (int v = 0; v < a.pools[p].outline.Count; v++)
                        Require(a.pools[p].outline[v] == b.pools[p].outline[v],"Pool contours are nondeterministic.");
                for (int s = 0; s < a.sites.Count; s++)
                    Require(a.sites[s].position == b.sites[s].position && a.sites[s].yaw == b.sites[s].yaw &&
                        a.sites[s].kind == b.sites[s].kind,"Decoration is nondeterministic.");
                varied |= a.pools[0].size != baseline.pools[0].size;
                Require(a.IsWater(new Vector3(0,-.55f,0)),"Arrival is not inside water.");
                Require(!a.IsWater(new Vector3(0,.22f,-31)),"Dry promenade classified as water.");
                var cell = new Vector2Int(i-64,i*37%41-20);
                var distant = new BalnearioLayout("balneario-infinite",cell);
                var rebuilt = new BalnearioLayout("balneario-infinite",cell);
                distant.Validate(); rebuilt.Validate(); themes.Add(distant.theme); counts.Add(distant.pools.Count);
                Require(distant.seed == rebuilt.seed && distant.sites.Count == rebuilt.sites.Count && distant.pools.Count == rebuilt.pools.Count,
                    "Sector changed after eviction/regeneration.");
                for (int s = 0; s < distant.sites.Count; s++) Require(distant.sites[s].position == rebuilt.sites[s].position,"Sector sites changed on revisit.");
                var east = new BalnearioLayout("balneario-infinite",cell+Vector2Int.right);
                var north = new BalnearioLayout("balneario-infinite",cell+Vector2Int.up);
                Require(Mathf.Abs(distant.HorizontalRoad(80)-east.HorizontalRoad(-80)) < .001f,"East/west promenade seam.");
                Require(Mathf.Abs(distant.VerticalRoad(80)-north.VerticalRoad(-80)) < .001f,"North/south promenade seam.");
            }
            Require(varied,"Different seeds generated identical pools.");
            Require(themes.Count == 4 && counts.Count > 1,"Sectors are copies of a single park template.");
            Require(BalnearioLayout.Coordinate(new Vector2(-80.01f,0)).x == -1 && BalnearioLayout.Coordinate(new Vector2(80,0)).x == 1,
                "Negative-coordinate sector ownership failed.");
            Debug.Log("ERMO BALNEARIO LAYOUT PASSED: 128 arrival seeds and distant sectors, four themes, variable pool counts, regeneration, signed coordinates, continuous promenades and spacing.");
        }
        public static void RunBatch()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batch editor.");
            ValidateLayout();
            AssetDatabase.ImportAsset("Assets/Ermo/Scripts/WaterWorld.cs",ImportAssetOptions.ForceUpdate);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Ermo/Scripts/WaterWorld.cs");
            Require(script != null && script.GetClass() == typeof(WaterWorld),"Destination MonoScript did not resolve to WaterWorld.");
            EditorSettings.enterPlayModeOptionsEnabled = false;
            AssetDatabase.ImportAsset(WaterWorld.ScenePath,ImportAssetOptions.ForceUpdate);
            EditorSceneManager.OpenScene(WaterWorld.ScenePath);
            Require(UnityEngine.Object.FindFirstObjectByType<WaterWorld>() != null,"Balneario scene lost its WaterWorld component.");
            SessionState.SetBool(Key,true);
            SessionState.SetFloat(Key+".started",(float)EditorApplication.timeSinceStartup);
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }
        private static void Tick()
        {
            if (!EditorApplication.isPlaying || WaterWorld.Instance == null)
            {
                if (EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".started",0) > 180) Finish("Timeout creating balneario.");
                return;
            }
            try
            {
                var world = WaterWorld.Instance; world.ActivateEnvironment(); Physics.SyncTransforms();
                Require(world.IsWater(world.Arrival),"Player does not arrive inside a pool.");
                foreach (var pool in world.Layout.pools)
                {
                    Vector3 p = world.transform.TransformPoint(new Vector3(pool.center.x,pool.Surface+.1f,pool.center.y));
                    Require(Physics.Raycast(p,Vector3.down,out var hit,pool.depth+.5f),"Pool floor has no collider.");
                    Require(Mathf.Abs(hit.point.y-(world.transform.position.y+pool.Floor)) < .04f,"Solid deck caps the pool hole.");
                }
                // Test the actual character capsule, stairs and graded connection, not just mesh presence.
                var settings = AssetDatabase.LoadAssetAtPath<WorldSettings>("Assets/Ermo/Settings/MeadowWorld.asset");
                var playerObject = new GameObject("Balneario validation walker",typeof(CharacterController));
                var player = playerObject.AddComponent<FirstPersonWalker>(); player.Initialize(settings);
                Require(world.GetComponentsInChildren<TextMesh>().Length == 0,"Balneario contains visible signs.");
                Require(Resources.Load<AudioClip>("Ermo/Audio/balneario") != null,"Radio music is missing from the player build.");
                var rides = world.GetComponentsInChildren<WaterSlideRide>();
                Require(rides.Length >= 2,"Arrival slides have no rides.");
                // Finish wake-up without waiting for wall-clock time in batch mode.
                typeof(FirstPersonWalker).GetField("wakeStartedAt",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(player,Time.unscaledTime-3);
                foreach (var ride in rides)
                {
                    player.SetTraversalPose(world.Arrival,0,0);
                    Require(!ride.TryBegin(player),"Slide can be activated from the pool.");
                    player.SetTraversalPose(ride.Launch,0,0);
                    Require(ride.TryBegin(player),"Slide cannot be activated at its launch.");
                    Vector3 pausedAt = player.transform.position; player.MenuOpen = true; ride.Advance(1);
                    Require(player.transform.position == pausedAt,"Slide continues while menu is open."); player.MenuOpen = false;
                    for (int step = 0; step < 3600 && ride.Riding; step++) ride.Advance(1f/60);
                    Require(!ride.Riding && !player.TraversalActive,"Slide did not restore walking.");
                    Require(world.IsWater(player.transform.position),"Slide did not end inside its pool.");
                    Require(Mathf.Abs(player.ViewOrientation.localPosition.y-1.65f) < .01f,"Slide did not restore eye height.");
                }
                Debug.Log("ERMO BALNEARIO RIDES PASSED: launch range, pause, full descent/dive, pool landing and restored walking/camera.");
                foreach (var radio in world.GetComponentsInChildren<BalnearioRadio>())
                {
                    var source = radio.GetComponent<AudioSource>();
                    Require(source != null && source.clip != null && source.loop && source.spatialBlend == 1,"Table radio has no spatial music.");
                    Require(radio.GetComponent<AudioHighPassFilter>() != null && radio.GetComponent<AudioDistortionFilter>() != null,"Radio filters are missing.");
                }
                player.enabled = false;
                var deepPool = world.Layout.pools[world.Layout.slidePool];
                player.SetTraversalPose(world.transform.TransformPoint(new Vector3(deepPool.center.x,deepPool.Floor+.05f,deepPool.center.y)),0,0);
                for (int i = 0; i < 360; i++) player.MoveCharacter(Vector3.zero,1f/60);
                Require(Mathf.Abs(player.transform.position.y-world.transform.position.y-(deepPool.Surface-1.25f)) < .15f,"Deep pool buoyancy did not return player to the surface.");
                float deepExit = deepPool.outline[16].y;
                for (int i = 0; i < 600 && player.transform.position.z < world.transform.position.z+deepExit+2; i++) player.MoveCharacter(Vector3.forward*3,1f/60);
                Require(player.transform.position.z > world.transform.position.z+deepExit+1 && player.Grounded,"Cannot leave deep pool through its stairs.");
                player.SetTraversalPose(world.Arrival,0,0);
                for (int i = 0; i < 60; i++) player.MoveCharacter(Vector3.zero,1f/60);
                Require(player.Grounded,"Arrival floor does not support player.");
                float exitZ = world.Layout.pools[0].outline[16].y;
                for (int i = 0; i < 600 && player.transform.position.z < exitZ+2; i++) player.MoveCharacter(Vector3.forward*3,1f/60);
                Require(player.transform.position.z > exitZ+1 && Mathf.Abs(player.transform.position.y-world.transform.position.y-.22f) < .12f,
                    "Player cannot walk up the pool exit steps.");
                world.Follow(player.transform);
                var far = new Vector2Int(13,-9);
                var farLayout = new BalnearioLayout(world.Seed,far);
                world.EnsureSector(far);
                var farPool = farLayout.pools[0];
                player.SetTraversalPose(world.transform.TransformPoint(new Vector3(far.x*160+farPool.center.x,farPool.Floor,far.y*160+farPool.center.y)),0,0);
                world.TickStreaming(); Physics.SyncTransforms();
                Require(world.CurrentSector == far && world.IsWater(player.transform.position),"Streaming/floating origin lost logical pool coordinates.");
                Vector3 rebasedFeet = world.transform.InverseTransformPoint(player.transform.position);
                Require(new Vector2(rebasedFeet.x,rebasedFeet.z).magnitude < 100 && !world.HasSector(Vector2Int.zero) && world.LoadedSectorCount <= 25,
                    "Distant travel retained old sectors or failed to rebase.");
                for (int i = 0; i < 360; i++) player.MoveCharacter(Vector3.zero,1f/60);
                bool deepDistant = farPool.Surface-farPool.Floor > 1.7f;
                Require(deepDistant ? Mathf.Abs(player.transform.position.y-world.transform.position.y-(farPool.Surface-1.25f)) < .15f : player.Grounded,
                    "Regenerated distant pool lost collision/buoyancy.");
                world.EnsureSector(Vector2Int.zero); player.SetTraversalPose(world.Arrival,0,0); world.TickStreaming(); Physics.SyncTransforms();
                Require(world.HasSector(Vector2Int.zero) && world.IsWater(world.Arrival),"Origin did not regenerate on return.");
                world.EnsureSector(Vector2Int.right); Physics.SyncTransforms();
                player.SetTraversalPose(world.transform.TransformPoint(new Vector3(78,.22f,0)),0,0);
                for (int i = 0; i < 120; i++) player.MoveCharacter(Vector3.right*3,1f/60);
                world.TickStreaming();
                Require(player.Grounded && world.CurrentSector == Vector2Int.right && player.transform.position.x > world.transform.position.x+82,
                    "Character cannot walk across the sector seam.");
                // Raised service plazas are local features, not walls spanning every sector.
                for (int i = 1; i < 32; i++)
                {
                    var cell = new Vector2Int(i%7-3,i/7+1);
                    if (!new BalnearioLayout(world.Seed,cell).raisedGarden) continue;
                    world.EnsureSector(cell); Physics.SyncTransforms();
                    player.SetTraversalPose(world.transform.TransformPoint(new Vector3(cell.x*160+8.5f,.22f,cell.y*160-60)),0,0);
                    for (int step = 0; step < 620; step++) player.MoveCharacter(Vector3.left*3,1f/60);
                    Require(player.Grounded && Mathf.Abs(player.transform.position.y-world.transform.position.y-3.42f) < .12f,
                        "Local terrace ramp is not walkable: cell="+cell+", feet="+world.transform.InverseTransformPoint(player.transform.position)+", grounded="+player.Grounded);
                    break;
                }
                world.EnsureSector(Vector2Int.zero); player.SetTraversalPose(world.Arrival,0,0); world.TickStreaming(); Physics.SyncTransforms();
                int rendered = 0;
                foreach (var renderer in world.GetComponentsInChildren<MeshRenderer>()) if (renderer.enabled) rendered++;
                Require(rendered <= world.LoadedSectorCount*22 && rendered > 10,"Material batching failed.");
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++) world.EnsureSector(new Vector2Int(x,z));
                    world.TickStreaming();
                    Capture(world,new Vector3(0,1.1f,0),new Vector3(36,3,-30),"Logs/ErmoBalnearioArrival.png");
                    Capture(world,new Vector3(-85,90,-85),new Vector3(5,0,5),"Logs/ErmoBalnearioOverview.png");
                }
                foreach (string shaderPath in new[] { "Ermo/BalnearioSurface", "Ermo/Water/PoolWater", "Ermo/Water/SlideWater", "Ermo/Water/ReservoirWater" })
                    foreach (var message in ShaderUtil.GetShaderMessages(Resources.Load<Shader>(shaderPath)))
                        Require(message.severity != UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error,message.message);
                UnityEngine.Object.Destroy(playerObject);
                Debug.Log("ERMO BALNEARIO PHYSICS PASSED: uncapped pools, arrival grounding, walkable exits, distant streaming, bounded resident sectors, floating origin, eviction and deterministic return.");
                Finish(null);
            }
            catch (Exception error) { Finish(error.ToString()); }
        }
        private static void Capture(WaterWorld world, Vector3 position, Vector3 target, string path)
        {
            var go = new GameObject("Balneario test camera",typeof(Camera)); var camera = go.GetComponent<Camera>();
            go.transform.position = world.transform.TransformPoint(position); go.transform.LookAt(world.transform.TransformPoint(target));
            camera.nearClipPlane = .07f; camera.farClipPlane = 600;
            var rt = new RenderTexture(1280,960,24); camera.targetTexture = rt;
            var image = new Texture2D(1280,960,TextureFormat.RGB24,false);
            RenderTexture previous = RenderTexture.active;
            Vector4 horizon = Shader.GetGlobalVector("_BalnearioHorizon");
            bool asyncCompilation = ShaderUtil.allowAsyncCompilation;
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                if (path.Contains("Overview")) Shader.SetGlobalVector("_BalnearioHorizon",new Vector4(300,600,0,0));
                camera.Render(); camera.Render();
                RenderTexture.active = rt; image.ReadPixels(new Rect(0,0,1280,960),0,0); image.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = null; rt.Release();
                ShaderUtil.allowAsyncCompilation = asyncCompilation; Shader.SetGlobalVector("_BalnearioHorizon",horizon);
                UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(image); UnityEngine.Object.Destroy(go);
            }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void Finish(string error)
        {
            SessionState.SetBool(Key,false); EditorApplication.update -= Tick;
            if (error != null) Debug.LogError("ERMO BALNEARIO FAILED: "+error);
            EditorApplication.Exit(error == null ? 0 : 1);
        }
    }
}
