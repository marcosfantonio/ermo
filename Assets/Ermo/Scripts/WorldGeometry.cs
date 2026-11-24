using System.Collections.Generic;
using UnityEngine;

namespace Ermo
{
    public sealed class WorldPalette
    {
        private readonly List<Material> owned = new List<Material>();
        public readonly Material grass, road, plaster, roof, wood, glass, stone, door, dark, white, tank, wire, water, waterUnder, leaf;
        public WorldPalette(Shader shader, WorldSettings settings = null)
        {
            grass = Make(shader, "Meadow", Color.white);
            road = Make(shader, "Dirt road", new Color(.76f, .62f, .46f));
            plaster = Make(shader, "Ivory walls", new Color(.94f, .91f, .71f));
            roof = Make(shader, "Faded terracotta roof", new Color(.47f, .57f, .53f));
            wood = Make(shader, "Weathered timber", new Color(.31f, .23f, .15f));
            glass = Make(shader, "Blue windows", new Color(.17f, .38f, .48f));
            stone = Make(shader, "Stone floor", new Color(.84f, .87f, .83f));
            door = Make(shader, "Blue painted door", Color.white);
            dark = Make(shader, "Unlit interior", new Color(.055f, .075f, .075f));
            white = Make(shader, "Chalk white", new Color(.91f, .94f, .84f));
            tank = Make(shader, "Water tank", new Color(.69f, .77f, .70f));
            wire = Make(shader, "Power cable", new Color(.12f, .15f, .16f));
            leaf = Make(shader, "Olive canopy", Color.white);
            water = Make(Resources.Load<Shader>("Ermo/Water/ReservoirWater"),"Clear reservoir water",new Color(.35f,.75f,.78f));
            water.SetFloat("_WaterOpacity",.32f);
            water.SetFloat("_WellDepth",7.5f);
            water.SetFloat("_WellRadius",WaterTowerAccess.InnerRadius);
            waterUnder = Make(Resources.Load<Shader>("Ermo/Water/ReservoirWater"),"Inverted lower water surface",new Color(.35f,.75f,.78f));
            waterUnder.SetFloat("_OtherWorldView",1);
            waterUnder.SetFloat("_PortalInverted",1);
            waterUnder.SetFloat("_WaterOpacity",1);
            grass.SetFloat("_Grass", 1);
            road.SetFloat("_Road", 1);
            if (settings != null)
            {
                grass.SetTexture("_MainTex", settings.grassTexture);
                road.SetTexture("_MainTex", settings.roadTexture);
                road.SetColor("_BaseColor", settings.roadTexture != null ? Color.white : new Color(.84f,.77f,.59f));
                road.SetFloat("_Grass", 0);
                road.SetFloat("_TextureScale", 1);
                plaster.SetTexture("_MainTex", settings.plasterTexture);
                plaster.SetColor("_BaseColor", new Color(1,.98f,.91f)); plaster.SetFloat("_TextureScale",2);
                roof.SetTexture("_MainTex", settings.roofTexture); roof.SetColor("_BaseColor", Color.white); roof.SetFloat("_TextureScale",4);
                wood.SetTexture("_MainTex", settings.woodTexture); wood.SetColor("_BaseColor",Color.white);
                door.SetTexture("_MainTex", Resources.Load<Texture2D>("Ermo/blue_door")); door.SetFloat("_TextureScale",2);
                glass.SetTexture("_MainTex", Resources.Load<Texture2D>("Ermo/glass"));
                glass.SetColor("_BaseColor",new Color(.68f,.84f,.91f));
                stone.SetTexture("_MainTex", Resources.Load<Texture2D>("Ermo/stone_ps1"));
                stone.SetFloat("_TextureScale",1.5f);
                foreach (var material in owned)
                    if (material != grass && material != road && material != water && material != waterUnder) material.SetFloat("_Wobble",settings.vertexWobble);
            }
        }
        private Material Make(Shader shader, string name, Color color)
        {
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            owned.Add(material);
            return material;
        }
        public void Dispose()
        {
            foreach (var material in owned)
                if (Application.isPlaying) Object.Destroy(material);
                else Object.DestroyImmediate(material);
        }
    }

    public static class WorldGeometry
    {
        private static GameObject houseModel;
        private static bool houseModelLoaded;

