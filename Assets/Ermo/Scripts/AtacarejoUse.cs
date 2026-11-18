using UnityEngine;
using UnityEngine.InputSystem;

namespace Ermo
{
    // E inside the market: looks through the centre of the screen for a
    // cold-room door (ColdDoor) or a way out (AtacarejoExitDoor), shows the
    // prompt and uses it.
    public sealed class AtacarejoUse : MonoBehaviour
    {
        public const float Reach = 3.5f;
        FirstPersonWalker walker;
        GUIStyle promptStyle;

        public Component Target { get; private set; }
        public string PromptText { get; private set; }

        void Awake() => walker = GetComponent<FirstPersonWalker>();

        // The batch test calls this with requireCursor false: a batch editor
        // has no locked cursor.
        public Component Probe(bool requireCursor = true)
        {
            Target = null;
            PromptText = null;
            if (!CanUse(requireCursor)) return null;
            Ray ray = walker.View.ViewportPointToRay(new Vector3(.5f, .5f));
            bool solid = Physics.Raycast(ray, out var hit, Reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var door = solid ? hit.collider.GetComponentInParent<ColdDoor>() : null;
            var exit = solid ? hit.collider.GetComponentInParent<AtacarejoExitDoor>() : null;
            if (door == null && exit == null) door = OpenDoorInFrame(ray, solid ? hit.distance + .05f : Reach);
            if (door != null) { Target = door; PromptText = door.Prompt; }
            else if (exit != null) { Target = exit; PromptText = exit.Prompt; }
            return Target;
        }

        public bool Activate()
        {
            switch (Target)
            {
                case ColdDoor door:
                    door.Toggle(walker.View.transform.position);
                    return true;
                case AtacarejoExitDoor exit:
                    Target = null; PromptText = null;
                    return exit.Use();
                default:
                    return false;
            }
        }

        bool CanUse(bool requireCursor)
        {
            if (walker == null || walker.View == null || walker.MenuOpen || walker.TraversalActive) return false;
            var market = AtacarejoWorld.Instance;
            if (market == null || !market.Started || market.Looping || PhaseTransition.Busy) return false;
            return !requireCursor || Cursor.lockState == CursorLockMode.Locked;
        }

        // An open door has no leaf in the frame any more: aim at the empty frame.
        static ColdDoor OpenDoorInFrame(Ray ray, float distance)
        {
            foreach (var gate in Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                var door = ColdDoor.FromDoorway(gate.collider);
                if (door != null) return door;
            }
            return null;
        }

        void Update()
        {
            Probe();
            if (Target != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) Activate();
        }

        void OnGUI()
        {
            if (string.IsNullOrEmpty(PromptText) || walker == null || walker.MenuOpen) return;
            if (promptStyle == null)
                promptStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 18, fontStyle = FontStyle.Bold };
            float width = Mathf.Max(340f, Screen.width * .26f);
            GUI.Box(new Rect((Screen.width - width) * .5f, Screen.height * .62f, width, 40f), PromptText, promptStyle);
        }
    }
}
