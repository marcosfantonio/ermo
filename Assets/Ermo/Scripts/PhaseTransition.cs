using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ermo
{
    // Moves the player from one phase to another: fade out, load the next
    // scene in place of the current one, optionally place the player, fade in.
    // Every phase builds its own player when its scene starts, so phases can
    // come in any order and each can use its own trigger to call Go().
    public static class PhaseTransition
    {
        const float FadeOutSeconds = 1.6f;
        const float FadeInSeconds = 1.2f;
        const float WaitForPlayerSeconds = 5f;

        public static bool Busy { get; private set; }
        public static int Completed { get; private set; }

        // Raised after the next scene is loaded and the player placed.
        public static event Action<PhaseDestination> Arrived;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Busy = false;
            Completed = 0;
            Arrived = null;
        }

        public static bool Go(PhaseDestination destination, Color fadeColor)
        {
            if (Busy || destination == null || !destination.IsValid) return false;
            Busy = true;
            var fade = ScreenFade.Instance;
            fade.StartCoroutine(Run(fade, destination, fadeColor));
            return true;
        }

        static IEnumerator Run(ScreenFade fade, PhaseDestination destination, Color fadeColor)
        {
            yield return fade.To(1f, FadeOutSeconds, fadeColor);
            var load = SceneManager.LoadSceneAsync(destination.scenePath, LoadSceneMode.Single);
            if (load == null)
            {
                Debug.LogError($"Próxima fase não encontrada: {destination.scenePath}. Adicione a cena à Scene List do Build Profile.");
                yield return fade.To(0f, FadeInSeconds);
                Busy = false;
                yield break;
            }
            while (!load.isDone) yield return null;
            yield return null; // let the new scene's Start() build its player

            if (!destination.useSceneSpawn) yield return PlacePlayer(destination);

            Completed++;
            Arrived?.Invoke(destination);
            yield return fade.To(0f, FadeInSeconds);
            Busy = false;
        }

        static IEnumerator PlacePlayer(PhaseDestination destination)
        {
            FirstPersonWalker walker = null;
            for (float waited = 0f; walker == null && waited < WaitForPlayerSeconds; waited += Time.unscaledDeltaTime)
            {
                walker = UnityEngine.Object.FindAnyObjectByType<FirstPersonWalker>();
                if (walker == null) yield return null;
            }
            if (walker == null)
            {
                Debug.LogWarning($"Nenhum jogador em {destination.scenePath}; o spawn configurado foi ignorado.");
                yield break;
            }
            walker.SetSlidePose(destination.spawnPosition, destination.spawnYaw, -3f, 1.65f);
            Physics.SyncTransforms();
        }
    }
}
