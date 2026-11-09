using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Ermo
{
    [Serializable]
    public sealed class PropRecord
    {
        public string id;
        public string groupId;
        public Vector2 groupAnchor;
        public PropKind kind;
        public Vector3 position;
        public float rotation;
        public float scale = 1;
        // Reserved for persistent interactions, e.g. opening a particular door.
        public bool interacted;
    }

    [Serializable]
    public sealed class ChunkRecord
    {
        public int version = 1;
        public int x, z;
        public List<PropRecord> props = new List<PropRecord>();
    }

    [Serializable]
    internal sealed class WorldManifest
    {
        public int version = 1;
        public string seed;
        public int chunkSize;
        public int terrainResolution;
        public float hillHeight, hillScale, roadWidth;
        public float locationSpacing, minimumLocationDistance, locationChance;
        public float houseChance, waterTowerChance, turbineChance;
        public LandmarkRule[] landmarks;
        public TerrainStyle terrainStyle;
        public bool flattenYards;
        public Vector2 contourRepeat;
        public HillContour[] contours;
        public float contourHeightScale, contourWidthScale;
        public bool smoothTerrainNormals;
        public bool organicHills;
        public float terrainWarp, terrainWaveHeight;
    }

    // Never uses string.GetHashCode: its result is not stable across runtimes.
    public static class WorldHash
    {
        public static uint Of(string value)
        {
            uint hash = 2166136261;
            foreach (char c in value) { hash ^= c; hash *= 16777619; }
            return hash;
        }

        public static System.Random Random(string seed, int x, int z, string salt)
            => new System.Random(unchecked((int)Of($"{seed}:{x}:{z}:{salt}")));
    }

    public sealed class WorldStore
    {
        public string DirectoryPath { get; }
        public string LastError { get; private set; }
        public WorldStore(WorldSettings settings, string rootDirectory = null)
        {
            // SHA256 avoids path traversal, invalid filenames, and short hash collisions.
            using (var sha = SHA256.Create())
            {
                string key = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(settings.seed))).Replace("-", "").ToLowerInvariant();
                // New population layout: leave every original save untouched.
                DirectoryPath = Path.Combine(rootDirectory ?? Application.persistentDataPath, "Ermo", "worlds", key, "layout-6-organic-sites");
            }
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                string path = Path.Combine(DirectoryPath, "world.json");
                if (File.Exists(path))
                {
                    var manifest = JsonUtility.FromJson<WorldManifest>(File.ReadAllText(path));
                    if (manifest == null || manifest.version != 1 || manifest.seed != settings.seed)
                        throw new InvalidDataException("Unsupported world manifest.");
                    // Terrain parameters belong to the saved world, not current inspector values.
                    settings.chunkSize = manifest.chunkSize;
                    settings.terrainResolution = manifest.terrainResolution;
                    settings.hillHeight = manifest.hillHeight;
                    settings.hillScale = manifest.hillScale;
                    settings.roadWidth = manifest.roadWidth;
                    settings.locationSpacing = manifest.locationSpacing;
                    settings.minimumLocationDistance = manifest.minimumLocationDistance;
                    settings.locationChance = manifest.locationChance;
                    settings.houseChance = manifest.houseChance;
                    settings.waterTowerChance = manifest.waterTowerChance;
                    settings.turbineChance = manifest.turbineChance;
                    settings.landmarks = manifest.landmarks;
                    settings.terrainStyle = manifest.terrainStyle;
                    settings.flattenYards = manifest.flattenYards;
                    settings.contourRepeat = manifest.contourRepeat;
                    settings.contours = manifest.contours;
                    settings.contourHeightScale = manifest.contourHeightScale;
                    settings.contourWidthScale = manifest.contourWidthScale;
                    settings.smoothTerrainNormals = manifest.smoothTerrainNormals;
                    settings.organicHills = manifest.organicHills;
                    settings.terrainWarp = manifest.terrainWarp;
                    settings.terrainWaveHeight = manifest.terrainWaveHeight;
                }
                else Write(path, JsonUtility.ToJson(new WorldManifest
                {
                    seed = settings.seed, chunkSize = settings.chunkSize,
                    terrainResolution = settings.terrainResolution,
                    hillHeight = settings.hillHeight, hillScale = settings.hillScale,
                    roadWidth = settings.roadWidth,
                    locationSpacing = settings.locationSpacing, minimumLocationDistance = settings.minimumLocationDistance,
                    locationChance = settings.locationChance, houseChance = settings.houseChance,
                    waterTowerChance = settings.waterTowerChance, turbineChance = settings.turbineChance,
                    landmarks = settings.landmarks, terrainStyle = settings.terrainStyle,
                    flattenYards = settings.flattenYards, contourRepeat = settings.contourRepeat, contours = settings.contours,
                    contourHeightScale = settings.contourHeightScale, contourWidthScale = settings.contourWidthScale,
                    smoothTerrainNormals = settings.smoothTerrainNormals, organicHills = settings.organicHills,
                    terrainWarp = settings.terrainWarp, terrainWaveHeight = settings.terrainWaveHeight
                }, true));
            }
            catch (Exception e) { Report(e); }
        }

        public ChunkRecord Load(Vector2Int coordinate)
        {
            string path = ChunkPath(coordinate);
            if (!File.Exists(path)) return null;
            try
            {
                var record = JsonUtility.FromJson<ChunkRecord>(File.ReadAllText(path));
                if (record == null || record.version != 1 || record.x != coordinate.x || record.z != coordinate.y || record.props == null)
                    throw new InvalidDataException("Invalid chunk: " + path);
                return record;
            }
            catch (Exception e)
            {
                Report(e);
                // Preserve corrupt data for recovery instead of silently overwriting it.
                try { File.Move(path, path + ".corrupt-" + DateTime.UtcNow.Ticks); }
                catch (Exception moveError) { Report(moveError); }
                return null;
            }
        }

        public void Save(ChunkRecord record)
        {
            try { Write(ChunkPath(new Vector2Int(record.x, record.z)), JsonUtility.ToJson(record, true)); }
            catch (Exception e) { Report(e); }
        }

        private string ChunkPath(Vector2Int c) => Path.Combine(DirectoryPath, $"{c.x}_{c.y}.json");
        private static void Write(string path, string text)
        {
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, text);
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
        private void Report(Exception e)
        {
            LastError = e.Message;
            Debug.LogError("Ermo save: " + e.Message);
        }
    }

    public sealed class WorldSampler
    {
        private readonly WorldSettings settings;
        private readonly float offsetX, offsetZ;
        private readonly WorldLocations locations;
        public WorldSampler(WorldSettings settings)
        {
            this.settings = settings;
            var random = WorldHash.Random(settings.seed, 0, 0, "terrain");
            offsetX = 1000 + (float)random.NextDouble() * 8000;
            offsetZ = 1000 + (float)random.NextDouble() * 8000;
            locations = new WorldLocations(settings, this);
        }
        public float Height(float x, float z)
        {
            float h = BaseHeight(x, z);
            if (settings.flattenYards)
            {
                var cell = new Vector2Int(Mathf.FloorToInt(x / settings.chunkSize), Mathf.FloorToInt(z / settings.chunkSize));
                foreach (var yard in locations.Yards(cell))
                {
                    float distance = Vector2.Distance(new Vector2(x, z), yard);
                    if (distance >= 28) continue;
                    float blend = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(10, 28, distance));
                    h = Mathf.Lerp(h, BaseHeight(yard.x, yard.y) + 1, blend);
                }
            }
            return h;
        }
        public float BaseHeight(float x, float z)
        {
            if (settings.terrainStyle == TerrainStyle.GodotContours) return GodotTerrain.Contours(settings, x, z);
            if (settings.terrainStyle == TerrainStyle.GodotLegacyWaves) return GodotTerrain.Waves(settings.seed, x, z);
            float scale = Mathf.Max(1, settings.hillScale);
            float a = Mathf.PerlinNoise(offsetX + x / scale, offsetZ + z / scale);
            float b = Mathf.PerlinNoise(offsetX + x / 59f, offsetZ + z / 59f);
            return a * settings.hillHeight * 2f + b * 2.5f;
        }
        public const float RoadSpacing = 256f;
        public int RoadLane(float x) => Mathf.RoundToInt(x / RoadSpacing);
        public float RoadX(float z, int lane = 0) => lane*RoadSpacing + 10*Mathf.Sin(z/37+lane*1.7f) + 14*Mathf.Sin(z/93+lane*1.7f);
        public float RoadDistance(float x, float z)
        {
            int lane = RoadLane(x);
            float slope = 10f/37*Mathf.Cos(z/37+lane*1.7f) + 14f/93*Mathf.Cos(z/93+lane*1.7f);
            return Mathf.Abs(x-RoadX(z,lane))/Mathf.Sqrt(1+slope*slope);
        }
        public Vector3 Point(float x, float z) => new Vector3(x, Height(x, z), z);
        // Same triangle interpolation as Godot mesh_height(), not a second smooth surface.
        public float MeshHeight(float x, float z)
        {
            float step = settings.chunkSize / (float)settings.terrainResolution;
            float gx = Mathf.Floor(x / step) * step, gz = Mathf.Floor(z / step) * step;
            float fx = (x - gx) / step, fz = (z - gz) / step;
            float a = Height(gx, gz), b = Height(gx + step, gz), c = Height(gx, gz + step), d = Height(gx + step, gz + step);
            return fx + fz <= 1 ? a + (b-a)*fx + (c-a)*fz : d + (c-d)*(1-fx) + (b-d)*(1-fz);
        }

        public ChunkRecord Generate(Vector2Int c)
        {
            var record = new ChunkRecord { x = c.x, z = c.y };
            float size = settings.chunkSize;
            locations.Populate(record);

            // Poles follow a global sequence; ownership is decided by floor (also at negative coordinates).
            int firstPole = Mathf.CeilToInt(c.y * size / 32f);
            for (int lane = Mathf.FloorToInt(c.x * size / RoadSpacing) - 1;
                lane <= Mathf.CeilToInt((c.x + 1) * size / RoadSpacing) + 1; lane++)
                for (int i = firstPole; i * 32f < (c.y + 1) * size; i++)
                {
                    float pz = i * 32f, px = RoadX(pz, lane) + 6;
                    if (Mathf.FloorToInt(px / size) == c.x)
                        record.props.Add(new PropRecord { id = $"pole-{lane}-{i}", kind = PropKind.Pole, position = Point(px, pz) });
                }
            return record;
        }
    }
}
