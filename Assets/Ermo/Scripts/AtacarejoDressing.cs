using UnityEngine;

namespace Ermo
{
    // Pieces of the market built from Unity primitives at run time, with URP
    // materials copied from the kit: the inside of cold room 13, the meat
    // locker behind it (rails, carcasses, blood, red light, signs) and the
    // lintels that close the wall above each door. Deterministic: fixed
    // random seed. To swap any of it for Blender models, this is the file.
    public sealed class AtacarejoDressing
    {
        readonly Transform root;
        readonly Vector3 origin;
        readonly AtacarejoKit kit;

        public AtacarejoDressing(Transform root, Vector3 origin, AtacarejoKit kit)
        {
            this.root = root;
            this.origin = origin;
            this.kit = kit;
        }

        Material WallMaterial => kit != null && kit.parede != null
            ? kit.parede.GetComponentInChildren<Renderer>().sharedMaterial
            : Paint(null, new Color(.78f, .76f, .71f), .1f);

        // Inside cold room 13: a cold light that leaks a little past the
        // walls (no shadows) and the evaporator hum.
        public void BuildColdRoom()
        {
            var cold = new GameObject("Fria");
            cold.transform.SetParent(root, false);
            cold.transform.localPosition = new Vector3(105.3f, 4.2f, 39f) - origin;
            var light = cold.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(.62f, .82f, 1f);
            light.intensity = 6f;
            light.range = 9f;
            light.shadows = LightShadows.None;
            AtacarejoLights.Register(light, light.intensity);
            AtacarejoAudio.Loop3D(cold, AtacarejoSounds.Evaporator, .55f, 1.5f, 14f);
            // A cold wash on the "13" plate and the door from the aisle side.
            var sign = new GameObject("Luz da placa 13");
            sign.transform.SetParent(root, false);
            sign.transform.localPosition = new Vector3(101.2f, 3.6f, 39f) - origin;
            var wash = sign.AddComponent<Light>();
            wash.type = LightType.Point;
            wash.color = AtacarejoBuilder.ColdColor;
            wash.intensity = 1.6f;
            wash.range = 5.5f;
            wash.shadows = LightShadows.None;
        }

        static Mesh cube, cylinder, capsule, sphere;
        static Material blood, meat, fat, bone, rail, bulb, letters;

        static Mesh Primitive(PrimitiveType type)
        {
            var temp = GameObject.CreatePrimitive(type);
            var mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(temp);
            return mesh;
        }

        Material Paint(Material cached, Color color, float smoothness, Color emission = default)
        {
            if (cached != null) return cached;
            var template = kit != null && kit.parede != null ? kit.parede.GetComponentInChildren<Renderer>().sharedMaterial : null;
            var material = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Glossiness", smoothness);
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            return material;
        }