        public static GameObject BuildChunk(ChunkRecord record, WorldSettings settings, WorldSampler sampler,
            WorldPalette palette, Transform parent)
        {
            var root = new GameObject($"Chunk [{record.x}, {record.z}]");
            root.transform.SetParent(parent, false);
            float size = settings.chunkSize;
            root.transform.position = new Vector3(record.x * size, 0, record.z * size);
            int n = settings.terrainResolution;
            var vertices = new Vector3[(n + 1) * (n + 1)];
            var normals = new Vector3[vertices.Length];
            var colors = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[n * n * 6];
            for (int z = 0; z <= n; z++)
                for (int x = 0; x <= n; x++)
                {
                    int i = z * (n + 1) + x;
                    float lx = x * size / n, lz = z * size / n;
                    float wx = record.x * size + lx, wz = record.z * size + lz;
                    vertices[i] = new Vector3(lx, sampler.Height(wx, wz), lz);
                    // World-space derivatives keep lighting continuous along chunk boundaries.
                    normals[i] = new Vector3(sampler.Height(wx - .5f, wz) - sampler.Height(wx + .5f, wz),
                        1, sampler.Height(wx, wz - .5f) - sampler.Height(wx, wz + .5f)).normalized;
                    float patch = Mathf.Sin(wx*.07f + Mathf.Sin(wz*.1f)) * Mathf.Cos(wz*.05f);
                    colors[i] = new Color(.31f+patch*.04f,.53f+patch*.05f,.15f+patch*.025f);
                    uv[i] = new Vector2(wx,wz)*.24f;
                    if (x == n || z == n) continue;
                    int t = (z * n + x) * 6;
                    triangles[t] = i; triangles[t + 1] = i + n + 1; triangles[t + 2] = i + 1;
                    triangles[t + 3] = i + 1; triangles[t + 4] = i + n + 1; triangles[t + 5] = i + n + 2;
                }
            var mesh = new Mesh { name = "Meadow terrain", vertices = vertices, normals = normals, colors = colors, uv = uv, triangles = triangles };
            if (settings.terrainStyle != TerrainStyle.Perlin && !settings.smoothTerrainNormals) MakeFlat(mesh);
            mesh.RecalculateBounds();
            MeshObject("Ground", root.transform, mesh, palette.grass, true);
            BuildRoad(root.transform, record, settings, sampler, palette.road);
            BuildDriveways(root.transform, record, settings, sampler, palette);
            BuildFences(root.transform, record, settings, sampler, palette);
            BuildTrees(root.transform, record, settings, sampler, palette);
            foreach (var prop in record.props) BuildProp(prop, root.transform, sampler, palette);
            root.AddComponent<ChunkMeshes>();
            return root;
        }

        private static void BuildRoad(Transform root, ChunkRecord record, WorldSettings settings, WorldSampler sampler, Material material)
        {
            // Global sample grid, clipped to chunk ownership: no double roads or seams.
            const int strips = 64;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            float startZ = record.z * settings.chunkSize;
            for (int lane = Mathf.FloorToInt(record.x * settings.chunkSize / WorldSampler.RoadSpacing) - 1;
                lane <= Mathf.CeilToInt((record.x + 1) * settings.chunkSize / WorldSampler.RoadSpacing) + 1; lane++)
            for (int i = 0; i < strips; i++)
            {
                float z0 = startZ + i * settings.chunkSize / (float)strips;
                float z1 = startZ + (i + 1) * settings.chunkSize / (float)strips;
                float left0 = sampler.RoadX(z0, lane) - settings.roadWidth / 2;
                float left1 = sampler.RoadX(z1, lane) - settings.roadWidth / 2;
                float minX = record.x * settings.chunkSize, maxX = minX + settings.chunkSize;
                if (Mathf.Max(left0, left1) + settings.roadWidth < minX || Mathf.Min(left0, left1) > maxX) continue;
                float lx0 = Mathf.Clamp(left0, minX, maxX), rx0 = Mathf.Clamp(left0 + settings.roadWidth, minX, maxX);
                float lx1 = Mathf.Clamp(left1, minX, maxX), rx1 = Mathf.Clamp(left1 + settings.roadWidth, minX, maxX);
                // u runs across the road so the two ruts stay under the wheels; v follows its length.
                int v = vertices.Count;
                AddRoadVertex(vertices, uvs, lx0, z0, (lx0-left0)/settings.roadWidth, z0*.18f, sampler, root.position);
                AddRoadVertex(vertices, uvs, rx0, z0, (rx0-left0)/settings.roadWidth, z0*.18f, sampler, root.position);
                AddRoadVertex(vertices, uvs, lx1, z1, (lx1-left1)/settings.roadWidth, z1*.18f, sampler, root.position);
                AddRoadVertex(vertices, uvs, rx1, z1, (rx1-left1)/settings.roadWidth, z1*.18f, sampler, root.position);
                triangles.AddRange(new[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
            }
            if (vertices.Count == 0) return;
            var mesh = new Mesh { name = "Winding path" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0,uvs); mesh.RecalculateNormals();
            MeshObject("Path", root, mesh, material, false);
        }
        private static void AddRoadVertex(List<Vector3> vertices, List<Vector2> uv, float x, float z, float u, float v,
            WorldSampler sampler, Vector3 origin)
        {
            vertices.Add(new Vector3(x,sampler.MeshHeight(x,z)+.045f,z) - origin);
            uv.Add(new Vector2(u,v));
        }

        private static void MakeFlat(Mesh mesh)
        {
            int[] indices = mesh.triangles;
            var source = mesh.vertices; var sourceUV = mesh.uv; var sourceColor = mesh.colors;
            var vertices = new Vector3[indices.Length]; var uv = new Vector2[indices.Length]; var colors = new Color[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                vertices[i] = source[indices[i]]; uv[i] = sourceUV[indices[i]]; colors[i] = sourceColor[indices[i]];
                indices[i] = i;
            }
            mesh.Clear(); mesh.vertices = vertices; mesh.uv = uv; mesh.colors = colors; mesh.triangles = indices;
            mesh.RecalculateNormals();
        }

        private sealed class MeshBuilder
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<int> Triangles = new List<int>();
            public readonly List<Vector2> UVs = new List<Vector2>();
            public readonly List<Color> Colors = new List<Color>();
            public bool Empty => Vertices.Count == 0;

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
                => Quad(a,b,c,d,color,new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1));

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color,
                Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                int index = Vertices.Count;
                Vertices.Add(a); Vertices.Add(b); Vertices.Add(c); Vertices.Add(d);
                UVs.Add(ua); UVs.Add(ub); UVs.Add(uc); UVs.Add(ud);
                Colors.Add(color); Colors.Add(color); Colors.Add(color); Colors.Add(color);
                Triangles.Add(index); Triangles.Add(index+1); Triangles.Add(index+2);
                Triangles.Add(index); Triangles.Add(index+2); Triangles.Add(index+3);
            }

