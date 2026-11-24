using UnityEngine;

namespace Ermo
{
    public static class GodotAtmosphere
    {
        public static void Apply(WorldSettings settings, bool updateSceneEnvironment = true)
        {
            float range = settings.chunkSize*settings.viewDistance;
            bool reference = settings.fogStyle == FogStyle.GodotExponential;
            // The sunset haze is authored per world; never replace it with a fixed sky blue.
            Color tint = settings.hazeColor;
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
            Color ambient = new Color(.84f,.63f,.51f).linear*.20f; ambient.a = 1;
            Shader.SetGlobalColor("_ErmoAmbientColor",ambient);
            // Default low afternoon sun; ErmoBootstrap replaces it with the live light.
            Shader.SetGlobalVector("_ErmoSunDirection",new Vector4(.32f,.28f,-.90f,0));
            Shader.SetGlobalFloat("_ErmoFoundFootage",settings.foundFootage ? 1 : 0);
        }
    }
}