        GameObject Piece(string name, Mesh mesh, Material material, Vector3 layout, Vector3 euler, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = layout - origin;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        // A puddle is a few flat discs of different sizes on top of each
        // other, so the edge is never a perfect circle.
        void Puddle(System.Random random, float x, float z, float size)
        {
            int blobs = 3 + random.Next(3);
            for (int i = 0; i < blobs; i++)
            {
                float r = size * (.45f + (float)random.NextDouble() * .55f);
                float ox = (float)(random.NextDouble() - .5) * size * .9f, oz = (float)(random.NextDouble() - .5) * size * .9f;
                float stretch = .7f + (float)random.NextDouble() * .6f;
                Piece("Sangue", cylinder, blood, new Vector3(x + ox, .002f + i * .0004f, z + oz),
                    new Vector3(0f, (float)random.NextDouble() * 180f, 0f), new Vector3(r * 2f * stretch, .002f, r * 2f));
            }
        }

        // Blood thrown at a wall: one main stain, a couple of overlapping
        // ones, a spray of small droplets fanning out, and runs going down.
        void Spatter(System.Random random, Vector3 at, Vector3 normal, float size)
        {
            Vector3 along = Mathf.Abs(normal.x) > .5f ? Vector3.forward : Vector3.right;
            Quaternion face = Quaternion.FromToRotation(Vector3.up, normal);
            int layer = 0;
            void Stain(Vector3 center, float width, float height)
            {
                var turn = Quaternion.AngleAxis((float)random.NextDouble() * 180f, normal) * face;
                Piece("Respingo", cylinder, blood, center + normal * (.004f + .0007f * layer++), turn.eulerAngles, new Vector3(width, .002f, height));
            }
            float main = size * (.45f + (float)random.NextDouble() * .2f);
            Stain(at, main, main * (.6f + (float)random.NextDouble() * .3f));
            int extra = 1 + random.Next(2);
            for (int i = 0; i < extra; i++)
            {
                Vector3 offset = along * (float)(random.NextDouble() - .5) * main * .6f + Vector3.up * (float)(random.NextDouble() - .5) * main * .4f;
                float w = main * (.35f + (float)random.NextDouble() * .3f);
                Stain(at + offset, w, w * .7f);
            }
            float fan = (float)random.NextDouble() * Mathf.PI * 2f;
            int drops = 9 + random.Next(7);
            for (int i = 0; i < drops; i++)
            {
                float angle = fan + (float)(random.NextDouble() - .5) * 2.2f;
                float distance = main * (.55f + (float)random.NextDouble() * 1.1f);
                Vector3 offset = (along * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle)) * distance;
                float d = .015f + (float)random.NextDouble() * .04f;
                Stain(at + offset, d, d * .75f);
            }
            int runs = 2 + random.Next(3);
            for (int i = 0; i < runs; i++)
            {
                float length = .2f + (float)random.NextDouble() * Mathf.Min(1.1f, at.y - main * .3f - .15f);
                float width = .012f + (float)random.NextDouble() * .022f;
                Vector3 top = at + along * (float)(random.NextDouble() - .5) * main * .7f + Vector3.down * main * .2f;
                Vector3 scale = Mathf.Abs(normal.x) > .5f ? new Vector3(.003f, length, width) : new Vector3(width, length, .003f);
                Piece("Escorrido", cube, blood, top + Vector3.down * length * .5f + normal * .0055f, Vector3.zero, scale);
            }
        }

        // Wall running along Z, stain facing +X or -X.
        void Splash(System.Random random, float x, float facing, float y, float z, float size) =>
            Spatter(random, new Vector3(x, y, z), new Vector3(facing, 0f, 0f), size);

        // Wall running along X, stain facing +Z or -Z.
        void SplashSide(System.Random random, float x, float y, float z, float facing, float size) =>
            Spatter(random, new Vector3(x, y, z), new Vector3(0f, 0f, facing), size);

        // A plate with blocky letters (PixelLetters), facing -X: read from
        // the cold room side, walking toward +X.
        void Sign(string text, Vector3 layout, float width, Color back)
        {
            int columns = PixelLetters.Columns(text);
            int lines = text.Split('\n').Length;
            float pixel = (width - .12f) / columns;
            float height = (lines * PixelLetters.Rows + (lines - 1) * 2f) * pixel + .1f;
            Piece("Placa " + text, cube, Paint(null, back, .2f), layout, new Vector3(0f, 90f, 0f), new Vector3(width, height, .02f));
            letters = Paint(letters, new Color(.93f, .91f, .86f), .2f, new Color(.95f, .93f, .88f));
            var label = Piece("Letreiro", PixelLetters.Build(text, pixel), letters, layout + new Vector3(-.012f, 0f, 0f),
                new Vector3(0f, 90f, 0f), Vector3.one);
            label.name = "Letreiro " + text.Replace('\n', ' ');
        }

