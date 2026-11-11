using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ermo
{
    // All meshes/textures are owned by the destination and released when its seed changes.
    public sealed class BalnearioGeometry
    {
        private readonly Transform root;
        private readonly BalnearioLayout layout;
        private readonly List<Object> owned;
        private readonly List<Object> materialResources;
        private readonly Dictionary<string,Material> sharedMaterials;
        private readonly Material paving, tile, coping, stone, grass, wood, thatch, white, metal, leaf, shrub, orange, blue, yellow, red, water;
        private readonly System.Random random;
        public BalnearioGeometry(Transform parent, BalnearioLayout plan, List<Object> resources,
            Dictionary<string,Material> shared = null, List<Object> paletteResources = null)
        {
            root = parent; layout = plan; owned = resources; random = new System.Random(plan.seed);
            sharedMaterials = shared ?? new Dictionary<string,Material>(); materialResources = paletteResources ?? resources;
            paving = Material("Warm non-slip paving",new Color(.85f,.78f,.62f),"paving");
            paving.SetFloat("_DepthBias",-3);
            tile = Material("Blue ceramic mosaic",new Color(.17f,.64f,.77f),"tile");
            coping = Material("Ivory rounded coping",new Color(.94f,.89f,.72f),"paving");
            stone = Material("Irregular stone retaining walls",new Color(.63f,.54f,.39f),"stone");
            grass = Material("Tropical planted beds",new Color(.32f,.44f,.13f),"grass");
            wood = Material("Weathered timber",new Color(.34f,.23f,.12f),"wood");
            thatch = Material("Palm thatch",new Color(.43f,.32f,.16f),"thatch");
            thatch.SetFloat("_Cull",0);
            white = Material("White plastic furniture",new Color(.92f,.94f,.87f));
            metal = Material("Galvanised slide supports",new Color(.53f,.64f,.64f));
            leaf = Material("Palm fronds",new Color(.29f,.43f,.10f)); leaf.SetFloat("_Cull",0);
            shrub = Material("Dense tropical foliage",new Color(.19f,.34f,.09f));
            orange = Material("Orange fibreglass",new Color(1,.36f,.055f));
            blue = Material("Turquoise fibreglass",new Color(.045f,.62f,.81f));
            yellow = Material("Sun yellow",new Color(.98f,.79f,.08f));
            red = Material("Faded coral",new Color(.85f,.16f,.085f));
            orange.SetFloat("_Cull",0); blue.SetFloat("_Cull",0); yellow.SetFloat("_Cull",0);
            if (!sharedMaterials.TryGetValue("Pool water",out water))
            {
                water = new Material(Resources.Load<Shader>("Ermo/Water/PoolWater")) { name = "Clear chlorinated pool water" };
                materialResources.Add(water); sharedMaterials.Add("Pool water",water);
            }
        }
        private float Range(float a, float b) => Mathf.Lerp(a,b,(float)random.NextDouble());
        private Material Material(string name, Color color, string texture = null)
        {
            if (sharedMaterials.TryGetValue(name,out var existing)) return existing;
            var material = new Material(Resources.Load<Shader>("Ermo/BalnearioSurface")) { name = name };
            material.SetColor("_BaseColor",color); materialResources.Add(material); sharedMaterials.Add(name,material);
            if (texture != null) material.SetTexture("_MainTex",Texture(texture));
            return material;
        }
        private Texture2D Texture(string kind)
        {
            const int size = 128;
            var texture = new Texture2D(size,size,TextureFormat.RGB24,false) { name = "Balneario / "+kind,
                wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var pixels = new Color[size*size];
            for (int z = 0; z < size; z++) for (int x = 0; x < size; x++)
            {
                float noise = Mathf.PerlinNoise(x*.2f,z*.2f), value = .85f+noise*.22f;
                if (kind == "tile") value = x%16 < 1 || z%16 < 1 ? .55f : .85f+.15f*noise;
                if (kind == "paving") value = x%64 < 1 || (z+(x/64)*32)%64 < 1 ? .68f : .9f+.1f*noise;
                if (kind == "wood" || kind == "thatch") value = .7f+.3f*Mathf.PerlinNoise(x*.5f,z*.035f);
                if (kind == "stone")
                {
                    // Periodic jittered Voronoi with mortar; no photograph pasted over the geometry.
                    Vector2 p = new Vector2(x,z)/size*8;
                    int cx = Mathf.FloorToInt(p.x), cz = Mathf.FloorToInt(p.y);
                    float first = 100, second = 100, tint = 0;
                    for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
                    {
                        int sx = cx+dx, sz = cz+dz;
                        float h = Mathf.Repeat(Mathf.Sin((sx+8)%8*127.1f+(sz+8)%8*311.7f)*43758.5f,1);
                        float h2 = Mathf.Repeat(h*17.37f,1);
                        float distance = Vector2.Distance(p,new Vector2(sx+.2f+h*.6f,sz+.2f+h2*.6f));
                        if (distance < first) { second = first; first = distance; tint = h; }
                        else second = Mathf.Min(second,distance);
                    }
                    value = second-first < .065f ? .52f : .68f+tint*.4f+noise*.12f;
                }
                pixels[z*size+x] = new Color(value,value,value);
            }
            texture.SetPixels(pixels); texture.Apply(false,true); materialResources.Add(texture); return texture;
        }
        private Transform Group(string name, Vector3 position, float yaw = 0)
        {
            var go = new GameObject(name); go.transform.SetParent(root,false);
            go.transform.localPosition = position; go.transform.localRotation = Quaternion.Euler(0,yaw,0);
            return go.transform;
        }
        private GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material mat, bool solid = true)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent,false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!solid) { var c = go.GetComponent<Collider>(); c.enabled = false; Object.Destroy(c); }
            return go;
        }
        private GameObject Box(string name, Transform parent, Vector3 p, Vector3 size, Material mat, bool solid = true)
            => Primitive(name,PrimitiveType.Cube,parent,p,size,mat,solid);
        private void Pole(string name, Transform parent, Vector3 a, Vector3 b, float radius, Material mat, bool solid = false)
        {
            var go = Primitive(name,PrimitiveType.Cylinder,parent,(a+b)*.5f,new Vector3(radius*2,(b-a).magnitude*.5f,radius*2),mat,solid);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up,b-a);
        }
        private GameObject MeshObject(string name, Transform parent, List<Vector3> vertices, List<int> triangles, Material mat, bool solid = false)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0);
            var uv = new List<Vector2>(); var colors = new List<Color>();
            foreach (Vector3 v in vertices) { uv.Add(new Vector2(v.x+v.y*.35f,v.z+v.y*.6f)); colors.Add(Color.white); }
            mesh.SetUVs(0,uv); mesh.SetColors(colors); mesh.RecalculateNormals(); mesh.RecalculateBounds(); owned.Add(mesh);
            var go = new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<Renderer>().sharedMaterial = mat;
            if (solid) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }
        private static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); t.AddRange(new[] { i,i+1,i+2,i,i+2,i+3 });
        }
        private static Vector3 At(Vector2 p, float y) => new Vector3(p.x,y,p.y);
        public void Build()
        {
            foreach (int step in BuildSteps()) { }
        }
        public IEnumerable<int> BuildSteps()
        {
            layout.Validate();
            BuildDeck(-BalnearioLayout.HalfSector,BalnearioLayout.HalfSector,BalnearioLayout.LowerDeck);
            yield return 0;
            BuildPaths();
            foreach (var pool in layout.pools) { BuildPool(pool); BuildPoolDeck(pool); yield return 0; }
            if (layout.slidePool >= 0) { BuildSlides(); yield return 0; }
            if (layout.raisedGarden) BuildRaisedGarden();
            foreach (Vector2 kiosk in layout.kiosks) BuildKiosk(kiosk,layout.IsArrival ? "Guichê / informações" : "Lanchonete",layout.theme == 1 ? yellow : blue);
            for (int i = 0; i < layout.sites.Count; i++)
            {
                var site = layout.sites[i];
                var group = Group(site.kind < 2 ? "Poolside furniture" : "Planted island",At(site.position,BalnearioLayout.DeckAt(site.position)),site.yaw);
                if (site.kind == 0) Table(group);
                else if (site.kind == 1) { Lounger(group,new Vector3(-.8f,0,0)); Lounger(group,new Vector3(.8f,0,0)); }
                else if (site.kind == 2) Palm(group,site.scale);
                else Bush(group,site.scale);
                if (i%12 == 11) yield return 0;
            }
            if (layout.playground && layout.pools.Count > 0)
            {
                Mushroom(layout.pools[0],new Vector2(-4,0),1.6f);
                Mushroom(layout.pools[0],new Vector2(4,2),1.15f);
            }
            BuildWaterPlay(); yield return 0;
            BatchRenderers();
        }
        private void BuildDeck(float minZ, float maxZ, float y)
        {
            // Subtract each star-shaped pool as a fan of convex triangles. This leaves real holes,
            // including concave kidney outlines, instead of hiding a solid plane under water.
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            var cutouts = new List<BalnearioLayout.Pool>();
            foreach (var pool in layout.pools)
            {
                var cutout = new BalnearioLayout.Pool { center = pool.center, size = pool.size+Vector2.one*7, deck = pool.deck };
                for (int i = 0; i < pool.outline.Count; i++)
                {
                    Vector2 p = pool.outline[i]; cutout.outline.Add(p+(p-pool.center).normalized*PavingWidth(i));
                }
                cutouts.Add(cutout);
            }
            for (float z = minZ; z < maxZ; z += 4) for (float x = -BalnearioLayout.HalfSector; x < BalnearioLayout.HalfSector; x += 4)
            {
                float endZ = Mathf.Min(z+4,maxZ);
                var pieces = new List<List<Vector2>> { new List<Vector2> { new Vector2(x,z),new Vector2(x+4,z),new Vector2(x+4,endZ),new Vector2(x,endZ) } };
                foreach (var pool in cutouts)
                {
                    if (Mathf.Abs(pool.deck-y) > .1f || Mathf.Abs(x+2-pool.center.x) > pool.size.x*1.4f+5 ||
                        Mathf.Abs(z+2-pool.center.y) > pool.size.y*1.4f+5) continue;
                    Vector2 cellCenter = new Vector2(x+2,(z+endZ)*.5f);
                    float cellRadius = new Vector2(2,(endZ-z)*.5f).magnitude;
                    if (pool.EdgeDistance(cellCenter) > cellRadius+.001f)
                    {
                        if (pool.Contains(cellCenter)) { pieces.Clear(); break; }
                        continue;
                    }
                    for (int edge = 0; edge < pool.outline.Count && pieces.Count > 0; edge++)
                    {
                        var cut = new[] { pool.center,pool.outline[edge],pool.outline[(edge+1)%pool.outline.Count] };
                        var next = new List<List<Vector2>>();
                        foreach (var piece in pieces) SubtractTriangle(piece,cut,next);
                        pieces = next;
                    }
                }
                foreach (var piece in pieces)
                {
                    if (piece.Count < 3) continue;
                    int start = vertices.Count;
                    foreach (Vector2 p in piece) vertices.Add(At(p,y));
                    for (int i = 1; i < piece.Count-1; i++)
                        if (Mathf.Abs(Cross(piece[i]-piece[0],piece[i+1]-piece[0])) > .00001f)
                            triangles.AddRange(new[] { start,start+i+1,start+i });
                }
            }
            MeshObject("Continuous garden ground / real pool cutouts",root,vertices,triangles,grass,true);
        }
        private void BuildPoolDeck(BalnearioLayout.Pool pool)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < pool.outline.Count; i++)
            {
                int j = (i+1)%pool.outline.Count;
                Vector2 a = pool.outline[i], b = pool.outline[j];
                Vector2 da = (a-pool.center).normalized, db = (b-pool.center).normalized;
                float wa = PavingWidth(i), wb = PavingWidth(j);
                Quad(v,t,At(a,pool.deck),At(b,pool.deck),At(b+db*wb,pool.deck),At(a+da*wa,pool.deck));
            }
            MeshObject("Organic sandstone pool surround",root,v,t,paving,true);
        }
        private float PavingWidth(int i) => 6+.7f*Mathf.Sin(i*.19f+layout.seed%11);
        private void BuildPaths()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int pathIndex = 0; pathIndex < layout.paths.Count; pathIndex++)
            {
                var path = layout.paths[pathIndex];
                for (int i = pathIndex < 2 ? 1 : 2; i < path.Count; i++)
                {
                    Vector2 a = path[i-1], b = path[i];
                    // Cardinal offsets at shared edges give matching cross-sections between sectors.
                    Vector2 right = pathIndex == 0 ? Vector2.up*2 : pathIndex == 1 ? Vector2.right*2 :
                        new Vector2((b-a).normalized.y,-(b-a).normalized.x)*2;
                    Quad(v,t,At(a-right,LowerPath),At(b-right,LowerPath),At(b+right,LowerPath),At(a+right,LowerPath));
                }
            }
            // Some strips wind clockwise and others counterclockwise; paving is two-sided.
            paving.SetFloat("_Cull",0);
            MeshObject("Winding connected promenades",root,v,t,paving);
        }
        private const float LowerPath = BalnearioLayout.LowerDeck+.012f;
        private static float Cross(Vector2 a, Vector2 b) => a.x*b.y-a.y*b.x;
        private static List<Vector2> Clip(List<Vector2> polygon, Vector2 a, Vector2 b, bool inside)
        {
            var result = new List<Vector2>();
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 p = polygon[i], q = polygon[(i+1)%polygon.Count];
                float dp = Cross(b-a,p-a), dq = Cross(b-a,q-a);
                bool pIn = inside ? dp >= 0 : dp <= 0, qIn = inside ? dq >= 0 : dq <= 0;
                if (pIn) AddDistinct(result,p);
                if (pIn != qIn) AddDistinct(result,Vector2.Lerp(p,q,dp/(dp-dq)));
            }
            if (result.Count > 1 && (result[0]-result[result.Count-1]).sqrMagnitude < .00000001f) result.RemoveAt(result.Count-1);
            float area = 0;
            for (int i = 1; i < result.Count-1; i++) area += Cross(result[i]-result[0],result[i+1]-result[0]);
            // Reject zero-area boundary fragments immediately. Retaining them through successive
            // fan cuts can multiply collinear polygons and exhaust memory during preload.
            if (Mathf.Abs(area) < .00001f) result.Clear();
            return result;
        }
        private static void AddDistinct(List<Vector2> points, Vector2 p)
        {
            if (points.Count == 0 || (points[points.Count-1]-p).sqrMagnitude > .00000001f) points.Add(p);
        }
        private static void SubtractTriangle(List<Vector2> polygon, Vector2[] cut, List<List<Vector2>> output)
        {
            var remaining = polygon;
            for (int i = 0; i < 3 && remaining.Count >= 3; i++)
            {
                var outside = Clip(remaining,cut[i],cut[(i+1)%3],false);
                if (outside.Count >= 3) output.Add(outside);
                remaining = Clip(remaining,cut[i],cut[(i+1)%3],true);
            }
        }
        private void BuildPool(BalnearioLayout.Pool pool)
        {
            var floorV = new List<Vector3> { At(pool.center,pool.Floor) }; var floorT = new List<int>();
            foreach (Vector2 p in pool.outline) floorV.Add(At(p,pool.Floor));
            for (int i = 0; i < pool.outline.Count; i++) floorT.AddRange(new[] { 0,1+(i+1)%pool.outline.Count,1+i });
            MeshObject("Mosaic basin / walkable shallow floor",root,floorV,floorT,tile,true);
            var waterV = new List<Vector3>();
            foreach (Vector3 p in floorV) waterV.Add(new Vector3(p.x,pool.Surface,p.z));
            MeshObject("Bounded swimming pool water",root,waterV,floorT,water);
            var wallV = new List<Vector3>(); var wallT = new List<int>();
            var rimV = new List<Vector3>(); var rimT = new List<int>();
            for (int i = 0; i < pool.outline.Count; i++)
            {
                Vector2 a = pool.outline[i], b = pool.outline[(i+1)%pool.outline.Count];
                Vector2 oa = a+(a-pool.center).normalized*.36f, ob = b+(b-pool.center).normalized*.36f;
                Quad(wallV,wallT,At(a,pool.Floor),At(b,pool.Floor),At(b,pool.deck),At(a,pool.deck));
                Quad(rimV,rimT,At(a,pool.deck+.025f),At(b,pool.deck+.025f),At(ob,pool.deck+.025f),At(oa,pool.deck+.025f));
            }
            MeshObject("Blue mosaic pool walls",root,wallV,wallT,tile,true);
            MeshObject("Continuous ivory coping",root,rimV,rimT,coping);
            // Keep every riser walkable, including the deeper flume landing basins.
            Vector2 edge = pool.outline[16], inward = (pool.center-edge).normalized;
            var steps = Group("Pool exit / submerged steps",At(edge,pool.Floor),Mathf.Atan2(inward.x,inward.y)*Mathf.Rad2Deg);
            int stepCount = Mathf.CeilToInt(pool.depth/.20f);
            float riser = pool.depth/stepCount, stairLength = stepCount*.42f;
            for (int i = 0; i < stepCount; i++)
                Box("Non-slip pool step",steps,new Vector3(0,(stepCount-i)*riser*.5f,.22f+i*.42f),new Vector3(2.2f,(stepCount-i)*riser,.46f),coping);
            foreach (float x in new[] { -.96f,.96f })
            {
                Pole("Pool handrail",steps,new Vector3(x,pool.depth+.8f,.1f),new Vector3(x,.85f,stairLength),.035f,metal);
                Pole("Handrail post",steps,new Vector3(x,0,stairLength),new Vector3(x,.85f,stairLength),.035f,metal);
            }
        }
        private void Fence(Vector3 a, Vector3 b)
        {
            int count = Mathf.CeilToInt(Vector3.Distance(a,b)/2.8f);
            for (int i = 0; i <= count; i++)
            {
                Vector3 p = Vector3.Lerp(a,b,i/(float)count);
                Pole("Timber fence post",root,p,p+Vector3.up*1.12f,.065f,wood);
            }
            Pole("Upper rail",root,a+Vector3.up,b+Vector3.up,.055f,wood);
            Pole("Lower rail",root,a+Vector3.up*.45f,b+Vector3.up*.45f,.045f,wood);
            // Continuous collision prevents falling out between decorative posts.
            Vector3 direction = b-a;
            var barrier = new GameObject("Fence collision",typeof(BoxCollider)); barrier.transform.SetParent(root,false);
            barrier.transform.localPosition = (a+b)*.5f+Vector3.up*.6f;
            barrier.transform.localRotation = Quaternion.LookRotation(direction.normalized,Vector3.up);
            barrier.GetComponent<BoxCollider>().size = new Vector3(.16f,1.2f,direction.magnitude);
        }
        private void BuildRaisedGarden()
        {
            Box("Local stone terrace",root,new Vector3(-28,1.81f,-60),new Vector3(24,3.18f,16),stone);
            Box("Terrace sandstone paving",root,new Vector3(-28,3.41f,-60),new Vector3(24,.02f,16),paving);
            var v = new List<Vector3>(); var t = new List<int>();
            Quad(v,t,new Vector3(-16,3.42f,-62),new Vector3(-16,3.42f,-58),new Vector3(8,.22f,-58),new Vector3(8,.22f,-62));
            MeshObject("Local garden access ramp",root,v,t,paving,true);
            Fence(new Vector3(-16,3.42f,-62),new Vector3(8,.22f,-62));
            Fence(new Vector3(-16,3.42f,-58),new Vector3(8,.22f,-58));
            Fence(new Vector3(-40,3.42f,-68),new Vector3(-16,3.42f,-68));
            Fence(new Vector3(-40,3.42f,-52),new Vector3(-16,3.42f,-52));
            Fence(new Vector3(-40,3.42f,-68),new Vector3(-40,3.42f,-52));
        }
        private void Table(Transform group)
        {
            Primitive("Round white table",PrimitiveType.Cylinder,group,new Vector3(0,.76f,0),new Vector3(1.05f,.055f,1.05f),white);
            Pole("Table pedestal",group,Vector3.zero,Vector3.up*.75f,.09f,white);
            Umbrella(group,2.45f,1.8f,random.Next(3) == 0 ? thatch : (random.Next(2) == 0 ? blue : yellow));
            for (int i = 0; i < 4; i++)
            {
                float a = i*Mathf.PI*.5f;
                var seat = new GameObject("White plastic chair").transform; seat.SetParent(group,false);
                seat.localPosition = new Vector3(Mathf.Sin(a)*1.05f,0,Mathf.Cos(a)*1.05f);
                seat.localRotation = Quaternion.Euler(0,i*90+180,0);
                Box("Seat",seat,new Vector3(0,.45f,0),new Vector3(.47f,.07f,.46f),white);
                for (int slat = -1; slat <= 1; slat++)
                    Box("Open plastic back slat",seat,new Vector3(slat*.16f,.74f,-.2f),new Vector3(.055f,.54f,.055f),white,false);
                Box("Chair back rim",seat,new Vector3(0,1,-.2f),new Vector3(.47f,.055f,.06f),white,false);
                foreach (float x in new[] { -.19f,.19f }) foreach (float z in new[] { -.17f,.17f })
                    Pole("Chair leg",seat,new Vector3(x,0,z),new Vector3(x,.44f,z),.025f,white);
            }
            // Separate hash: radios do not perturb the existing furniture/layout RNG.
            int hash = unchecked(layout.seed*397 ^ Mathf.RoundToInt(group.localPosition.x*31) ^ Mathf.RoundToInt(group.localPosition.z*131));
            if ((uint)hash%100 < 18)
            {
                var radio = new GameObject("Small tabletop radio").transform; radio.SetParent(group,false);
                radio.localPosition = new Vector3(.24f,.815f,.08f); radio.localRotation = Quaternion.Euler(0,hash%360,0);
                Box("Radio casing",radio,new Vector3(0,.105f,0),new Vector3(.28f,.21f,.12f),wood,false);
                Box("Speaker grille",radio,new Vector3(-.045f,.105f,-.064f),new Vector3(.15f,.15f,.012f),metal,false);
                Primitive("Tuning knob",PrimitiveType.Sphere,radio,new Vector3(.095f,.14f,-.07f),Vector3.one*.035f,white,false);
                Pole("Radio aerial",radio,new Vector3(.10f,.21f,0),new Vector3(.16f,.51f,0),.004f,metal);
                radio.gameObject.AddComponent<BalnearioRadio>().Initialize(((uint)hash%1000)/1000f);
            }
        }
        private void Lounger(Transform group, Vector3 p)
        {
            Box("Sun lounger seat",group,p+new Vector3(0,.35f,.25f),new Vector3(.64f,.09f,1.25f),white);
            var back = Box("Reclining back",group,p+new Vector3(0,.59f,-.65f),new Vector3(.64f,.09f,.85f),white);
            back.transform.localRotation = Quaternion.Euler(32,0,0);
            foreach (float x in new[] { -.26f,.26f }) foreach (float z in new[] { -.6f,.7f })
                Pole("Lounger frame",group,p+new Vector3(x,0,z),p+new Vector3(x,.35f,z),.035f,white);
        }
        private void Umbrella(Transform parent, float height, float radius, Material mat)
        {
            Pole("Umbrella pole",parent,Vector3.zero,Vector3.up*(height+.35f),.045f,wood);
            Canopy(parent,height,radius,mat);
        }
        private void Canopy(Transform parent, float height, float radius, Material mat)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            float rise = mat == thatch ? radius*.45f : .65f;
            for (int i = 0; i < 24; i++)
            {
                float a = i*Mathf.PI*2/24, b = (i+1)*Mathf.PI*2/24;
                Quad(v,t,new Vector3(0,height+rise,0),new Vector3(Mathf.Sin(a)*radius,height,Mathf.Cos(a)*radius),
                    new Vector3(Mathf.Sin(b)*radius,height,Mathf.Cos(b)*radius),new Vector3(0,height+rise,0));
            }
            if (mat == thatch) for (int i = 0; i < 96; i++)
            {
                float a = i*Mathf.PI*2/96, b = (i+.7f)*Mathf.PI*2/96;
                Vector3 p = new Vector3(Mathf.Sin(a)*radius,height,Mathf.Cos(a)*radius);
                Vector3 q = new Vector3(Mathf.Sin(b)*radius,height,Mathf.Cos(b)*radius);
                Quad(v,t,p,p-Vector3.up*Range(.12f,.28f),q-Vector3.up*.16f,q);
            }
            MeshObject("Conical shade canopy",parent,v,t,mat);
        }
        private void Palm(Transform parent, float scale)
        {
            float h = Range(4.8f,7.4f)*scale;
            Pole("Leaning palm trunk",parent,Vector3.zero,new Vector3(.3f,h,0),.15f*scale,wood,true);
            var v = new List<Vector3>(); var t = new List<int>();
            for (int frond = 0; frond < 11; frond++)
            {
                float a = frond*Mathf.PI*2/11+Range(-.1f,.1f), length = Range(2.3f,3.5f)*scale;
                Vector3 direction = new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                Vector3 right = new Vector3(direction.z,0,-direction.x);
                for (int segment = 0; segment < 7; segment++)
                {
                    float u = segment/7f, w = (segment+1)/7f;
                    Vector3 p = new Vector3(.3f,h,0)+direction*(length*u)+Vector3.up*(Mathf.Sin(u*Mathf.PI)*.65f-u*u*.9f);
                    Vector3 q = new Vector3(.3f,h,0)+direction*(length*w)+Vector3.up*(Mathf.Sin(w*Mathf.PI)*.65f-w*w*.9f);
                    // A thin rachis with paired leaflets, not eleven solid banana-leaf ribbons.
                    Quad(v,t,p-right*.025f,q-right*.02f,q+right*.02f,p+right*.025f);
                    float width = Mathf.Sin((u*.8f+.1f)*Mathf.PI)*.48f*scale*(1-u*.65f);
                    foreach (float side in new[] { -1f,1f })
                    {
                        Vector3 tip = p+right*(width*side)+direction*(length*.12f)-Vector3.up*.12f;
                        Quad(v,t,p,q,tip+direction*.09f,tip);
                    }
                }
            }
            MeshObject("Drooping palm fronds",parent,v,t,leaf);
        }
        private void Bush(Transform parent, float scale)
        {
            for (int i = 0; i < 3; i++)
                Primitive("Tropical shrub",PrimitiveType.Sphere,parent,new Vector3(Range(-.35f,.35f),.4f,Range(-.35f,.35f)),new Vector3(1.1f,.95f,1.1f)*scale,shrub,false);
        }
        private void BuildKiosk(Vector2 p, string label, Material paint)
        {
            var group = Group(label,At(p,layout.raisedGarden ? BalnearioLayout.UpperDeck : BalnearioLayout.LowerDeck));
            if (!layout.raisedGarden) Box("Service patio",group,new Vector3(0,.025f,0),new Vector3(11,.05f,10),paving);
            Box("Painted kiosk base",group,new Vector3(0,.5f,0),new Vector3(8,1,6),paint);
            Box("Back wall",group,new Vector3(0,1.8f,2.8f),new Vector3(8,2.6f,.25f),paint);
            foreach (float x in new[] { -3.8f,3.8f })
            {
                Box("Kiosk side",group,new Vector3(x,1.8f,0),new Vector3(.22f,2.6f,6),paint);
                Pole("Canopy support",group,new Vector3(x,0,-2.6f),new Vector3(x,3.4f,-2.6f),.10f,wood,true);
            }
            Box("Service counter",group,new Vector3(0,1.08f,-2.9f),new Vector3(7.8f,.16f,.9f),coping);
            Canopy(group,3.4f,6,thatch);
        }
        private void Mushroom(BalnearioLayout.Pool pool, Vector2 offset, float radius)
        {
            var group = Group("Mushroom splash fountain",At(pool.center+offset,pool.Floor));
            Pole("Fountain column",group,Vector3.zero,Vector3.up*2,.13f,yellow,true);
            var v = new List<Vector3>(); var t = new List<int>();
            for (int ring = 0; ring <= 6; ring++) for (int i = 0; i <= 32; i++)
            {
                float r = ring/6f, a = i*Mathf.PI*2/32;
                v.Add(new Vector3(Mathf.Sin(a)*radius*r,1.85f+.45f*Mathf.Sqrt(1-r*r),Mathf.Cos(a)*radius*r));
                if (ring == 0 || i == 0) continue;
                int index = ring*33+i;
                t.AddRange(new[] { index,index-1,index-34,index,index-34,index-33 });
            }
            MeshObject("Rounded blue mushroom cap",group,v,t,blue);
            // Thin separated streams leave the scenery visible through the water curtain.
            for (int i = 0; i < 32; i++)
            {
                float a = i*Mathf.PI*2/32;
                Vector3 top = new Vector3(Mathf.Sin(a)*radius,1.85f,Mathf.Cos(a)*radius);
                Pole("Falling water stream",group,top,new Vector3(top.x*1.12f,.55f,top.z*1.12f),.013f,water);
            }
        }
        private void BuildSlides()
        {
            var runout = layout.pools[layout.slidePool];
            var tower = Group("Slide tower / accessible switchback stairs",At(layout.tower,BalnearioLayout.LowerDeck));
            foreach (float x in new[] { -4.65f,4.65f }) foreach (float z in new[] { -4.65f,4.65f })
                Pole("Tower steel column",tower,new Vector3(x,0,z),new Vector3(x,10,z),.14f,metal,true);
            Box("Slide launch platform",tower,new Vector3(0,9.9f,0),new Vector3(8,.2f,8),metal);
            // Two stair flights, 20 cm rise / 30 cm tread. Landings meet both flights exactly.
            for (int flight = 0; flight < 2; flight++)
            {
                float x = flight == 0 ? 5.2f : 7.6f;
                for (int i = 0; i < 25; i++)
                {
                    float z = flight == 0 ? -3.75f+i*.3f : 3.75f-i*.3f;
                    float y = flight*5+(i+1)*.2f;
                    Box("Tower stair tread",tower,new Vector3(x,y-.1f,z),new Vector3(2.3f,.2f,.32f),metal);
                }
                Vector3 a = new Vector3(x+(flight == 0 ? -1.1f : 1.1f),flight*5+1,flight == 0 ? -3.9f : 3.9f);
                Vector3 b = new Vector3(a.x,flight*5+6,-a.z);
                Pole("Stair handrail",tower,a,b,.045f,metal);
            }
            Box("Middle stair landing",tower,new Vector3(6.4f,4.9f,4.15f),new Vector3(4.7f,.2f,1.1f),metal);
            Box("Upper stair landing",tower,new Vector3(3.3f,9.9f,-4.05f),new Vector3(10.8f,.2f,1.2f),metal);
            Fence(At(layout.tower+new Vector2(-4,4),10.22f),At(layout.tower+new Vector2(4,4),10.22f));
            // Closed orange spiral plus an open turquoise flume, both ending inside a real pool.
            var lanes = new List<List<Vector3>>();
            for (int lane = 0; lane < 2; lane++)
            {
                var path = new List<Vector3>();
                for (int i = 0; i <= 96; i++)
                {
                    float u = i/96f;
                    Vector3 p;
                    if (lane == 0 && u < .7f)
                    {
                        float a = u/.7f*Mathf.PI*2;
                        p = new Vector3(layout.tower.x-6+6*Mathf.Cos(a),11.45f-u*10,layout.tower.y-4-6*Mathf.Sin(a));
                    }
                    else if (lane == 0)
                    {
                        float s = (u-.7f)/.3f;
                        p = Vector3.Lerp(At(layout.tower+new Vector2(0,-4),4.45f),At(runout.center+new Vector2(6,2),1.3f),s);
                    }
                    else
                    {
                        p = Vector3.Lerp(At(layout.tower+new Vector2(3,-4),11.45f),At(runout.center+new Vector2(1.5f,3),1.3f),u);
                        p.x += 2*Mathf.Sin(u*Mathf.PI*2);
                    }
                    path.Add(p);
                }
                Flume(path,lane == 0 ? orange : blue,lane == 0);
                lanes.Add(path);
                root.gameObject.AddComponent<WaterSlideRide>().Initialize(path,runout.Surface,runout.Floor);
                BuildSlideWater(path,runout.Surface);
                // Supports for a separate overhead service pipe intrude at the sides of
                // the middle descent, while leaving the rider's centreline unobstructed.
                int middle = path.Count/2;
                Vector3 mid = path[middle];
                Vector3 lateral = Vector3.Cross(Vector3.up,path[middle+1]-path[middle-1]).normalized;
                float baseY = BalnearioLayout.DeckAt(new Vector2(mid.x,mid.z));
                foreach (float side in new[] { -1f,1f })
                {
                    Vector3 column = mid+lateral*(side*.94f);
                    Pole("Overhead utility structure / mid-descent column",root,new Vector3(column.x,baseY,column.z),column+Vector3.up*2.1f,.075f,metal,true);
                }
                Pole("Utility pipe carrier above slide",root,mid-lateral*1.2f+Vector3.up*2.1f,mid+lateral*1.2f+Vector3.up*2.1f,.09f,metal);
                Pole("Separate overhead water supply pipe",root,mid-lateral*3+Vector3.up*2.3f,mid+lateral*3+Vector3.up*2.3f,.12f,blue);
            }
            foreach (var path in lanes)
            {
                for (int i = 8; i < path.Count-8; i += 12)
                {
                    Vector3 p = path[i]; float bottom = BalnearioLayout.DeckAt(new Vector2(p.x,p.z));
                    bool inPool = false;
                    foreach (var pool in layout.pools) if (pool.Contains(new Vector2(p.x,p.z))) inPool = true;
                    if (!inPool)
                    {
                        Vector3 right = Vector3.Cross(Vector3.up,path[i+1]-path[i-1]).normalized;
                        foreach (float side in new[] { -1f,1f })
                        {
                            Vector3 top = p+right*side*1.65f-Vector3.up*1.45f;
                            bool clear = top.y > bottom+.3f;
                            foreach (var other in lanes) foreach (Vector3 q in other)
                                if (new Vector2(q.x-top.x,q.z-top.z).sqrMagnitude < 2.5f && q.y-1.4f < top.y && q.y+1.4f > bottom) clear = false;
                            if (!clear) continue;
                            Pole("Outboard flume column",root,new Vector3(top.x,bottom,top.z),top,.12f,metal,true);
                            Pole("Under-flume bracket",root,top,p-Vector3.up*1.45f,.08f,metal);
                            Box("Support footing",root,new Vector3(top.x,bottom+.08f,top.z),new Vector3(.9f,.16f,.9f),coping);
                        }
                    }
                }
            }
        }
        private void BuildWaterPlay()
        {
            if (layout.playground && layout.pools.Count > 0) BuildPlayground(layout.pools[0]);
            if (layout.waterfall && layout.pools.Count > 0) BuildWaterfall(layout.pools[0]);
        }
        private void BuildPlayground(BalnearioLayout.Pool kids)
        {
            var play = Group("Children's aqua playground",At(kids.center+new Vector2(2,0),kids.Floor));
            Box("Raised splash platform",play,new Vector3(0,1.9f,0),new Vector3(3,.18f,3),blue);
            foreach (float x in new[] { -1.3f,1.3f }) foreach (float z in new[] { -1.3f,1.3f })
                Pole("Play tower post",play,new Vector3(x,0,z),new Vector3(x,3.9f,z),.065f,red,true);
            Canopy(play,3.8f,2.2f,yellow);
            for (int i = 0; i < 10; i++)
                Box("Children's access steps",play,new Vector3(0,(i+1)*.19f-.095f,4.05f-i*.27f),new Vector3(1.3f,.19f,.29f),blue);
            for (int i = 0; i < 9; i++)
            {
                float x = -1.4f+i*.35f;
                Pole("Play balustrade",play,new Vector3(x,1.9f,1.4f),new Vector3(x,2.8f,1.4f),.025f,yellow);
            }
            Pole("Play guard rail",play,new Vector3(-1.4f,2.8f,1.4f),new Vector3(1.4f,2.8f,1.4f),.045f,yellow);
            var path = new List<Vector3>();
            for (int i = 0; i <= 24; i++)
            {
                float u = i/24f;
                path.Add(At(kids.center+new Vector2(2,-1.5f-u*4.2f),kids.Floor+3.05f-u*1.45f));
            }
            Flume(path,yellow,false);
            root.gameObject.AddComponent<WaterSlideRide>().Initialize(path,kids.Surface,kids.Floor);
            BuildSlideWater(path,kids.Surface);
            // Rainbow spray arches in the circular splash pool.
            var splash = kids;
            for (int arch = 0; arch < 4; arch++)
            {
                Vector3 origin = At(splash.center+new Vector2(-3+arch*1.8f,-4),splash.Floor);
                for (int segment = 0; segment < 16; segment++)
                {
                    float a = segment*Mathf.PI/16, b = (segment+1)*Mathf.PI/16;
                    Vector3 p = origin+new Vector3(0,Mathf.Sin(a)*2.4f,Mathf.Cos(a)*1.3f);
                    Vector3 q = origin+new Vector3(0,Mathf.Sin(b)*2.4f,Mathf.Cos(b)*1.3f);
                    Pole("Rainbow spray arch",root,p,q,.045f,arch%2 == 0 ? yellow : blue);
                }
            }
        }
        private void BuildWaterfall(BalnearioLayout.Pool leisure)
        {
            // A local stone-clad cascade, not a retaining wall that divides the whole world.
            Vector2 edge = leisure.outline[32];
            Box("Stone waterfall wall",root,At(edge+Vector2.left*.6f,leisure.deck+1.25f),new Vector3(.8f,2.5f,6),stone);
            Box("Waterfall lip",root,At(edge,leisure.deck+2.45f),new Vector3(1.1f,.18f,5),coping);
            for (int i = 0; i < 48; i++)
            {
                float z = -2.3f+i*4.6f/47;
                Pole("Cascade ribbon",root,At(edge+new Vector2(.15f,z),leisure.deck+2.38f),
                    At(edge+new Vector2(.65f,z),leisure.Surface+.01f),.025f,water);
            }
        }
        private void BuildSlideWater(List<Vector3> path, float surface)
        {
            if (!sharedMaterials.TryGetValue("Flowing slide water",out var flow))
            {
                flow = new Material(Resources.Load<Shader>("Ermo/Water/SlideWater"));
                sharedMaterials.Add("Flowing slide water",flow); materialResources.Add(flow);
            }
            var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            float length = 0;
            for (int i = 0; i < path.Count; i++)
            {
                if (i > 0) length += Vector3.Distance(path[i],path[i-1]);
                Vector3 right = Vector3.Cross(Vector3.up,path[Mathf.Min(i+1,path.Count-1)]-path[Mathf.Max(0,i-1)]).normalized;
                for (int j = 0; j <= 6; j++)
                {
                    float x = (j/6f-.5f)*.95f;
                    v.Add(path[i]+right*x-Vector3.up*(Mathf.Sqrt(1.12f*1.12f-x*x)-.025f));
                    uv.Add(new Vector2(j/6f,length));
                    if (i > 0 && j > 0) { int n = v.Count-1; t.AddRange(new[] { n,n-1,n-8,n,n-8,n-7 }); }
                }
            }
            Vector3 end = path[path.Count-1]-Vector3.up*1.08f;
            Vector3 direction = (path[path.Count-1]-path[path.Count-2]).normalized;
            Vector3 across = Vector3.Cross(Vector3.up,direction).normalized*.45f;
            Vector3 splash = end+new Vector3(direction.x,0,direction.z)*.7f; splash.y = surface+.025f;
            int filmVertices = v.Count;
            Vector3 previous = end;
            float spillLength = length;
            for (int i = 1; i <= 16; i++)
            {
                float u = i/16f;
                Vector3 point = Vector3.Lerp(end,splash,u); point.y = Mathf.Lerp(end.y,splash.y,u*u);
                float nextLength = spillLength+Vector3.Distance(previous,point);
                float width = Mathf.Lerp(1,.78f,u);
                float previousWidth = Mathf.Lerp(1,.78f,(i-1)/16f);
                Quad(v,t,previous-across*previousWidth,previous+across*previousWidth,point+across*width,point-across*width);
                uv.Add(new Vector2(0,spillLength)); uv.Add(new Vector2(1,spillLength));
                uv.Add(new Vector2(1,nextLength)); uv.Add(new Vector2(0,nextLength));
                previous = point; spillLength = nextLength;
            }
            var mesh = MeshObject("Running flume water and spilling lip",root,v,t,flow).GetComponent<MeshFilter>().sharedMesh;
            mesh.SetUVs(0,uv);
            var flowColors = new Color[v.Count];
            for (int i = 0; i < flowColors.Length; i++) flowColors[i] = i < filmVertices ? Color.black : Color.white;
            mesh.colors = flowColors;
            var sound = new GameObject("Slide water spill",typeof(AudioSource)); sound.transform.SetParent(root,false); sound.transform.localPosition = splash;
            var source = sound.GetComponent<AudioSource>();
            source.clip = MakeFlowSound(); source.loop = true; source.spatialBlend = 1; source.volume = .28f;
            source.minDistance = 2; source.maxDistance = 28; source.dopplerLevel = 0;
            source.rolloffMode = AudioRolloffMode.Logarithmic; source.playOnAwake = true;
        }
        private AudioClip flowSound;
        private AudioClip MakeFlowSound()
        {
            if (flowSound != null) return flowSound;
            const int rate = 24000;
            var samples = new float[rate*3]; var rng = new System.Random(layout.seed); float low = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float noise = (float)rng.NextDouble()*2-1; low += (noise-low)*.14f;
                float phase = i/(float)samples.Length*Mathf.PI*2;
                samples[i] = (low*.7f+noise*.11f)*(.7f+.15f*Mathf.Sin(phase*7)+.1f*Mathf.Sin(phase*13));
            }
            // Crossfade the loop seam.
            for (int i = 0; i < 480; i++) samples[samples.Length-480+i] = Mathf.Lerp(samples[samples.Length-480+i],samples[i],i/479f);
            flowSound = AudioClip.Create("Continuous flume rush and splash",samples.Length,1,rate,false);
            flowSound.SetData(samples,0); owned.Add(flowSound); return flowSound;
        }
        private void Flume(List<Vector3> path, Material mat, bool tube)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            const int sides = 16;
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 forward = path[Mathf.Min(i+1,path.Count-1)]-path[Mathf.Max(i-1,0)];
                Vector3 right = Vector3.Cross(Vector3.up,forward).normalized;
                float arc = tube ? Mathf.Lerp(Mathf.PI*2,Mathf.PI,Mathf.SmoothStep(0,1,Mathf.InverseLerp(60,76,i))) : Mathf.PI;
                for (int j = 0; j <= sides; j++)
                {
                    float a = j/(float)sides*arc;
                    v.Add(path[i]+right*(Mathf.Cos(a)*1.12f)-Vector3.up*(Mathf.Sin(a)*1.12f));
                    if (i == 0 || j == 0) continue;
                    int index = i*(sides+1)+j;
                    t.AddRange(new[] { index,index-1,index-sides-2,index,index-sides-2,index-sides-1 });
                }
            }
            MeshObject(tube ? "Orange spiral tube / open runout" : "Open turquoise water slide",root,v,t,mat,true);
            // Structural hoops make the tube read as assembled fibreglass rather than a smooth pipe.
            if (tube) for (int i = 4; i < 68; i += 5)
            {
                Vector3 forward = (path[i+1]-path[i-1]).normalized, right = Vector3.Cross(Vector3.up,forward).normalized;
                for (int j = 0; j < 16; j++)
                {
                    float a = j*Mathf.PI*2/16, b = (j+1)*Mathf.PI*2/16;
                    Pole("Tube joint hoop",root,path[i]+right*Mathf.Cos(a)*1.14f+Vector3.up*Mathf.Sin(a)*1.14f,
                        path[i]+right*Mathf.Cos(b)*1.14f+Vector3.up*Mathf.Sin(b)*1.14f,.025f,red);
                }
            }
        }
        private void BatchRenderers()
        {
            // One mesh per material/sector. Discard source renderers, not thousands of disabled copies.
            var batches = new Dictionary<Material,List<CombineInstance>>();
            var sources = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (var filter in sources)
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || filter.sharedMesh == null || !renderer.enabled) continue;
                Material mat = renderer.sharedMaterial;
                if (!batches.TryGetValue(mat,out var list)) { list = new List<CombineInstance>(); batches.Add(mat,list); }
                list.Add(new CombineInstance { mesh = filter.sharedMesh, transform = root.worldToLocalMatrix*filter.transform.localToWorldMatrix });
                renderer.enabled = false;
            }
            foreach (var pair in batches)
            {
                var mesh = new Mesh { name = "Balneario batch / "+pair.Key.name, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(pair.Value.ToArray(),true,true); owned.Add(mesh);
                var go = new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(root,false);
                if (pair.Key == water || pair.Key.shader.name == "Ermo/Slide Water") go.layer = 4;
                go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = pair.Key;
            }
            foreach (var filter in sources)
            {
                var collider = filter.GetComponent<Collider>();
                if ((collider == null || !collider.enabled) && filter.transform.childCount == 0) Object.Destroy(filter.gameObject);
                else { Object.Destroy(filter.GetComponent<MeshRenderer>()); Object.Destroy(filter); }
            }
        }
    }
}