            // A ground quad that always faces up, whichever way the strip was walked.
            public void Ground(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color,
                Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                if (Vector3.Cross(b-a,c-a).y < 0) Quad(a,d,c,b,color,ua,ud,uc,ub);
                else Quad(a,b,c,d,color,ua,ub,uc,ud);
            }

            public void Box(Vector3 center, Vector3 size, Color color)
            {
                Vector3 h = size*.5f;
                Quad(center+new Vector3(h.x,-h.y,-h.z),center+new Vector3(h.x,h.y,-h.z),center+new Vector3(h.x,h.y,h.z),center+new Vector3(h.x,-h.y,h.z),color);
                Quad(center+new Vector3(-h.x,-h.y,h.z),center+new Vector3(-h.x,h.y,h.z),center+new Vector3(-h.x,h.y,-h.z),center+new Vector3(-h.x,-h.y,-h.z),color);
                Quad(center+new Vector3(-h.x,h.y,-h.z),center+new Vector3(-h.x,h.y,h.z),center+new Vector3(h.x,h.y,h.z),center+new Vector3(h.x,h.y,-h.z),color);
                Quad(center+new Vector3(-h.x,-h.y,h.z),center+new Vector3(-h.x,-h.y,-h.z),center+new Vector3(h.x,-h.y,-h.z),center+new Vector3(h.x,-h.y,h.z),color);
                Quad(center+new Vector3(-h.x,-h.y,h.z),center+new Vector3(h.x,-h.y,h.z),center+new Vector3(h.x,h.y,h.z),center+new Vector3(-h.x,h.y,h.z),color);
                Quad(center+new Vector3(h.x,-h.y,-h.z),center+new Vector3(-h.x,-h.y,-h.z),center+new Vector3(-h.x,h.y,-h.z),center+new Vector3(h.x,h.y,-h.z),color);
            }

            public void Beam(Vector3 a, Vector3 b, float thickness, Color color)
            {
                Vector3 axis = b-a;
                if (axis.sqrMagnitude < .000001f) return;
                Vector3 dir = axis.normalized;
                Vector3 up = Mathf.Abs(Vector3.Dot(dir,Vector3.up)) > .95f ? Vector3.right : Vector3.up;
                Vector3 right = Vector3.Cross(dir,up).normalized*thickness*.5f;
                Vector3 forward = Vector3.Cross(right.normalized,dir).normalized*thickness*.5f;
                Vector3[] corners = { right+forward, -right+forward, -right-forward, right-forward };
                for (int i = 0; i < 4; i++)
                {
                    Vector3 next = corners[(i+1)%4];
                    Quad(a+corners[i],b+corners[i],b+next,a+next,color);
                }
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(Vertices); mesh.SetUVs(0,UVs); mesh.SetColors(Colors);
                mesh.SetTriangles(Triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                return mesh;
            }
        }

        private static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble()*(max-min);

        // A short dirt spur from the road to each house, with a gate at the fence line.
        private static void BuildDriveways(Transform root, ChunkRecord record, WorldSettings settings,
            WorldSampler sampler, WorldPalette palette)
        {
            var drive = new MeshBuilder();
            var gate = new MeshBuilder();
            foreach (var prop in record.props)
            {
                if (prop.kind != PropKind.House && prop.kind != PropKind.OpenHouse) continue;
                int lane = sampler.RoadLane(prop.position.x);
                float roadX = sampler.RoadX(prop.position.z,lane);
                float side = roadX >= prop.position.x ? 1 : -1;
                float startX = prop.position.x + side*5.2f;
                float endX = roadX - side*settings.roadWidth*.5f;
                if (Mathf.Abs(endX-startX) < 2.5f || Mathf.Abs(endX-startX) > 42f) continue;
                const int segments = 7;
                float halfWidth = 1.45f;
                Vector3 previousRight = Vector3.zero, previousLeft = Vector3.zero;
                for (int i = 0; i <= segments; i++)
                {
                    float t = i/(float)segments;
                    float x = Mathf.Lerp(startX,endX,t);
                    float z = prop.position.z + Mathf.Sin(t*Mathf.PI)*side*.55f;
                    Vector3 right = DrivewayVertex(x,z-halfWidth,sampler,root.position);
                    Vector3 left = DrivewayVertex(x,z+halfWidth,sampler,root.position);
                    if (i > 0)
                    {
                        float v0 = (i-1)/(float)segments*1.5f, v1 = i/(float)segments*1.5f;
                        drive.Ground(previousRight,right,left,previousLeft,Color.white,
                            new Vector2(0,v0),new Vector2(0,v1),new Vector2(1,v1),new Vector2(1,v0));
                    }
                    previousRight = right; previousLeft = left;
                }
                float gateX = roadX + side*(settings.roadWidth*.5f+2.4f);
                BuildGate(gate,gateX,prop.position.z,side,sampler,root.position);
            }
            if (!drive.Empty) MeshObject("Ermo driveway",root,drive.ToMesh("Ermo driveway"),palette.road,false);
            if (!gate.Empty) MeshObject("Ermo gate",root,gate.ToMesh("Ermo gate"),palette.wood,true);
        }

        private static Vector3 DrivewayVertex(float x, float z, WorldSampler sampler, Vector3 origin)
            => new Vector3(x,sampler.MeshHeight(x,z)+.03f,z) - origin;

