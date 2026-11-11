using UnityEngine;
using UnityEngine.Rendering;

namespace Ermo
{
    public sealed class BalnearioReflections : MonoBehaviour
    {
        private ReflectionProbe probe;
        private int render = -1;
        private float nextCapture;
        private void Awake()
        {
            probe = gameObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime; probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution = 128; probe.hdr = true; probe.size = Vector3.one*600;
            probe.nearClipPlane = .15f; probe.farClipPlane = 260;
            probe.cullingMask = ~(1 << 4); probe.clearFlags = ReflectionProbeClearFlags.Skybox;
        }
        private void Update()
        {
            if (render >= 0 && probe.IsFinishedRendering(render))
            {
                Shader.SetGlobalTexture("_BalnearioReflection",probe.texture);
                Shader.SetGlobalVector("_BalnearioReflectionHDR",probe.textureHDRDecodeValues);
                Shader.SetGlobalFloat("_BalnearioReflectionReady",1); render = -1;
            }
            if (render >= 0 || Time.time < nextCapture || Camera.main == null) return;
            transform.position = Camera.main.transform.position+Vector3.up*.5f;
            render = probe.RenderProbe(); nextCapture = Time.time+2;
        }
        private void OnDisable() => Shader.SetGlobalFloat("_BalnearioReflectionReady",0);
    }
}
