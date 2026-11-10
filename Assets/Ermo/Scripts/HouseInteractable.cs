using UnityEngine;

namespace Ermo
{
    public sealed class HouseInteractable : MonoBehaviour
    {
        private static AudioClip doorOpenClip, doorCloseClip, windowOpenClip, windowCloseClip;
        private static bool soundsLoaded;
        private HouseHinge[] hinges;
        private Quaternion closedRotation;
        private Quaternion openRotation;
        private bool isDoor;
        private bool isOpen;

        public float OpeningFraction
        {
            get
            {
                if (isDoor) return Mathf.Clamp01(Quaternion.Angle(closedRotation, transform.rotation)/90f);
                if (hinges == null || hinges.Length == 0) return 0;
                float amount = 0;
                foreach (var hinge in hinges) amount += hinge.OpeningFraction;
                return amount/hinges.Length;
            }
        }

        public void Bind(string houseId, bool forceDoorOpen = false)
        {
            LoadSounds();
            isDoor = transform.name.ToLowerInvariant().Contains("door");
            closedRotation = transform.rotation;
            openRotation = Quaternion.AngleAxis(90f, Vector3.up) * closedRotation;
            hinges = GetComponentsInChildren<HouseHinge>(true);
            foreach (var hinge in hinges) hinge.Bind();
            var random = new System.Random(unchecked((int)WorldHash.Of(houseId + ":" + transform.name)));
            if (isDoor && (forceDoorOpen || random.NextDouble() < .16)) isOpen = true;
            else if (!isDoor && random.NextDouble() < .24)
                isOpen = true;
            ApplyOpenState(true);
        }

        public void Toggle(Vector3 actorPosition, Vector3 soundPosition)
        {
            isOpen = !isOpen;
            if (isOpen && isDoor)
            {
                // Swing the quarter-turn toward the side where the player is standing.
                float side = Vector3.Dot(actorPosition - transform.position, transform.forward);
                openRotation = Quaternion.AngleAxis(side >= 0 ? 90f : -90f, Vector3.up) * closedRotation;
            }
            ApplyOpenState(false);
            AudioClip clip = isDoor
                ? (isOpen ? doorOpenClip : doorCloseClip)
                : (isOpen ? windowOpenClip : windowCloseClip);
            if (clip != null) AudioSource.PlayClipAtPoint(clip, soundPosition, .72f);
        }

        private static void LoadSounds()
        {
            if (soundsLoaded) return;
            doorOpenClip = Resources.Load<AudioClip>("Ermo/Audio/door_open");
            doorCloseClip = Resources.Load<AudioClip>("Ermo/Audio/door_close");
            windowOpenClip = Resources.Load<AudioClip>("Ermo/Audio/window_open");
            windowCloseClip = Resources.Load<AudioClip>("Ermo/Audio/window_close");
            soundsLoaded = true;
        }

        private void ApplyOpenState(bool instantly)
        {
            if (isDoor)
            {
                if (instantly) transform.rotation = isOpen ? openRotation : closedRotation;
                return;
            }
            foreach (var hinge in hinges) hinge.SetOpen(isOpen, instantly);
        }

        private void Update()
        {
            if (!isDoor) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                isOpen ? openRotation : closedRotation, 240f * Time.deltaTime);
        }
    }

}
