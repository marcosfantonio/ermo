using UnityEngine;
using UnityEngine.InputSystem;

namespace Ermo
{
    public sealed class HouseInteraction : MonoBehaviour
    {
        private Camera view;
        private HouseInteractable target;
        private GUIStyle prompt;

        public void Initialize(Camera cameraView) => view = cameraView;

        private void Update()
        {
            target = null;
            if (view == null || Cursor.lockState != CursorLockMode.Locked || GetComponent<FirstPersonWalker>().TraversalActive) return;
            Ray ray = view.ViewportPointToRay(new Vector3(.5f, .5f));
            Vector3 soundPosition = transform.position;
            if (Physics.Raycast(ray, out var hit, 3.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                target = hit.collider.GetComponentInParent<HouseInteractable>();
                soundPosition = hit.point;
            }
            if (target != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                target.Toggle(transform.position, soundPosition);
        }

        private void OnGUI()
        {
            if (target == null) return;
            if (prompt == null)
                prompt = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height / 42f, 16, 26)),
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(.96f, .98f, .9f) }
                };
            const float width = 280, height = 42;
            GUI.Box(new Rect((Screen.width - width) * .5f, Screen.height * .57f, width, height),
                "Aperte E para interagir", prompt);
        }
    }
}
