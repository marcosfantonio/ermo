using UnityEngine;

namespace Ermo
{
    // The supermarket PA. A chime, an unintelligible voice and a subtitle.
    // Lines fire once at places along the routes, when the player is lost,
    // after the SAÍDA door sends them back, and now and then for no reason.
    // Also draws the short controls hint on arrival.
    public sealed class AtacarejoAnnouncer : MonoBehaviour
    {
        enum Cue { Arrival, Junction, ColdNear, ColdInside, Locker, Exit, Count }

        // One line per Cue, same order.
        static readonly string[] cueLines =
        {
            "Atenção, clientes: a saída acompanha as luzes acesas.",
            "Setor treze, câmara fria: siga a luz azul. A saída continua em frente.",
            "Reposição na câmara treze. A porta está destrancada.",
            "Setor treze. Ninguém chamou o senhor aqui.",
            "Açougue fechado. Quem passa dessa porta não volta pelo mesmo corredor.",
            "Obrigado pela preferência. Volte sempre."
        };

        // After each trip through the SAÍDA door. The last one repeats.
        static readonly string[] loopLines =
        {
            "Bem-vindo ao Atacarejo. A saída de clientes está temporariamente fechada.",
            "Clientes não saem pela frente. Entrada de funcionários: setor treze, câmara fria.",
            "Bem-vindo ao Atacarejo. Bem-vindo ao Atacarejo. Setor treze."
        };

        static readonly string[] ambientLines =
        {
            "Caixa três chama o cliente pelo nome. Ninguém respondeu.",
            "Promoção válida enquanto durar o corredor.",
            "Pedimos que não corra entre as gôndolas.",
            "Funcionário da reposição, comparecer ao setor treze.",
            "Atenção: o horário de funcionamento foi estendido. Indefinidamente.",
            "O mercado fecha quando a última luz apagar."
        };

        const string LostLine = "Cliente, por favor, retorne às luzes acesas.";
        const string HintText = "As luzes acesas levam à saída.\nE  abrir portas";
        const float LostDistance = 22f;
        const float HintSeconds = 9f;

        static bool hintShown;
        readonly bool[] said = new bool[(int)Cue.Count];
        AtacarejoWorld world;
        FirstPersonWalker walker;
        AudioSource chime, voice;
        System.Func<float> volume;
        string line;
        float startedAt, lineStarts, lineUntil, lastLine = -999f, lastLost = -999f, nextAmbient, hintStart = -1f;
        int ambientIndex;
        GUIStyle captionStyle, subtitleStyle, hintStyle;

        public int LinesSpoken { get; private set; }
        public bool Speaking => line != null;
        // The subtitle on screen right now, or null.
        public string Line => line != null && Time.time >= lineStarts ? line : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => hintShown = false;

        public void Begin(AtacarejoWorld market, FirstPersonWalker player, System.Func<float> masterVolume)
        {
            world = market;
            walker = player;
            volume = masterVolume;
            if (chime == null) CreateSources();
            for (int i = 0; i < said.Length; i++) said[i] = false;
            line = null;
            startedAt = Time.time;
            nextAmbient = Time.time + 70f;
            if (!hintShown) { hintShown = true; hintStart = Time.unscaledTime + 2.6f; }
        }

        // The player was sent back to the vestibule by the SAÍDA door.
        public void AnnounceLoop(int loopsSoFar)
        {
            said[(int)Cue.Arrival] = true;
            said[(int)Cue.Exit] = false;
            lastLine = -999f;
            Say(loopLines[Mathf.Min(loopsSoFar, loopLines.Length - 1)]);
        }

        public void Silence()
        {
            line = null;
            if (voice != null) voice.Stop();
        }

        // The chime pitched down, used when the staff door closes behind you.
        public void LowChime()
        {
            Silence();
            if (chime.clip == null) return;
            chime.pitch = .62f;
            chime.Play();
        }

        void CreateSources()
        {
            chime = AtacarejoAudio.Source(transform, "Chamada do alto-falante", AtacarejoSounds.Chime, false);
            var chimeEcho = chime.gameObject.AddComponent<AudioEchoFilter>();
            chimeEcho.delay = 210f; chimeEcho.decayRatio = .35f; chimeEcho.wetMix = .45f; chimeEcho.dryMix = 1f;
            voice = AtacarejoAudio.Source(transform, "Alto-falante", AtacarejoSounds.Babble, false);
            var echo = voice.gameObject.AddComponent<AudioEchoFilter>();
            echo.delay = 210f; echo.decayRatio = .38f; echo.wetMix = .5f; echo.dryMix = 1f;
        }

        void Update()
        {
            if (world == null || walker == null) return;
            float master = volume != null ? volume() : .7f;
            chime.volume = master * .55f;
            voice.volume = master * .42f;
            if (voice.isPlaying && Time.time > lineUntil - .4f) voice.Stop();
            if (line != null && Time.time > lineUntil) line = null;
            if (line != null || walker.MenuOpen || world.Looping) return;
            PickLine();
        }

