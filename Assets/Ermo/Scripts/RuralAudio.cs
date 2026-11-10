using UnityEngine;

namespace Ermo
{
    public sealed class RuralAudio : MonoBehaviour
    {
        public AudioSource Breeze { get; private set; }
        public AudioSource Footsteps { get; private set; }
        public bool HasClips => settings != null && settings.breezeClip != null && settings.grassStepClip != null
            && settings.dirtStepClip != null && settings.cementStepClip != null;
        private FirstPersonWalker walker;
        private ProceduralWorld world;
        private WorldSettings settings;
        private readonly StepCadence cadence = new StepCadence();
        private Vector3 previousPosition;
        private System.Random variation;
        private bool leftFoot;
        private bool indoors;

        public void Initialize(FirstPersonWalker player, ProceduralWorld proceduralWorld, WorldSettings config)
        {
            walker = player; world = proceduralWorld; settings = config;
            previousPosition = transform.position;
            variation = WorldHash.Random(config.seed,0,0,"audio-variation");
            var breeze = new GameObject("Soft breeze"); breeze.transform.SetParent(transform,false);
            Breeze = breeze.AddComponent<AudioSource>(); Breeze.playOnAwake = false; Breeze.loop = true;
            Breeze.spatialBlend = 0; Breeze.clip = config.breezeClip; Breeze.volume = config.breezeVolume*config.audioVolume;
            var filter = breeze.AddComponent<AudioLowPassFilter>(); filter.cutoffFrequency = 4200;
            if (Breeze.clip != null) Breeze.Play();
            var steps = new GameObject("Footsteps"); steps.transform.SetParent(transform,false);
            Footsteps = steps.AddComponent<AudioSource>(); Footsteps.playOnAwake = false; Footsteps.spatialBlend = 0;
        }

        private void LateUpdate()
        {
            if (settings == null) return;
            bool underHouseRoof = IsUnderHouseRoof();
            if (underHouseRoof != indoors)
            {
                indoors = underHouseRoof;
                if (indoors) Breeze.Stop();
                else if (Breeze.clip != null) Breeze.Play();
            }
            float gust = .78f+Mathf.PerlinNoise(Time.time*.07f,29.1f)*.34f;
            Breeze.volume = settings.audioVolume*settings.breezeVolume*1.3f*gust;
            Vector3 delta = transform.position-previousPosition; previousPosition = transform.position;
            float distance = new Vector2(delta.x,delta.z).magnitude;
            if (walker.MenuOpen && Footsteps.isPlaying) Footsteps.Stop();
            if (!cadence.Advance(distance,walker.Grounded,walker.MenuOpen,walker.IsRunning)) return;
            var clip = SurfaceClip();
            if (clip == null || settings.audioVolume <= 0) return;
            leftFoot = !leftFoot;
            Footsteps.panStereo = leftFoot ? -.045f : .045f;
            Footsteps.pitch = .95f+(float)variation.NextDouble()*.1f+(walker.IsRunning ? .04f : 0);
            float gain = settings.audioVolume*settings.footstepVolume*(walker.IsRunning ? 1.18f : .85f);
            Footsteps.PlayOneShot(clip,gain*(.92f+(float)variation.NextDouble()*.16f));
        }

        private bool IsUnderHouseRoof()
        {
            Vector3 origin = transform.position + Vector3.up*.35f;
            bool previousBackfaces = Physics.queriesHitBackfaces;
            try
            {
                Physics.queriesHitBackfaces = true;
                foreach (var hit in Physics.RaycastAll(origin,Vector3.up,12f,
                    Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                {
                    string name = hit.collider.name.ToLowerInvariant();
                    if (name == "roof" || name.Contains("house_teto") || name.Contains("casa_teto"))
                        return true;
                }
                return false;
            }
            finally { Physics.queriesHitBackfaces = previousBackfaces; }
        }

        private AudioClip SurfaceClip()
        {
            if (Physics.Raycast(transform.position+Vector3.up*.45f,Vector3.down,out var hit,1.4f,
                Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore) && !(hit.collider is MeshCollider))
                return settings.cementStepClip;
            return world.Sampler.RoadDistance(transform.position.x,transform.position.z) <= settings.roadWidth*.6f
                ? settings.dirtStepClip : settings.grassStepClip;
        }
    }
}
