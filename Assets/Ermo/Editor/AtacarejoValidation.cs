using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ermo.Editor
{
    public static class AtacarejoValidation
    {
        [MenuItem("Ermo/Validate Atacarejo Layout")]
        public static void Validate()
        {
            var a = new AtacarejoLayout("ermo-001", Vector2Int.zero);
            var b = new AtacarejoLayout("ermo-001", Vector2Int.zero);
            Require(a.Items.Count == b.Items.Count && a.Theme == b.Theme, "O setor (0,0) mudou com a mesma seed.");
            for (int i = 0; i < a.Items.Count; i++)
                Require(a.Items[i].World == b.Items[i].World && a.Items[i].Kind == b.Items[i].Kind && a.Items[i].Yaw == b.Items[i].Yaw,
                    "A planta do vestíbulo não é determinística.");
            Require(a.Clearance(new Vector2(AtacarejoLayout.Spawn.x, AtacarejoLayout.Spawn.z)) > .8f,
                "O spawn está a menos de 0,8 m de um sólido.");
            Require(a.Clearance(new Vector2(4f, 10f)) > 2f, "O centro do corredor não deixa o raio de 0,28 m passar.");
            Require(a.Clearance(new Vector2(4f, 0f)) > 2f, "A travessia em z = 0 está ocupada.");
            Require(Count(a, AtacarejoLayout.Kind.Portal) == 1, "O vestíbulo não tem o portal.");
            Require(Count(a, AtacarejoLayout.Kind.CaixaAtendimento) == 3, "O vestíbulo não tem as três caixas.");
            var cold = new AtacarejoLayout("ermo-001", AtacarejoLayout.ColdSector);
            var exit = new AtacarejoLayout("ermo-001", AtacarejoLayout.ExitSector);
            Require(Count(cold, AtacarejoLayout.Kind.PortaCamara) == 1 && Count(cold, AtacarejoLayout.Kind.PlacaSetor) == 1,
                "O Setor 13 perdeu a câmara.");
            Require(Count(exit, AtacarejoLayout.Kind.PortaSaida) == 1, "A saída não está no setor (0,2).");
            Require(Count(cold, AtacarejoLayout.Kind.PortaFrigorifico) == 1 && Count(cold, AtacarejoLayout.Kind.PortaFundo) == 1,
                "O frigorífico atrás da câmara 13 perdeu uma porta.");
            Require(Count(a, AtacarejoLayout.Kind.PortaCamara) == 0 && Count(a, AtacarejoLayout.Kind.PortaSaida) == 0,
                "Câmara ou saída vazou para o vestíbulo.");
            var east = new AtacarejoLayout("ermo-001", Vector2Int.right);
            var north = new AtacarejoLayout("ermo-001", Vector2Int.up);
            Require(Shared(a, east).Count == 0, "Gôndola duplicada na emenda (0,0)–(1,0).");
            Require(Shared(a, north).Count == 0, "Gôndola duplicada na emenda (0,0)–(0,1).");
            Require(AtacarejoLayout.IsCrossAisle(0f) && AtacarejoLayout.IsCrossAisle(32f) && !AtacarejoLayout.IsCrossAisle(8f),
                "A faixa da travessia mudou.");
            Require(AtacarejoLayout.SectorOf(15.9f, 0f) == Vector2Int.zero && AtacarejoLayout.SectorOf(16f, 0f) == Vector2Int.right,
                "A fronteira x = 16 não tem um dono só.");
            bool themeChanged = false;
            for (int i = 0; i < 12; i++)
                themeChanged |= new AtacarejoLayout("atacarejo-" + i, new Vector2Int(2, 2)).Theme != a.Theme;
            Require(themeChanged, "O miolo não muda com a seed.");
            var again = new AtacarejoLayout("atacarejo-7", new Vector2Int(-3, 4));
            var repeat = new AtacarejoLayout("atacarejo-7", new Vector2Int(-3, 4));
            Require(again.Items.Count == repeat.Items.Count, "Setor negativo mudou ao ser refeito.");
            ValidateRoutes("ermo-001");
            ValidateRoutes("atacarejo-7");
            Debug.Log("ERMO ATACAREJO LAYOUT PASSED: vestíbulo, Setor 13, frigorífico, saída, spawn, corredor, travessia, emenda, luzes e rotas.");
        }

        // Batch entry: Unity.exe -batchmode -executeMethod Ermo.Editor.AtacarejoValidation.ValidateBatch
        public static void ValidateBatch()
        {
            int code = 0;
            try { Validate(); }
            catch (System.Exception e) { Debug.LogError("ERMO ATACAREJO LAYOUT FAILED: " + e); code = 1; }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        // The walks the playable level depends on, checked against every solid
        // of every sector they cross, with the player capsule radius and margin.
        public static readonly Vector2[] ExitWalk =
        {
            new Vector2(0f, -8f), new Vector2(1.8f, -8f), new Vector2(1.8f, -2f), new Vector2(4f, -2f), new Vector2(4f, 61.3f)
        };
        public static readonly Vector2[] ColdWalk =
        {
            new Vector2(4f, 32f), new Vector2(100f, 32f), new Vector2(100f, 39f), new Vector2(104.6f, 39.6f),
            new Vector2(105.8f, 39f), new Vector2(111.3f, 39f)
        };

        static void ValidateRoutes(string seed)
        {
            var cache = new Dictionary<Vector2Int, AtacarejoLayout>();
            AtacarejoLayout Plan(Vector2Int cell)
            {
                if (!cache.TryGetValue(cell, out var plan)) cache[cell] = plan = new AtacarejoLayout(seed, cell);
                return plan;
            }
            float Clear(Vector2 p, bool ignoreDoor)
            {
                var home = AtacarejoLayout.SectorOf(p.x, p.y);
                float best = 99f;
                for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                    best = Mathf.Min(best, Plan(home + new Vector2Int(x, z)).Clearance(p, ignoreDoor));
                return best;
            }
            void Walk(Vector2[] points, bool ignoreDoor, string name)
            {
                for (int i = 0; i + 1 < points.Length; i++)
                {
                    float length = Vector2.Distance(points[i], points[i + 1]);
                    for (float d = 0f; d <= length; d += .25f)
                    {
                        var p = Vector2.Lerp(points[i], points[i + 1], d / Mathf.Max(length, .001f));
                        float c = Clear(p, ignoreDoor);
                        Require(c > .36f, $"{name} ({seed}) bloqueado em {p}: folga {c:0.00} m para a cápsula de 0,28 m.");
                    }
                }
            }
            Walk(ExitWalk, false, "Caminho até a saída");
            Walk(ColdWalk, true, "Caminho até a câmara 13");
            Require(Clear(new Vector2(AtacarejoLayout.ColdDoor.x, AtacarejoLayout.ColdDoor.z), false) < .05f, "A porta da câmara não fecha o vão.");
            Require(Clear(new Vector2(AtacarejoLayout.LockerDoor.x, AtacarejoLayout.LockerDoor.z), false) < .05f, "A porta do frigorífico não fecha o vão.");
            Require(Clear(new Vector2(AtacarejoLayout.NextDoor.x, AtacarejoLayout.NextDoor.z), false) < .05f, "A porta dos fundos não fecha o vão.");

            int exits = 0, plates = 0, cold = 0, coldSigns = 0;
            for (int x = -1; x <= 4; x++)
            for (int z = -1; z <= 3; z++)
            {
                var plan = Plan(new Vector2Int(x, z));
                foreach (var item in plan.Items)
                {
                    if (item.Kind == AtacarejoLayout.Kind.PortaSaida) exits++;
                    if (item.Kind == AtacarejoLayout.Kind.PlacaSaida) plates++;
                    if (item.Kind == AtacarejoLayout.Kind.Placa13) coldSigns++;
                    if (item.Kind != AtacarejoLayout.Kind.Luminaria) continue;
                    var lamp = (AtacarejoLayout.Lamp)item.Variant;
                    if (lamp == AtacarejoLayout.Lamp.Cold) { cold++; continue; }
                    bool vestibule = plan.Sector == Vector2Int.zero && Mathf.Abs(item.World.x) < .01f && Mathf.Abs(item.World.z + 6f) < .01f;
                    bool exitLamp = lamp == AtacarejoLayout.Lamp.Exit;
                    Require(vestibule || exitLamp || AtacarejoLayout.OnPath(item.World.x, item.World.z),
                        $"Luz acesa fora da fileira da saída em {item.World}.");
                }
            }
            Require(exits == 1 && plates == 1, $"A saída deveria ser uma porta com uma placa: {exits} portas, {plates} placas.");
            Require(cold == 7, $"A rota fria até o Setor 13 tem {cold} luzes, esperava 7.");
            Require(coldSigns == 2, $"Placas do 13: {coldSigns}, esperava 2.");
            for (float z = AtacarejoLayout.PathStartZ; z <= AtacarejoLayout.PathEndZ; z += 8f)
            {
                var plan = Plan(AtacarejoLayout.SectorOf(AtacarejoLayout.PathX, z));
                bool lit = false;
                foreach (var item in plan.Items)
                    lit |= item.Kind == AtacarejoLayout.Kind.Luminaria && Mathf.Abs(item.World.x - AtacarejoLayout.PathX) < .01f &&
                           Mathf.Abs(item.World.z - z) < .01f;
                Require(lit, $"Falta luz acesa na fileira em z = {z}.");
            }
        }

        static int Count(AtacarejoLayout layout, AtacarejoLayout.Kind kind)
        {
            int count = 0;
            foreach (var item in layout.Items) if (item.Kind == kind) count++;
            return count;
        }

        static List<Vector2> Shared(AtacarejoLayout left, AtacarejoLayout right)
        {
            var hits = new List<Vector2>();
            foreach (var a in left.Items)
            {
                if (a.Kind != AtacarejoLayout.Kind.Gondola && a.Kind != AtacarejoLayout.Kind.Piso) continue;
                foreach (var b in right.Items)
                    if (b.Kind == a.Kind && (a.World - b.World).sqrMagnitude < .01f)
                        hits.Add(new Vector2(a.World.x, a.World.z));
            }
            return hits;
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new System.InvalidOperationException(message);
        }
    }
}
