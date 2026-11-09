using UnityEngine;
using UnityEngine.InputSystem;

namespace Ermo
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonWalker : MonoBehaviour
    {
        public bool MenuOpen { get; set; }
        public bool ReducedMotion { get; set; }
        public Camera View { get; private set; }
        public bool Grounded => controller != null && controller.isGrounded;
        public bool IsRunning { get; private set; }
        private CharacterController controller;
        private WorldSettings settings;
        private float yaw, pitch, fallSpeed, stride, motionWeight, lean;
        private float smoothedSpeed;
        private float organicTime;
        private Vector3 sway, swayVelocity, rotationSway, rotationVelocity;
        public void Initialize(WorldSettings worldSettings)
        {
            settings = worldSettings;
            controller = GetComponent<CharacterController>();
            controller.height = 1.78f; controller.radius = .28f; controller.center = Vector3.up * .89f;
            controller.stepOffset = .32f; controller.slopeLimit = 55;
            var cameraObject = new GameObject("Eye"); cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform, false); cameraObject.transform.localPosition = Vector3.up * 1.65f;
            View = cameraObject.AddComponent<Camera>(); View.fieldOfView = settings.fieldOfView;
            View.nearClipPlane = .07f; View.farClipPlane = settings.chunkSize * settings.viewDistance;
            cameraObject.AddComponent<AudioListener>();
            yaw = 0; pitch = -3;
            LockCursor(true);
        }

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void Update()
        {
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if (settings == null || keyboard == null || MenuOpen || Cursor.lockState != CursorLockMode.Locked) return;
            Vector2 delta = mouse != null ? mouse.delta.ReadValue() * .075f : Vector2.zero;
            yaw += delta.x; pitch = Mathf.Clamp(pitch - delta.y, -82, 82);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            float x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            float z = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            Vector2 input = Vector2.ClampMagnitude(new Vector2(x, z), 1);
            bool running = keyboard.leftShiftKey.isPressed && input.y > 0;
            IsRunning = running;
            float targetSpeed = input.magnitude * (running ? 6.2f : 3.3f);
            smoothedSpeed = Mathf.MoveTowards(smoothedSpeed, targetSpeed, Time.deltaTime * 12);
            if (controller.isGrounded && fallSpeed < 0) fallSpeed = -2;
            fallSpeed = Mathf.Max(fallSpeed - 22 * Time.deltaTime, -35);
            Vector3 movement = (transform.right * input.x + transform.forward * input.y).normalized * smoothedSpeed;
            controller.Move((movement + Vector3.up * fallSpeed) * Time.deltaTime);
            float actualSpeed = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;
            float weight = controller.isGrounded ? Mathf.Clamp01(actualSpeed / 3.3f) : 0;
            motionWeight = Mathf.Lerp(motionWeight, weight, 1 - Mathf.Exp(-Time.deltaTime * 5));
            float gaitSpeed = running ? 6.2f : 3.3f;
            float frequency = running ? settings.runSwayFrequency : settings.walkSwayFrequency;
            stride += Mathf.Clamp01(actualSpeed / gaitSpeed) * frequency * Mathf.PI * 2 * Time.deltaTime;
            organicTime += Time.deltaTime * motionWeight;
            float intensity = ReducedMotion ? 0 : settings.headBob;
            float drift = (Mathf.PerlinNoise(organicTime * .35f, 17.3f) - .5f) * 2;
            float vertical = Mathf.Cos(stride * 2) * .018f + Mathf.Sin(stride * .73f) * .005f;
            float sideways = Mathf.Sin(stride) * .021f + drift * .008f;
            var targetSway = new Vector3(sideways, vertical * (running ? 1.25f : 1), Mathf.Sin(stride * 2 + .7f) * .004f)
                * (motionWeight * intensity);
            sway = Vector3.SmoothDamp(sway, targetSway, ref swayVelocity, settings.swaySmoothing);
            View.transform.localPosition = Vector3.up * 1.65f + sway;
            lean = Mathf.Lerp(lean, -input.x * .65f - delta.x * .025f, 1 - Mathf.Exp(-Time.deltaTime * 3));
            var targetRotation = new Vector3((Mathf.Sin(stride * 2) * .18f + drift * .12f) * motionWeight,
                drift * .10f * motionWeight, lean + Mathf.Sin(stride) * .35f * motionWeight) * intensity;
            rotationSway = Vector3.SmoothDamp(rotationSway, targetRotation, ref rotationVelocity, settings.swaySmoothing);
            if (ReducedMotion) { sway = rotationSway = swayVelocity = rotationVelocity = Vector3.zero; View.transform.localPosition = Vector3.up * 1.65f; }
            View.transform.localRotation = Quaternion.Euler(new Vector3(pitch, 0, 0) + rotationSway);
            View.fieldOfView = Mathf.Lerp(View.fieldOfView, settings.fieldOfView + (running && !ReducedMotion ? 3 : 0), 1 - Mathf.Exp(-Time.deltaTime * 2));
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus) { MenuOpen = true; LockCursor(false); }
        }
        private void OnDisable() => LockCursor(false);
    }
}
