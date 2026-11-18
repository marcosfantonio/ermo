using System.Collections.Generic;
using UnityEngine;

namespace Ermo
{
    // Every light of the market that is not perfectly steady. The builder
    // registers lights as it spawns them; AtacarejoWorld ticks them once a
    // frame. Destroyed lights drop out on their own.
    public static class AtacarejoLights
    {
        public enum Flicker
        {
            Gentle, // a slow, barely visible breathing
            Sick,   // mostly on, then stretches where the starter fights
            Faint   // a dead tube that twitches orange now and then
        }

        struct Entry
        {
            public Light light;
            public float baseline;
            public Renderer tube;
            public Color glow;
            public Flicker mode;
            public AudioSource buzz;
            public float seed;
        }

        static readonly List<Entry> entries = new List<Entry>();
        static MaterialPropertyBlock block;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => entries.Clear();

        public static void Register(Light light, float baseline) =>
            Register(light, baseline, null, Color.black, Flicker.Gentle);

        // tube: renderer whose emission follows the light (optional).
        // buzz: a 3D buzz that gets louder while the tube struggles (optional).
        public static void Register(Light light, float baseline, Renderer tube, Color glow, Flicker mode, AudioSource buzz = null)
        {
            if (light == null) return;
            Vector3 p = light.transform.position;
            entries.Add(new Entry
            {
                light = light, baseline = baseline, tube = tube, glow = glow, mode = mode, buzz = buzz,
                seed = (p.x * .37f + p.z * .71f) % 97f
            });
        }

        public static void Clear() => entries.Clear();

        public static void Tick(float time, float volume)
        {
            if (block == null) block = new MaterialPropertyBlock();
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                if (entry.light == null) { entries.RemoveAt(i); continue; }
                float level = Level(entry.mode, time, entry.seed);
                entry.light.intensity = entry.baseline * level;
                entry.light.enabled = level > .02f;
                if (entry.mode != Flicker.Gentle && entry.tube != null)
                {
                    entry.tube.GetPropertyBlock(block);
                    block.SetColor("_EmissionColor", entry.glow * level);
                    entry.tube.SetPropertyBlock(block);
                }
                if (entry.buzz != null) entry.buzz.volume = volume * (.12f + .38f * (1f - level));
            }
        }

        static float Level(Flicker mode, float time, float seed)
        {
            switch (mode)
            {
                case Flicker.Sick:
                {
                    bool struggling = Mathf.PerlinNoise(time * .45f, seed) > .6f;
                    float noise = Mathf.PerlinNoise(time * 11f, seed + 3f);
                    return struggling ? (noise > .55f ? 1f : .06f) : Mathf.Lerp(.82f, 1f, noise);
                }
                case Flicker.Faint:
                {
                    bool alive = Mathf.PerlinNoise(time * .3f, seed) > .68f;
                    return alive && Mathf.PerlinNoise(time * 16f, seed + 1f) > .45f ? 1f : 0f;
                }
                default:
                    return Mathf.Lerp(.86f, 1f, Mathf.PerlinNoise(time * .35f, seed));
            }
        }
    }
}
