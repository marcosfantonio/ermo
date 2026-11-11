using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ermo.Editor
{
    public static class ErmoTools
    {
        [MenuItem("Ermo/Open Meadow Scene")]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene("Assets/Ermo/Scenes/Sitios.unity");
        }

        [MenuItem("Ermo/Select World Settings")]
        public static void SelectSettings()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<WorldSettings>("Assets/Ermo/Settings/MeadowWorld.asset");
        }

        [MenuItem("Ermo/Validate Generation")]
        public static void Validate()
        {
            var settings = ScriptableObject.CreateInstance<WorldSettings>();
            ValidateDigitalCamera(settings);
            ValidateDigitalCamera(AssetDatabase.LoadAssetAtPath<WorldSettings>("Assets/Ermo/Settings/MeadowWorld.asset"));
            CyberShotValidation.Validate();
            settings.seed = "ermo-validation-v1";
            var sampler = new WorldSampler(settings);
            var coordinates = new[] { Vector2Int.zero, new Vector2Int(-1, -1), new Vector2Int(4, -7), new Vector2Int(12, 9) };
            foreach (var c in coordinates)
            {
                string a = JsonUtility.ToJson(sampler.Generate(c));
                // Loading another chunk cannot influence the original chunk's contents.
                sampler.Generate(c + Vector2Int.one);
                string b = JsonUtility.ToJson(new WorldSampler(settings).Generate(c));
                Require(a == b, "Chunk determinism failed at " + c);
                foreach (var prop in sampler.Generate(c).props)
                {
                    Vector2 anchor = string.IsNullOrEmpty(prop.groupId) ? new Vector2(prop.position.x, prop.position.z) : prop.groupAnchor;
                    Require(Mathf.FloorToInt(anchor.x / settings.chunkSize) == c.x &&
                        Mathf.FloorToInt(anchor.y / settings.chunkSize) == c.y, "Wrong group chunk ownership");
                    Require(Mathf.Abs(prop.position.y - sampler.Height(prop.position.x, prop.position.z)) < .0001f, "Prop height mismatch");
                }
            }
            settings.terrainStyle = TerrainStyle.GodotLegacyWaves;
            settings.flattenYards = false;
            float original = new WorldSampler(settings).Height(122, -67);
            settings.seed = "a-different-seed";
            Require(Mathf.Abs(new WorldSampler(settings).Height(122, -67) - original) > .001f, "Seed does not change terrain");
            settings.terrainStyle = TerrainStyle.GodotContours;
            settings.flattenYards = true;
            ValidateGodotTerrain(settings);
            settings.landmarks = new[] { new LandmarkRule { id = "test-exact", exactChunk = true,
                chunk = new Vector2Int(-2, 3), localPosition = new Vector2(30, 30), kind = PropKind.OpenHouse } };
            sampler = new WorldSampler(settings);
            var exact = sampler.Generate(new Vector2Int(-2, 3));
            Require(exact.props.Exists(p => p.id.EndsWith("test-exact") && p.kind == PropKind.OpenHouse), "Exact landmark missing");
            Require(!sampler.Generate(new Vector2Int(-1, 3)).props.Exists(p => p.id.EndsWith("test-exact")), "Exact landmark leaked");
            settings.landmarks[0].exactChunk = false;
            settings.landmarks[0].chance = 0;
            Require(!new WorldSampler(settings).Generate(Vector2Int.zero).props.Exists(p => p.kind == PropKind.OpenHouse), "Zero chance spawned a landmark");
            settings.landmarks[0].chance = 1;
            settings.landmarks[0].spacingMeters = 1000;
            settings.landmarks[0].minimumDistance = 0;
            settings.locationChance = 0;
            bool found = false;
            for (int x = -10; x <= 10; x++)
                for (int z = -10; z <= 10; z++)
                    found |= new WorldSampler(settings).Generate(new Vector2Int(x, z)).props.Exists(p => p.kind == PropKind.OpenHouse);
            Require(found, "Guaranteed landmark missing");
            settings.landmarks[0].minimumDistance = 10000;
            Require(!new WorldSampler(settings).Generate(new Vector2Int(6, 5)).props.Exists(p => p.kind == PropKind.OpenHouse), "Minimum distance ignored");
            ValidateLocations();
            ValidateAudio();
            foreach (string name in new[] { "grass","plaster","roof","wood" })
            {
                string path = $"Assets/Ermo/Textures/{name}.png";
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Require(texture != null && importer != null,"Missing surface texture: "+name);
                Require(importer.filterMode == FilterMode.Point && importer.wrapMode == TextureWrapMode.Repeat &&
                    importer.sRGBTexture && !importer.mipmapEnabled,"Surface nearest/repeat import mismatch: "+name);
            }
            var store = new WorldStore(settings, Path.GetFullPath("Temp/ErmoValidation"));
            store.Save(exact);
            Require(store.LastError == null, "Save failure: " + store.LastError);
            Require(JsonUtility.ToJson(store.Load(new Vector2Int(-2, 3))) == JsonUtility.ToJson(exact), "Disk round trip failed");
            int size = settings.chunkSize;
            settings.chunkSize = 200;
            new WorldStore(settings, Path.GetFullPath("Temp/ErmoValidation"));
            Require(settings.chunkSize == size, "World manifest did not restore terrain parameters");
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(WaterWorld.ScenePath) != null,"Missing water destination scene.");
            Require(Array.Exists(EditorBuildSettings.scenes,s => s.enabled && s.path == WaterWorld.ScenePath),"Water destination must be included in builds.");
            foreach (string name in new[] { "Ermo/Meadow", "Ermo/Dream Sky", "Ermo/Pixel Lens", "Ermo/Deep Water", "Ermo/Reservoir Water", "Ermo/Other Side Sky", "Ermo/Pool Water", "Ermo/Balneario Surface" })
            {
                var shader = Shader.Find(name);
                Require(shader != null, "Missing shader " + name);
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    Require(message.severity != UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error, name + ": " + message.message);
            }
            BalnearioValidation.ValidateLayout();
            var palette = new WorldPalette(Shader.Find("Ermo/Meadow"));
            var parent = new GameObject("Ermo seam validation");
            try
            {
                var left = WorldGeometry.BuildChunk(new ChunkRecord { x = 4, z = -2 }, settings, sampler, palette, parent.transform);
                var right = WorldGeometry.BuildChunk(new ChunkRecord { x = 5, z = -2 }, settings, sampler, palette, parent.transform);
                Mesh a = left.GetComponentInChildren<MeshFilter>().sharedMesh;
                Mesh b = right.GetComponentInChildren<MeshFilter>().sharedMesh;
                var edge = new Dictionary<float,float>();
                foreach (var vertex in a.vertices)
                    if (Mathf.Abs(vertex.x-settings.chunkSize) < .001f) edge[vertex.z] = vertex.y;
                int matches = 0;
                foreach (var vertex in b.vertices)
                    if (Mathf.Abs(vertex.x) < .001f)
                    {
                        Require(edge.ContainsKey(vertex.z) && Mathf.Abs(edge[vertex.z]-vertex.y)<.001f,"Terrain seam detected");
                        matches++;
                    }
                Require(matches > settings.terrainResolution,"No terrain boundary checked");
                Physics.SyncTransforms();
                float wx = left.transform.position.x+17.3f, wz = left.transform.position.z+29.1f;
                Require(Physics.Raycast(new Vector3(wx,100,wz),Vector3.down,out var hit,200),"Missing terrain collider");
                Require(Mathf.Abs(hit.point.y-sampler.MeshHeight(wx,wz)) < .01f,"Road height and triangle collision diverged");
            }
            finally { UnityEngine.Object.DestroyImmediate(parent); palette.Dispose(); }
            UnityEngine.Object.DestroyImmediate(settings);
            Debug.Log("ERMO VALIDATION PASSED: determinism, negative coordinates, group spacing/combinations, landmark rules, persistence, terrain seams and shaders.");
        }

        private static void ValidateDigitalCamera(WorldSettings settings)
        {
            Require(settings != null && settings.foundFootage && !settings.legacyVhs,"Digital camera profile must be active, not VHS.");
            Require(settings.captureInstability == 0 && settings.analogArtifacts == 0 && settings.dither == 0,
                "Digital camera defaults must not include analog artifacts.");
            Require(settings.lensDistortion > 0 && settings.lensDistortion <= .08f && settings.chromatic > 0 && settings.chromatic <= .5f,
                "Digital camera defaults must retain barrel optics and subtle chromatic aberration.");
            Require(!settings.pixelatedUpscale && !settings.godotPixelGrid && settings.renderHeight == 480,
                "Digital camera profile must use a bilinear 480-line capture, not the legacy pixel grid.");
            Require(settings.captureExposure > 0 && settings.captureSaturation > 1 && settings.captureSharpness > 0 &&
                settings.captureBloom > 0 && settings.captureBloom <= .15f && settings.vignette <= .05f && settings.filmGrain <= .02f &&
                settings.digitalCompression > 0 && settings.digitalCompression <= .45f,
                "Digital camera defaults must remain bright, colorful and subtly textured.");
        }

        private static void ValidateGodotTerrain(WorldSettings settings)
        {
            float heightScale = settings.contourHeightScale, widthScale = settings.contourWidthScale;
            bool organic = settings.organicHills;
            settings.organicHills = false;
            settings.contourHeightScale = settings.contourWidthScale = 1;
            settings.flattenYards = false;
            var field = new WorldSampler(settings);
            float x = 12.5f, z = -31.5f, expected = 0;
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                    foreach (var hill in settings.contours)
                    {
                        float px = (x-dx*settings.contourRepeat.x-hill.position.x)/hill.width.x;
                        float pz = (z-dz*settings.contourRepeat.y-hill.position.z)/hill.width.y;
                        expected += hill.position.y*Mathf.Exp(-2*(px*px+pz*pz));
                    }
            Require(Mathf.Abs(expected-field.Height(x,z)) < .0001f,"Gaussian contour formula diverged");
            settings.contourHeightScale = heightScale; settings.contourWidthScale = widthScale;
            settings.organicHills = organic;
            float maximumSlope = 0;
            for (int px = -150; px <= 150; px += 12)
                for (int pz = -138; pz <= 138; pz += 12)
                {
                    float dx = (field.BaseHeight(px+1,pz)-field.BaseHeight(px-1,pz))*.5f;
                    float dz = (field.BaseHeight(px,pz+1)-field.BaseHeight(px,pz-1))*.5f;
                    maximumSlope = Mathf.Max(maximumSlope,new Vector2(dx,dz).magnitude);
                }
            Require(maximumSlope < .6f,"Organic hill profile is too steep");
            Require(Mathf.Abs(field.BaseHeight(150-.001f,0)-field.BaseHeight(150+.001f,0))<.01f,
                "Broadened Gaussian field jumps at repeat boundary");
            settings.flattenYards = true;
            var oldRules = settings.landmarks;
            settings.landmarks = new[] { new LandmarkRule { id = "yard",exactChunk = true,chunk = Vector2Int.zero,localPosition = new Vector2(38,38) } };
            field = new WorldSampler(settings);
            Require(Mathf.Abs(field.Height(38,38)-field.BaseHeight(38,38)-1)<.0001f,"Yard flattening failed");
            settings.landmarks = oldRules;
        }

        private static void ValidateLocations()
        {
            var settings = ScriptableObject.CreateInstance<WorldSettings>();
            try
            {
                settings.seed = "location-validation";
                settings.landmarks = new[]
                {
                    new LandmarkRule { id = "reserved", exactChunk = true, chunk = new Vector2Int(1, 2), localPosition = new Vector2(90, 90), includeTurbine = true, includeWaterTower = true }
                };
                var sampler = new WorldSampler(settings);
                var groups = new Dictionary<string, Vector2>();
                for (int x = -16; x <= 16; x++)
                    for (int z = -16; z <= 16; z++)
                        foreach (var prop in sampler.Generate(new Vector2Int(x, z)).props)
                            if (!string.IsNullOrEmpty(prop.groupId))
                            {
                                groups[prop.groupId] = prop.groupAnchor;
                                Require((new Vector2(prop.position.x, prop.position.z) - prop.groupAnchor).magnitude < 40, "Accessory detached from its location");
                            }
                Require(groups.Count > 10, "No meaningful population generated");
                foreach (var a in groups)
                    foreach (var b in groups)
                        if (a.Key != b.Key)
                            Require(Vector2.Distance(a.Value, b.Value) >= settings.minimumLocationDistance - .001f,
                                "Locations too close across chunks: " + a.Key + " / " + b.Key);
                float largestGap = 0;
                for (int x = -1152; x <= 1152; x += 64)
                    for (int z = -1152; z <= 1152; z += 64)
                    {
                        float nearest = float.MaxValue;
                        foreach (var group in groups.Values) nearest = Mathf.Min(nearest,Vector2.Distance(new Vector2(x,z),group));
                        largestGap = Mathf.Max(largestGap,nearest);
                    }
                Require(largestGap < 280,"Population coverage has an excessive empty region: "+largestGap);
                Debug.Log($"ERMO COVERAGE PASSED: {groups.Count} locations; largest sampled gap {largestGap:F1} m; minimum group spacing {settings.minimumLocationDistance} m.");
                settings.locationChance = 0;
                settings.landmarks = new LandmarkRule[4];
                for (int mask = 0; mask < 4; mask++)
                    settings.landmarks[mask] = new LandmarkRule { id = "combo-" + mask, kind = PropKind.House,
                        exactChunk = true, chunk = new Vector2Int(mask * 12, -3), localPosition = new Vector2(87, 87),
                        includeWaterTower = (mask & 1) != 0, includeTurbine = (mask & 2) != 0 };
                sampler = new WorldSampler(settings);
                for (int mask = 0; mask < 4; mask++)
                {
                    var props = sampler.Generate(settings.landmarks[mask].chunk).props.FindAll(p => p.groupId == "combo-" + mask);
                    Require(props.Count == 1 + ((mask & 1) != 0 ? 1 : 0) + ((mask & 2) != 0 ? 1 : 0), "House combination missing");
                    Require(props.TrueForAll(p => p.groupAnchor == props[0].groupAnchor), "Combination counted as separate locations");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }

        private static void ValidateAudio()
        {
            var config = AssetDatabase.LoadAssetAtPath<WorldSettings>("Assets/Ermo/Settings/MeadowWorld.asset");
            foreach (var clip in new[] { config.breezeClip,config.grassStepClip,config.dirtStepClip,config.cementStepClip })
            {
                Require(clip != null && clip.samples > 1000,"Missing breeze/footstep clip");
                var data = new float[clip.samples*clip.channels];
                Require(clip.GetData(data,0),"Audio clip could not be decoded");
                double energy = 0; float peak = 0;
                foreach (float sample in data) { energy += sample*sample; peak = Mathf.Max(peak,Mathf.Abs(sample)); }
                Require(energy > .01 && peak < .96f,"Silent or clipping audio: "+clip.name);
            }
            var walk = new StepCadence(); var run = new StepCadence();
            int walkCount = 0, runCount = 0;
            for (int i = 0; i < 600; i++)
            {
                if (walk.Advance(3.3f/60,true,false,false)) walkCount++;
                if (run.Advance(6.2f/60,true,false,true)) runCount++;
            }
            Require(walkCount > 10 && runCount > walkCount,"Footstep cadence does not follow speed");
            for (int i = 0; i < 120; i++)
            {
                Require(!walk.Advance(0,true,false,false),"Idle player emits footsteps");
                Require(!walk.Advance(.1f,false,false,false),"Airborne player emits footsteps");
                Require(!walk.Advance(.1f,true,true,false),"Paused player emits footsteps");
            }
            Require(!walk.Advance(20,true,false,false),"Teleport emitted footsteps");
            Debug.Log("ERMO AUDIO PASSED: decoded clips, headroom, idle/air/pause suppression and walk/run cadence.");
        }

        public static void ValidateBatch()
        {
            try { Validate(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("Ermo validation: " + message);
        }
    }
}
