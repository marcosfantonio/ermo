using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Ermo
{
    public sealed class ErmoBootstrap : MonoBehaviour
    {
        [SerializeField] private WorldSettings settings;
        [SerializeField] private Shader surfaceShader;
        [SerializeField] private Shader skyShader;
        [SerializeField] private Shader lensShader;
        [SerializeField] private Camera outputCamera;
        public WorldSettings Configuration => settings;
        public Shader SurfaceShader => surfaceShader;
        private WorldSettings runtimeSettings;
        private ProceduralWorld world;
        private FirstPersonWalker walker;
        private PixelDisplay display;
        private GameObject session;
        private Material sky;
        private string seedInput;
        private bool showInfo = true;
        private bool reducedMotion;
        private GUIStyle title, label, small;
        private float startedAt;
        private Vector2 menuScroll;

        private void Start()
        {
            foreach (var preview in ErmoPreview.FindLive())
            {
                preview.gameObject.SetActive(false); Destroy(preview.gameObject);
            }
            if (settings == null || surfaceShader == null || skyShader == null || lensShader == null)
            {
                Debug.LogError("Ermo: scene references are missing. Open Assets/Ermo/Scenes/Ermo.unity.");
                enabled = false; return;
            }
            seedInput = settings.seed;
            StartWorld(seedInput);
        }

        private void StartWorld(string seed)
        {
            if (session != null) { session.SetActive(false); Destroy(session); }
            if (sky != null) Destroy(sky);
            if (runtimeSettings != null) Destroy(runtimeSettings);
            runtimeSettings = Instantiate(settings);
            runtimeSettings.seed = seed.Trim(); seedInput = runtimeSettings.seed;
            session = new GameObject("Ermo — " + seedInput); session.transform.SetParent(transform, false);
            var player = new GameObject("Walker"); player.transform.SetParent(session.transform, false);
            // WorldStore restores saved terrain parameters before the spawn height is sampled.
            world = session.AddComponent<ProceduralWorld>();
            world.Initialize(runtimeSettings, player.transform, surfaceShader);
            ConfigureAtmosphere();
            float z = 7, x = world.Sampler.RoadX(z);
            player.transform.position = world.Sampler.Point(x, z) + Vector3.up * .6f;
            player.AddComponent<CharacterController>();
            walker = player.AddComponent<FirstPersonWalker>(); walker.Initialize(runtimeSettings);
            walker.ReducedMotion = reducedMotion;
            player.AddComponent<RuralAudio>().Initialize(walker,world,runtimeSettings);
            display = session.AddComponent<PixelDisplay>(); display.Initialize(walker.View, runtimeSettings, lensShader, outputCamera);
            display.ReducedMotion = reducedMotion;
            startedAt = Time.unscaledTime;
        }

        private void ConfigureAtmosphere()
        {
            GodotAtmosphere.Apply(runtimeSettings);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.48f,.68f,.92f)*.5f;
            sky = new Material(skyShader); RenderSettings.skybox = sky;
            var sunObject = new GameObject("Pale afternoon"); sunObject.transform.SetParent(session.transform, false);
            var sun = sunObject.AddComponent<Light>(); sun.type = LightType.Directional;
            sun.color = new Color(1,.88f,.62f); sun.intensity = 1.22f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(28,-48,0); RenderSettings.sun = sun;
        }

        private void Update()
        {
            if (walker == null || Keyboard.current == null) return;
            var keyboard = Keyboard.current;
            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.f1Key.wasPressedThisFrame)
            {
                walker.MenuOpen = !walker.MenuOpen;
                FirstPersonWalker.LockCursor(!walker.MenuOpen);
            }
            if (keyboard.hKey.wasPressedThisFrame && !walker.MenuOpen) showInfo = !showInfo;
            if (keyboard.f3Key.wasPressedThisFrame) display.EffectsEnabled = !display.EffectsEnabled;
            if (keyboard.f4Key.wasPressedThisFrame)
            {
                runtimeSettings.godotPixelGrid = !runtimeSettings.godotPixelGrid;
                if (!runtimeSettings.godotPixelGrid) runtimeSettings.renderHeight = 0;
            }
        }

        private void OnGUI()
        {
            if (walker == null) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 29, fontStyle = FontStyle.Bold };
                label = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
                small = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            }
            float scale = Mathf.Clamp(Screen.height / 800f, .8f, 1.8f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.color = new Color(.94f, .97f, .83f, .9f);
            if (showInfo && !walker.MenuOpen)
            {
                GUI.Label(new Rect(26, 20, 360, 44), "E R M O", title);
                GUI.Label(new Rect(28, 62, 450, 24), "um lugar entre lugar nenhum e casa", small);
                if (Time.unscaledTime - startedAt < 14)
                    GUI.Label(new Rect(28, height - 62, 650, 48), "WASD  caminhar    SHIFT  correr    MOUSE  olhar\nESC / F1  opções    F3  óptica    F4  pixelização / nativo    H  interface", small);
            }
            if (!walker.MenuOpen) return;
            GUI.color = Color.white;
            float panelHeight = Mathf.Min(600,height-24);
            var box = new Rect((width - 530) / 2, Mathf.Max(12, (height - panelHeight) / 2), 530, panelHeight);
            GUI.Box(box, GUIContent.none);
            GUILayout.BeginArea(new Rect(box.x + 26, box.y + 20, box.width - 52, box.height - 35));
            menuScroll = GUILayout.BeginScrollView(menuScroll);
            GUILayout.Label("E R M O  /  MUNDO", title);
            GUILayout.Label("Morros sem fim. Você já esteve aqui?", label);
            GUILayout.Space(20);
            GUILayout.Label("Seed do mundo", label);
            seedInput = GUILayout.TextField(seedInput, 100, GUILayout.Height(30));
            GUILayout.Label("A mesma seed reencontra os chunks salvos. Uma nova seed cria outro mundo sem apagar o anterior.", small);
            GUILayout.Space(12);
            bool nextReduced = GUILayout.Toggle(reducedMotion, " Movimento reduzido — sem balanço, lente ou variação de FOV");
            if (nextReduced != reducedMotion)
            {
                reducedMotion = nextReduced; walker.ReducedMotion = nextReduced; display.ReducedMotion = nextReduced;
            }
            bool pixels = GUILayout.Toggle(runtimeSettings.godotPixelGrid," Godot 640 × 360 / escala inteira (F4)");
            if (pixels != runtimeSettings.godotPixelGrid)
            {
                runtimeSettings.godotPixelGrid = pixels;
                if (!pixels) runtimeSettings.renderHeight = 0;
            }
            display.EffectsEnabled = GUILayout.Toggle(display.EffectsEnabled,runtimeSettings.foundFootage
                ? " Óptica found footage: lente, foco suave e cor (F3)" : " Óptica de memória: grão, dithering, lente (F3)");
            bool fog = GUILayout.Toggle(runtimeSettings.fogStyle == FogStyle.GodotExponential," Névoa Godot (desmarque para névoa só no horizonte)");
            if (fog != (runtimeSettings.fogStyle == FogStyle.GodotExponential))
            {
                runtimeSettings.fogStyle = fog ? FogStyle.GodotExponential : FogStyle.HorizonOnly;
                GodotAtmosphere.Apply(runtimeSettings);
            }
            GUILayout.Label("Volume — brisa e passos",small);
            runtimeSettings.audioVolume = GUILayout.HorizontalSlider(runtimeSettings.audioVolume,0,1);
            GUILayout.Space(14);
            GUI.enabled = !string.IsNullOrWhiteSpace(seedInput);
            if (GUILayout.Button("Entrar nesta seed (voltar ao início)", GUILayout.Height(34)))
            {
                StartWorld(seedInput); GUIUtility.ExitGUI();
            }
            GUI.enabled = true;
            if (GUILayout.Button("Continuar caminhando", GUILayout.Height(32)))
            {
                walker.MenuOpen = false; FirstPersonWalker.LockCursor(true);
            }
            GUILayout.Space(15);
            GUILayout.Label($"Chunk {world.CurrentChunk.x}, {world.CurrentChunk.y}  /  {world.LoadedCount} carregados", small);
            GUILayout.Label("Seed ativa: " + runtimeSettings.seed, small);
            GUILayout.Label(world.Store.LastError == null ? "Chunks persistentes • salvamento automático" : "Falha no salvamento: " + world.Store.LastError, small);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            if (runtimeSettings != null) Destroy(runtimeSettings);
            if (sky != null) Destroy(sky);
            FirstPersonWalker.LockCursor(false);
        }
    }
}