        static void Part(GameObject parent, Mesh mesh, Material material, Vector3 position, Vector3 euler, Vector3 scale)
        {
            var go = new GameObject(mesh.name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // A 0.2 m wall slab from the top of a 2.4 m door to the ceiling.
        public void Lintel(Vector3 doorLayout, bool alongX)
        {
            if (cube == null) cube = Primitive(PrimitiveType.Cube);
            var slab = Piece("Verga", cube, WallMaterial, new Vector3(doorLayout.x, 3.95f, doorLayout.z), Vector3.zero,
                alongX ? new Vector3(1.62f, 3.1f, .2f) : new Vector3(.2f, 3.1f, 1.62f));
            slab.AddComponent<BoxCollider>();
        }

        public void BuildLocker()
        {
            if (cube == null) cube = Primitive(PrimitiveType.Cube);
            if (cylinder == null) cylinder = Primitive(PrimitiveType.Cylinder);
            if (capsule == null) capsule = Primitive(PrimitiveType.Capsule);
            if (sphere == null) sphere = Primitive(PrimitiveType.Sphere);
            blood = Paint(blood, new Color(.42f, .02f, .03f), .65f);
            meat = Paint(meat, new Color(.60f, .20f, .17f), .45f);
            fat = Paint(fat, new Color(.86f, .76f, .62f), .35f);
            bone = Paint(bone, new Color(.90f, .86f, .78f), .3f);
            rail = Paint(rail, new Color(.36f, .39f, .42f), .5f);
            bulb = Paint(bulb, new Color(.6f, .05f, .03f), .3f, new Color(1.6f, .12f, .06f));
            var random = new System.Random(1313);
            var door = AtacarejoLayout.LockerDoor;
            var next = AtacarejoLayout.NextDoor;
            var locker = AtacarejoLayout.Locker;
            float lane = door.z;

            // Rails, chains, carcasses. The lane at z = 39 stays clear.
            float[] rails = { 108.9f, 110.7f };
            float[] hooks = { 35.1f, 36.3f, 37.45f, 40.55f, 41.7f, 42.9f };
            foreach (float x in rails)
            {
                Piece("Trilho", cube, rail, new Vector3(x, 4.3f, (locker.yMin + locker.yMax) * .5f), Vector3.zero, new Vector3(.08f, .08f, locker.height - .4f));
                foreach (float z in hooks)
                {
                    if (random.NextDouble() < .12) continue;
                    float drop = (float)random.NextDouble() * .35f;
                    Piece("Corrente", cylinder, rail, new Vector3(x, 3.85f - drop * .5f, z), Vector3.zero, new Vector3(.025f, .45f + drop * .5f, .025f));
                    float yaw = (float)random.NextDouble() * 360f, tilt = (float)(random.NextDouble() - .5) * 6f;
                    float top = 3.4f - drop;
                    // A side of beef hung by the shank: two shanks to the
                    // hook with bone ends, a long tapered body, white ribs on
                    // the cut face, a fat layer on the back.
                    var carcass = new GameObject("Carcaça");
                    carcass.transform.SetParent(root, false);
                    carcass.transform.localPosition = new Vector3(x, top, z) - origin;
                    carcass.transform.localRotation = Quaternion.Euler(tilt, yaw, 0f);
                    Part(carcass, sphere, meat, new Vector3(-.09f, -.32f, 0f), new Vector3(0f, 0f, -7f), new Vector3(.13f, .62f, .13f));
                    Part(carcass, sphere, meat, new Vector3(.09f, -.32f, 0f), new Vector3(0f, 0f, 7f), new Vector3(.13f, .62f, .13f));
                    Part(carcass, sphere, bone, new Vector3(-.07f, -.03f, 0f), Vector3.zero, Vector3.one * .1f);
                    Part(carcass, sphere, bone, new Vector3(.07f, -.03f, 0f), Vector3.zero, Vector3.one * .1f);
                    Part(carcass, sphere, meat, new Vector3(0f, -1.05f, 0f), Vector3.zero, new Vector3(.5f, 1.15f, .32f));
                    Part(carcass, sphere, meat, new Vector3(0f, -1.72f, .01f), Vector3.zero, new Vector3(.3f, .5f, .22f));
                    Part(carcass, sphere, fat, new Vector3(0f, -1.0f, .085f), Vector3.zero, new Vector3(.44f, .98f, .18f));
                    for (int r = 0; r < 6; r++)
                    {
                        float dy = .3f - r * .11f;
                        float k = Mathf.Sqrt(Mathf.Max(0f, 1f - (dy / .575f) * (dy / .575f)));
                        Part(carcass, cube, bone, new Vector3(0f, -1.05f + dy, -.16f * k + .03f), new Vector3(0f, 0f, r % 2 == 0 ? 4f : -3f),
                            new Vector3(.36f * k, .032f, .05f));
                    }
                    var collider = carcass.AddComponent<CapsuleCollider>();
                    collider.center = new Vector3(0f, -1.05f, 0f);
                    collider.height = 2.1f; collider.radius = .25f;
                    Puddle(random, x + (float)(random.NextDouble() - .5) * .2f, z, .3f + (float)random.NextDouble() * .25f);
                }
            }

            // A drag trail from the cold room, through the locker, to the back door.
            for (float x = AtacarejoLayout.ColdRoom.xMin + .6f; x < next.x - .3f; x += .32f)
            {
                float z = lane + Mathf.Sin(x * 1.7f) * .18f + (float)(random.NextDouble() - .5) * .1f;
                float size = .1f + (float)random.NextDouble() * .12f;
                if (x > door.x) size *= 1.8f;
                Piece("Rastro", cylinder, blood, new Vector3(x, .0025f, z), new Vector3(0f, (float)random.NextDouble() * 40f - 20f, 0f),
                    new Vector3(size * 2.4f, .002f, size));
            }
            Puddle(random, next.x - 1.1f, lane + .1f, 1.15f);
            Puddle(random, 109.8f, lane - .2f, .6f);
            Puddle(random, 108.3f, 35.0f, .5f);
            Puddle(random, 111.2f, 43.0f, .55f);
            Puddle(random, 106.8f, 39.4f, .35f);

            // Walls: side walls run along X, the back wall along Z.
            for (int i = 0; i < 5; i++)
                SplashSide(random, 108.2f + i * .85f, .6f + (float)random.NextDouble() * 1.8f, locker.yMin + .01f, 1f, .35f + (float)random.NextDouble() * .4f);
            for (int i = 0; i < 5; i++)
                SplashSide(random, 108.4f + i * .8f, .6f + (float)random.NextDouble() * 1.8f, locker.yMax - .01f, -1f, .35f + (float)random.NextDouble() * .4f);
            Splash(random, next.x - .1f, -1f, 1.5f, 36.6f, .6f);
            Splash(random, next.x - .1f, -1f, 1.1f, 41.3f, .5f);
            // A hand on the back door, at the height a hand would be.
            Splash(random, next.x - .065f, -1f, 1.25f, lane + .35f, .28f);

            // Lintels: the wall modules stop at the door gaps all the way up
            // to the ceiling. Close the gap above each door leaf.
            Lintel(new Vector3(AtacarejoLayout.ColdDoor.x, 0f, AtacarejoLayout.ColdDoor.z), false);
            Lintel(door, false);
            Lintel(next, false);

            // Floor drain.
            Piece("Ralo", cylinder, rail, new Vector3(109.8f, .003f, lane), Vector3.zero, new Vector3(.32f, .003f, .32f));

            // Signs: one over the locker door, seen from the cold room; one on
            // the back door.
            Sign("FRIGORÍFICO", new Vector3(door.x - .12f, 2.72f, door.z), 1.4f, new Color(.42f, .05f, .04f));
            Sign("ENTRADA DE\nFUNCIONÁRIOS", new Vector3(next.x - .08f, 1.9f, next.z), 1.2f, new Color(.16f, .17f, .18f));

            // Red light. A ceiling spot keeps it inside the room; a caged bulb
            // over the back door marks the way on.
            var red = new GameObject("Luz do frigorífico");
            red.transform.SetParent(root, false);
            red.transform.localPosition = new Vector3(109.8f, 5.1f, lane) - origin;
            red.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var spot = red.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1f, .16f, .10f);
            spot.intensity = 8f;
            spot.range = 8f;
            spot.spotAngle = 100f;
            spot.innerSpotAngle = 55f;
            spot.shadows = LightShadows.None;
            AtacarejoLights.Register(spot, spot.intensity, null, Color.black, AtacarejoLights.Flicker.Sick);
            Piece("Lâmpada vermelha", sphere, bulb, new Vector3(next.x - .16f, 2.62f, lane), Vector3.zero, Vector3.one * .14f);
            var cage = new GameObject("Luz da porta dos fundos");
            cage.transform.SetParent(root, false);
            cage.transform.localPosition = new Vector3(next.x - .45f, 2.5f, lane) - origin;
            var glow = cage.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, .22f, .12f);
            glow.intensity = 1.8f;
            glow.range = 3.2f;
            glow.shadows = LightShadows.None;

            var sound = new GameObject("Gotejamento");
            sound.transform.SetParent(root, false);
            sound.transform.localPosition = new Vector3(109.8f, 1.5f, lane) - origin;
            AtacarejoAudio.Loop3D(sound, AtacarejoSounds.Drips, .7f, 1.5f, 11f);
        }
    }
}
