using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ermo
{
    // A sector is a sampling window, never an enclosed park/prefab. Its roads use world coordinates.
    public sealed class BalnearioLayout
    {
        public const float SectorSize = 160, HalfSector = SectorSize/2;
        public const float LowerDeck = .22f, UpperDeck = 3.42f, PoolDepth = .77f, SlidePoolDepth = 2.82f;
        public sealed class Pool
        {
            public Vector2 center, size;
            public float deck;
            public float depth = PoolDepth;
            public int shape;
            public readonly List<Vector2> outline = new List<Vector2>();
            public float Surface => deck-.22f;
            public float Floor => deck-depth;
            public bool Contains(Vector2 p)
            {
                bool inside = false;
                for (int i = 0, j = outline.Count-1; i < outline.Count; j = i++)
                {
                    Vector2 a = outline[i], b = outline[j];
                    if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside = !inside;
                }
                return inside;
            }
            public float EdgeDistance(Vector2 p)
            {
                float distance = float.MaxValue;
                for (int i = 0; i < outline.Count; i++)
                {
                    Vector2 a = outline[i], ab = outline[(i+1)%outline.Count]-a;
                    distance = Mathf.Min(distance,Vector2.Distance(p,a+ab*Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude)));
                }
                return distance;
            }
        }
        public struct Site { public Vector2 position; public float radius, yaw, scale; public int kind; }
        public readonly List<Pool> pools = new List<Pool>();
        public readonly List<Site> sites = new List<Site>();
        public readonly List<List<Vector2>> paths = new List<List<Vector2>>();
        public readonly List<Vector2> kiosks = new List<Vector2>();
        public readonly int seed, theme;
        public readonly Vector2Int coordinate;
        public readonly int slidePool = -1;
        public readonly bool playground, waterfall, raisedGarden;
        public readonly Vector2 tower;
        private readonly System.Random random;
        private readonly int worldHash;
        public bool IsArrival => coordinate == Vector2Int.zero;

        public static Vector2Int Coordinate(Vector2 p) => new Vector2Int(
            Mathf.FloorToInt((p.x+HalfSector)/SectorSize),Mathf.FloorToInt((p.y+HalfSector)/SectorSize));
        public static int Hash(string text)
        {
            unchecked { uint h = 2166136261; foreach (char c in text ?? "ermo-001") h = (h^c)*16777619; return (int)h; }
        }
        public BalnearioLayout(string worldSeed) : this(worldSeed,Vector2Int.zero) { }
        public BalnearioLayout(string worldSeed, Vector2Int cell)
        {
            coordinate = cell; worldHash = Hash(worldSeed);
            unchecked { seed = Hash(worldSeed+"/balneario/"+cell.x+"/"+cell.y); }
            random = new System.Random(seed);
            theme = IsArrival ? 0 : random.Next(4); // lagoons, family, flumes, quiet gardens
            bool slides = IsArrival || theme == 2;
            playground = theme == 1; waterfall = theme == 0;
            raisedGarden = theme == 3;
            if (raisedGarden) kiosks.Add(new Vector2(-28,-60));
            else if (IsArrival || random.Next(3) != 0) kiosks.Add(new Vector2(-35,Range(-64,-58)));
            BuildPromenades();
            if (IsArrival) AddPool(Vector2.zero,new Vector2(Range(26,29),Range(20,22)),0);
            if (slides)
            {
                slidePool = pools.Count;
                AddPool(new Vector2(Range(36,40),Range(-38,-34)),new Vector2(16,9),2);
                pools[slidePool].depth = SlidePoolDepth;
                tower = pools[slidePool].center+new Vector2(20,51);
            }
            int desired = IsArrival ? 3 : random.Next(2,5);
            for (int attempt = 0; attempt < 150 && pools.Count < desired; attempt++)
            {
                Vector2 center = new Vector2(Range(-43,42),Range(-39,30));
                Vector2 size = theme == 3 ? new Vector2(Range(9,15),Range(7,11)) : new Vector2(Range(12,22),Range(9,15));
                float radius = Mathf.Max(size.x,size.y)*1.16f;
                if (Mathf.Abs(center.x)+radius > 57 || center.y-radius < -65 || center.y+radius > 47) continue;
                if (InAttraction(center,radius+4)) continue;
                int shape = theme == 1 ? random.Next(1,3) : (random.Next(5) == 0 ? 2 : 0);
                Pool candidate = MakePool(center,size,shape);
                bool valid = true;
                foreach (Pool pool in pools)
                {
                    float other = Mathf.Max(pool.size.x,pool.size.y)*1.16f;
                    if (!PoolsClear(candidate,pool,8) ||
                        SegmentDistance(center,pool.outline[16],new Vector2(pool.outline[16].x,66)) < radius+3 ||
                        SegmentDistance(pool.center,candidate.outline[16],new Vector2(candidate.outline[16].x,66)) < other+3) valid = false;
                }
                if (valid) pools.Add(candidate);
            }
            // Every pool gets a north exit spur into the shared, continuous promenade network.
            foreach (Pool pool in pools)
            {
                Vector2 edge = pool.outline[16]; var path = new List<Vector2>();
                float end = HorizontalRoad(edge.x);
                for (int i = 0; i <= 16; i++)
                {
                    float u = i/16f;
                    path.Add(new Vector2(edge.x+Mathf.Sin(u*Mathf.PI)*1.5f,Mathf.Lerp(edge.y,end,u)));
                }
                paths.Add(path);
            }
            // Service buildings are occasional stops, not an entrance gate repeated in every tile.
            if (!raisedGarden) foreach (Vector2 kiosk in kiosks)
            {
                Vector2 start = kiosk+Vector2.down*4.5f;
                var servicePath = new List<Vector2>();
                for (int i = 0; i <= 12; i++)
                {
                    float u = i/12f;
                    servicePath.Add(new Vector2(Mathf.Lerp(start.x,VerticalRoad(start.y),u),start.y+Mathf.Sin(u*Mathf.PI)));
                }
                paths.Add(servicePath);
            }
            foreach (Pool pool in pools)
                for (int i = 0; i < 16; i++)
                {
                    Vector2 edge = pool.outline[i*4]; Vector2 outward = (edge-pool.center).normalized;
                    TrySite(edge+outward*Range(3.3f,4.5f),i%3 == 0 ? 1 : 0,1.9f,
                        Mathf.Atan2(-outward.x,-outward.y)*Mathf.Rad2Deg,1);
                }
            for (int cluster = 0; cluster < 90; cluster++)
            {
                Vector2 anchor = new Vector2(Range(-74,74),Range(-74,74));
                for (int member = 0; member < 3; member++)
                    TrySite(anchor+new Vector2(Range(-4,4),Range(-4,4)),member == 0 ? 2 : 3,
                        member == 0 ? 1.7f : 1.05f,Range(0,360),Range(.8f,1.35f));
            }
        }
        private float Range(float a, float b) => Mathf.Lerp(a,b,(float)random.NextDouble());
        // Coordinate-independent phase keeps both sides of every sector seam identical.
        private float Phase => (worldHash & 65535)*.001f;
        public float HorizontalRoad(float localX) => 60+10*(float)Math.Sin((coordinate.x*(double)SectorSize+localX)*.017+Phase+coordinate.y*.713);
        public float VerticalRoad(float localZ) => -65+6*(float)Math.Sin((coordinate.y*(double)SectorSize+localZ)*.019+Phase+coordinate.x*.977);
        private void BuildPromenades()
        {
            var horizontal = new List<Vector2>(); var vertical = new List<Vector2>();
            for (int i = 0; i <= 40; i++)
            {
                float p = -HalfSector+i*SectorSize/40;
                horizontal.Add(new Vector2(p,HorizontalRoad(p))); vertical.Add(new Vector2(VerticalRoad(p),p));
            }
            paths.Add(horizontal); paths.Add(vertical);
        }
        private void AddPool(Vector2 center, Vector2 size, int shape) => pools.Add(MakePool(center,size,shape));
        private Pool MakePool(Vector2 center, Vector2 size, int shape)
        {
            var pool = new Pool { center = center, size = size, deck = LowerDeck, shape = shape };
            float phase = Range(-Mathf.PI,Mathf.PI);
            for (int i = 0; i < 64; i++)
            {
                float a = i*Mathf.PI*2/64, x = Mathf.Cos(a), z = Mathf.Sin(a);
                if (shape == 2) { x = Mathf.Sign(x)*Mathf.Pow(Mathf.Abs(x),.42f); z = Mathf.Sign(z)*Mathf.Pow(Mathf.Abs(z),.42f); }
                float bend = Mathf.DeltaAngle(a*Mathf.Rad2Deg,phase*Mathf.Rad2Deg)*Mathf.Deg2Rad;
                float r = shape == 0 ? 1+.09f*Mathf.Cos(2*a+phase)-.35f*Mathf.Exp(-bend*bend/.5f) : 1;
                pool.outline.Add(center+new Vector2(x*size.x,z*size.y)*r);
            }
            return pool;
        }
        private static bool PoolsClear(Pool a, Pool b, float gap)
        {
            float boundA = Mathf.Max(a.size.x,a.size.y)*1.16f, boundB = Mathf.Max(b.size.x,b.size.y)*1.16f;
            if (Vector2.Distance(a.center,b.center) > boundA+boundB+gap) return true;
            foreach (Vector2 p in a.outline) if (b.Contains(p) || b.EdgeDistance(p) < gap) return false;
            foreach (Vector2 p in b.outline) if (a.Contains(p) || a.EdgeDistance(p) < gap) return false;
            return true;
        }
        public static float DeckAt(Vector2 p) => LowerDeck;
        public static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b-a; return Vector2.Distance(p,a+ab*Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(ab.sqrMagnitude,.000001f)));
        }
        public bool InCirculation(Vector2 p, float radius)
        {
            foreach (var path in paths) for (int i = 1; i < path.Count; i++)
                if (SegmentDistance(p,path[i-1],path[i]) < radius+2) return true;
            return false;
        }
        public bool InAttraction(Vector2 p, float radius)
        {
            if (raisedGarden && p.x > -42-radius && p.x < 12+radius && p.y > -70-radius && p.y < -48+radius) return true;
            if (slidePool >= 0 && p.x > 18-radius && p.x < 73+radius && p.y > -49-radius && p.y < 29+radius) return true;
            foreach (Vector2 kiosk in kiosks) if (Vector2.Distance(p,kiosk) < radius+7.5f) return true;
            return false;
        }
        private void TrySite(Vector2 p, int kind, float radius, float yaw, float scale)
        {
            if (Mathf.Abs(p.x)+radius > 76 || Mathf.Abs(p.y)+radius > 76 || InCirculation(p,radius) || InAttraction(p,radius)) return;
            foreach (Pool pool in pools)
                if (pool.Contains(p) || pool.EdgeDistance(p) < radius+.8f ||
                    (kind >= 2 && pool.EdgeDistance(p) < radius+6)) return;
            foreach (Site site in sites) if (Vector2.Distance(p,site.position) < radius+site.radius+.65f) return;
            sites.Add(new Site { position = p, kind = kind, radius = radius, yaw = yaw, scale = scale });
        }
        public bool IsWater(Vector3 feet)
        {
            Vector2 p = new Vector2(feet.x,feet.z);
            foreach (Pool pool in pools) if (feet.y < pool.Surface+.08f && feet.y > pool.Floor-.2f && pool.Contains(p)) return true;
            return false;
        }
        public void Validate()
        {
            if (IsArrival && (!pools[0].Contains(Vector2.zero) || pools[0].EdgeDistance(Vector2.zero) < 10))
                throw new InvalidOperationException("Arrival must be inside the unobstructed lagoon.");
            foreach (Pool pool in pools) foreach (Vector2 p in pool.outline)
                if (Mathf.Abs(p.x) > 70 || Mathf.Abs(p.y) > 70) throw new InvalidOperationException("Pool crosses sector ownership.");
            for (int i = 0; i < pools.Count; i++) for (int j = i+1; j < pools.Count; j++)
                if (!PoolsClear(pools[i],pools[j],.5f)) throw new InvalidOperationException("Pool basins overlap.");
            for (int i = 0; i < sites.Count; i++)
            {
                Site a = sites[i];
                if (InCirculation(a.position,a.radius) || InAttraction(a.position,a.radius)) throw new InvalidOperationException("Blocked route/attraction.");
                foreach (Pool pool in pools) if (pool.Contains(a.position) || pool.EdgeDistance(a.position) < a.radius+.79f)
                    throw new InvalidOperationException("Blocked pool coping.");
                for (int j = i+1; j < sites.Count; j++) if (Vector2.Distance(a.position,sites[j].position) < a.radius+sites[j].radius+.64f)
                    throw new InvalidOperationException("Overlapping decoration.");
            }
        }
    }
}
