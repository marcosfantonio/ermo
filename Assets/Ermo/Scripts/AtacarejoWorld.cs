using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ermo
{
    // The market itself: builds 32 m sectors around the player a few pieces
    // per frame, keeps a floating origin, sets the dark atmosphere, and owns
    // the two ways a door can take the player:
    //   LoopToVestibule  - the SAÍDA door: dark screen, back at the arrival.
    //   LeaveToNextPhase - the staff entrance behind room 13: the next phase.
    // Sound lives in AtacarejoAudio, the PA in AtacarejoAnnouncer, flickering
    // in AtacarejoLights. Setup (player, spawn, next phase) in AtacarejoBootstrap.
    public sealed class AtacarejoWorld : MonoBehaviour
    {
        public const string ScenePath = "Assets/Ermo/Scenes/Atacarejo.unity";
        public const int ViewRadius = 1;
        const int StepsPerFrame = 12;
        const float AheadDistance = 12f;
        const int RebaseSectors = 8;

        // The eye clears to the fog colour: an unbuilt gap or the end of a
        // corridor reads as dark distance, never sky, never a hole.
        public static readonly Color Darkness = new Color(.034f, .039f, .031f, 1f);
        public const float FogDensity = .052f;
        static readonly Color Ambient = new Color(.05f, .056f, .048f);
        static readonly Color LoopFade = new Color(.012f, .014f, .011f);
        static readonly Color NextPhaseFade = new Color(.05f, .004f, .004f);

        sealed class Sector
        {
            public GameObject root;
            public IEnumerator<int> steps;
            public bool ready;
        }

        [SerializeField] AtacarejoKit kit;

        readonly Dictionary<Vector2Int, Sector> sectors = new Dictionary<Vector2Int, Sector>();
        readonly List<Vector2Int> wanted = new List<Vector2Int>();
        GameObject environment;
        Sector building;
        Vector2Int originSector;
        Transform player;
        FirstPersonWalker walker;
        Vector3 arrivalLayout;
        float arrivalYaw;
        PhaseDestination nextPhase;
        AtacarejoAudio sound;
        AtacarejoAnnouncer announcer;

        public static AtacarejoWorld Instance { get; private set; }
        public string Seed { get; private set; }
        public bool Started => player != null;
        public bool Looping { get; private set; }
        public int Loops { get; private set; }
        public Vector2Int CurrentSector { get; private set; }
        public int LoadedSectorCount => sectors.Count;
        public Vector3 Arrival => LayoutToWorld(arrivalLayout);
        public AtacarejoAudio Sound => sound;
        public AtacarejoAnnouncer Announcer => announcer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake()
        {
            Instance = this;
            sound = gameObject.AddComponent<AtacarejoAudio>();
            announcer = gameObject.AddComponent<AtacarejoAnnouncer>();
        }

        // Called once by AtacarejoBootstrap with the player and the phase settings.
        public void Begin(FirstPersonWalker player, WorldSettings settings, Vector3 arrival, float yaw, PhaseDestination next)
        {
            walker = player;
            arrivalLayout = arrival;
            arrivalYaw = yaw;
            nextPhase = next;
            Generate(settings != null ? settings.seed : "ermo-001");
            this.player = player.transform;
            ApplyAtmosphere(player.View);
            player.SetSlidePose(Arrival, arrivalYaw, 30f, 1.12f);
            Physics.SyncTransforms();
            sound.Begin(player, settings, announcer);
            announcer.Begin(this, player, () => sound.Volume);
        }

        // ---- Doors -----------------------------------------------------

        // The SAÍDA door. Customers do not get out that way.
        public bool LoopToVestibule()
        {
            if (!Started || Looping || PhaseTransition.Busy) return false;
            StartCoroutine(Loop());
            return true;
        }

        IEnumerator Loop()
        {
            Looping = true;
            announcer.Silence();
            sound.PlayDoor("door_open");
            walker.BeginTraversal(true);
            var fade = ScreenFade.Instance;
            yield return fade.To(1f, 1.1f, LoopFade);
            BuildAround(arrivalLayout);
            walker.SetSlidePose(Arrival, arrivalYaw, -3f, 1.65f);
            Physics.SyncTransforms();
            walker.EndTraversal();
            sound.ResetFeet();
            announcer.AnnounceLoop(Loops);
            Loops++;
            yield return new WaitForSecondsRealtime(.6f);
            yield return fade.To(0f, 1.2f);
            Looping = false;
        }

        // The staff entrance at the back of the meat locker.
        public bool LeaveToNextPhase()
        {
            if (!Started || Looping || PhaseTransition.Busy) return false;
            if (!PhaseTransition.Go(nextPhase, NextPhaseFade)) return false;
            walker.BeginTraversal(true);
            announcer.LowChime();
            sound.StopMusic();
            sound.PlayDoor("door_close", 1.1f);
            return true;
        }

        // ---- Plan coordinates --------------------------------------------

        // Market plan coordinates (the ones AtacarejoLayout uses) for a world
        // point, independent of the floating origin, and back.
        public Vector3 LayoutPosition(Vector3 world) => transform.InverseTransformPoint(world) + Offset(originSector);
        public Vector3 LayoutToWorld(Vector3 layout) => transform.TransformPoint(layout - Offset(originSector));
        public bool IsSectorReady(Vector2Int cell) => sectors.TryGetValue(cell, out var sector) && sector.ready;

        static Vector3 Offset(Vector2Int cell) => new Vector3(cell.x * AtacarejoLayout.SectorSize, 0f, cell.y * AtacarejoLayout.SectorSize);

        // ---- Atmosphere --------------------------------------------------

        void ApplyAtmosphere(Camera eye)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = Darkness;
            RenderSettings.fogDensity = FogDensity;
            // Played on its own, the scene would still reflect and be lit by
            // Unity's default blue sky. Reflections come from the dark instead.
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = DarkCubemap();
            DynamicGI.UpdateEnvironment();
            if (eye == null) return;
            eye.clearFlags = CameraClearFlags.SolidColor;
            eye.backgroundColor = Darkness;
        }

        static Cubemap darkCubemap;

        static Cubemap DarkCubemap()
        {
            if (darkCubemap != null) return darkCubemap;
            darkCubemap = new Cubemap(4, TextureFormat.RGBA32, false) { name = "Reflexo escuro do mercado" };
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Darkness;
            foreach (CubemapFace face in System.Enum.GetValues(typeof(CubemapFace)))
                if (face != CubemapFace.Unknown) darkCubemap.SetPixels(pixels, face);
            darkCubemap.Apply();
            return darkCubemap;
        }

        // Only a whisper of directional light: the ceiling would shadow it
        // anyway. The fixtures do the real lighting.
        static Light CreateFillLight(Transform parent)
        {
            var go = new GameObject("Luz geral fraca");
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(70f, -18f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(.70f, .78f, .62f);
            light.intensity = .07f;
            light.shadows = LightShadows.None;
            return light;
        }

        // ---- Streaming ---------------------------------------------------

        void Generate(string seed)
        {
            if (environment != null) { environment.SetActive(false); Destroy(environment); }
            foreach (var sector in sectors.Values) Dispose(sector);
            sectors.Clear();
            wanted.Clear();
            building = null;
            AtacarejoLights.Clear();
            Seed = seed;
            originSector = Vector2Int.zero;
            environment = new GameObject("Atacarejo / " + seed);
            environment.transform.SetParent(transform, false);
            RenderSettings.sun = CreateFillLight(environment.transform);
            BuildAround(arrivalLayout);
        }

        // Finishes the sector holding this plan point and its neighbours now
        // (on arrival, and while the screen is dark after a loop).
        void BuildAround(Vector3 layout)
        {
            var cell = AtacarejoLayout.SectorOf(layout.x, layout.z);
            RefreshWanted(cell);
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
                CompleteNow(cell + new Vector2Int(x, z));
        }

        Sector BeginSector(Vector2Int coordinate)
        {
            var sector = new Sector { root = new GameObject("Setor " + coordinate) };
            sector.root.transform.SetParent(environment.transform, false);
            sector.root.transform.localPosition = Offset(coordinate - originSector);
            sector.root.SetActive(false);
            var plan = new AtacarejoLayout(Seed, coordinate);
            sector.steps = new AtacarejoBuilder(sector.root.transform, plan, kit).BuildSteps().GetEnumerator();
            sectors.Add(coordinate, sector);
            return sector;
        }

        void CompleteNow(Vector2Int coordinate)
        {
            if (!sectors.TryGetValue(coordinate, out var sector)) sector = BeginSector(coordinate);
            if (sector.ready) return;
            while (sector.steps.MoveNext()) { }
            Finish(sector);
            if (building == sector) building = null;
        }

        static void Finish(Sector sector)
        {
            sector.steps.Dispose();
            sector.steps = null;
            sector.ready = true;
            sector.root.SetActive(true);
        }

        void Update()
        {
            if (!Started) return;
            TickStreaming();
            AtacarejoLights.Tick(Time.time, sound.Volume);
        }

        void TickStreaming()
        {
            Vector3 feet = transform.InverseTransformPoint(player.position);
            Vector2Int local = AtacarejoLayout.SectorOf(feet.x, feet.z);
            Vector2Int cell = originSector + local;
            if (cell != CurrentSector) RefreshWanted(cell);
            CompleteNow(cell);
            FinishAhead(feet, cell);
            if (Mathf.Abs(local.x) > RebaseSectors || Mathf.Abs(local.y) > RebaseSectors) Rebase(local);

            while (building == null && wanted.Count > 0)
            {
                Vector2Int next = wanted[0];
                wanted.RemoveAt(0);
                if (!sectors.ContainsKey(next)) building = BeginSector(next);
            }
            if (building == null) return;
            for (int step = 0; step < StepsPerFrame; step++)
            {
                if (building.steps.MoveNext()) continue;
                Finish(building);
                building = null;
                break;
            }
        }

        // A sector the player can reach within 12 m is finished now, floor
        // and all, corners included, so no edge is ever walked into empty.
        void FinishAhead(Vector3 feet, Vector2Int cell)
        {
            float dx = feet.x - (cell.x - originSector.x) * AtacarejoLayout.SectorSize;
            float dz = feet.z - (cell.y - originSector.y) * AtacarejoLayout.SectorSize;
            float edge = AtacarejoLayout.Half;
            int x0 = dx + edge < AheadDistance ? -1 : 0, x1 = edge - dx < AheadDistance ? 1 : 0;
            int z0 = dz + edge < AheadDistance ? -1 : 0, z1 = edge - dz < AheadDistance ? 1 : 0;
            for (int x = x0; x <= x1; x++)
            for (int z = z0; z <= z1; z++)
                if (x != 0 || z != 0) CompleteNow(cell + new Vector2Int(x, z));
        }

        void RefreshWanted(Vector2Int center)
        {
            CurrentSector = center;
            wanted.Clear();
            var far = new List<Vector2Int>();
            foreach (var key in sectors.Keys)
                if (Mathf.Abs(key.x - center.x) > ViewRadius || Mathf.Abs(key.y - center.y) > ViewRadius) far.Add(key);
            foreach (var key in far)
            {
                if (building == sectors[key]) building = null;
                Dispose(sectors[key]);
                sectors.Remove(key);
            }
            for (int x = -ViewRadius; x <= ViewRadius; x++)
            for (int z = -ViewRadius; z <= ViewRadius; z++)
            {
                var cell = center + new Vector2Int(x, z);
                if (!sectors.ContainsKey(cell)) wanted.Add(cell);
            }
            wanted.Sort((a, b) => (a - center).sqrMagnitude.CompareTo((b - center).sqrMagnitude));
        }

        // Keeps coordinates small on long walks: shifts the market back under
        // the player instead of letting positions grow.
        void Rebase(Vector2Int delta)
        {
            Vector3 shift = Offset(delta);
            var controller = player.GetComponent<CharacterController>();
            bool collision = controller != null && controller.enabled;
            if (collision) controller.enabled = false;
            player.position -= transform.TransformVector(shift);
            foreach (var sector in sectors.Values) sector.root.transform.localPosition -= shift;
            originSector += delta;
            if (collision) controller.enabled = true;
            sound.ResetFeet();
            Physics.SyncTransforms();
        }

        static void Dispose(Sector sector)
        {
            sector.steps?.Dispose();
            if (sector.root != null) { sector.root.SetActive(false); Destroy(sector.root); }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var sector in sectors.Values) Dispose(sector);
            AtacarejoLights.Clear();
        }
    }
}
