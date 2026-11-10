using UnityEngine;

namespace Ermo
{
    public sealed class HouseHinge : MonoBehaviour
    {
        [SerializeField] private float openAngle = 90f;
        private Quaternion closedRotation;
        private Quaternion openRotation;
        private bool isOpen;

        public float OpeningFraction => Mathf.Clamp01(Quaternion.Angle(closedRotation, transform.rotation)/Mathf.Max(1, Mathf.Abs(openAngle)));

        public void Bind()
        {
            closedRotation = transform.rotation;
            float direction = transform.name.Contains("OpenN") ? 1f : -1f;
            openRotation = Quaternion.AngleAxis(openAngle * direction, Vector3.up) * closedRotation;
        }

        public void SetOpen(bool open, bool instantly)
        {
            isOpen = open;
            if (instantly) transform.rotation = isOpen ? openRotation : closedRotation;
        }

        private void Update()
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                isOpen ? openRotation : closedRotation, 240f * Time.deltaTime);
        }
    }
}
