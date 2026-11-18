using UnityEngine;

namespace Ermo
{
    // The market's own sound bed: fluorescent hum, muzak (which ducks under
    // the PA), hard shoes on polished concrete, and the doors. Volumes follow
    // the same WorldSettings sliders as the rest of the game.
    public sealed class AtacarejoAudio : MonoBehaviour
    {
        const float HumLevel = .09f;
        const float MusicLevel = .07f;
        const float DuckedMusic = .35f;

        AudioSource hum, music, steps;
        WorldSettings settings;
        FirstPersonWalker walker;
        AtacarejoAnnouncer announcer;
        readonly StepCadence cadence = new StepCadence();
        readonly System.Random variation = new System.Random(32);
        Vector3 previousFeet;
        bool leftFoot;

        public float Volume => settings != null ? settings.audioVolume : .7f;
        float FootstepVolume => settings != null ? settings.footstepVolume : .48f;
        public int StepsPlayed { get; private set; }

        public static AudioSource Source(Transform parent, string name, AudioClip clip, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = loop;
            source.clip = clip;
            return source;
        }

        // A looping 3D sound fixed to an object (lamp buzz, evaporator, drips).
        public static AudioSource Loop3D(GameObject host, AudioClip clip, float volume, float minDistance, float maxDistance)
        {
            var source = host.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.volume = volume;
            source.dopplerLevel = 0f;
            source.Play();
            return source;
        }

        public void Begin(FirstPersonWalker player, WorldSettings worldSettings, AtacarejoAnnouncer pa)
        {
            walker = player;
            settings = worldSettings;
            announcer = pa;
            if (hum == null) CreateSources();
            previousFeet = player.transform.position;
            if (!hum.isPlaying) hum.Play();
            if (!music.isPlaying) music.Play();
        }

        // After a teleport (the SAÍDA loop) or a floating-origin shift: the
        // distance jump must not count as a stride.
        public void ResetFeet()
        {
            if (walker != null) previousFeet = walker.transform.position;
        }

        public void StopMusic() => music.Stop();

        public void PlayDoor(string resourceName, float gain = 1f)
        {
            var clip = Resources.Load<AudioClip>("Ermo/Audio/" + resourceName);
            if (clip != null) steps.PlayOneShot(clip, Volume * gain);
        }

        void CreateSources()
        {
            hum = Source(transform, "Hum das fluorescentes", AtacarejoSounds.Hum, true);
            music = Source(transform, "Música do mercado", AtacarejoSounds.Muzak, true);
            music.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = 2200f;
            steps = Source(transform, "Passos de sapato", null, false);
            // A short slap off the far shelves: a big, hard, empty room.
            var slap = steps.gameObject.AddComponent<AudioEchoFilter>();
            slap.delay = 95f; slap.decayRatio = .22f; slap.wetMix = .3f; slap.dryMix = 1f;
        }

        void Update()
        {
            if (walker == null) return;
            hum.volume = Volume * HumLevel;
            float duck = announcer != null && announcer.Speaking ? DuckedMusic : 1f;
            music.volume = Mathf.MoveTowards(music.volume, Volume * MusicLevel * duck, Time.deltaTime * .05f);
        }

        void LateUpdate()
        {
            if (walker == null) return;
            Vector3 feet = walker.transform.position;
            float distance = new Vector2(feet.x - previousFeet.x, feet.z - previousFeet.z).magnitude;
            previousFeet = feet;
            if (distance > 4f) return; // teleport, not a stride
            bool paused = walker.MenuOpen || walker.TraversalActive;
            if (!cadence.Advance(distance, walker.Grounded, paused, walker.IsRunning)) return;
            PlayStep();
        }

        void PlayStep()
        {
            leftFoot = !leftFoot;
            steps.panStereo = leftFoot ? -.05f : .05f;
            steps.pitch = .95f + (float)variation.NextDouble() * .08f + (walker.IsRunning ? .04f : 0f);
            float gain = Volume * FootstepVolume * (walker.IsRunning ? 1.25f : .95f) * (.9f + (float)variation.NextDouble() * .2f);
            steps.PlayOneShot(AtacarejoSounds.ShoeStep(variation.Next()), gain);
            StepsPlayed++;
        }
    }
}
