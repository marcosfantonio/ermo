using System.Collections.Generic;
using UnityEngine;

namespace Ermo
{
    // Deterministic plan of one 32 m sector. Corridor positions are global.
    // The seed only changes the interior, never the aisle or a border module.
    public sealed class AtacarejoLayout
    {
        public const float SectorSize = 32f;
        public const float Half = 16f;
        public const float Inset = 4f;
        public const float AislePitch = 8f;
        public const float GondolaDepth = 1.2f;
        public const float GondolaStep = 2f;
        public const float RackStep = 2.2f;
        public const float CrossHalf = 2f;
        public static readonly Vector2Int ColdSector = new Vector2Int(3, 1);
        public static readonly Vector2Int ExitSector = new Vector2Int(0, 2);
        public static readonly Vector3 Spawn = new Vector3(0f, 0.1f, -8f);
        // The lit row. It starts at the vestibule and stops at the exit wall.
        public const float PathX = 4f;
        public const float PathStartZ = -8f;
        public const float PathEndZ = 56f;
        public static readonly Vector3 ExitDoor = new Vector3(4f, 0f, 62f);
        public static readonly Vector3 ColdDoor = new Vector3(102.9f, 0f, 39f);
        public const float ColdRouteZ = 32f;
        public const float ColdAisleX = 100f;
        // Room interior, wall faces included.
        public static readonly Rect ColdRoom = Rect.MinMaxRect(103f, 34.3f, 107.6f, 43.7f);
        // Behind the cold room: the meat locker. Its back door leaves the
        // market for the next phase (AtacarejoExitDoor, Leads.NextPhase).
        public static readonly Vector3 LockerDoor = new Vector3(107.7f, 0f, 39f);
        public static readonly Vector3 NextDoor = new Vector3(111.9f, 0f, 39f);
        public static readonly Rect Locker = Rect.MinMaxRect(107.8f, 34.3f, 111.8f, 43.7f);
        // Cold lamps every 16 m along the cross aisle, then up the last aisle.
        static readonly Vector2[] ColdLamps =
        {
            new Vector2(20f, 32f), new Vector2(36f, 32f), new Vector2(52f, 32f),
            new Vector2(68f, 32f), new Vector2(84f, 32f), new Vector2(100f, 32f), new Vector2(100f, 40f)
        };

        // Variant of Kind.Luminaria.
        public enum Lamp { Path = 0, Cold = 1, Exit = 2, Sick = 3 }

        public enum Kind
        {
            Piso, Forro, Luminaria, Pilar, Gondola, Bandeja, Testeira, Produto,
            Palete, Caixa, PortaPalete, Ilha, Freezer, Portal, CaixaAtendimento,
            Parede, PortaCamara, PlacaSetor, PlacaCorredor, PortaSaida,
            LuminariaMorta, PlacaSaida, Seta, Placa13, Evaporador,
            PortaFrigorifico, PortaFundo
        }

        public readonly struct Item
        {
            public readonly Kind Kind;
            public readonly Vector3 World;
            public readonly float Yaw;
            public readonly int Variant;
            public Item(Kind kind, Vector3 world, float yaw, int variant = 0)
            {
                Kind = kind; World = world; Yaw = yaw; Variant = variant;
            }
        }

        public Vector2Int Sector { get; }
        public int Theme { get; }
        public List<Item> Items { get; } = new List<Item>();

        public AtacarejoLayout(string seed, Vector2Int sector)
        {
            Sector = sector;
            Theme = (int)(WorldHash.Of(seed + "/atacarejo/" + sector.x + "/" + sector.y) % 4u);
            AddShell();
            AddAisles(seed);
            AddRacks(seed);
            AddPillarsAndSigns();
            AddLamps(seed);
            AddRouteSigns();
            if (sector == Vector2Int.zero) AddVestibule();
            if (sector == ColdSector) AddColdRoom();
            if (sector == ExitSector) AddExit();
        }

        public static bool IsCrossAisle(float worldZ)
        {
            float nearest = 32f * Mathf.Round(worldZ / 32f);
            return Mathf.Abs(worldZ - nearest) <= CrossHalf;
        }

        public static Vector2Int SectorOf(float x, float z) => new Vector2Int(
            Mathf.FloorToInt((x + Half) / SectorSize),
            Mathf.FloorToInt((z + Half) / SectorSize));

        public static Vector3 SectorOrigin(Vector2Int sector) => new Vector3(sector.x * SectorSize, 0f, sector.y * SectorSize);

