using UnityEngine;

namespace Ermo
{
    public static class GodotTerrain
    {
        // Port of authored_terrain.gd: sum(height * exp(-2 * ellipticalDistance²)).
        // Only the continuation is Unity-specific: neighboring copies retain infinite streaming.
        public static float Contours(WorldSettings settings, float x, float z)
        {
            float undulation = 0;
            if (settings.organicHills)
            {
                float phase = (WorldHash.Of(settings.seed)%10000)*.01f;
                float wx = (Mathf.PerlinNoise(phase+x*.003f,phase+z*.003f)-.5f)*2*settings.terrainWarp;
                float wz = (Mathf.PerlinNoise(phase+37+x*.0027f,phase+19+z*.0027f)-.5f)*2*settings.terrainWarp;
                undulation = settings.terrainWaveHeight * (.65f*Mathf.Sin(x/118+Mathf.Sin(z/173)*.8f+phase)
                    + .35f*Mathf.Cos(z/143-x/219+phase));
                undulation += settings.terrainWaveHeight*.32f
                    * (Mathf.PerlinNoise(phase+127+x*.011f,phase+78+z*.011f)-.5f)*2;
                x += wx; z += wz;
            }
            float sx = Mathf.Max(64, settings.contourRepeat.x), sz = Mathf.Max(64, settings.contourRepeat.y);
            int cx = Mathf.FloorToInt((x + sx * .5f) / sx), cz = Mathf.FloorToInt((z + sz * .5f) / sz);
            float result = 0;
            if (settings.contours == null) return result;
            float spread = Mathf.Max(1,settings.contourWidthScale);
            float widestX = 1, widestZ = 1;
            foreach (var hill in settings.contours)
                if (hill != null) { widestX = Mathf.Max(widestX,hill.width.x*spread); widestZ = Mathf.Max(widestZ,hill.width.y*spread); }
            // Broader hills need more neighbors; otherwise the repeated field can jump at tile boundaries.
            int radiusX = Mathf.Max(1,Mathf.CeilToInt(widestX*2/sx));
            int radiusZ = Mathf.Max(1,Mathf.CeilToInt(widestZ*2/sz));
            // Gaussian separability avoids evaluating every X/Z pair as hills get broader.
            foreach (var hill in settings.contours)
            {
                if (hill == null) continue;
                float sumX = 0, sumZ = 0;
                for (int dx = -radiusX; dx <= radiusX; dx++)
                {
                    float p = (x-(cx+dx)*sx-hill.position.x)/Mathf.Max(1,hill.width.x*spread);
                    sumX += Mathf.Exp(-2*p*p);
                }
                for (int dz = -radiusZ; dz <= radiusZ; dz++)
                {
                    float p = (z-(cz+dz)*sz-hill.position.z)/Mathf.Max(1,hill.width.y*spread);
                    sumZ += Mathf.Exp(-2*p*p);
                }
                result += hill.position.y*settings.contourHeightScale*sumX*sumZ;
            }
            return result+undulation;
        }

        public static float Waves(string seed, float x, float z)
        {
            int number;
            if (!int.TryParse(seed, out number)) number = (int)(WorldHash.Of(seed) % 1000000);
            float phase = ((number % 997) - 225) * .001f;
            return 4.5f * Mathf.Sin((z + 4) / 21 + phase) + 4 * Mathf.Sin(x / 31 + z / 68)
                + 2.8f * Mathf.Cos(x / 61 - z / 88) + 1.6f * Mathf.Cos(x / 19 - z / 27);
        }
    }
}