        private static void BuildGate(MeshBuilder builder, float x, float z, float side, WorldSampler sampler, Vector3 origin)
        {
            float y = sampler.MeshHeight(x,z);
            Vector3 left = new Vector3(x,y,z-1.9f)-origin;
            Vector3 right = new Vector3(x,y,z+1.9f)-origin;
            builder.Box(left+Vector3.up*.75f,new Vector3(.18f,1.5f,.18f),Color.white);
            builder.Box(right+Vector3.up*.75f,new Vector3(.18f,1.5f,.18f),Color.white);
            // The leaf stands open along the road, leaving the driveway walkable.
            for (int i = 0; i < 3; i++)
            {
                float h = .45f+i*.36f;
                builder.Beam(right+Vector3.up*h,right+new Vector3(0,0,2.6f)+Vector3.up*h,.07f,Color.white);
            }
            builder.Beam(right+new Vector3(0,0,2.6f)+Vector3.up*.45f,right+new Vector3(0,0,2.6f)+Vector3.up*1.2f,.07f,Color.white);
        }

        // Wire fences run in 48 m stretches, so they continue across chunk borders.
        private static void BuildFences(Transform root, ChunkRecord record, WorldSettings settings,
            WorldSampler sampler, WorldPalette palette)
        {
            if (settings.fenceDensity <= 0) return;
            var posts = new MeshBuilder();
            var wires = new MeshBuilder();
            float size = settings.chunkSize, startZ = record.z*size, endZ = startZ+size;
            float minX = record.x*size-2, maxX = (record.x+1)*size+2;
            float offset = settings.roadWidth*.5f+2.4f;
            for (int side = -1; side <= 1; side += 2)
            for (int lane = Mathf.FloorToInt(record.x*size/WorldSampler.RoadSpacing)-1;
                lane <= Mathf.CeilToInt((record.x+1)*size/WorldSampler.RoadSpacing)+1; lane++)
            {
                bool hasPrevious = false;
                float previousZ = 0;
                Vector3 previous = Vector3.zero;
                for (float z = Mathf.Ceil(startZ/6f)*6f; z <= endZ+.01f; z += 6f)
                {
                    int stretch = Mathf.FloorToInt(z/48f);
                    var rng = WorldHash.Random(settings.seed,stretch,side,"fence-stretch");
                    float x = sampler.RoadX(z,lane)+side*offset;
                    bool inside = x >= minX && x <= maxX;
                    bool open = rng.NextDouble() >= settings.fenceDensity
                        || NearDriveway(record,settings,sampler,x,z,3.5f);
                    if (!inside || open) { hasPrevious = false; continue; }
                    Vector3 ground = new Vector3(x,sampler.MeshHeight(x,z),z)-root.position;
                    posts.Box(ground+Vector3.up*.7f,new Vector3(.16f,1.4f,.16f),Color.white);
                    if (hasPrevious && Mathf.Abs(z-previousZ) < 7.5f)
                        for (int wireIndex = 0; wireIndex < 3; wireIndex++)
                        {
                            float h = .55f+wireIndex*.37f;
                            wires.Beam(previous+Vector3.up*h,ground+Vector3.up*h,.04f,Color.white);
                        }
                    previous = ground; previousZ = z; hasPrevious = true;
                }
            }
            if (!posts.Empty) MeshObject("Ermo fence posts",root,posts.ToMesh("Ermo fence posts"),palette.wood,true);
            if (!wires.Empty) MeshObject("Ermo fence wire",root,wires.ToMesh("Ermo fence wire"),palette.wire,true);
        }

        private static bool NearDriveway(ChunkRecord record, WorldSettings settings, WorldSampler sampler,
            float x, float z, float margin)
        {
            foreach (var prop in record.props)
            {
                if (prop.kind != PropKind.House && prop.kind != PropKind.OpenHouse) continue;
                int lane = sampler.RoadLane(prop.position.x);
                float roadX = sampler.RoadX(prop.position.z,lane);
                float side = roadX >= prop.position.x ? 1 : -1;
                float startX = prop.position.x+side*5.2f;
                float endX = roadX-side*settings.roadWidth*.5f;
                if (x >= Mathf.Min(startX,endX)-margin && x <= Mathf.Max(startX,endX)+margin &&
                    Mathf.Abs(z-prop.position.z) <= margin+1.5f) return true;
            }
            return false;
        }

