using System.Collections;
using UnityEngine;

namespace Ermo
{
    // A full-screen colour drawn over everything (including the Dreamcore
    // screen), with an optional caption. One instance, created on demand and
    // kept across scene loads, so a fade can start in one phase and end in
    // the next.
    public sealed class ScreenFade : MonoBehaviour
    {
        static ScreenFade instance;
        Color color = Color.black;
        string caption;
        GUIStyle captionStyle;

        public float Opacity { get; private set; }

        public static ScreenFade Instance
        {
            get
            {
                if (instance != null) return instance;
                var host = new GameObject("Fade de tela");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<ScreenFade>();
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        // Animates the opacity to target (0 = clear, 1 = covered).
        public IEnumerator To(float target, float seconds, Color? tint = null)
        {
            if (tint.HasValue) color = tint.Value;
            float start = Opacity;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                Opacity = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            Opacity = target;
        }

        public void SetCaption(string text) => caption = text;

        void OnGUI()
        {
            if (Opacity <= 0f) return;
            GUI.depth = -1000;
            GUI.color = new Color(color.r, color.g, color.b, Opacity);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            if (!string.IsNullOrEmpty(caption))
            {
                if (captionStyle == null)
                    captionStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                captionStyle.fontSize = Mathf.RoundToInt(22 * Mathf.Clamp(Screen.height / 800f, .8f, 1.8f));
                GUI.color = new Color(.86f, .80f, .74f, Opacity);
                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), caption, captionStyle);
            }
            GUI.color = Color.white;
        }
    }
}
