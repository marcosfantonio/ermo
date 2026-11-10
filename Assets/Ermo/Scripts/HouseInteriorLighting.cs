using System.Collections.Generic;
using UnityEngine;

namespace Ermo
{
    // A room and its apertures, not a camera effect. No global lighting or shared assets are changed.
    public sealed class HouseInteriorLighting : MonoBehaviour
    {
        public const int MaximumPortals = 8;
        private readonly List<Material> materials = new List<Material>();
        private readonly Vector4[] portalMin = new Vector4[MaximumPortals];
        private readonly Vector4[] portalMax = new Vector4[MaximumPortals];
        private readonly HouseInteractable[] openings = new HouseInteractable[MaximumPortals];
        private int portalCount;
        private Bounds room;
        private Matrix4x4 lastMatrix;

        public Bounds RoomBounds => room;
        public int PortalCount => portalCount;

        public void Initialize(Material wallMaterial, Material roofMaterial, bool procedural = false, bool doorOpen = false)
        {
            var renderers = GetComponentsInChildren<MeshRenderer>(true);
            Vector4 roofPlane = Vector4.zero;
            if (procedural)
                room = new Bounds(new Vector3(0, 2.575f, 0), new Vector3(6.75f, 4.35f, 7.75f));
            else if (!TryFindRoom(renderers, wallMaterial, out room))
            {
                Debug.LogWarning("Cannot identify enclosed house walls; leaving lighting unchanged.", this);
                return;
            }
            if (!procedural)
            {
                foreach (var renderer in renderers)
                {
                    if (renderer.sharedMaterial == roofMaterial)
                    {
                        Bounds roof = LocalBounds(renderer);
                        if (roof.min.x > room.center.x || roof.max.x < room.center.x ||
                            roof.min.z > room.center.z || roof.max.z < room.center.z) continue;
                        // This model has an X-sloped pitched roof. Its gable top meets the inner ridge.
                        // Clip the room below those slopes, not an axis-aligned box above the roof.
                        roofPlane = new Vector4(roof.center.x, room.max.y,
                            Mathf.Max(0, room.max.y - roof.min.y)/Mathf.Max(.01f, roof.extents.x), 1);
                        break;
                    }
                }
            }

            // Capture CLOSED aperture bounds before Bind randomizes door/window rotations.
            foreach (var opening in GetComponentsInChildren<HouseInteractable>(true))
            {
                if (portalCount == MaximumPortals)
                {
                    Debug.LogWarning("House has more openings than the interior shader supports.", this);
                    break;
                }
                bool found = false;
                Bounds aperture = default;
                foreach (var renderer in opening.GetComponentsInChildren<MeshRenderer>(true))
                {
                    Bounds bounds = LocalBounds(renderer);
                    if (!found) { aperture = bounds; found = true; }
                    else aperture.Encapsulate(bounds);
                }
                if (!found) continue;
                openings[portalCount] = opening;
                SetPortal(portalCount++, aperture, 0);
            }
            if (procedural)
                SetPortal(portalCount++, new Bounds(new Vector3(0, 1.7f, -4), new Vector3(1.6f, 2.6f, .25f)), doorOpen ? 1 : 0);

            // Reuse clones within this house, never mutate the palette shared by other props/chunks.
            var copies = new Dictionary<Material, Material>();
            foreach (var renderer in renderers)
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    Material source = slots[i];
                    if (source == null || source.shader.name != "Ermo/Meadow") continue;
                    if (!copies.TryGetValue(source, out Material copy))
                    {
                        copy = new Material(source) { name = source.name + " — room lighting" };
                        copy.SetFloat("_ErmoInteriorEnabled", 1);
                        copy.SetVector("_ErmoInteriorMin", room.min);
                        copy.SetVector("_ErmoInteriorMax", room.max);
                        copy.SetVector("_ErmoInteriorCeiling", roofPlane);
                        copy.SetFloat("_ErmoInteriorRoof", source == roofMaterial ? 1 : 0);
                        copy.SetInteger("_ErmoPortalCount", portalCount);
                        copies.Add(source, copy);
                        materials.Add(copy);
                    }
                    slots[i] = copy;
                }
                renderer.sharedMaterials = slots;
            }
            Refresh(true);
        }

        private bool TryFindRoom(MeshRenderer[] renderers, Material wallMaterial, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            float thicknessX = float.PositiveInfinity, thicknessZ = float.PositiveInfinity;
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial != wallMaterial) continue;
                Bounds panel = LocalBounds(renderer);
                Vector3 size = panel.size;
                // Ignore porch columns: only long, thin wall panels bound the enclosed room.
                if (size.y < .5f || Mathf.Max(size.x, size.z) < .6f || Mathf.Min(size.x, size.z) > .4f) continue;
                if (size.x < size.z) thicknessX = Mathf.Min(thicknessX, size.x);
                else thicknessZ = Mathf.Min(thicknessZ, size.z);
                if (!found) { bounds = panel; found = true; }
                else bounds.Encapsulate(panel);
            }
            if (!found || float.IsPositiveInfinity(thicknessX) || float.IsPositiveInfinity(thicknessZ)) return false;
            Vector3 inset = new Vector3(thicknessX, 0, thicknessZ);
            bounds.SetMinMax(bounds.min + inset, bounds.max - inset);
            return bounds.size.x > 0 && bounds.size.z > 0;
        }

        private Bounds LocalBounds(Renderer renderer)
        {
            Bounds source = renderer.localBounds;
            Matrix4x4 matrix = transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            Bounds result = new Bounds(matrix.MultiplyPoint3x4(source.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = source.center + Vector3.Scale(source.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                result.Encapsulate(matrix.MultiplyPoint3x4(corner));
            }
            return result;
        }

        private void SetPortal(int index, Bounds aperture, float amount)
        {
            Vector3 min = aperture.min, max = aperture.max;
            portalMin[index] = new Vector4(min.x, min.y, min.z, amount);
            portalMax[index] = new Vector4(max.x, max.y, max.z, 0);
        }

        private void LateUpdate() => Refresh(false);

        public void RefreshOpenings() => Refresh(true);

        private void Refresh(bool force)
        {
            bool changed = force || lastMatrix != transform.worldToLocalMatrix;
            for (int i = 0; i < portalCount; i++)
            {
                if (openings[i] == null) continue;
                float amount = openings[i].OpeningFraction;
                changed |= Mathf.Abs(portalMin[i].w - amount) > .0001f;
                portalMin[i].w = amount;
            }
            if (!changed) return;
            lastMatrix = transform.worldToLocalMatrix;
            foreach (var material in materials)
            {
                material.SetMatrix("_ErmoInteriorWorldToLocal", lastMatrix);
                material.SetVectorArray("_ErmoPortalMin", portalMin);
                material.SetVectorArray("_ErmoPortalMax", portalMax);
            }
        }

        private void OnDestroy()
        {
            foreach (var material in materials)
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
        }
    }
}