        // Sparse groves and lone trees, deterministically owned by their chunk.
        private static void BuildTrees(Transform root, ChunkRecord record, WorldSettings settings,
            WorldSampler sampler, WorldPalette palette)
        {
            if (settings.treeDensity <= 0) return;
            var trunks = new MeshBuilder();
            var leaves = new MeshBuilder();
            var rng = WorldHash.Random(settings.seed,record.x,record.z,"trees-v1");
            float size = settings.chunkSize, baseX = record.x*size, baseZ = record.z*size;
            var placed = new List<Vector2>();

            void TryTree(float x, float z)
            {
                if (x < baseX+2 || x > baseX+size-2 || z < baseZ+2 || z > baseZ+size-2) return;
                if (sampler.RoadDistance(x,z) < settings.roadWidth*.5f+4f) return;
                if (NearDriveway(record,settings,sampler,x,z,3f)) return;
                var point = new Vector2(x,z);
                foreach (var prop in record.props)
                    if ((new Vector2(prop.position.x,prop.position.z)-point).sqrMagnitude < 11f*11f) return;
                foreach (var other in placed)
                    if ((other-point).sqrMagnitude < 5f*5f) return;
                placed.Add(point);

                float height = Range(rng,4.4f,7.8f);
                float radius = Range(rng,.17f,.29f);
                Vector3 ground = new Vector3(x,sampler.MeshHeight(x,z)-.18f,z)-root.position;
                Vector3 top = ground+Vector3.up*height+new Vector3(Range(rng,-.5f,.5f),0,Range(rng,-.5f,.5f));
                Trunk(trunks,ground,top,radius,radius*.42f,Color.white);
                Color canopy = Color.Lerp(new Color(.20f,.27f,.10f),new Color(.36f,.42f,.18f),(float)rng.NextDouble());
                canopy = Color.Lerp(canopy,new Color(.44f,.36f,.17f),.18f);
                int blobs = 2+rng.Next(0,2);
                for (int blob = 0; blob < blobs; blob++)
                {
                    float y = height*(.60f+blob*.15f);
                    Vector3 center = ground+Vector3.up*y+new Vector3(Range(rng,-.9f,.9f),0,Range(rng,-.9f,.9f));
                    Blob(leaves,center,Range(rng,height*.21f,height*.31f),canopy);
                }
            }

            int groves = 2+rng.Next(0,3);
            for (int grove = 0; grove < groves; grove++)
            {
                float cx = baseX+Range(rng,10,size-10), cz = baseZ+Range(rng,10,size-10);
                int count = 2+rng.Next(0,4);
                for (int i = 0; i < count; i++)
                {
                    float angle = Range(rng,0,Mathf.PI*2), distance = Range(rng,2.5f,10f);
                    TryTree(cx+Mathf.Cos(angle)*distance,cz+Mathf.Sin(angle)*distance);
                }
            }
            int lone = Mathf.RoundToInt(6*settings.treeDensity);
            for (int i = 0; i < lone; i++) TryTree(baseX+Range(rng,4,size-4),baseZ+Range(rng,4,size-4));

            if (!trunks.Empty) MeshObject("Ermo tree trunks",root,trunks.ToMesh("Ermo tree trunks"),palette.wood,true);
            if (!leaves.Empty) MeshObject("Ermo tree leaves",root,leaves.ToMesh("Ermo tree leaves"),palette.leaf,false);
        }

        private static void Trunk(MeshBuilder builder, Vector3 bottom, Vector3 top, float bottomRadius, float topRadius, Color color)
        {
            const int sides = 6;
            Vector3 dir = (top-bottom).normalized;
            Vector3 up = Mathf.Abs(Vector3.Dot(dir,Vector3.up)) > .95f ? Vector3.right : Vector3.up;
            Vector3 right = Vector3.Cross(dir,up).normalized;
            Vector3 forward = Vector3.Cross(right,dir).normalized;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i*Mathf.PI*2/sides, a1 = (i+1)*Mathf.PI*2/sides;
                Vector3 d0 = right*Mathf.Cos(a0)+forward*Mathf.Sin(a0);
                Vector3 d1 = right*Mathf.Cos(a1)+forward*Mathf.Sin(a1);
                builder.Quad(bottom+d0*bottomRadius,top+d0*topRadius,top+d1*topRadius,bottom+d1*bottomRadius,color,
                    new Vector2(i/(float)sides,0),new Vector2(i/(float)sides,1),
                    new Vector2((i+1)/(float)sides,1),new Vector2((i+1)/(float)sides,0));
            }
        }

        private static void Blob(MeshBuilder builder, Vector3 center, float radius, Color color)
        {
            const int segments = 6, rings = 4;
            var grid = new Vector3[(rings+1)*(segments+1)];
            for (int ring = 0; ring <= rings; ring++)
            {
                float phi = Mathf.PI*ring/rings, y = Mathf.Cos(phi)*radius, r = Mathf.Sin(phi)*radius;
                for (int segment = 0; segment <= segments; segment++)
                {
                    float theta = Mathf.PI*2*segment/segments;
                    grid[ring*(segments+1)+segment] = center+new Vector3(Mathf.Cos(theta)*r,y,Mathf.Sin(theta)*r);
                }
            }
            for (int ring = 0; ring < rings; ring++)
                for (int segment = 0; segment < segments; segment++)
                {
                    int i0 = ring*(segments+1)+segment, i1 = i0+1;
                    int i2 = i0+segments+1, i3 = i2+1;
                    builder.Quad(grid[i0],grid[i1],grid[i3],grid[i2],color,
                        new Vector2(segment/(float)segments,1-ring/(float)rings),
                        new Vector2((segment+1)/(float)segments,1-ring/(float)rings),
                        new Vector2((segment+1)/(float)segments,1-(ring+1)/(float)rings),
                        new Vector2(segment/(float)segments,1-(ring+1)/(float)rings));
                }
        }

        private static void BuildProp(PropRecord record, Transform parent, WorldSampler sampler, WorldPalette p)
        {
            var root = new GameObject(record.id).transform;
            root.SetParent(parent, true); root.position = record.position;
            root.rotation = Quaternion.Euler(0, record.rotation, 0);
            root.localScale = Vector3.one * record.scale;
            switch (record.kind)
            {
                case PropKind.House: House(root, false, p); break;
                case PropKind.OpenHouse: House(root, true, p); break;
                case PropKind.WaterTower: Tower(root, p); break;
                case PropKind.Turbine: Turbine(root, p); break;
                case PropKind.Pole: Pole(root, record.position.z, sampler, p); break;
            }
        }

