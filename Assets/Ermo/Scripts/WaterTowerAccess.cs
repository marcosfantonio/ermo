using System.Collections.Generic;
using UnityEngine;

namespace Ermo
{
    // Pure tower-local coordinates; generated prop rotations/scales and old saves
    // keep working without changing the procedural distribution or persistence.
    public sealed class WaterTowerAccess : MonoBehaviour
    {
        public const float RimHeight = 12.05f;
        public const float SurfaceHeight = 11.35f;
        public const float BottomSurfaceHeight = 8.62f;
        public const float InnerRadius = 1.55f;
        public static readonly List<WaterTowerAccess> Live = new List<WaterTowerAccess>();
        public Vector3 SurfaceCenter => transform.TransformPoint(Vector3.up*SurfaceHeight);
        public Vector3 BottomSurfaceCenter => transform.TransformPoint(Vector3.up*BottomSurfaceHeight);
        public Vector3 LadderPoint(float height) => transform.TransformPoint(new Vector3(0,height,-2.95f));
        public Vector3 TopLanding => transform.TransformPoint(new Vector3(0,RimHeight+.10f,-2.15f));
        private void OnEnable() { if (Application.isPlaying && !Live.Contains(this)) Live.Add(this); }
        private void OnDisable() => Live.Remove(this);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => Live.Clear();

        public bool NearLadder(Vector3 player)
        {
            Vector3 local = transform.InverseTransformPoint(player);
            return local.y >= -1.5f && local.y <= RimHeight+.5f &&
                new Vector2(local.x,local.z+2.95f).magnitude*transform.lossyScale.x < 1.25f;
        }

        public bool ContainsHead(Vector3 head)
        {
            Vector3 local = transform.InverseTransformPoint(head);
            return local.y <= SurfaceHeight && local.y > SurfaceHeight-3.5f &&
                new Vector2(local.x,local.z).magnitude < InnerRadius-.18f;
        }
    }
}
