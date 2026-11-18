using UnityEngine;
using UnityEngine.InputSystem;

namespace Ermo
{
    // Entry point of the Atacarejo phase. Whatever loads Atacarejo.unity,
    // this builds the player with the Dreamcore camera (the same pieces the
    // meadow uses), places them at the arrival and starts the market.
    // Everything a designer may want to change is here, in the Inspector:
    // the arrival point and the phase the staff entrance leads to.
    public sealed class AtacarejoBootstrap : MonoBehaviour
    {
        [Header("Jogador")]
        [SerializeField] WorldSettings settings;
        [SerializeField] Shader lensShader;

        [Header("Chegada no mercado (coordenadas da planta)")]
        [Tooltip("Onde o jogador acorda. Padrão: vestíbulo, diante dos caixas. A porta SAÍDA também devolve para cá.")]
        [SerializeField] Vector3 arrival = AtacarejoLayout.Spawn;
        [SerializeField] float arrivalYaw;

        [Header("Próxima fase (entrada de funcionários, atrás da câmara 13)")]
        [SerializeField] PhaseDestination nextPhase = new PhaseDestination();

        WorldSettings runtimeSettings;
        CyberShotProfile runtimeProfile;
        FirstPersonWalker walker;
        bool ownsPlayer;
        GUIStyle pauseTitle, pauseText;

        public PhaseDestination NextPhase => nextPhase;
        public WorldSettings Settings => runtimeSettings != null ? runtimeSettings : settings;

        void Start()
        {
            var world = AtacarejoWorld.Instance;
            if (world == null || settings == null || lensShader == null)
            {
                Debug.LogError("Atacarejo: AtacarejoWorld, WorldSettings ou PixelLens ausente na cena.");
                enabled = false;
                return;
            }
            walker = FindAnyObjectByType<FirstPersonWalker>();
            if (walker == null) { walker = CreatePlayer(); ownsPlayer = true; }
            if (walker.GetComponent<AtacarejoUse>() == null) walker.gameObject.AddComponent<AtacarejoUse>();
            world.Begin(walker, Settings, arrival, arrivalYaw, nextPhase);
        }

        // Same rig as the meadow's ErmoBootstrap: walker, hands-free camera
        // motion, Dreamcore capture on its own output camera.
        FirstPersonWalker CreatePlayer()
        {
            runtimeSettings = Instantiate(settings);
            runtimeProfile = CyberShotProfile.CreateGameplayProfile();
            runtimeProfile.ApplyTo(runtimeSettings);
            var rig = new GameObject("Jogador e câmera");
            var body = new GameObject("Walker");
            body.transform.SetParent(rig.transform, false);
            body.AddComponent<CharacterController>();
            var player = body.AddComponent<FirstPersonWalker>();
            player.Initialize(runtimeSettings);
            body.AddComponent<HouseInteraction>().Initialize(player.View);
            rig.AddComponent<PixelDisplay>().Initialize(player.View, runtimeSettings, lensShader);
            return player;
        }

        // Esc / F1 pauses, like in the meadow (only for a player we built).
        void Update()
        {
            if (!ownsPlayer || walker == null || Keyboard.current == null) return;
            var keyboard = Keyboard.current;
            if (!keyboard.escapeKey.wasPressedThisFrame && !keyboard.f1Key.wasPressedThisFrame) return;
            walker.MenuOpen = !walker.MenuOpen;
            FirstPersonWalker.LockCursor(!walker.MenuOpen);
        }

        void OnGUI()
        {
            if (!ownsPlayer || walker == null || !walker.MenuOpen) return;
            if (pauseTitle == null)
            {
                pauseTitle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                pauseText = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            }
            GUI.color = new Color(0f, 0f, 0f, .6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, Screen.height * .42f, Screen.width, 40), "A T A C A R E J O", pauseTitle);
            GUI.Label(new Rect(0, Screen.height * .42f + 44, Screen.width, 24), "Esc  continuar", pauseText);
        }

        void OnDestroy()
        {
            if (runtimeSettings != null) Destroy(runtimeSettings);
            if (runtimeProfile != null) Destroy(runtimeProfile);
            if (ownsPlayer) FirstPersonWalker.LockCursor(false);
        }
    }
}