        static bool Overlaps(float a0, float a1, float b0, float b1, float c0, float c1, float d0, float d1)
            => a0 < c1 && a1 > c0 && b0 < d1 && b1 > d0;

        static bool HitsCross(float z0, float z1)
        {
            float nearest = 32f * Mathf.Round(((z0 + z1) * 0.5f) / 32f);
            return z0 < nearest + CrossHalf && z1 > nearest - CrossHalf;
        }

        static bool HitsReserve(float x0, float x1, float z0, float z1) =>
            Overlaps(x0, x1, z0, z1, -10f, 10f, -12f, -2f) ||
            Overlaps(x0, x1, z0, z1, -6f, 10f, 58f, 70f) ||
            Overlaps(x0, x1, z0, z1, 102.5f, 112.05f, 34f, 44f);

        bool Interior(float x0, float x1, float z0, float z1)
        {
            float minX = Sector.x * SectorSize - Half + Inset;
            float maxX = Sector.x * SectorSize + Half - Inset;
            float minZ = Sector.y * SectorSize - Half + Inset;
            float maxZ = Sector.y * SectorSize + Half - Inset;
            return x0 >= minX && x1 <= maxX && z0 >= minZ && z1 <= maxZ;
        }

        bool Owns(float x, float z) => SectorOf(x, z) == Sector;

        void Add(Kind kind, float x, float y, float z, float yaw, int variant = 0)
            => Items.Add(new Item(kind, new Vector3(x, y, z), yaw, variant));

        void AddShell()
        {
            for (int ix = 0; ix < 8; ix++)
            for (int iz = 0; iz < 8; iz++)
            {
                float x = Sector.x * SectorSize - 14f + ix * 4f;
                float z = Sector.y * SectorSize - 14f + iz * 4f;
                Add(Kind.Piso, x, 0f, z, 0f);
                Add(Kind.Forro, x, 5.5f, z, 0f);
            }
        }

