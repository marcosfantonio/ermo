using System.Collections.Generic;
using UnityEngine;

namespace Ermo
{
    // Owned by the persistent player rig, not either world environment.
    public sealed class WaterImmersionEffects : MonoBehaviour
    {
        public float Wetness { get; private set; }
        public float EffectTime { get; private set; }
        public float Splash { get; private set; }
        public int Entries { get; private set; }
        public int Exits { get; private set; }
        public float MuffleCutoff { get; private set; } = 22000;
        public bool HasAudio => entry != null && exit != null && underwater != null;
        private WorldSettings settings;
        private AudioSource events, bed;
        private AudioClip entry, exit, underwater, waterStep, waterJump, ladderStep;
        private float wetRemaining;
        private bool submerged, paused;
        private readonly List<FilterState> filters = new List<FilterState>();
        private sealed class FilterState
        {
            public AudioLowPassFilter filter;
            public float cutoff;
            public bool enabled, owned;
        }

        public void Initialize(WorldSettings config)
        {
            settings = config;
            var audio = new GameObject("Splash, bubbles and underwater pressure");
            audio.transform.SetParent(transform,false);
            events = audio.AddComponent<AudioSource>(); events.playOnAwake = false; events.spatialBlend = 0;
            bed = audio.AddComponent<AudioSource>(); bed.playOnAwake = false; bed.spatialBlend = 0; bed.loop = true;
            // Deterministic synthesized liquid effects; no external recordings.
            entry = MakeLiquid("Water entry splash",1.1f,17,false);
            exit = MakeLiquid("Surface break and draining water",1.35f,43,false);
            underwater = MakeLiquid("Submerged low rumble and bubble resonance",3,79,true);
            waterStep = MakeLiquid("Water walking splash",.48f,113,false);
            ladderStep = MakeLadderStep();
            waterJump = Resources.Load<AudioClip>("Ermo/Audio/water_jump");
            bed.clip = underwater;
        }

        public void Enter()
        {
            Entries++; submerged = true; Splash = 1;
            CaptureFilters();
            events.volume = Volume; events.PlayOneShot(waterJump != null ? waterJump : entry,.85f);
            bed.volume = Volume*.36f; bed.Play();
        }

        public void PlayWaterStep(bool running)
        {
            if (events == null || paused || waterStep == null || Volume <= 0) return;
            events.volume = Volume;
            events.PlayOneShot(waterStep,running ? .34f : .25f);
        }

        public void PlayLadderStep()
        {
            if (events == null || paused || ladderStep == null || Volume <= 0) return;
            events.volume = Volume;
            events.PlayOneShot(ladderStep,.35f);
        }

        public void Emerge()
        {
            if (!submerged) return;
            submerged = false; Exits++; wetRemaining = 7; Wetness = 1; Splash = .65f;
            events.volume = Volume; events.PlayOneShot(exit,.7f);
        }

        private float Volume => settings != null ? settings.audioVolume : .7f;

        public void Tick(float immersion, bool menuOpen, float dt)
        {
            if (events == null) return;
            if (menuOpen != paused)
            {
                paused = menuOpen;
                if (paused) { events.Pause(); bed.Pause(); }
                else { events.UnPause(); bed.UnPause(); }
            }
            if (paused) return;
            EffectTime += dt;
            Splash = Mathf.MoveTowards(Splash,0,dt*1.8f);
            wetRemaining = Mathf.Max(0,wetRemaining-dt);
            Wetness = Mathf.SmoothStep(0,1,wetRemaining/7);
            events.volume = Volume;
            bed.volume = Mathf.MoveTowards(bed.volume,Volume*immersion*.36f,dt*.8f);
            if (!submerged && immersion <= 0 && bed.volume < .001f && bed.isPlaying) bed.Stop();
            MuffleCutoff = Mathf.Lerp(22000,650,Mathf.SmoothStep(0,1,immersion));
            foreach (var state in filters)
            {
                if (state.filter == null) continue;
                state.filter.enabled = immersion > .01f || state.enabled;
                state.filter.cutoffFrequency = Mathf.Min(state.cutoff,MuffleCutoff);
            }
        }

        private void CaptureFilters()
        {
            RestoreFilters();
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (source == events || source == bed) continue;
                var filter = source.GetComponent<AudioLowPassFilter>();
                bool owned = filter == null;
                if (owned) filter = source.gameObject.AddComponent<AudioLowPassFilter>();
                filters.Add(new FilterState { filter = filter, cutoff = owned ? 22000 : filter.cutoffFrequency,
                    enabled = !owned && filter.enabled, owned = owned });
            }
        }

        private void RestoreFilters()
        {
            foreach (var state in filters)
            {
                if (state.filter == null) continue;
                state.filter.cutoffFrequency = state.cutoff; state.filter.enabled = state.enabled;
                if (state.owned) Destroy(state.filter);
            }
            filters.Clear();
        }

        private static AudioClip MakeLiquid(string name, float seconds, int seed, bool loop)
        {
            const int rate = 24000;
            var data = new float[Mathf.RoundToInt(seconds*rate)];
            var random = new System.Random(seed);
            float low = 0, phase = 0, bubbleStart = 0, nextBubble = 0, bubbleFrequency = 220, bubbleGain = .1f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i/(float)rate;
                float noise = (float)random.NextDouble()*2-1;
                low += (noise-low)*(loop ? .025f : .18f);
                if (t >= nextBubble)
                {
                    bubbleStart = t;
                    nextBubble = t+.025f+(float)random.NextDouble()*(loop ? .28f : .12f);
                    bubbleFrequency = (loop ? 100 : 240)+(float)random.NextDouble()*(loop ? 400 : 1600);
                    bubbleGain = .04f+(float)random.NextDouble()*.12f;
                    phase = 0;
                }
                float pulseTime = t-bubbleStart;
                phase += 2*Mathf.PI*bubbleFrequency*(1+pulseTime*2)/rate;
                float bubble = Mathf.Sin(phase)*Mathf.Exp(-pulseTime*55)*bubbleGain;
                float envelope = loop ? .6f : Mathf.Exp(-t*3.8f)*(1-Mathf.Exp(-t*95));
                float edge = Mathf.Min(1,Mathf.Min(t,seconds-t)*80);
                data[i] = Mathf.Clamp((low*(loop ? 1.1f : 1.8f)+bubble)*envelope*edge,-.85f,.85f);
            }
            var clip = AudioClip.Create(name,data.Length,1,rate,false); clip.SetData(data,0); return clip;
        }

        private static AudioClip MakeLadderStep()
        {
            const int rate = 24000;
            var data = new float[Mathf.RoundToInt(.2f*rate)];
            var random = new System.Random(127);
            for (int i = 0; i < data.Length; i++)
            {
                float t = i/(float)rate;
                float noise = (float)random.NextDouble()*2-1;
                float attack = Mathf.Exp(-t*34);
                float ring = Mathf.Sin(2*Mathf.PI*760*t)*Mathf.Exp(-t*24)
                    +.45f*Mathf.Sin(2*Mathf.PI*1320*t)*Mathf.Exp(-t*38);
                data[i] = (noise*.16f+ring*.42f)*attack;
            }
            var clip = AudioClip.Create("Metal ladder rung",data.Length,1,rate,false);
            clip.SetData(data,0);
            return clip;
        }

        private void OnDestroy()
        {
            RestoreFilters();
            if (entry != null) Destroy(entry);
            if (exit != null) Destroy(exit);
            if (underwater != null) Destroy(underwater);
            if (waterStep != null) Destroy(waterStep);
            if (ladderStep != null) Destroy(ladderStep);
        }
    }
}
