using UnityEngine;

namespace Ermo
{
    public static class GodotAtmosphere
    {
        public static void Apply(WorldSettings settings, bool updateSceneEnvironment = true)
        {
            float range = settings.chunkSize*settings.viewDistance;
            bool reference = settings.fogStyle == FogStyle.GodotExponential;
            Color tint = reference ? new Color(.35f,.64f,.82f) : settings.hazeColor;
            float start = range*settings.fogStartFraction;
            float end = Mathf.Max(start+1,range*settings.fogFullFraction);
            if (updateSceneEnvironment)
            {
                RenderSettings.fog = true; RenderSettings.fogColor = tint;
                RenderSettings.fogMode = reference ? FogMode.Exponential : FogMode.Linear;
                RenderSettings.fogDensity = settings.godotFogDensity;
                RenderSettings.fogStartDistance = start; RenderSettings.fogEndDistance = end;
            }
            Shader.SetGlobalColor("_ErmoHazeColor",tint.linear);
            Shader.SetGlobalVector("_ErmoHazeParams",new Vector4(start,end,
                settings.fogOpticalDepth,settings.fogCloudiness));
            Shader.SetGlobalFloat("_ErmoViewRange",range);
            Shader.SetGlobalFloat("_ErmoHazeHeight",settings.hillHeight*1.15f);
            Shader.SetGlobalFloat("_ErmoFogStyle",reference ? 1 : 0);
            Shader.SetGlobalVector("_ErmoGodotFog",new Vector4(settings.godotFogDensity,settings.fogAerialPerspective,.65f,.04f));
            Color ambient = new Color(.48f,.68f,.92f).linear*.5f; ambient.a = 1;
            Shader.SetGlobalColor("_ErmoAmbientColor",ambient);
            Shader.SetGlobalFloat("_ErmoFoundFootage",settings.foundFootage ? 1 : 0);
        }
    }
}
