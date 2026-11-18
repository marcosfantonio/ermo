using UnityEditor;
using UnityEditor.SceneManagement;

namespace Ermo.Editor
{
    // Menu Ermo > Jogar o Atacarejo: opens the market scene and presses Play.
    // The phase builds its own player, so it runs on its own.
    public static class AtacarejoStart
    {
        [MenuItem("Ermo/Jogar o Atacarejo")]
        static void Play()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Ermo.AtacarejoWorld.ScenePath);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Ermo/Jogar o Atacarejo", true)]
        static bool CanPlay() => !EditorApplication.isPlaying;
    }
}
