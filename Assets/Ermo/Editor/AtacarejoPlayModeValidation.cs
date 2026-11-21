using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ermo.Editor
{
    // Plays the whole phase in a real Play Mode session, from Atacarejo.unity:
    // 1. the scene builds its own player in the vestibule,
    // 2. the lit row leads to SAÍDA, which sends the player back (shoe steps checked),
    // 3. open, enter, leave and close cold room 13,
    // 4. the meat locker behind it, up to the staff entrance,
    // 5. E there loads the next phase (the meadow by default).
    // The player walks with its own CharacterController; E goes through the
    // same centre-screen raycast as the keyboard. Frames come from the
    // Dreamcore output (what the screen shows) into Logs/Atacarejo/.
    [InitializeOnLoad]
    public static class AtacarejoPlayModeValidation
    {
        const string Key = "Ermo.AtacarejoSmoke";
        const string ShotDir = "Logs/Atacarejo";
        const float WalkSpeed = 5f;
        static IEnumerator<object> run;
        static string failure;
        static float waitUntil;
        static bool finishing;
        static readonly StringBuilder report = new StringBuilder();
        static FirstPersonWalker walker;
        static AtacarejoWorld market;
        static PixelDisplay display;
        static AtacarejoUse use;

        static AtacarejoPlayModeValidation()
        {
            if (SessionState.GetBool(Key, false)) Attach();
        }

        static void Attach()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= Capture;
            Application.logMessageReceived += Capture;
        }

        [MenuItem("Ermo/Test Atacarejo Play Mode")]
        public static void RunFromMenu() => Begin();

        // Unity.exe -batchmode -projectPath ... -executeMethod Ermo.Editor.AtacarejoPlayModeValidation.RunBatch
        public static void RunBatch()
        {
            try { Begin(); }
            catch (Exception e)
            {
                Debug.LogError("ERMO ATACAREJO PLAYMODE FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static void Begin()
        {
            AtacarejoValidation.Validate();
            EditorSceneManager.OpenScene(AtacarejoWorld.ScenePath);
            Directory.CreateDirectory(ShotDir);
            report.Clear();
            run = null; failure = null; finishing = false; waitUntil = 0f;
            SessionState.SetBool(Key, true);
            SessionState.SetInt(Key + ".exit", -1);
            SessionState.SetFloat(Key + ".start", (float)EditorApplication.timeSinceStartup);
            Attach();
            EditorApplication.isPlaying = true;
        }

        static void Capture(string condition, string trace, LogType type)
        {
            if (trace != null && trace.Contains("UnityEditor.Search.")) return;
            if (!EditorApplication.isPlaying || finishing) return;
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && failure == null)
                failure = condition + "\n" + trace;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Key, false)) return;
            int exit = SessionState.GetInt(Key + ".exit", -1);
            if (exit >= 0)
            {
                if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
                SessionState.SetBool(Key, false);
                EditorApplication.update -= Tick;
                Application.logMessageReceived -= Capture;
                if (Application.isBatchMode) EditorApplication.Exit(exit);
                return;
            }
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".start", 0f) > 420f)
            {
                End("timeout de 420 s");
                return;
            }
            if (!EditorApplication.isPlaying) return;
            if (failure != null) { End(failure); return; }
            if (run == null) run = Sequence();
            if (Time.time < waitUntil) return;
            try
            {
                if (!run.MoveNext()) { End(null); return; }
                if (run.Current is float seconds) waitUntil = Time.time + seconds;
            }
            catch (Exception e) { End(e.Message + "\n" + e.StackTrace); }
        }

        static void End(string error)
        {
            finishing = true;
            File.WriteAllText(Path.Combine(ShotDir, "relatorio.txt"), report.ToString() + (error == null ? "PASSOU\n" : "FALHOU: " + error + "\n"));
            if (error == null) Debug.Log("ERMO ATACAREJO PLAYMODE PASSED:\n" + report);
            else Debug.LogError("ERMO ATACAREJO PLAYMODE FAILED: " + error + "\n" + report);
            SessionState.SetInt(Key + ".exit", error == null ? 0 : 1);
            run = null;
            EditorApplication.isPlaying = false;
        }

        static void Note(string text)
        {
            report.AppendLine(text);
            Debug.Log("[Atacarejo] " + text);
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        static IEnumerator<object> Sequence()
        {
            // 1. Atacarejo.unity starts alone and builds its own player.
            float started = Time.time;
            while (true)
            {
                walker = UnityEngine.Object.FindAnyObjectByType<FirstPersonWalker>();
                market = AtacarejoWorld.Instance;
                if (walker != null && market != null && market.Started && !walker.WakingUp) break;
                Require(Time.time - started < 25f, "O Atacarejo não montou o jogador sozinho.");
                yield return null;
            }
            display = UnityEngine.Object.FindAnyObjectByType<PixelDisplay>();
            use = walker.GetComponent<AtacarejoUse>();
            Require(display != null && use != null, "Tela Dreamcore ou AtacarejoUse ausente no jogador do mercado.");
            Require(UnityEngine.Object.FindAnyObjectByType<WaterPassage>() == null && UnityEngine.Object.FindAnyObjectByType<WaterImmersionEffects>() == null,
                "O mercado não deve depender da água do Sítio (WaterPassage / efeito molhado).");
            Require(UnityEngine.Object.FindObjectsByType<FirstPersonWalker>().Length == 1, "Há mais de um jogador.");
            Require(walker.View.isActiveAndEnabled, "A câmera do olho está desligada no mercado.");
            Require(walker.View.clearFlags == CameraClearFlags.SolidColor && RenderSettings.skybox == null, "Céu aparecendo no mercado.");
            Require(!Bluish(walker.View.backgroundColor) && !Bluish(RenderSettings.fogColor), "O fundo do mercado está azul.");
            Vector3 spawn = market.LayoutPosition(walker.transform.position);
            Require(Vector2.Distance(Flat(spawn), Flat(AtacarejoLayout.Spawn)) < .6f, $"Nasceu fora do vestíbulo: {spawn}.");
            Note($"Vestíbulo: a cena montou o próprio jogador em {spawn:F2}, setor {market.CurrentSector}.");
            Hold();
            Look(0f, -3f);
            yield return 1.5f;
            Shot("01_vestibulo", .06f);

            // 2. Follow the lights to the SAÍDA door. It sends you back.
            int stepsBefore = market.Sound.StepsPlayed;
            foreach (var step in WalkTo(new Vector2(1.8f, -8f), new Vector2(1.8f, -2f), new Vector2(4f, -2f), new Vector2(4f, 12f))) yield return step;
            Require(market.Sound.StepsPlayed - stepsBefore >= 8, $"Poucos passos de sapato: {market.Sound.StepsPlayed - stepsBefore} em 22 m.");
            Note($"Passos: {market.Sound.StepsPlayed - stepsBefore} passos de sapato em 22 m de corredor.");
            Look(0f, -3f); yield return 1f;
            Shot("02_corredor_aceso", .06f);
            Look(90f, -3f); yield return 1f;
            Shot("03_corredor_lateral_apagado", .015f);
            foreach (var step in WalkTo(new Vector2(4f, 26f))) yield return step;
            Look(0f, 6f); yield return 1f;
            Shot("04_placas_saida_e_13", .06f);
            foreach (var step in WalkTo(new Vector2(4f, 32f))) yield return step;
            Look(90f, -2f); yield return 1.2f;
            Shot("05_rota_azul_para_13", .03f);
            foreach (var step in WalkTo(new Vector2(4f, 57.4f))) yield return step;
            Look(0f, 4f); yield return 1.2f;
            Shot("06_saida", .08f);
            foreach (var step in WalkTo(new Vector2(4f, 59.6f))) yield return step;
            Look(0f, 4f); yield return .3f;
            var exitDoor = use.Probe(false) as AtacarejoExitDoor;
            Require(exitDoor != null && exitDoor.Destination == AtacarejoExitDoor.Leads.BackToVestibule && use.PromptText == "Aperte E para sair",
                "A porta SAÍDA não oferece \"Aperte E para sair\".");
            int loops = market.Loops;
            Require(use.Activate(), "A porta SAÍDA recusou o E.");
            yield return null;
            Require(market.Looping, "A porta SAÍDA não escureceu a tela.");
            started = Time.time;
            while (market.Looping)
            {
                Require(Time.time - started < 8f, "A volta pela porta SAÍDA não terminou.");
                yield return null;
            }
            Vector3 looped = market.LayoutPosition(walker.transform.position);
            Require(market.Loops == loops + 1, "A volta não foi contada.");
            Require(Vector2.Distance(Flat(looped), Flat(AtacarejoLayout.Spawn)) < .6f, $"A porta SAÍDA não devolveu ao vestíbulo: {looped:F2}.");
            Require(market.IsSectorReady(AtacarejoLayout.SectorOf(looped.x, looped.z)), "O vestíbulo não estava pronto na volta.");
            Require(ScreenFade.Instance.Opacity <= 0f, "A tela ficou escura depois da volta.");
            Hold(); Look(0f, -3f); yield return 1f;
            Shot("06b_de_volta_ao_vestibulo", .06f);
            Note($"Saída: \"Aperte E para sair\" devolve ao vestíbulo em {looped:F2}. Alto-falante: \"{market.Announcer.Line}\".");
            foreach (var step in WalkTo(new Vector2(1.8f, -8f), new Vector2(1.8f, -2f), new Vector2(4f, -2f), new Vector2(4f, 32f))) yield return step;

            // 3. Cold room 13: open, look, enter, leave, close.
            foreach (var step in WalkTo(new Vector2(100f, 32f), new Vector2(100.4f, 39f))) yield return step;
            Look(90f, 0f); yield return 1.2f;
            Shot("07_porta_13_fechada", .05f);
            var door = use.Probe(false) as ColdDoor;
            Require(door != null && use.PromptText == "Aperte E para abrir a câmara", "A porta da câmara 13 não responde ao E.");
            use.Activate();
            yield return 1.3f;
            Require(door.IsOpen && door.Angle > 85f, $"A porta abriu só {door.Angle:0} graus.");
            Require(!Physics.Raycast(walker.View.transform.position, walker.View.transform.forward, out var leaf, 1.2f) ||
                    leaf.collider.GetComponentInParent<ColdDoor>() == null, "A folha da porta ficou na frente da câmera.");
            Shot("08_porta_13_aberta", .06f);
            foreach (var step in WalkTo(new Vector2(104.6f, 39.6f))) yield return step;
            Look(100f, -6f); yield return 1.2f;
            Shot("09_dentro_da_camara_13", .07f);
            Look(270f, 0f); yield return .8f;
            Shot("10_olhando_de_dentro_para_fora", .03f);
            foreach (var step in WalkTo(new Vector2(100.4f, 39f))) yield return step;
            Look(90f, 0f); yield return .3f;
            Require(use.Probe(false) == door && use.PromptText == "Aperte E para fechar a câmara", "Não dá para fechar a câmara de fora.");
            use.Activate();
            yield return 1.3f;
            Require(!door.IsOpen && door.Angle < 1f, "A porta da câmara não fechou.");
            Note("Câmara 13: abriu com E, folha girou na dobradiça, entrou, saiu e fechou com E.");

            // 4. The meat locker behind the cold room, then the staff entrance.
            Require(use.Probe(false) == door, "A porta da câmara sumiu da mira.");
            use.Activate();
            yield return 1.3f;
            foreach (var step in WalkTo(new Vector2(104.6f, 39f), new Vector2(105.8f, 39f))) yield return step;
            Look(90f, 2f); yield return 1.2f;
            Shot("11_porta_frigorifico", .03f);
            var locker = use.Probe(false) as ColdDoor;
            Require(locker != null && locker != door && use.PromptText == "Aperte E para abrir o frigorífico", "A porta do frigorífico não responde ao E.");
            use.Activate();
            yield return 1.3f;
            Require(locker.IsOpen && locker.Angle > 85f, $"O frigorífico abriu só {locker.Angle:0} graus.");
            Shot("12_frigorifico_aberto", .03f);
            foreach (var step in WalkTo(new Vector2(108.4f, 39f))) yield return step;
            Look(90f, -4f); yield return 1.2f;
            Shot("13_dentro_do_frigorifico", .03f);
            foreach (var step in WalkTo(new Vector2(110.4f, 39f))) yield return step;
            Look(325f, 2f); yield return .8f;
            Shot("14_carcacas", .02f);
            Look(90f, 0f); yield return .8f;
            Shot("15_entrada_de_funcionarios", .03f);
            var staff = use.Probe(false) as AtacarejoExitDoor;
            Require(staff != null && staff.Destination == AtacarejoExitDoor.Leads.NextPhase && use.PromptText == "Aperte E para entrar",
                "A entrada de funcionários não oferece \"Aperte E para entrar\".");
            Note("Frigorífico: abriu com E atrás da câmara 13, atravessou o corredor de carcaças até a entrada de funcionários.");

            // 5. The staff entrance loads the next phase (the meadow by default).
            var next = UnityEngine.Object.FindAnyObjectByType<AtacarejoBootstrap>().NextPhase;
            int lines = market.Announcer.LinesSpoken;
            int transitions = PhaseTransition.Completed;
            Require(use.Activate(), "A entrada de funcionários recusou o E.");
            yield return null;
            Require(PhaseTransition.Busy && use.Probe(false) == null, "A transição não travou o E durante o fade.");
            started = Time.time;
            while (PhaseTransition.Busy)
            {
                Require(Time.time - started < 20f, "A transição para a próxima fase não terminou.");
                yield return null;
            }
            Require(PhaseTransition.Completed == transitions + 1, "A transição não foi concluída.");
            Require(SceneManager.GetActiveScene().path == next.scenePath, $"Cena ativa {SceneManager.GetActiveScene().path}, esperava {next.scenePath}.");
            Require(AtacarejoWorld.Instance == null, "O mercado continuou carregado depois da transição.");
            Note($"Transição: entrada de funcionários → {next} em {Time.time - started:0.0} s.");

            started = Time.time;
            while (true)
            {
                walker = UnityEngine.Object.FindAnyObjectByType<FirstPersonWalker>();
                if (walker != null && !walker.WakingUp) break;
                Require(Time.time - started < 15f, "A próxima fase não criou o jogador.");
                yield return null;
            }
            display = UnityEngine.Object.FindAnyObjectByType<PixelDisplay>();
            Require(UnityEngine.Object.FindObjectsByType<FirstPersonWalker>().Length == 1, "Há mais de um jogador na próxima fase.");
            int listeners = 0;
            foreach (var listener in UnityEngine.Object.FindObjectsByType<AudioListener>())
                if (listener.isActiveAndEnabled) listeners++;
            Require(listeners == 1, $"{listeners} AudioListeners ativos na próxima fase.");
            if (next.scenePath == PhaseDestination.SitioScene)
                Require(UnityEngine.Object.FindAnyObjectByType<ErmoBootstrap>() != null && RenderSettings.skybox != null, "O Sítio não abriu com céu.");
            Hold();
            Look(walker.transform.eulerAngles.y, -3f); yield return 2.5f;
            Shot("16_proxima_fase", .15f);
            Note($"Próxima fase: jogador em {walker.transform.position:F1}, 1 AudioListener. Falas do alto-falante no mercado: {lines}.");
        }

        // Scripted control: the walker's own Update needs a focused, locked
        // cursor, which a batch editor does not have. Movement still goes
        // through FirstPersonWalker.MoveCharacter and its CharacterController.
        static void Hold()
        {
            walker.enabled = false;
            walker.Handheld.enabled = false;
            walker.Handheld.transform.localPosition = Vector3.zero;
            walker.Handheld.transform.localRotation = Quaternion.identity;
        }

        static void Look(float yaw, float pitch)
        {
            walker.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            walker.ViewOrientation.localPosition = Vector3.up * 1.65f;
            walker.ViewOrientation.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        static IEnumerable<object> WalkTo(params Vector2[] points)
        {
            foreach (var target in points)
            {
                float best = float.MaxValue, progressAt = Time.time;
                while (true)
                {
                    Vector3 here = market.LayoutPosition(walker.transform.position);
                    Vector2 delta = target - Flat(here);
                    if (delta.magnitude < .3f) break;
                    if (delta.magnitude < best - .05f) { best = delta.magnitude; progressAt = Time.time; }
                    Require(Time.time - progressAt < 3f, $"Jogador bloqueado indo para {target}: parou em {here:F2}.");
                    Vector3 direction = new Vector3(delta.x, 0f, delta.y).normalized;
                    Look(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, -3f);
                    float speed = Mathf.Min(WalkSpeed, delta.magnitude / Mathf.Max(Time.deltaTime, .001f));
                    walker.MoveCharacter(direction * speed, Mathf.Min(Time.deltaTime, .05f));
                    CheckStreaming();
                    yield return null;
                }
            }
        }

        // The rule: any sector within 12 m of the feet is complete.
        static void CheckStreaming()
        {
            Vector3 feet = market.LayoutPosition(walker.transform.position);
            var cell = AtacarejoLayout.SectorOf(feet.x, feet.z);
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                var other = cell + new Vector2Int(x, z);
                Vector3 center = AtacarejoLayout.SectorOrigin(other);
                float dx = Mathf.Max(Mathf.Abs(feet.x - center.x) - AtacarejoLayout.Half, 0f);
                float dz = Mathf.Max(Mathf.Abs(feet.z - center.z) - AtacarejoLayout.Half, 0f);
                if (Mathf.Sqrt(dx * dx + dz * dz) < 8f)
                    Require(market.IsSectorReady(other), $"Setor {other} ainda incompleto com o jogador a {Mathf.Sqrt(dx * dx + dz * dz):0.0} m da borda (em {feet:F1}).");
            }
            Require(walker.transform.position.y > market.transform.position.y - 2f, "O jogador caiu do piso.");
        }

        static void Shot(string name, float minimumMean)
        {
            var frame = display.ProcessedFrame;
            Require(frame != null && frame.IsCreated(), "A tela Dreamcore não tem imagem.");
            var previous = RenderTexture.active;
            var image = new Texture2D(frame.width, frame.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = frame;
                image.ReadPixels(new Rect(0, 0, frame.width, frame.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(ShotDir, name + ".png"), image.EncodeToPNG());
                var pixels = image.GetPixels32();
                double sum = 0, r = 0, b = 0;
                int dark = 0;
                foreach (var p in pixels)
                {
                    float l = (.2126f * p.r + .7152f * p.g + .0722f * p.b) / 255f;
                    sum += l; r += p.r; b += p.b;
                    if (l < .03f) dark++;
                }
                float mean = (float)(sum / pixels.Length), darkShare = dark / (float)pixels.Length;
                float blue = (float)((b - r) / pixels.Length / 255.0);
                Note($"{name}: brilho médio {mean:0.000}, pixels quase pretos {darkShare:P0}, azul-vermelho {blue:+0.000;-0.000}, exposição {display.CameraSimulation.ExposureEV:+0.00;-0.00} EV.");
                Require(mean >= minimumMean && darkShare < .92f, $"Tela escura em {name}: brilho {mean:0.000} (mínimo {minimumMean:0.000}), {darkShare:P0} preto.");
                // Sky check only before the cold route: from 07 on, the blue of
                // room 13 and the red of the locker are on purpose.
                if (string.CompareOrdinal(name, "07") < 0) Require(blue < .06f, $"Azul de céu aparecendo no mercado em {name}.");
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        static Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);
        static bool Bluish(Color c) => c.b > c.r + .08f && c.b > c.g;
    }
}