        void AddAisles(string seed)
        {
            int n0 = Mathf.FloorToInt((Sector.x * SectorSize - Half) / AislePitch) - 1;
            int n1 = Mathf.CeilToInt((Sector.x * SectorSize + Half) / AislePitch) + 1;
            int k0 = Mathf.FloorToInt((Sector.y * SectorSize - Half) / GondolaStep) - 1;
            int k1 = Mathf.CeilToInt((Sector.y * SectorSize + Half) / GondolaStep) + 1;
            for (int n = n0; n <= n1; n++)
            for (int side = 0; side < 2; side++)
            {
                float x0 = side == 0 ? n * AislePitch : n * AislePitch + 6.8f;
                float x1 = x0 + GondolaDepth;
                float yaw = side == 0 ? 0f : 180f;
                for (int k = k0; k <= k1; k++)
                {
                    float z0 = k * GondolaStep, z1 = z0 + GondolaStep;
                    if (!ShelfHere(x0, x1, z0, z1, true)) continue;
                    bool stocked = !Interior(x0, x1, z0, z1) || Theme == 0;
                    if (stocked) AddBay(seed, n, k, side, (x0 + x1) * 0.5f, (z0 + z1) * 0.5f, yaw, z0);
                    else if (Interior(x0, x1, z0, z1) && Theme == 2)
                        Add(k % 2 == 0 ? Kind.Ilha : Kind.Freezer, (x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f, yaw);
                    else if (Interior(x0, x1, z0, z1) && Theme == 3 && (WorldHash.Of(seed + ":vazio:" + n + ":" + k + ":" + side) % 5u) == 0)
                        Add(Kind.Caixa, (x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f, yaw, 1);
                }
            }
        }

        bool ShelfHere(float x0, float x1, float z0, float z1, bool gondola)
        {
            if (HitsCross(z0, z1) || HitsReserve(x0, x1, z0, z1)) return false;
            if (!Owns((x0 + x1) * 0.5f, z0 + 0.01f)) return false;
            if (!gondola && !Interior(x0, x1, z0, z1)) return false;
            return true;
        }

        void AddBay(string seed, int n, int k, int side, float x, float z, float yaw, float z0)
        {
            Add(Kind.Gondola, x, 0f, z, yaw);
            Add(Kind.Bandeja, x, 0.45f, z, yaw);
            Add(Kind.Bandeja, x, 1.05f, z, yaw);
            Add(Kind.Bandeja, x, 1.65f, z, yaw);
            if (!ShelfHere(x - 0.6f, x + 0.6f, z0 - GondolaStep, z0, true) && !IsCrossAisle(z0 - 0.075f))
                Add(Kind.Testeira, x, 0f, z0 - 0.075f, yaw);
            int count = (int)(WorldHash.Of(seed + ":baia:" + n + ":" + k + ":" + side) % 5u);
            for (int i = 0; i < count; i++)
            {
                float localZ = -0.7f + (i % 4) * 0.45f;
                float localX = 0.12f;
                float y = 0.49f + (i % 3) * 0.6f;
                Vector3 point = Rotate(x, z, yaw, localX, localZ);
                Add(Kind.Produto, point.x, y, point.z, yaw, i % 4);
            }
        }

        void AddRacks(string seed)
        {
            if (Theme != 1) return;
            int n0 = Mathf.FloorToInt((Sector.x * SectorSize - Half) / AislePitch) - 1;
            int n1 = Mathf.CeilToInt((Sector.x * SectorSize + Half) / AislePitch) + 1;
            int r0 = Mathf.FloorToInt((Sector.y * SectorSize - Half) / RackStep) - 1;
            int r1 = Mathf.CeilToInt((Sector.y * SectorSize + Half) / RackStep) + 1;
            for (int n = n0; n <= n1; n++)
            for (int side = 0; side < 2; side++)
            {
                float x0 = side == 0 ? n * AislePitch : n * AislePitch + 6.8f;
                float x1 = x0 + GondolaDepth;
                float yaw = side == 0 ? 0f : 180f;
                for (int r = r0; r <= r1; r++)
                {
                    float z0 = r * RackStep, z1 = z0 + RackStep;
                    if (!ShelfHere(x0, x1, z0, z1, false)) continue;
                    float x = (x0 + x1) * 0.5f, z = (z0 + z1) * 0.5f;
                    Add(Kind.PortaPalete, x, 0f, z, yaw);
                    if ((WorldHash.Of(seed + ":palete:" + n + ":" + r + ":" + side) % 3u) == 0)
                    {
                        Vector3 front = Rotate(x, z, yaw, 0.9f, 0f);
                        Add(Kind.Palete, front.x, 0f, front.z, yaw);
                        Add(Kind.Caixa, front.x, 0.14f, front.z, yaw);
                    }
                }
            }
        }

        void AddPillarsAndSigns()
        {
            float minX = Sector.x * SectorSize - Half - 1f, maxX = Sector.x * SectorSize + Half + 1f;
            float minZ = Sector.y * SectorSize - Half - 1f, maxZ = Sector.y * SectorSize + Half + 1f;
            for (float x = Mathf.Floor(minX / AislePitch) * AislePitch; x <= maxX; x += AislePitch)
            for (float z = Mathf.Floor(minZ / AislePitch) * AislePitch; z <= maxZ; z += AislePitch)
            {
                if (!Owns(x, z) || IsCrossAisle(z) || HitsReserve(x - 0.2f, x + 0.2f, z - 0.2f, z + 0.2f)) continue;
                Add(Kind.Pilar, x, 0f, z, 0f);
            }
            int n0 = Mathf.FloorToInt(minX / AislePitch) - 1;
            int n1 = Mathf.CeilToInt(maxX / AislePitch) + 1;
            for (int n = n0; n <= n1; n++)
            for (float z = Mathf.Round(minZ / 32f) * 32f - 32f; z <= maxZ + 32f; z += 32f)
            {
                float x = n * AislePitch + 4f;
                if (!Owns(x, z) || !IsCrossAisle(z)) continue;
                Add(Kind.PlacaCorredor, x + 2.45f, 3.15f, z, 90f, (int)Mathf.Repeat(n, 4));
            }
        }

        public static bool OnPath(float x, float z) =>
            Mathf.Abs(x - PathX) < .01f && z >= PathStartZ - .01f && z <= PathEndZ + .01f;

        public static bool OnColdRoute(float x, float z)
        {
            foreach (var lamp in ColdLamps)
                if (Mathf.Abs(lamp.x - x) < .01f && Mathf.Abs(lamp.y - z) < .01f) return true;
            return false;
        }

        // One fixture over every aisle every 8 m. Lit only on the path to the
        // exit and on the cold route. Everything else is a dead tube.
        void AddLamps(string seed)
        {
            float minX = Sector.x * SectorSize - Half - 1f, maxX = Sector.x * SectorSize + Half + 1f;
            float minZ = Sector.y * SectorSize - Half - 1f, maxZ = Sector.y * SectorSize + Half + 1f;
            int n0 = Mathf.FloorToInt(minX / AislePitch) - 1;
            int n1 = Mathf.CeilToInt(maxX / AislePitch) + 1;
            for (int n = n0; n <= n1; n++)
            for (float z = Mathf.Floor(minZ / 8f) * 8f; z <= maxZ; z += 8f)
            {
                float x = n * AislePitch + 4f;
                if (!Owns(x, z)) continue;
                // Ceiling fixtures collide with nothing. Reserves only drop the
                // dead ones, so the vestibule keeps its lit fixture at z = -8.
                if (!OnPath(x, z) && HitsReserve(x - 0.6f, x + 0.6f, z - 0.15f, z + 0.15f)) continue;
                uint hash = WorldHash.Of(seed + ":lampada:" + n + ":" + Mathf.RoundToInt(z / 8f));
                if (OnPath(x, z))
                {
                    bool sick = z > PathStartZ + 9f && z < PathEndZ - 1f && hash % 5u == 0u;
                    Add(Kind.Luminaria, x, 5.5f, z, 90f, (int)(sick ? Lamp.Sick : Lamp.Path));
                }
                else if (OnColdRoute(x, z)) Add(Kind.Luminaria, x, 5.5f, z, 90f, (int)Lamp.Cold);
                else
                {
                    // Rarely a dead aisle still twitches, far from the routes.
                    bool faint = Mathf.Abs(x - PathX) >= 16f && Mathf.Abs(z - ColdRouteZ) >= 8f && hash % 23u == 7u;
                    Add(Kind.LuminariaMorta, x, 5.5f, z, 90f, faint ? 1 : 0);
                }
            }
        }

        // Hanging signs. Green arrows point down the lit row. The blue 13 sign
        // points off it, to the cold room, so it never reads as the exit.
        void AddRouteSigns()
        {
            AddIfOwned(Kind.Seta, PathX, 3.3f, 12f, 90f);
            AddIfOwned(Kind.Seta, 3.1f, 3.3f, 29f, 90f);
            AddIfOwned(Kind.Placa13, 5.15f, 3.3f, 29f, 90f);
            AddIfOwned(Kind.Seta, PathX, 3.3f, 46f, 90f);
            AddIfOwned(Kind.Placa13, ColdAisleX - 1.5f, 3.3f, ColdRouteZ, 0f);
        }

        void AddIfOwned(Kind kind, float x, float y, float z, float yaw, int variant = 0)
        {
            if (Owns(x, z)) Add(kind, x, y, z, yaw, variant);
        }

        void AddVestibule()
        {
            Add(Kind.Portal, 0f, 0f, -12f, 90f);
            Add(Kind.CaixaAtendimento, -3.6f, 0f, -4.5f, 90f);
            Add(Kind.CaixaAtendimento, 0f, 0f, -4.5f, 90f);
            Add(Kind.CaixaAtendimento, 3.6f, 0f, -4.5f, 90f);
            Add(Kind.Luminaria, 0f, 5.5f, -6f, 90f, (int)Lamp.Path);
        }

        void AddColdRoom()
        {
            const float x = 102.9f;
            Add(Kind.Parede, x, 0f, 36.2f, 0f);
            Add(Kind.Parede, x, 0f, 41.8f, 0f);
            Add(Kind.PortaCamara, x, 0f, 39f, 180f);
            Add(Kind.Parede, 104.8f, 0f, 34.2f, 90f);
            Add(Kind.Parede, 106.4f, 0f, 34.2f, 90f);
            Add(Kind.Parede, 104.8f, 0f, 43.8f, 90f);
            Add(Kind.Parede, 106.4f, 0f, 43.8f, 90f);
            Add(Kind.Parede, 107.7f, 0f, 36.2f, 0f);
            Add(Kind.Parede, 107.7f, 0f, 41.8f, 0f);
            Add(Kind.PortaFrigorifico, LockerDoor.x, 0f, LockerDoor.z, 180f);
            Add(Kind.Ilha, 105.3f, 0f, 36.6f, 0f);
            Add(Kind.Freezer, 106.2f, 0f, 41.4f, 180f);
            Add(Kind.Caixa, 106.9f, 0f, 35.4f, 20f);
            Add(Kind.Caixa, 106.9f, 0.4f, 35.5f, -15f);
            Add(Kind.PlacaSetor, 102.55f, 2.7f, 39f, 180f);
            Add(Kind.Evaporador, 107.6f, 3.9f, 39f, 180f);
            AddLocker();
        }

        // Meat locker: 4 x 9.4 m, one lane down the middle at z = 39 from the
        // cold room to the back door. Carcasses and blood are built at run
        // time by AtacarejoBuilder (primitives, no model needed).
        void AddLocker()
        {
            Add(Kind.Parede, 109.8f, 0f, 34.2f, 90f);
            Add(Kind.Parede, 109.8f, 0f, 43.8f, 90f);
            Add(Kind.Parede, NextDoor.x, 0f, 36.2f, 0f);
            Add(Kind.Parede, NextDoor.x, 0f, 41.8f, 0f);
            Add(Kind.PortaFundo, NextDoor.x, 0f, NextDoor.z, 180f);
        }

        // The lit row ends at a wall with one door in it, centred on the row.
        void AddExit()
        {
            Add(Kind.PortaSaida, ExitDoor.x, 0f, ExitDoor.z, 90f);
            Add(Kind.Parede, 1.3f, 0f, ExitDoor.z, 90f);
            Add(Kind.Parede, -2.7f, 0f, ExitDoor.z, 90f);
            Add(Kind.Parede, 6.7f, 0f, ExitDoor.z, 90f);
            Add(Kind.PlacaSaida, ExitDoor.x, 2.4f, ExitDoor.z, 90f);
            Add(Kind.Luminaria, PathX, 5.5f, 59.5f, 90f, (int)Lamp.Exit);
        }

        static Vector3 Rotate(float x, float z, float yaw, float localX, float localZ)
        {
            float rad = yaw * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return new Vector3(x + localX * c + localZ * s, 0f, z - localX * s + localZ * c);
        }

        public float Clearance(Vector2 point) => Clearance(point, false);

        public float Clearance(Vector2 point, bool ignoreColdDoor)
        {
            float best = 99f;
            foreach (var item in Items)
            {
                if (!Solid(item.Kind)) continue;
                if (ignoreColdDoor && (item.Kind == Kind.PortaCamara || item.Kind == Kind.PortaFrigorifico)) continue;
                Extents(item, out float hx, out float hz);
                float dx = point.x - item.World.x, dz = point.y - item.World.z;
                float rad = item.Yaw * Mathf.Deg2Rad;
                float c = Mathf.Cos(-rad), s = Mathf.Sin(-rad);
                float lx = dx * c + dz * s, lz = -dx * s + dz * c;
                float ox = Mathf.Max(Mathf.Abs(lx) - hx, 0f), oz = Mathf.Max(Mathf.Abs(lz) - hz, 0f);
                best = Mathf.Min(best, Mathf.Sqrt(ox * ox + oz * oz));
            }
            return best;
        }

        static bool Solid(Kind kind)
        {
            switch (kind)
            {
                case Kind.Piso: case Kind.Forro: case Kind.Luminaria:
                case Kind.Bandeja: case Kind.PlacaSetor: case Kind.PlacaCorredor:
                case Kind.LuminariaMorta: case Kind.PlacaSaida: case Kind.Seta:
                case Kind.Placa13: case Kind.Evaporador:
                    return false;
                default: return true;
            }
        }

        static void Extents(Item item, out float hx, out float hz)
        {
            if (item.Kind == Kind.Portal)
            {
                // Jambs only. The opening stays empty. Yaw 90 turns local Z into world X.
                hx = 0.225f; hz = 0.15f;
                return;
            }
            switch (item.Kind)
            {
                case Kind.Gondola: hx = 0.6f; hz = 1f; break;
                case Kind.Testeira: hx = 0.6f; hz = 0.075f; break;
                case Kind.Pilar: hx = 0.2f; hz = 0.2f; break;
                case Kind.Produto: hx = 0.25f; hz = 0.2f; break;
                case Kind.Palete: hx = 0.6f; hz = 0.5f; break;
                case Kind.Caixa: hx = 0.3f; hz = 0.225f; break;
                case Kind.PortaPalete: hx = 0.55f; hz = 1.1f; break;
                case Kind.Ilha: hx = 0.5f; hz = 1f; break;
                case Kind.Freezer: hx = 0.375f; hz = 0.6f; break;
                case Kind.CaixaAtendimento: hx = 0.4f; hz = 0.8f; break;
                case Kind.Parede: hx = 0.1f; hz = 2f; break;
                case Kind.PortaCamara: case Kind.PortaFrigorifico: hx = 0.1f; hz = 0.8f; break;
                case Kind.PortaFundo: hx = 0.06f; hz = 0.8f; break;
                default: hx = 0.06f; hz = 0.7f; break;
            }
        }
    }
}
