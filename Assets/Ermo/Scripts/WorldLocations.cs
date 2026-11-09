using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ermo
{
    // Candidate selection uses world coordinates, never the set of loaded chunks.
    // All members belong to the anchor's chunk, even if an accessory crosses its edge.
    public sealed class WorldLocations
    {
        private sealed class Candidate
        {
            public string id;
            public Vector2 anchor;
            public PropKind kind;
            public bool tower, turbine;
            public float rotation;
            public int rank;
            public uint priority;
        }

        private readonly WorldSettings settings;
        private readonly WorldSampler sampler;
        private readonly Dictionary<Vector2Int, List<Vector2>> yards = new Dictionary<Vector2Int, List<Vector2>>();
        private float Distance => Mathf.Max(60, settings.minimumLocationDistance);
        public WorldLocations(WorldSettings settings, WorldSampler sampler)
        {
            this.settings = settings; this.sampler = sampler;
        }

        public List<Vector2> Yards(Vector2Int cell)
        {
            if (yards.TryGetValue(cell, out var cached)) return cached;
            Vector2 min = new Vector2(cell.x, cell.y) * settings.chunkSize - Vector2.one * 28;
            Vector2 max = new Vector2(cell.x + 1, cell.y + 1) * settings.chunkSize + Vector2.one * 28;
            var all = Gather(min - Vector2.one * Distance, max + Vector2.one * Distance);
            var result = new List<Vector2>();
            foreach (var current in all)
            {
                if (current.kind != PropKind.House && current.kind != PropKind.OpenHouse) continue;
                if (current.anchor.x < min.x || current.anchor.x > max.x || current.anchor.y < min.y || current.anchor.y > max.y) continue;
                bool blocked = all.Exists(other => !ReferenceEquals(current,other) &&
                    (current.anchor-other.anchor).sqrMagnitude < Distance*Distance && HigherPriority(other,current));
                if (!blocked) result.Add(current.anchor);
            }
            // Bounding the memoization prevents exploration from retaining the whole world.
            if (yards.Count > 512) yards.Clear();
            yards[cell] = result;
            return result;
        }

        public void Populate(ChunkRecord record)
        {
            float size = settings.chunkSize;
            Vector2 min = new Vector2(record.x * size, record.z * size);
            Vector2 max = min + Vector2.one * size;
            var candidates = Gather(min - Vector2.one * Distance, max + Vector2.one * Distance);
            foreach (var current in candidates)
            {
                if (Mathf.FloorToInt(current.anchor.x / size) != record.x || Mathf.FloorToInt(current.anchor.y / size) != record.z) continue;
                bool blocked = false;
                foreach (var other in candidates)
                {
                    if (ReferenceEquals(current, other)) continue;
                    if ((current.anchor - other.anchor).sqrMagnitude >= Distance * Distance) continue;
                    if (HigherPriority(other, current)) { blocked = true; break; }
                }
                if (blocked) continue;
                Add(record, current, current.kind, Vector2.zero, current.id);
                float side = current.anchor.x >= sampler.RoadX(current.anchor.y,sampler.RoadLane(current.anchor.x)) ? 1 : -1;
                if ((current.kind == PropKind.House || current.kind == PropKind.OpenHouse) && current.tower)
                    Add(record, current, PropKind.WaterTower, new Vector2(side*16, 13), current.id + "/tank");
                if ((current.kind == PropKind.House || current.kind == PropKind.OpenHouse) && current.turbine)
                    Add(record, current, PropKind.Turbine, new Vector2(side*27, -17), current.id + "/turbine");
            }
        }

        private List<Candidate> Gather(Vector2 min, Vector2 max)
        {
            var result = new List<Candidate>();
            float spacing = Mathf.Max(Distance, settings.locationSpacing);
            float jitter = Mathf.Max(0, (spacing - Distance) * .5f);
            for (int x = Mathf.FloorToInt(min.x / spacing); x <= Mathf.FloorToInt(max.x / spacing); x++)
                for (int z = Mathf.FloorToInt(min.y / spacing); z <= Mathf.FloorToInt(max.y / spacing); z++)
                {
                    var rng = WorldHash.Random(settings.seed, x, z, "locations-v2");
                    if (rng.NextDouble() >= settings.locationChance) continue;
                    var anchor = new Vector2((x + .5f) * spacing + Range(rng, -jitter, jitter),
                        (z + .5f) * spacing + Range(rng, -jitter, jitter));
                    // Do not discard an entire site just because its first sample hit a road.
                    // Every retry stays in the bounded jitter box, preserving the 150 m separation.
                    for (int attempt = 0; attempt < 12 && sampler.RoadDistance(anchor.x,anchor.y)<14; attempt++)
                    {
                        float ox = attempt < 8 ? Range(rng,-jitter,jitter) : ((attempt&1)==0 ? -jitter : jitter);
                        float oz = attempt < 8 ? Range(rng,-jitter,jitter) : ((attempt&2)==0 ? -jitter : jitter);
                        anchor = new Vector2((x+.5f)*spacing+ox,(z+.5f)*spacing+oz);
                    }
                    if (sampler.RoadDistance(anchor.x,anchor.y)<14) continue;
                    bool house = rng.NextDouble() < settings.houseChance;
                    var candidate = new Candidate
                    {
                        id = $"location:{x}:{z}", anchor = anchor,
                        kind = house ? PropKind.House : (rng.NextDouble() < settings.turbineChance ? PropKind.Turbine : PropKind.WaterTower),
                        tower = rng.NextDouble() < settings.waterTowerChance,
                        turbine = rng.NextDouble() < settings.turbineChance,
                        rotation = Range(rng, -25, 25)
                    };
                    candidate.priority = WorldHash.Of(settings.seed + candidate.id);
                    result.Add(candidate);
                }
            if (settings.landmarks == null) return result;
            foreach (var rule in settings.landmarks)
            {
                if (rule == null || string.IsNullOrWhiteSpace(rule.id)) continue;
                if (rule.exactChunk)
                {
                    var anchor = new Vector2(rule.chunk.x * settings.chunkSize + Mathf.Clamp(rule.localPosition.x, 9, settings.chunkSize - 9),
                        rule.chunk.y * settings.chunkSize + Mathf.Clamp(rule.localPosition.y, 9, settings.chunkSize - 9));
                    if (anchor.x >= min.x && anchor.x <= max.x && anchor.y >= min.y && anchor.y <= max.y)
                        result.Add(Authored(rule, anchor, rule.id, 2));
                    continue;
                }
                float cell = Mathf.Max(settings.chunkSize, rule.spacingMeters);
                for (int x = Mathf.FloorToInt(min.x / cell); x <= Mathf.FloorToInt(max.x / cell); x++)
                    for (int z = Mathf.FloorToInt(min.y / cell); z <= Mathf.FloorToInt(max.y / cell); z++)
                    {
                        var rng = WorldHash.Random(settings.seed, x, z, rule.id);
                        if (rng.NextDouble() >= rule.chance) continue;
                        var anchor = new Vector2((x + Range(rng, .15f, .85f)) * cell, (z + Range(rng, .15f, .85f)) * cell);
                        if (anchor.magnitude < rule.minimumDistance || sampler.RoadDistance(anchor.x, anchor.y) < 40) continue;
                        result.Add(Authored(rule, anchor, $"{rule.id}:{x}:{z}", 1));
                    }
            }
            return result;
        }

        private Candidate Authored(LandmarkRule rule, Vector2 anchor, string id, int rank) => new Candidate
        {
            id = id, anchor = anchor, kind = rule.kind, tower = rule.includeWaterTower, turbine = rule.includeTurbine,
            rank = rank, priority = WorldHash.Of(settings.seed + id)
        };
        private static bool HigherPriority(Candidate a, Candidate b)
        {
            if (a.rank != b.rank) return a.rank > b.rank;
            if (a.priority != b.priority) return a.priority > b.priority;
            return string.CompareOrdinal(a.id, b.id) < 0;
        }
        private void Add(ChunkRecord record, Candidate group, PropKind kind, Vector2 offset, string id)
        {
            Vector3 local = Quaternion.Euler(0, group.rotation, 0) * new Vector3(offset.x, 0, offset.y);
            float x = group.anchor.x + local.x, z = group.anchor.y + local.z;
            record.props.Add(new PropRecord
            {
                id = id, groupId = group.id, groupAnchor = group.anchor, kind = kind,
                position = sampler.Point(x, z), rotation = group.rotation
            });
        }
        private static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
    }
}