        private static void House(Transform root, bool open, WorldPalette p)
        {
            if (TryBuildHouseModel(root, p, open)) return;

            // Separate walls leave a real doorway, not a painted black rectangle.
            Part(root, "Foundation", PrimitiveType.Cube, new Vector3(0, -1.3f, 0), new Vector3(7.4f, 3.2f, 8.4f), p.plaster);
            Part(root, "Floor", PrimitiveType.Cube, new Vector3(0, .3f, 0), new Vector3(7, .2f, 8), p.wood);
            Part(root, "Back", PrimitiveType.Cube, new Vector3(0, 2.5f, 4), new Vector3(7, 4.5f, .25f), p.plaster);
            Part(root, "Left", PrimitiveType.Cube, new Vector3(-3.5f, 2.5f, 0), new Vector3(.25f, 4.5f, 8), p.plaster);
            Part(root, "Right", PrimitiveType.Cube, new Vector3(3.5f, 2.5f, 0), new Vector3(.25f, 4.5f, 8), p.plaster);
            Part(root, "Front left", PrimitiveType.Cube, new Vector3(-2.15f, 2.5f, -4), new Vector3(2.7f, 4.5f, .25f), p.plaster);
            Part(root, "Front right", PrimitiveType.Cube, new Vector3(2.15f, 2.5f, -4), new Vector3(2.7f, 4.5f, .25f), p.plaster);
            Part(root, "Lintel", PrimitiveType.Cube, new Vector3(0, 3.9f, -4), new Vector3(1.6f, 1.7f, .25f), p.plaster);
            var door = Part(root, "Door", PrimitiveType.Cube, new Vector3(0, 1.65f, -4.12f), new Vector3(1.5f, 2.6f, .12f), p.wood);
            if (open) { door.localPosition = new Vector3(-1.42f, 1.65f, -4.7f); door.localRotation = Quaternion.Euler(0, -80, 0); }
            foreach (float x in new[] { -2.15f, 2.15f })
            {
                Part(root, "Window trim", PrimitiveType.Cube, new Vector3(x, 2.6f, -4.17f), new Vector3(1.15f, 1.65f, .08f), p.white, false);
                Part(root, "Window", PrimitiveType.Cube, new Vector3(x, 2.6f, -4.23f), new Vector3(.88f, 1.36f, .05f), p.glass, false);
                Part(root, "Mullion", PrimitiveType.Cube, new Vector3(x, 2.6f, -4.27f), new Vector3(.06f, 1.38f, .04f), p.white, false);
            }
            // A triangular prism gives the iconic simple, pitched silhouette in the reference.
            var roofMesh = new Mesh { name = "Pitched roof" };
            roofMesh.vertices = new[]
            {
                new Vector3(-3.85f,4.75f,-4.4f),new Vector3(3.85f,4.75f,-4.4f),new Vector3(0,7,-4.4f),
                new Vector3(-3.85f,4.75f,4.4f),new Vector3(3.85f,4.75f,4.4f),new Vector3(0,7,4.4f)
            };
            roofMesh.triangles = new[] { 0,2,1, 3,4,5, 0,3,5, 0,5,2, 1,2,5, 1,5,4, 0,1,4, 0,4,3 };
            roofMesh.RecalculateNormals();
            var roofUV = new Vector2[roofMesh.vertexCount];
            for (int i = 0; i < roofUV.Length; i++) roofUV[i] = new Vector2(roofMesh.vertices[i].x,roofMesh.vertices[i].z)*.15f;
            roofMesh.uv = roofUV;
            MeshObject("Roof", root, roofMesh, p.roof, false);
            root.gameObject.AddComponent<HouseInteriorLighting>().Initialize(p.plaster, p.roof, true, open);
            Porch(root, p);
            Shutters(root, p);
        }

        private static void Porch(Transform root, WorldPalette p)
        {
            Part(root, "Porch slab", PrimitiveType.Cube, new Vector3(0, .43f, -5.15f), new Vector3(7, .26f, 2.3f), p.stone);
            Part(root, "Porch step", PrimitiveType.Cube, new Vector3(0, .18f, -6.45f), new Vector3(1.9f, .28f, .5f), p.stone);
            foreach (float x in new[] { -3.25f, 3.25f })
                Part(root, "Porch post", PrimitiveType.Cube, new Vector3(x, 2.1f, -6.15f), new Vector3(.18f, 3.4f, .18f), p.wood);
            Part(root, "Porch beam", PrimitiveType.Cube, new Vector3(0, 3.78f, -6.15f), new Vector3(7, .18f, .2f), p.wood);
            var roof = new MeshBuilder();
            roof.Quad(new Vector3(-3.7f, 4.2f, -4.2f), new Vector3(3.7f, 4.2f, -4.2f),
                new Vector3(3.7f, 3.72f, -6.6f), new Vector3(-3.7f, 3.72f, -6.6f), Color.white);
            MeshObject("Ermo porch roof", root, roof.ToMesh("Ermo porch roof"), p.roof, false);
            Chair(root, new Vector3(2.05f, .56f, -5.5f), 12f, p.wood);
        }

        private static void Shutters(Transform root, WorldPalette p)
        {
            foreach (float x in new[] { -2.15f, 2.15f })
                foreach (float side in new[] { -1f, 1f })
                    Part(root, "Shutter", PrimitiveType.Cube, new Vector3(x + side*.79f, 2.6f, -4.19f),
                        new Vector3(.42f, 1.66f, .06f), p.wood, false);
        }

