using System.Collections.Generic;
using UnityEngine;

namespace Ermo
{
    public sealed class WaterWorld : MonoBehaviour
    {
        public const string ScenePath = "Assets/Ermo/Scenes/Balneario.unity";
        public const int ViewRadius = 2;
        public static WaterWorld Instance { get; private set; }
        public Vector3 Arrival => transform.TransformPoint(new Vector3(-originSector.x*BalnearioLayout.SectorSize,-.55f,-originSector.y*BalnearioLayout.SectorSize));
        public float SurfaceY => transform.position.y;
        public Material Sky { get; private set; }
        public BalnearioLayout Layout { get; private set; }
        public string Seed { get; private set; }
        public int LoadedSectorCount => sectors.Count;
        public Vector2Int CurrentSector { get; private set; }
        private sealed class Sector
        {
            public GameObject root;
            public BalnearioLayout layout;
            public readonly List<Object> resources = new List<Object>();
            public IEnumerator<int> steps;
            public bool ready;
        }
        private readonly Dictionary<Vector2Int,Sector> sectors = new Dictionary<Vector2Int,Sector>();
        private readonly List<Vector2Int> wanted = new List<Vector2Int>();
        private readonly List<Vector2Int> remove = new List<Vector2Int>();
        private readonly List<Object> paletteResources = new List<Object>();
        private readonly Dictionary<string,Material> palette = new Dictionary<string,Material>();
        private GameObject environment;
        private Light sun;
        private Transform player;
        private Sector building;
        private Vector2Int originSector;
        private bool visible;
        private static string requestedSeed = "ermo-001";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; requestedSeed = "ermo-001"; }
        public static void RequestSeed(string seed)
        {
            requestedSeed = seed;
            // A new rural run always prepares origin again, even when the seed is unchanged.
            if (Instance != null) Instance.Generate(seed);
        }
        private void Awake() { Generate(requestedSeed); Instance = this; }
        private void Generate(string seed)
        {
            if (environment != null) { environment.SetActive(false); Destroy(environment); }
            ReleaseResources();
            Seed = seed; player = null; visible = false; originSector = Vector2Int.zero; CurrentSector = Vector2Int.zero;
            environment = new GameObject("Endless balneario / "+seed); environment.transform.SetParent(transform,false);
            environment.SetActive(false);
            var reflections = new GameObject("Local water reflections",typeof(BalnearioReflections));
            reflections.transform.SetParent(environment.transform,false);
            Sky = new Material(Resources.Load<Shader>("Ermo/Water/WaterSky")); paletteResources.Add(Sky);
            var light = new GameObject("Balneario afternoon",typeof(Light)); light.transform.SetParent(environment.transform,false);
            sun = light.GetComponent<Light>(); sun.type = LightType.Directional; sun.shadows = LightShadows.Soft;
            sun.color = new Color(1,.94f,.8f); sun.intensity = 1.15f; light.transform.rotation = Quaternion.Euler(38,-35,0);
            // Only arrival is synchronous. The surrounding ring warms up during the rural preload.
            Complete(BeginSector(Vector2Int.zero)); Layout = sectors[Vector2Int.zero].layout;
            RefreshWanted(Vector2Int.zero); SetShaderOrigin();
        }
        private Sector BeginSector(Vector2Int coordinate)
        {
            var sector = new Sector { root = new GameObject("Aquatic sector "+coordinate), layout = new BalnearioLayout(Seed,coordinate) };
            sector.root.transform.SetParent(environment.transform,false);
            sector.root.transform.localPosition = Offset(coordinate-originSector);
            sector.root.SetActive(false);
            sector.steps = new BalnearioGeometry(sector.root.transform,sector.layout,sector.resources,palette,paletteResources).BuildSteps().GetEnumerator();
            sectors.Add(coordinate,sector); return sector;
        }
        private static Vector3 Offset(Vector2Int cell) => new Vector3(cell.x*BalnearioLayout.SectorSize,0,cell.y*BalnearioLayout.SectorSize);
        private static void Complete(Sector sector)
        {
            while (sector.steps.MoveNext()) { }
            FinishSector(sector);
        }
        private static void FinishSector(Sector sector)
        {
            sector.steps.Dispose(); sector.steps = null; sector.ready = true; sector.root.SetActive(true);
        }
        private void Update() => TickStreaming();
        public void TickStreaming()
        {
            if (environment == null) return;
            if (visible && player != null)
            {
                Vector3 feet = transform.InverseTransformPoint(player.position);
                Vector2Int local = BalnearioLayout.Coordinate(new Vector2(feet.x,feet.z));
                Vector2Int cell = originSector+local;
                if (cell != CurrentSector) RefreshWanted(cell);
                // A teleport or exceptionally fast move must never outrun ground collision.
                if (!sectors.TryGetValue(cell,out var current)) Complete(BeginSector(cell));
                else if (!current.ready) { Complete(current); if (building == current) building = null; }
                if (Mathf.Abs(local.x) > 8 || Mathf.Abs(local.y) > 8) Rebase(local);
            }
            if (building == null && wanted.Count > 0)
            {
                Vector2Int next = wanted[0]; wanted.RemoveAt(0);
                if (!sectors.ContainsKey(next)) building = BeginSector(next);
            }
            // Bounded work per frame; hidden preloading also advances this generator.
            if (building != null && !building.steps.MoveNext()) { FinishSector(building); building = null; }
            UpdateHorizon();
        }
        private void RefreshWanted(Vector2Int center)
        {
            CurrentSector = center; wanted.Clear(); remove.Clear();
            foreach (var pair in sectors)
                if (!InRange(pair.Key,center)) remove.Add(pair.Key);
            foreach (Vector2Int key in remove)
            {
                if (building == sectors[key]) building = null;
                Dispose(sectors[key]); sectors.Remove(key);
            }
            for (int x = -ViewRadius; x <= ViewRadius; x++) for (int z = -ViewRadius; z <= ViewRadius; z++)
            {
                var cell = center+new Vector2Int(x,z);
                if (!sectors.ContainsKey(cell)) wanted.Add(cell);
            }
            wanted.Sort((a,b) => (a-center).sqrMagnitude.CompareTo((b-center).sqrMagnitude));
        }
        private static bool InRange(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x-b.x) <= ViewRadius && Mathf.Abs(a.y-b.y) <= ViewRadius;
        public void Follow(Transform target) => player = target;
        // Explicit safety hook used by the isolated physics test / debug teleports.
        public void EnsureSector(Vector2Int coordinate)
        {
            RefreshWanted(coordinate);
            if (!sectors.TryGetValue(coordinate,out var sector)) Complete(BeginSector(coordinate));
            else if (!sector.ready) { Complete(sector); if (building == sector) building = null; }
        }
        public bool HasSector(Vector2Int coordinate) => sectors.TryGetValue(coordinate,out var sector) && sector.ready;
        public void SetVisible(bool value) { visible = value; environment.SetActive(value); }
        public bool IsWater(Vector3 worldFeet)
        {
            Vector3 feet = transform.InverseTransformPoint(worldFeet);
            Vector2Int local = BalnearioLayout.Coordinate(new Vector2(feet.x,feet.z));
            Vector2Int cell = local+originSector;
            return sectors.TryGetValue(cell,out var sector) && sector.ready && sector.layout.IsWater(feet-Offset(local));
        }
        public bool TryGetPoolWater(Vector3 worldPoint, out float surface, out float floor)
        {
            surface = floor = 0;
            if (!visible) return false;
            Vector3 point = transform.InverseTransformPoint(worldPoint);
            Vector2Int local = BalnearioLayout.Coordinate(new Vector2(point.x,point.z));
            if (!sectors.TryGetValue(local+originSector,out var sector) || !sector.ready) return false;
            point -= Offset(local);
            foreach (var pool in sector.layout.pools)
            {
                if (!pool.Contains(new Vector2(point.x,point.z)) || point.y < pool.Floor-.2f || point.y > pool.Surface+.1f) continue;
                surface = transform.TransformPoint(new Vector3(0,pool.Surface,0)).y;
                floor = transform.TransformPoint(new Vector3(0,pool.Floor,0)).y;
                return true;
            }
            return false;
        }
        public void ActivateEnvironment()
        {
            SetVisible(true); RenderSettings.skybox = Sky; RenderSettings.sun = sun;
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.24f,.63f,.73f);
            RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = .006f;
            UpdateHorizon();
        }
        private void UpdateHorizon()
        {
            Vector3 feet = player != null ? transform.InverseTransformPoint(player.position) : Vector3.zero;
            float distance = float.MaxValue;
            for (int x = -ViewRadius-1; x <= ViewRadius+1; x++) for (int z = -ViewRadius-1; z <= ViewRadius+1; z++)
            {
                var cell = CurrentSector+new Vector2Int(x,z);
                if (HasSector(cell)) continue;
                Vector3 center = Offset(cell-originSector);
                float dx = Mathf.Max(0,Mathf.Abs(feet.x-center.x)-BalnearioLayout.HalfSector);
                float dz = Mathf.Max(0,Mathf.Abs(feet.z-center.z)-BalnearioLayout.HalfSector);
                distance = Mathf.Min(distance,Mathf.Sqrt(dx*dx+dz*dz));
            }
            float end = Mathf.Clamp(distance-10,25,280);
            Shader.SetGlobalVector("_BalnearioHorizon",new Vector4(Mathf.Min(95,end*.55f),end,0,0));
        }
        private void Rebase(Vector2Int delta)
        {
            Vector3 shift = Offset(delta);
            var controller = player.GetComponent<CharacterController>();
            bool collision = controller != null && controller.enabled;
            if (collision) controller.enabled = false;
            player.position -= transform.TransformVector(shift);
            foreach (var sector in sectors.Values) sector.root.transform.localPosition -= shift;
            originSector += delta;
            if (collision) controller.enabled = true;
            Physics.SyncTransforms(); SetShaderOrigin();
        }
        private void SetShaderOrigin() => Shader.SetGlobalVector("_BalnearioWorldOffset",new Vector4(originSector.x*BalnearioLayout.SectorSize,originSector.y*BalnearioLayout.SectorSize,0,0));
        private static void Dispose(Sector sector)
        {
            sector.steps?.Dispose();
            if (sector.root != null) { sector.root.SetActive(false); Destroy(sector.root); }
            foreach (var resource in sector.resources) if (resource != null) Destroy(resource);
        }
        private void ReleaseResources()
        {
            foreach (var sector in sectors.Values) Dispose(sector);
            sectors.Clear(); wanted.Clear(); building = null;
            foreach (var resource in paletteResources) if (resource != null) Destroy(resource);
            paletteResources.Clear(); palette.Clear();
        }
        private void OnDestroy() { if (Instance == this) Instance = null; ReleaseResources(); }
    }
}
