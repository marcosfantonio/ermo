using System.Collections.Generic;
using UnityEngine;

namespace Ermo
{
    // Cold-room door. It turns on a hinge at the edge of its collider, never
    // around the middle of the mesh, and always swings away from whoever
    // pushed it, so the leaf cannot sweep through the camera.
    public sealed class ColdDoor : MonoBehaviour
    {
        const float OpenAngle = 92f;
        const float Speed = 120f;
        static AudioClip openClip, closeClip;
        // Trigger collider in each door frame -> its door (see AtacarejoUse).
        static readonly Dictionary<Collider, ColdDoor> doorways = new Dictionary<Collider, ColdDoor>();
        Transform hinge;
        Quaternion closedRotation;
        float angle, target;
        bool open;

        public bool IsOpen => open;
        public float Angle => Mathf.Abs(angle);
        public Transform Hinge => hinge;
        // "a câmara" for room 13, "o frigorífico" for the locker behind it.
        public string Noun { get; set; } = "a câmara";
        public string Prompt => (open ? "Aperte E para fechar " : "Aperte E para abrir ") + Noun;

        void Awake()
        {
            var box = GetComponent<BoxCollider>();
            Vector3 center = box != null ? box.center : Vector3.zero;
            float half = box != null ? box.size.z * .5f : .8f;
            hinge = new GameObject("Dobradiça — " + name).transform;
            hinge.SetParent(transform.parent, false);
            hinge.localPosition = transform.localPosition + transform.localRotation * new Vector3(center.x, 0f, center.z + half);
            hinge.localRotation = transform.localRotation;
            hinge.localScale = Vector3.one;
            transform.SetParent(hinge, true);
            closedRotation = hinge.localRotation;
            // The opening itself, as a trigger that stays where the closed leaf
            // was: with the door open, looking at the gap still offers "close".
            var gap = new GameObject("Vão — " + name);
            gap.transform.SetParent(hinge.parent, false);
            gap.transform.localPosition = hinge.localPosition - hinge.localRotation * new Vector3(0f, 0f, half);
            gap.transform.localRotation = hinge.localRotation;
            var trigger = gap.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, box != null ? box.center.y : 1.2f, 0f);
            trigger.size = box != null ? box.size : new Vector3(.2f, 2.4f, 1.6f);
            doorways[trigger] = this;
            if (openClip == null) openClip = Resources.Load<AudioClip>("Ermo/Audio/door_open");
            if (closeClip == null) closeClip = Resources.Load<AudioClip>("Ermo/Audio/door_close");
        }

        public static ColdDoor FromDoorway(Collider collider) =>
            collider != null && doorways.TryGetValue(collider, out var door) && door != null ? door : null;

        void OnDestroy()
        {
            var stale = new List<Collider>();
            foreach (var pair in doorways) if (pair.Value == this || pair.Key == null) stale.Add(pair.Key);
            foreach (var key in stale) doorways.Remove(key);
        }

        public void Toggle(Vector3 from)
        {
            open = !open;
            if (open)
            {
                // In hinge space the leaf lies along -Z. A positive turn sends it
                // to -X, so open toward the side the pusher is not standing on.
                Vector3 local = hinge.InverseTransformPoint(from);
                target = local.x >= 0f ? OpenAngle : -OpenAngle;
            }
            else target = 0f;
            var clip = open ? openClip : closeClip;
            if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position, .55f);
        }

        void Update()
        {
            if (hinge == null) return;
            angle = Mathf.MoveTowards(angle, target, Speed * Time.deltaTime);
            hinge.localRotation = closedRotation * Quaternion.Euler(0f, angle, 0f);
        }
    }
}