        private static void Chair(Transform parent, Vector3 position, float yaw, Material material)
        {
            var root = new GameObject("Ermo chair").transform;
            root.SetParent(parent, false); root.localPosition = position; root.localRotation = Quaternion.Euler(0, yaw, 0);
            Part(root, "Seat", PrimitiveType.Cube, new Vector3(0, .45f, 0), new Vector3(.56f, .09f, .52f), material, false);
            Part(root, "Back", PrimitiveType.Cube, new Vector3(0, .78f, .23f), new Vector3(.56f, .62f, .08f), material, false);
            foreach (float x in new[] { -.23f, .23f })
                foreach (float z in new[] { -.21f, .21f })
                    Part(root, "Leg", PrimitiveType.Cube, new Vector3(x, .22f, z), new Vector3(.07f, .44f, .07f), material, false);
            foreach (float x in new[] { -.28f, .28f })
                Part(root, "Arm", PrimitiveType.Cube, new Vector3(x, .62f, 0), new Vector3(.06f, .06f, .48f), material, false);
        }

        private static bool TryBuildHouseModel(Transform root, WorldPalette p, bool open)
        {
            if (!houseModelLoaded)
            {
                houseModel = Resources.Load<GameObject>("Ermo/casa_alpende_1");
                houseModelLoaded = true;
            }
            if (houseModel == null) return false;

            var instance = Object.Instantiate(houseModel, root, false);
            instance.name = "Casa Alpende 1";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            // The authored model is about twice the dimensions of the procedural cottage.
            instance.transform.localScale = Vector3.one * .5f;
            float lowestPoint = float.PositiveInfinity;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                lowestPoint = Mathf.Min(lowestPoint, renderer.bounds.min.y);
            if (!float.IsPositiveInfinity(lowestPoint))
            {
                Vector3 position = instance.transform.position;
                position.y += root.position.y - .08f - lowestPoint;
                instance.transform.position = position;
            }
            foreach (Transform node in instance.GetComponentsInChildren<Transform>(true))
            {
                string nodeName = node.name.ToLowerInvariant();
                if (nodeName.StartsWith("house_door") || nodeName.StartsWith("house_window"))
                    node.gameObject.AddComponent<HouseInteractable>();
                if (nodeName.Contains("windowleaf_openp") || nodeName.Contains("windowleaf_openn"))
                    node.gameObject.AddComponent<HouseHinge>();
            }
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material material = null;
                for (Transform node = renderer.transform; node != null && node != instance.transform; node = node.parent)
                {
                    string nodeName = node.name.ToLowerInvariant();
                    if (nodeName.Contains("house_teto")) { material = p.roof; break; }
                    if (nodeName.Contains("house_walls")) { material = p.plaster; break; }
                    if (nodeName.Contains("house_floor")) { material = p.stone; break; }
                    if (nodeName.Contains("house_door")) { material = p.door; break; }
                    if (nodeName.Contains("house_window")) { material = p.glass; break; }
                }
                if (material != null) renderer.sharedMaterial = material;
            }
            var interior = root.gameObject.AddComponent<HouseInteriorLighting>();
            interior.Initialize(p.plaster, p.roof);
            foreach (var interactable in instance.GetComponentsInChildren<HouseInteractable>(true))
                interactable.Bind(root.name, open);
            interior.RefreshOpenings();
            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null) continue;
                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }
            // The authored house has a porch but no furniture; a single chair sits by the door.
            Transform doorNode = null;
            foreach (Transform node in instance.GetComponentsInChildren<Transform>(true))
                if (node.name.ToLowerInvariant().StartsWith("house_door")) { doorNode = node; break; }
            if (doorNode != null)
            {
                Vector3 seat = doorNode.position + root.right*1.35f - root.forward*.25f;
                Chair(root, root.InverseTransformPoint(seat) + Vector3.up*.02f, root.eulerAngles.y, p.wood);
            }
            return true;
        }

        private static void Tower(Transform root, WorldPalette p)
        {
            foreach (float x in new[] { -1.4f, 1.4f })
                foreach (float z in new[] { -1.4f, 1.4f })
                    Part(root, "Leg", PrimitiveType.Cube, new Vector3(x, 4.5f, z), new Vector3(.28f, 10, .28f), p.white);
            Part(root, "Platform", PrimitiveType.Cube, new Vector3(0, 8.4f, 0), new Vector3(3.8f, .22f, 3.8f), p.white);
            TowerRing(root,"Water tower shell",1.55f,1.75f,8.5f,11.95f,p.tank);
            TowerRing(root,"Water tower rim",1.55f,2.55f,11.85f,WaterTowerAccess.RimHeight,p.white);
            for (int side = -1; side <= 1; side += 2)
                Part(root,"Ladder rail",PrimitiveType.Cube,new Vector3(side*.48f,6.35f,-2.55f),new Vector3(.075f,12.7f,.075f),p.white,false);
            for (int i = 1; i <= 38; i++)
                Part(root,"Ladder rung",PrimitiveType.Cube,new Vector3(0,i*.32f,-2.55f),new Vector3(1,.065f,.075f),p.white,false);
            var surface = new Mesh { name = "Water tower surface" };
            var points = new List<Vector3> { Vector3.up*WaterTowerAccess.SurfaceHeight };
            var uv = new List<Vector2> { new Vector2(.5f,.5f) };
            var indices = new List<int>();
            for (int i = 0; i <= 48; i++)
            {
                float angle = i*Mathf.PI*2/48;
                points.Add(new Vector3(Mathf.Cos(angle)*1.55f,WaterTowerAccess.SurfaceHeight,Mathf.Sin(angle)*1.55f));
                uv.Add(new Vector2(.5f+Mathf.Cos(angle)*.5f,.5f+Mathf.Sin(angle)*.5f));
                if (i < 48) indices.AddRange(new[] { 0,i+2,i+1 });
            }
            surface.SetVertices(points); surface.SetUVs(0,uv); surface.SetTriangles(indices,0); surface.RecalculateNormals(); surface.RecalculateBounds();
            MeshObject("Reservoir water — no collision lid",root,surface,p.water,false);
            var lowerPoints = new List<Vector3>(points.Count);
            foreach (var point in points) lowerPoints.Add(new Vector3(point.x,WaterTowerAccess.BottomSurfaceHeight,point.z));
            var lowerIndices = new List<int>(indices.Count);
            for (int i = 0; i < indices.Count; i += 3)
                lowerIndices.AddRange(new[] { indices[i],indices[i+2],indices[i+1] });
            var lowerSurface = new Mesh { name = "Water tower inverted lower surface" };
            lowerSurface.SetVertices(lowerPoints); lowerSurface.SetUVs(0,uv); lowerSurface.SetTriangles(lowerIndices,0);
            lowerSurface.RecalculateNormals(); lowerSurface.RecalculateBounds();
            MeshObject("Water tower inverted lower water — no collision",root,lowerSurface,p.waterUnder,false);
            root.gameObject.AddComponent<WaterTowerAccess>();
        }

        private static void TowerRing(Transform root, string name, float inner, float outer, float bottom, float top, Material material)
        {
            var vertices = new List<Vector3>(); var indices = new List<int>();
            const int segments = 40;
            for (int i = 0; i < segments; i++)
            {
                float a = i*Mathf.PI*2/segments, b = (i+1)*Mathf.PI*2/segments;
                Vector3 ia = new Vector3(Mathf.Cos(a)*inner,bottom,Mathf.Sin(a)*inner);
                Vector3 ib = new Vector3(Mathf.Cos(b)*inner,bottom,Mathf.Sin(b)*inner);
                Vector3 oa = new Vector3(Mathf.Cos(a)*outer,bottom,Mathf.Sin(a)*outer);
                Vector3 ob = new Vector3(Mathf.Cos(b)*outer,bottom,Mathf.Sin(b)*outer);
                Vector3 up = Vector3.up*(top-bottom);
                AddQuad(vertices,indices,oa,oa+up,ob+up,ob);
                AddQuad(vertices,indices,ib,ib+up,ia+up,ia);
                AddQuad(vertices,indices,ia+up,ib+up,ob+up,oa+up);
                AddQuad(vertices,indices,oa,ob,ib,ia);
            }
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); MeshObject(name,root,mesh,material,true);
        }

        private static void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int index = vertices.Count; vertices.AddRange(new[] { a,b,c,d });
            triangles.AddRange(new[] { index,index+1,index+2,index,index+2,index+3 });
        }

        private static void Turbine(Transform root, WorldPalette p)
        {
            Part(root, "Mast", PrimitiveType.Cylinder, new Vector3(0, 12, 0), new Vector3(.85f, 12, .85f), p.white);
            Part(root, "Nacelle", PrimitiveType.Cube, new Vector3(0, 24, 0), new Vector3(1.4f, 1.3f, 3), p.white);
            var rotor = new GameObject("Slow rotor").transform;
            rotor.SetParent(root, false); rotor.localPosition = new Vector3(0, 24, -1.7f);
            for (int i = 0; i < 3; i++)
            {
                var pivot = new GameObject("Blade pivot").transform;
                pivot.SetParent(rotor, false); pivot.localRotation = Quaternion.Euler(0, 0, i * 120);
                var blade = Part(pivot, "Blade", PrimitiveType.Cube, new Vector3(.3f, 4.3f, 0), new Vector3(.55f, 8.5f, .13f), p.white, false);
                blade.localRotation = Quaternion.Euler(0, 0, -5);
            }
            rotor.gameObject.AddComponent<SlowRotor>();
        }

        private static void Pole(Transform root, float z, WorldSampler sampler, WorldPalette p)
        {
            Part(root, "Timber pole", PrimitiveType.Cylinder, new Vector3(0, 4.2f, 0), new Vector3(.22f, 4.3f, .22f), p.wood);
            Part(root, "Cross arm", PrimitiveType.Cube, new Vector3(0, 7.8f, 0), new Vector3(2.3f, .16f, .18f), p.wood);
            int lane = sampler.RoadLane(root.position.x - 6);
            Vector3 next = sampler.Point(sampler.RoadX(z + 32, lane) + 6, z + 32) + Vector3.up * 8;
            for (int side = -1; side <= 1; side += 2)
            {
                Part(root, "Insulator", PrimitiveType.Cylinder, new Vector3(side * .85f, 8, 0), new Vector3(.16f, .16f, .16f), p.white, false);
                var go = new GameObject("Sagging wire"); go.transform.SetParent(root, false);
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = p.wire; line.startWidth = .035f; line.endWidth = .035f;
                line.positionCount = 9; line.useWorldSpace = false;
                Vector3 start = new Vector3(side * .85f, 8, 0);
                Vector3 end = root.InverseTransformPoint(next + Vector3.right * side * .85f);
                for (int i = 0; i < 9; i++)
                {
                    float t = i / 8f;
                    line.SetPosition(i, Vector3.Lerp(start, end, t) + Vector3.down * (Mathf.Sin(t * Mathf.PI) * 1.4f));
                }
            }
        }

        private static Transform Part(Transform parent, string name, PrimitiveType type, Vector3 position,
            Vector3 scale, Material material, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision)
            {
                if (Application.isPlaying) Object.Destroy(go.GetComponent<Collider>());
                else Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go.transform;
        }
        private static void MeshObject(string name, Transform parent, Mesh mesh, Material material, bool collision)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collision) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
    }

}
