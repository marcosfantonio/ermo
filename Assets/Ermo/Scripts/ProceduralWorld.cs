using System.Collections.Generic;
using UnityEngine;

namespace Ermo
{
    public sealed class ProceduralWorld : MonoBehaviour
    {
        public WorldSettings Settings { get; private set; }
        public WorldSampler Sampler { get; private set; }
        public WorldStore Store { get; private set; }
        public int LoadedCount => loaded.Count;
        public Vector2Int CurrentChunk => Coordinate(player.position);
        public bool Ready => loaded.ContainsKey(CurrentChunk);
        private readonly Dictionary<Vector2Int, GameObject> loaded = new Dictionary<Vector2Int, GameObject>();
        private readonly List<Vector2Int> wanted = new List<Vector2Int>();
        private readonly List<Vector2Int> remove = new List<Vector2Int>();
        private Transform player;
        private WorldPalette palette;
        private Vector2Int lastChunk = new Vector2Int(int.MinValue, int.MinValue);

        public void Initialize(WorldSettings settings, Transform target, Shader surfaceShader)
        {
            Settings = settings; player = target;
            Store = new WorldStore(settings); Sampler = new WorldSampler(settings);
            palette = new WorldPalette(surfaceShader, settings);
            // A small collision safety ring is synchronous; distant scenery streams over frames.
            Vector2Int spawn = Coordinate(player.position);
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++) Load(spawn + new Vector2Int(x, z));
            RefreshWanted();
        }

        private void Update()
        {
            if (player == null) return;
            if (CurrentChunk != lastChunk) RefreshWanted();
            for (int i = 0; i < Settings.chunksPerFrame && wanted.Count > 0; i++)
            {
                var coordinate = wanted[0]; wanted.RemoveAt(0);
                if (!loaded.ContainsKey(coordinate)) Load(coordinate);
            }
        }

        private void RefreshWanted()
        {
            lastChunk = CurrentChunk;
            wanted.Clear();
            int radius = Settings.viewDistance;
            for (int x = -radius; x <= radius; x++)
                for (int z = -radius; z <= radius; z++)
                {
                    var coordinate = lastChunk + new Vector2Int(x, z);
                    if (!loaded.ContainsKey(coordinate)) wanted.Add(coordinate);
                }
            wanted.Sort((a, b) => (a - lastChunk).sqrMagnitude.CompareTo((b - lastChunk).sqrMagnitude));
            remove.Clear();
            foreach (var pair in loaded)
                if (Mathf.Abs(pair.Key.x - lastChunk.x) > radius + 1 || Mathf.Abs(pair.Key.y - lastChunk.y) > radius + 1)
                    remove.Add(pair.Key);
            foreach (var key in remove) { Destroy(loaded[key]); loaded.Remove(key); }
        }

        private void Load(Vector2Int coordinate)
        {
            var record = Store.Load(coordinate);
            if (record == null) { record = Sampler.Generate(coordinate); Store.Save(record); }
            loaded.Add(coordinate, WorldGeometry.BuildChunk(record, Settings, Sampler, palette, transform));
        }
        public Vector2Int Coordinate(Vector3 position) => new Vector2Int(
            Mathf.FloorToInt(position.x / Settings.chunkSize), Mathf.FloorToInt(position.z / Settings.chunkSize));
        private void OnDestroy() => palette?.Dispose();
    }
}
