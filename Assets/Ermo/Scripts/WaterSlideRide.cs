using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ermo
{
    [DefaultExecutionOrder(30)]
    public sealed class WaterSlideRide : MonoBehaviour
    {
        private Vector3[] path;
        private float[] distances;
        private float distance, speed, diveTime;
        private float surface, floor;
        private FirstPersonWalker walker;
        private Vector3 entry, diveStart, diveEnd;
        private bool riding, diving;

        public void Initialize(List<Vector3> points, float waterSurface, float poolFloor)
        {
            path = points.ToArray(); surface = waterSurface; floor = poolFloor;
            distances = new float[path.Length];
            for (int i = 1; i < path.Length; i++) distances[i] = distances[i-1]+Vector3.Distance(path[i-1],path[i]);
        }
        private void Update()
        {
            if (path == null) return;
            if (walker == null) walker = FindFirstObjectByType<FirstPersonWalker>();
            if (walker == null || walker.MenuOpen) return;
            if (!riding)
            {
                if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame ||
                    Cursor.lockState != CursorLockMode.Locked || !TryBegin(walker)) return;
            }
            Advance(Time.deltaTime);
        }
        public Vector3 Launch => transform.TransformPoint(path[0]-Vector3.up*1.12f);
        public bool Riding => riding;
        public bool TryBegin(FirstPersonWalker player)
        {
            if (path == null || riding || player == null || Vector3.Distance(player.transform.position,Launch) > 2.1f || !player.BeginTraversal(true)) return false;
            walker = player; entry = transform.InverseTransformPoint(player.transform.position);
            distance = diveTime = 0; speed = 2; riding = true; diving = false;
            return true;
        }
        public void Advance(float dt)
        {
            if (!riding || walker == null || walker.MenuOpen || dt <= 0) return;
            if (diving) { TickDive(dt); return; }
            int index = 1;
            while (index < distances.Length-1 && distances[index] < distance) index++;
            float t = Mathf.InverseLerp(distances[index-1],distances[index],distance);
            Vector3 tangent = (path[index]-path[index-1]).normalized;
            speed = Mathf.Clamp(speed+(2.2f-9.81f*tangent.y)*dt,2,17);
            Vector3 p = Vector3.Lerp(path[index-1],path[index],t)-Vector3.up*1.08f;
            p = Vector3.Lerp(entry,p,Mathf.SmoothStep(0,1,distance/1.5f));
            Pose(p,tangent,Mathf.Lerp(1.65f,.38f,Mathf.SmoothStep(0,1,distance/1.5f)));
            distance += speed*dt;
            if (distance >= distances[distances.Length-1])
            {
                diving = true; diveTime = 0; diveStart = path[path.Length-1]-Vector3.up*1.08f;
                // Remain inside the runout pool instead of launching across its far wall.
                diveEnd = diveStart; diveEnd.y = floor+.025f;
            }
        }
        private void TickDive(float dt)
        {
            diveTime += dt;
            float t = Mathf.Clamp01(diveTime/1.45f);
            Vector3 p = Vector3.Lerp(diveStart,diveEnd,Mathf.SmoothStep(0,1,Mathf.Clamp01(t*2)));
            if (surface-floor > 1.7f && t > .5f)
                p.y = Mathf.Lerp(diveEnd.y,surface-1.25f,Mathf.SmoothStep(0,1,(t-.5f)*2));
            float eye = Mathf.Lerp(.25f,1.65f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,1,t)));
            Vector3 forward = path[path.Length-1]-path[path.Length-2]; forward.y = 0;
            Pose(p,forward,eye);
            walker.GetComponent<WaterPassage>()?.SetSlideImmersion(p.y+eye < surface);
            if (t >= 1) { riding = false; walker.EndTraversal(); }
        }
        private void Pose(Vector3 local, Vector3 tangent, float eye)
        {
            Vector3 forward = transform.TransformDirection(tangent);
            float yaw = Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg;
            float pitch = -Mathf.Asin(Mathf.Clamp(forward.normalized.y,-1,1))*Mathf.Rad2Deg-12;
            walker.SetSlidePose(transform.TransformPoint(local),yaw,pitch,eye);
        }
        private void OnDisable()
        {
            if (!riding || walker == null) return;
            walker.GetComponent<WaterPassage>()?.SetSlideImmersion(false);
            walker.EndTraversal(); riding = false;
        }
    }
}
