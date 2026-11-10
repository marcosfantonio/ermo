using UnityEngine;
using UnityEngine.InputSystem;

namespace Ermo
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonWalker : MonoBehaviour
    {
        public bool MenuOpen { get; set; }
        public bool ReducedMotion => false;
        public Camera View { get; private set; }
        public Transform ViewOrientation { get; private set; }
        public HandheldCameraMotion Handheld { get; private set; }
        public Vector2 LookDeltaDegrees { get; private set; }
        public bool WakingUp => Time.unscaledTime-wakeStartedAt < WakeDuration;
        public bool Grounded => controller != null && controller.isGrounded;
        public bool IsRunning { get; private set; }
        public bool TraversalActive { get; private set; }
        public bool TraversalLocksLook { get; private set; }
        public float VerticalSpeed => fallSpeed;
        public float LookPitch => pitch;
        private float traversalRoll;
        [SerializeField, Range(.2f, 2)] private float jumpHeight = .85f;
        private const float Gravity = 22;
        private CharacterController controller;
        private WorldSettings settings;
        private float yaw, pitch, fallSpeed;
        private float smoothedSpeed;
        private float wakeStartedAt;
        private const float WakeDuration = 2.2f;
        public void Initialize(WorldSettings worldSettings)
        {
            settings = worldSettings;
            controller = GetComponent<CharacterController>();
            controller.height = 1.78f; controller.radius = .28f; controller.center = Vector3.up * .89f;
            controller.stepOffset = .32f; controller.slopeLimit = 55;
            ViewOrientation = new GameObject("View orientation — mouse / wake").transform;
            ViewOrientation.SetParent(transform,false); ViewOrientation.localPosition = Vector3.up*1.65f;
            var motion = new GameObject("Handheld motion — additive offsets");
            motion.transform.SetParent(ViewOrientation,false);
            Handheld = motion.AddComponent<HandheldCameraMotion>(); Handheld.Initialize(this,settings);
            var cameraObject = new GameObject("Eye"); cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(motion.transform, false);
            View = cameraObject.AddComponent<Camera>(); View.fieldOfView = settings.fieldOfView;
            View.nearClipPlane = .07f; View.farClipPlane = settings.chunkSize * settings.viewDistance;
            cameraObject.AddComponent<AudioListener>();
            yaw = 0; pitch = 30;
            wakeStartedAt = Time.unscaledTime;
            ViewOrientation.localPosition = Vector3.up * 1.12f;
            ViewOrientation.localRotation = Quaternion.Euler(pitch, 0, 0);
            LockCursor(true);
        }

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void Update()
        {
            if (settings == null) return;
            LookDeltaDegrees = Vector2.zero;
            float wakeProgress = Mathf.Clamp01((Time.unscaledTime - wakeStartedAt) / WakeDuration);
            float wakeEase = wakeProgress * wakeProgress * (3f - 2f * wakeProgress);
            if (wakeProgress < 1f)
            {
                pitch = Mathf.Lerp(30f, -3f, wakeEase);
                ViewOrientation.localPosition = Vector3.up * Mathf.Lerp(1.12f, 1.65f, wakeEase);
                ViewOrientation.localRotation = Quaternion.Euler(pitch, 0, 0);
                return;
            }

            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if (keyboard == null || MenuOpen || Cursor.lockState != CursorLockMode.Locked) return;
            if (TraversalActive && TraversalLocksLook) return;
            Vector2 delta = mouse != null ? mouse.delta.ReadValue() * .075f : Vector2.zero;
            LookDeltaDegrees = delta;
            yaw += delta.x; pitch = Mathf.Clamp(pitch - delta.y, -82, 82);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            ViewOrientation.localPosition = Vector3.up*1.65f;
            ViewOrientation.localRotation = Quaternion.Euler(pitch,0,traversalRoll);
            if (TraversalActive) return;
            float x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            float z = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            Vector2 input = Vector2.ClampMagnitude(new Vector2(x, z), 1);
            bool running = keyboard.leftShiftKey.isPressed && input.y > 0;
            IsRunning = running;
            float targetSpeed = input.magnitude * (running ? 6.2f : 3.3f);
            smoothedSpeed = Mathf.MoveTowards(smoothedSpeed, targetSpeed, Time.deltaTime * 12);
            Vector3 movement = (transform.right * input.x + transform.forward * input.y).normalized * smoothedSpeed;
            if (keyboard.spaceKey.wasPressedThisFrame) TryJump();
            MoveCharacter(movement,Time.deltaTime);
            View.fieldOfView = Mathf.Lerp(View.fieldOfView, settings.fieldOfView + (running && !ReducedMotion ? 5.5f : 0), 1 - Mathf.Exp(-Time.deltaTime * 2));
        }

        public bool TryJump()
        {
            if (!enabled || MenuOpen || WakingUp || TraversalActive || controller == null || !controller.enabled || !controller.isGrounded || fallSpeed > 0)
                return false;
            fallSpeed = Mathf.Sqrt(2*Gravity*jumpHeight);
            return true;
        }

        // Shared by normal input and the batch physics test; horizontal speeds,
        // gravity and collision handling still belong to this same controller.
        public void MoveCharacter(Vector3 planarVelocity, float deltaTime)
        {
            if (TraversalActive || deltaTime <= 0 || controller == null || !controller.enabled) return;
            if (controller.isGrounded && fallSpeed < 0) fallSpeed = -2;
            fallSpeed = Mathf.Max(fallSpeed-Gravity*deltaTime,-35);
            var flags = controller.Move((planarVelocity+Vector3.up*fallSpeed)*deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && fallSpeed > 0) fallSpeed = 0;
        }

        public bool BeginTraversal(bool lockLook)
        {
            if (TraversalActive || MenuOpen || WakingUp || !enabled) return false;
            TraversalActive = true; TraversalLocksLook = lockLook;
            fallSpeed = 0; smoothedSpeed = 0; IsRunning = false;
            return true;
        }

        public void MoveOnLadder(Vector3 displacement)
        {
            if (TraversalActive && controller.enabled) controller.Move(displacement);
        }

        public void SetTraversalPose(Vector3 position, float lookPitch, float roll)
        {
            bool collision = controller.enabled; controller.enabled = false;
            transform.position = position; controller.enabled = collision;
            pitch = Mathf.Clamp(lookPitch,-82,82); traversalRoll = roll;
            ViewOrientation.localPosition = Vector3.up*1.65f;
            ViewOrientation.localRotation = Quaternion.Euler(pitch,0,roll);
        }

        public void EndTraversal(bool jump = false)
        {
            TraversalActive = TraversalLocksLook = false; traversalRoll = 0;
            fallSpeed = jump ? Mathf.Sqrt(2*Gravity*jumpHeight) : -2;
            ViewOrientation.localRotation = Quaternion.Euler(pitch,0,0);
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus) { MenuOpen = true; LockCursor(false); }
        }
        private void OnDisable() => LockCursor(false);
    }
}