        void PickLine()
        {
            Vector3 p = world.LayoutPosition(walker.transform.position);
            var flat = new Vector2(p.x, p.z);
            var coldDoor = new Vector2(AtacarejoLayout.ColdAisleX, AtacarejoLayout.ColdDoor.z);
            var exitDoor = new Vector2(AtacarejoLayout.ExitDoor.x, AtacarejoLayout.ExitDoor.z);
            bool onJunction = Mathf.Abs(p.x - AtacarejoLayout.PathX) < 6f && p.z > 21f && p.z < 34f;

            if (!said[(int)Cue.Arrival] && Time.time - startedAt > 5f) Speak(Cue.Arrival);
            else if (!said[(int)Cue.Junction] && onJunction) Speak(Cue.Junction);
            else if (!said[(int)Cue.Locker] && AtacarejoLayout.Locker.Contains(flat)) Speak(Cue.Locker);
            else if (!said[(int)Cue.ColdInside] && AtacarejoLayout.ColdRoom.Contains(flat)) Speak(Cue.ColdInside);
            else if (!said[(int)Cue.ColdNear] && Vector2.Distance(flat, coldDoor) < 13f) Speak(Cue.ColdNear);
            else if (!said[(int)Cue.Exit] && Vector2.Distance(flat, exitDoor) < 9f) Speak(Cue.Exit);
            else if (Time.time - lastLost > 80f && Time.time - lastLine > 20f && RouteDistance(flat) > LostDistance)
            {
                lastLost = Time.time;
                Say(LostLine);
            }
            else if (Time.time > nextAmbient && Time.time - lastLine > 30f)
            {
                nextAmbient = Time.time + Random.Range(55f, 85f);
                Say(ambientLines[ambientIndex++ % ambientLines.Length]);
            }
        }

        void Speak(Cue cue)
        {
            said[(int)cue] = true;
            Say(cueLines[(int)cue]);
        }

        void Say(string text)
        {
            line = text;
            LinesSpoken++;
            lastLine = Time.time;
            lineStarts = Time.time + .9f;
            lineUntil = lineStarts + Mathf.Clamp(text.Length * .065f, 3.2f, 6.5f);
            if (chime.clip != null) { chime.pitch = 1f; chime.Play(); }
            if (voice.clip == null) return;
            voice.time = Random.Range(0f, Mathf.Max(0f, voice.clip.length - 7f) + .01f);
            voice.pitch = Random.Range(.94f, 1.04f);
            voice.PlayDelayed(1.05f);
        }

        // Distance to the nearest route (lit row, cold route, locker lane), in plan metres.
        static float RouteDistance(Vector2 p)
        {
            float x = AtacarejoLayout.PathX, coldX = AtacarejoLayout.ColdAisleX, coldZ = AtacarejoLayout.ColdRouteZ;
            float lane = AtacarejoLayout.LockerDoor.z;
            float best = Segment(p, new Vector2(x, -12f), new Vector2(x, AtacarejoLayout.ExitDoor.z));
            best = Mathf.Min(best, Segment(p, new Vector2(x, coldZ), new Vector2(coldX, coldZ)));
            best = Mathf.Min(best, Segment(p, new Vector2(coldX, coldZ), new Vector2(coldX, lane)));
            return Mathf.Min(best, Segment(p, new Vector2(coldX, lane), new Vector2(AtacarejoLayout.NextDoor.x, lane)));
        }

        static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, .0001f));
            return Vector2.Distance(p, a + ab * t);
        }

        void OnGUI()
        {
            if (world == null || walker == null || walker.MenuOpen) return;
            float scale = Mathf.Clamp(Screen.height / 800f, .8f, 1.8f);
            var matrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            if (hintStyle == null)
            {
                hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
                captionStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.UpperCenter, wordWrap = true };
            }
            DrawHint(height);
            DrawSubtitle(width, height);
            GUI.matrix = matrix;
            GUI.color = Color.white;
        }

        // Same voice as the meadow's controls line: small, bottom left, gone in seconds.
        void DrawHint(float height)
        {
            if (hintStart < 0f) return;
            float t = Time.unscaledTime - hintStart;
            if (t < 0f) return;
            if (t > HintSeconds) { hintStart = -1f; return; }
            float alpha = Mathf.Clamp01(t / .8f) * Mathf.Clamp01((HintSeconds - t) / 1.2f);
            GUI.color = new Color(.94f, .97f, .83f, .9f * alpha);
            GUI.Label(new Rect(28, height - 100, 650, 34), HintText, hintStyle);
        }

        void DrawSubtitle(float width, float height)
        {
            if (Line == null) return;
            float fade = Mathf.Clamp01((lineUntil - Time.time) / .6f) * Mathf.Clamp01((Time.time - lineStarts) / .3f);
            var box = new Rect(width * .18f, height * .8f, width * .64f, 60f);
            GUI.color = new Color(0f, 0f, 0f, .55f * fade);
            GUI.DrawTexture(new Rect(box.x - 10f, box.y - 6f, box.width + 20f, box.height + 8f), Texture2D.whiteTexture);
            GUI.color = new Color(.72f, .80f, .66f, .9f * fade);
            GUI.Label(new Rect(box.x, box.y - 2f, box.width, 16f), "ALTO-FALANTE", captionStyle);
            GUI.color = new Color(.94f, .97f, .86f, fade);
            GUI.Label(new Rect(box.x, box.y + 15f, box.width, box.height - 15f), line, subtitleStyle);
        }
    }
}
