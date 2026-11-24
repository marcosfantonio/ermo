using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

namespace Ermo
{
    // Development shortcuts available in every phase: 1 = sítios, 2 = balneário,
    // 3 = atacarejo. The balneário is additive to the sítios, so entering it
    // from the atacarejo first loads the sítios and then uses its water passage.
    public sealed class PhaseHotkeys : MonoBehaviour
    {
        private const string Sitios = "Assets/Ermo/Scenes/Sitios.unity";
        private const string Atacarejo = AtacarejoWorld.ScenePath;
        private static bool pendingBalnearioEntry;
        private FirstPersonWalker walker;

        public void Initialize(FirstPersonWalker player) => walker = player;

        private void Update()
        {
            if (walker == null || walker.MenuOpen || PhaseTransition.Busy || Keyboard.current == null) return;
            var keyboard = Keyboard.current;
            if (Pressed(keyboard.digit1Key, keyboard.numpad1Key)) Load(Sitios);
            else if (Pressed(keyboard.digit2Key, keyboard.numpad2Key)) EnterBalneario();
            else if (Pressed(keyboard.digit3Key, keyboard.numpad3Key)) Load(Atacarejo);
        }

        private void EnterBalneario()
        {
            var passage = walker.GetComponentInParent<WaterPassage>();
            if (passage != null)
            {
                passage.JumpToWaterWorld();
                return;
            }

            // Atacarejo has no water passage. Return to its host phase first;
            // ErmoBootstrap completes this request once the player is created.
            pendingBalnearioEntry = true;
            Load(Sitios);
        }

        private static void Load(string path)
        {
            if (PhaseTransition.Busy || SceneManager.GetActiveScene().path == path) return;
            PhaseTransition.Go(new PhaseDestination { scenePath = path, useSceneSpawn = true }, Color.black);
        }

        private static bool Pressed(KeyControl topRow, KeyControl keypad) =>
            topRow.wasPressedThisFrame || keypad.wasPressedThisFrame;

        public static void CompletePendingBalnearioEntry(WaterPassage passage)
        {
            if (!pendingBalnearioEntry || passage == null) return;
            pendingBalnearioEntry = false;
            passage.JumpToWaterWorld();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => pendingBalnearioEntry = false;
    }
}
