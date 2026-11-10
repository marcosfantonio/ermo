using UnityEngine;

namespace Ermo
{
    // One secondary-motion owner. Mouse orientation belongs to the walker/pivot,
    // never to this component. Every frame writes absolute local offsets.
    public sealed class HandheldCameraMotion : MonoBehaviour
    {
        [SerializeField] private CyberShotProfile profile;
        public Vector3 PositionOffset { get; private set; }
        public Vector3 RotationOffset { get; private set; }
        private FirstPersonWalker walker;
        private WorldSettings settings;
        private Vector3 previousPosition, positionVelocity, rotationVelocity, inertia, inertiaVelocity;
        private float clock, breathPhase, stepPhase, walkWeight, idleTime, adjustmentTime = -1, nextAdjustment = 16;
        private int adjustmentIndex;

        public void Initialize(FirstPersonWalker player, WorldSettings config)
        {
            walker = player; settings = config; profile = config.cameraProfile;
            previousPosition = player != null ? player.transform.position : transform.position;
        }

        private void LateUpdate()
        {
            if (walker == null || settings == null) return;
            Vector3 delta = walker.transform.position-previousPosition;
            previousPosition = walker.transform.position;
            float distance = new Vector2(delta.x,delta.z).magnitude;
            if (distance > 2) distance = 0; // Teleports are not footsteps or camera impulses.
            Tick(Time.deltaTime,distance,walker.Grounded,walker.IsRunning,
                walker.MenuOpen || walker.WakingUp || walker.TraversalActive || !walker.enabled,walker.ReducedMotion,walker.LookDeltaDegrees);
            transform.localPosition = PositionOffset;
            transform.localRotation = Quaternion.Euler(RotationOffset);
        }

        // Explicit time/distance makes minute-long idle and cadence tests possible
        // without waiting in Play Mode or injecting fake keyboard input.
        public void Tick(float dt, float distance, bool grounded, bool running, bool paused, bool reduced, Vector2 lookDegrees)
        {
            if (settings != null) profile = settings.cameraProfile;
            if (profile == null || dt <= 0) return;
            var p = profile.handheld;
            if (reduced || p.intensity <= 0)
            {
                PositionOffset = RotationOffset = inertia = positionVelocity = rotationVelocity = inertiaVelocity = Vector3.zero;
                walkWeight = 0;
                return;
            }
            float speed = grounded && !paused ? distance/dt : 0;
            walkWeight = Mathf.Lerp(walkWeight,Mathf.Clamp01(speed/3.3f),1-Mathf.Exp(-dt*6));
            float idleGain = Mathf.Lerp(p.idleMotionGain,1,walkWeight);
            Vector3 position = Vector3.zero, rotation = Vector3.zero;
            if (!paused)
            {
                clock += dt;
                float variation = Mathf.PerlinNoise(clock*.047f,12.7f);
                breathPhase += dt*p.breathingHz*Mathf.PI*2*Mathf.Lerp(.92f,1.08f,variation);
                float breath = Mathf.Sin(breathPhase)*Mathf.Lerp(.86f,1.14f,Mathf.PerlinNoise(clock*.063f,27));
                position = new Vector3(Noise(clock*.16f,3)*p.posturalMeters,
                    breath*p.breathingMeters,Noise(clock*.13f,41)*p.posturalMeters*.35f);
                rotation = new Vector3(Noise(clock*.19f,71)*p.posturalDegrees*.45f,
                    Noise(clock*.14f,89)*p.posturalDegrees*.5f,Noise(clock*.17f,17)*p.posturalDegrees);
                rotation += new Vector3(Noise(clock*p.tremorHz,119),Noise(clock*p.tremorHz,157),
                    Noise(clock*p.tremorHz,193)*.5f)*p.tremorDegrees;
                position *= idleGain; rotation *= idleGain;
                if (grounded) stepPhase += distance/StepCadence.StrideLength(running)*Mathf.PI;
                float gait = walkWeight*(running ? 1.25f*p.runMotionGain : p.walkMotionGain)*settings.headBob;
                position += new Vector3(Mathf.Sin(stepPhase)*p.walkBobMeters*.45f,
                    -Mathf.Cos(stepPhase*2)*p.walkBobMeters,0)*gait;
                rotation.z += Mathf.Sin(stepPhase)*p.walkRollDegrees*gait;
                idleTime = speed < .03f ? idleTime+dt : 0;
                if (speed >= .03f) adjustmentTime = -1;
                if (idleTime >= nextAdjustment && adjustmentTime < 0)
                {
                    adjustmentTime = 0; idleTime = 0; adjustmentIndex++;
                    nextAdjustment = Mathf.Lerp(p.adjustmentInterval.x,p.adjustmentInterval.y,
                        Mathf.PerlinNoise(adjustmentIndex*2.137f,37.1f));
                }
                if (adjustmentTime >= 0)
                {
                    adjustmentTime += dt;
                    float t = Mathf.Clamp01(adjustmentTime/3.4f);
                    float envelope = Mathf.Pow(Mathf.Sin(t*Mathf.PI),2);
                    float sign = Noise(adjustmentIndex*3.71f,67) >= 0 ? 1 : -1;
                    rotation.z += envelope*p.adjustmentDegrees*sign*idleGain;
                    position.x += envelope*p.posturalMeters*.5f*sign*idleGain;
                    if (t >= 1) adjustmentTime = -1;
                }
            }
            Vector3 inertiaTarget = paused ? Vector3.zero : new Vector3(lookDegrees.y,-lookDegrees.x,-lookDegrees.x*.3f)
                * (p.mouseInertia/Mathf.Max(dt,.001f));
            inertiaTarget = Vector3.ClampMagnitude(inertiaTarget,p.maximumInertiaDegrees);
            inertia = Vector3.SmoothDamp(inertia,inertiaTarget,ref inertiaVelocity,p.dampingSeconds,Mathf.Infinity,dt);
            float gain = p.intensity;
            PositionOffset = Vector3.SmoothDamp(PositionOffset,position*gain,ref positionVelocity,p.dampingSeconds,Mathf.Infinity,dt);
            RotationOffset = Vector3.SmoothDamp(RotationOffset,(rotation+inertia)*gain,ref rotationVelocity,p.dampingSeconds,Mathf.Infinity,dt);
        }

        private static float Noise(float time, float seed) => (Mathf.PerlinNoise(time,seed)-.5f)*2;
    }
}
