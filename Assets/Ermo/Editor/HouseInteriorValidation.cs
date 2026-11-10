using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ermo.Editor
{
    public static class HouseInteriorValidation
    {
        // Run in a separate project copy, with graphics enabled. Never replaces an interactive scene.
        public static void RunBatch()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batch-mode project copy.");
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                ValidateHouse();
                ValidatePixels();
                Debug.Log("ERMO interior lighting: PASS (room, apertures, closed/open lighting and exterior identity).");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void ValidateHouse()
        {
            var palette = new WorldPalette(Shader.Find("Ermo/Meadow"));
            var house = new GameObject("Interior validation house");
            house.transform.SetPositionAndRotation(new Vector3(42, 7, -31), Quaternion.Euler(0, 73, 0));
            house.transform.localScale = Vector3.one * 1.3f;
            try
            {
                typeof(WorldGeometry).GetMethod("House", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { house.transform, false, palette });
                var lighting = house.GetComponent<HouseInteriorLighting>();
                Require(lighting != null && lighting.PortalCount == 6, "Imported house must identify one door and five windows.");
                Bounds room = lighting.RoomBounds;
                Debug.Log($"Interior local bounds: {room}; portals: {lighting.PortalCount}");
                Require(Mathf.Abs(room.size.x - 4) < .1f && Mathf.Abs(room.size.z - 7) < .1f,
                    "Room footprint must exclude the porch and wall thickness, independently of world rotation/scale.");
                Require(!palette.plaster.HasProperty("_ErmoInteriorEnabled") || palette.plaster.GetFloat("_ErmoInteriorEnabled") == 0,
                    "The shared palette was modified.");
                foreach (var renderer in house.GetComponentsInChildren<MeshRenderer>())
                    Require(renderer.sharedMaterial != palette.plaster && renderer.sharedMaterial != palette.roof,
                        "House lighting must use owned materials, not mutate the palette.");

                foreach (var aperture in house.GetComponentsInChildren<HouseInteractable>())
                {
                    var closed = (Quaternion)typeof(HouseInteractable).GetField("closedRotation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(aperture);
                    if (aperture.name.ToLowerInvariant().Contains("door"))
                    {
                        aperture.transform.rotation = closed;
                        Require(aperture.OpeningFraction < .001f, "Closed door must report no opening.");
                        aperture.transform.rotation = Quaternion.AngleAxis(45, Vector3.up) * closed;
                        Require(Mathf.Abs(aperture.OpeningFraction - .5f) < .001f, "Door lighting must track its actual animated angle.");
                    }
                    foreach (var hinge in aperture.GetComponentsInChildren<HouseHinge>())
                    {
                        var hingeClosed = (Quaternion)typeof(HouseHinge).GetField("closedRotation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hinge);
                        hinge.transform.rotation = hingeClosed;
                        Require(hinge.OpeningFraction < .001f, "Closed shutter must report no opening.");
                        hinge.transform.rotation = Quaternion.AngleAxis(90, Vector3.up) * hingeClosed;
                        Require(hinge.OpeningFraction > .999f, "Open shutter must report an opening.");
                        hinge.transform.rotation = hingeClosed;
                    }
                    aperture.transform.rotation = closed;
                }
                lighting.RefreshOpenings();
                ValidateHouseExterior(house, room);
            }
            finally { UnityEngine.Object.DestroyImmediate(house); palette.Dispose(); }
        }

        private static void ValidateHouseExterior(GameObject house, Bounds room)
        {
            Shader.SetGlobalFloat("_ErmoViewRange", 0);
            Shader.SetGlobalColor("_ErmoAmbientColor", new Color(.3f, .3f, .3f, 1));
            var cameraObject = new GameObject("Closed house exterior comparison");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = 8;
            var target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGBFloat);
            camera.targetTexture = target;
            var materials = new System.Collections.Generic.HashSet<Material>();
            foreach (var renderer in house.GetComponentsInChildren<MeshRenderer>())
                foreach (var material in renderer.sharedMaterials) materials.Add(material);
            try
            {
                Vector3 center = room.center;
                Vector3[] offsets = { new Vector3(14, 2, 0), new Vector3(-14, 2, 0),
                    new Vector3(0, 2, 14), new Vector3(0, 2, -14), new Vector3(0, 14, 1) };
                foreach (Vector3 offset in offsets)
                {
                    camera.transform.position = house.transform.TransformPoint(center + offset);
                    camera.transform.LookAt(house.transform.TransformPoint(center));
                    foreach (var material in materials) material.SetFloat("_ErmoInteriorEnabled", 0);
                    Color[] baseline = Capture(camera, target);
                    foreach (var material in materials) material.SetFloat("_ErmoInteriorEnabled", 1);
                    Color[] interior = Capture(camera, target);
                    float maximumDifference = 0;
                    for (int i = 0; i < baseline.Length; i++)
                    {
                        Color delta = baseline[i] - interior[i];
                        maximumDifference = Mathf.Max(maximumDifference, Mathf.Abs(delta.r) + Mathf.Abs(delta.g) + Mathf.Abs(delta.b));
                    }
                    Require(maximumDifference < .0001f, $"Closed house exterior changed from direction {offset}: {maximumDifference}.");
                }
            }
            finally
            {
                camera.targetTexture = null;
                target.Release(); UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void ValidatePixels()
        {
            Shader.SetGlobalFloat("_ErmoViewRange", 0);
            Shader.SetGlobalColor("_ErmoAmbientColor", new Color(.3f, .3f, .3f, 1));
            var surface = GameObject.CreatePrimitive(PrimitiveType.Plane);
            surface.transform.localScale = Vector3.one;
            var material = new Material(Shader.Find("Ermo/Meadow"));
            surface.GetComponent<Renderer>().sharedMaterial = material;
            var cameraObject = new GameObject("Interior pixel validation camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.Euler(90, 0, 0));
            camera.orthographic = true; camera.orthographicSize = 5;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var sunObject = new GameObject("Interior pixel validation sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1; sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(90, 0, 0);
            RenderSettings.sun = sun;
            var target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGBFloat);
            camera.targetTexture = target;
            try
            {
                Color[] baseline = Capture(camera, target);
                material.SetFloat("_ErmoInteriorEnabled", 1);
                material.SetMatrix("_ErmoInteriorWorldToLocal", Matrix4x4.identity);
                material.SetVector("_ErmoInteriorMin", new Vector4(-2, 0, -2, 0));
                material.SetVector("_ErmoInteriorMax", new Vector4(2, 3, 2, 0));
                material.SetInteger("_ErmoPortalCount", 1);
                var low = new Vector4[8]; var high = new Vector4[8];
                low[0] = new Vector4(-2.1f, 0, -.5f, 0);
                high[0] = new Vector4(-1.9f, 2, .5f, 0);
                material.SetVectorArray("_ErmoPortalMin", low); material.SetVectorArray("_ErmoPortalMax", high);
                Color[] closed = Capture(camera, target);
                Require(baseline[64*128+64].r > .01f, "Validation surface did not render.");
                Require(closed[64*128+64].r < baseline[64*128+64].r * .1f, "Sealed room did not become dark.");
                RequireExteriorIdentity(baseline, closed);

                low[0].w = 1;
                material.SetVectorArray("_ErmoPortalMin", low);
                Color[] open = Capture(camera, target);
                RequireExteriorIdentity(baseline, open);
                Require(open[64*128+45].r > closed[64*128+45].r * 2, "Opening did not illuminate nearby interior.");
                Require(open[64*128+45].r > open[64*128+83].r * 1.5f, "An opening must not brighten the whole room equally.");

                // Roof outside stays unchanged even when it lies over the room footprint.
                material.SetFloat("_ErmoInteriorRoof", 1);
                Color[] roof = Capture(camera, target);
                Require(Mathf.Abs(roof[64*128+64].r - baseline[64*128+64].r) < .0001f,
                    "Upward-facing exterior roof was darkened.");
                Require(!ShaderUtil.ShaderHasError(material.shader), "Interior shader has compiler errors.");
            }
            finally
            {
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(surface); UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(sunObject);
                target.Release(); UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void RequireExteriorIdentity(Color[] baseline, Color[] changed)
        {
            for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                if (x > 35 && x < 93 && y > 35 && y < 93) continue;
                Color delta = baseline[y*128+x] - changed[y*128+x];
                Require(Mathf.Abs(delta.r) + Mathf.Abs(delta.g) + Mathf.Abs(delta.b) < .0001f,
                    $"Exterior changed at pixel ({x}, {y}).");
            }
        }

        private static Color[] Capture(Camera camera, RenderTexture target)
        {
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(128, 128, TextureFormat.RGBAFloat, false, true);
            image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); image.Apply();
            Color[] pixels = image.GetPixels();
            UnityEngine.Object.DestroyImmediate(image);
            RenderTexture.active = previous;
            return pixels;
        }
    }
}
