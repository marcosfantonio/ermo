using System;
using UnityEngine;

namespace Ermo
{
    [CreateAssetMenu(menuName = "Ermo/World Settings")]
    public sealed class WorldSettings : ScriptableObject
    {
        [Header("World identity — change the seed to create another world")]
        public string seed = "ermo-001";
        [Min(32)] public int chunkSize = 96;
        [Range(12, 64)] public int terrainResolution = 24;
        [Range(2, 12)] public int viewDistance = 8;
        [Min(1)] public int chunksPerFrame = 3;
        [Header("Landscape")]
        public float hillHeight = 19f;
        public float hillScale = 180f;
        public float roadWidth = 2.8f;
        public TerrainStyle terrainStyle = TerrainStyle.GodotContours;
        public bool flattenYards = true;
        [Range(.2f, 1.5f)] public float contourHeightScale = .65f;
        [Range(1, 2.5f)] public float contourWidthScale = 1.65f;
        public bool smoothTerrainNormals = true;
        public bool organicHills = true;
        [Range(0, 60)] public float terrainWarp = 24;
        [Range(0, 10)] public float terrainWaveHeight = 3.2f;
        public Vector2 contourRepeat = new Vector2(300, 276);
        public HillContour[] contours =
        {
            new HillContour(-111,23,-12,30,115), new HillContour(-8,25,-103,121,30),
            new HillContour(110,20,4,35,108), new HillContour(18,17,110,125,37),
            new HillContour(9,10,-9,38,38), new HillContour(-48,5,-37,40,48),
            new HillContour(51,6,-39,42,40)
        };
        [Header("Locations — spacing applies to whole groups, not their members")]
        [Min(80)] public float locationSpacing = 180;
        [Min(60)] public float minimumLocationDistance = 150;
        [Range(0, 1)] public float locationChance = 1;
        [Range(0, 1)] public float houseChance = .8f;
        [Range(0, 1)] public float waterTowerChance = .3f;
        [Range(0, 1)] public float turbineChance = .12f;
        [Header("Camera")]
        [Tooltip("0 = native screen resolution; otherwise caps the internal height.")]
        [Range(0, 2160)] public int renderHeight = 540;
        public bool godotPixelGrid;
        public bool pixelatedUpscale = true;
        [Range(50, 100)] public float fieldOfView = 73f;
        [Range(0, .3f)] public float lensDistortion = .042f;
        [Range(0, 2)] public float headBob = 1f;
        [Range(.3f, 1.2f)] public float walkSwayFrequency = .68f;
        [Range(.5f, 1.8f)] public float runSwayFrequency = 1f;
        [Range(.05f, .4f)] public float swaySmoothing = .16f;
        [Header("Atmosphere")]
        public Color hazeColor = new Color(.69f, .80f, .85f);
        [Range(0, .98f)] public float fogStartFraction = .85f;
        [Range(.5f, 1f)] public float fogFullFraction = .995f;
        [Range(1, 6)] public float fogOpticalDepth = 3.8f;
        [Range(0, .6f)] public float fogCloudiness = .28f;
        public FogStyle fogStyle = FogStyle.GodotExponential;
        [Range(0, .02f)] public float godotFogDensity = .0035f;
        [Range(0, 1)] public float fogAerialPerspective = .3f;
        [Header("Godot materials / memory optics")]
        public Texture2D grassTexture, plasterTexture, roofTexture, woodTexture;
        [Range(0, 1)] public float vertexWobble = .035f;
        [Range(0, 1)] public float filmGrain = .014f;
        [Range(0, 1)] public float vignette = .18f;
        [Range(0, 2)] public float chromatic = .3f;
        [Range(0, 1)] public float dither = .08f;
        [Header("Found footage — subtle camcorder, not heavy VHS")]
        public bool foundFootage = true;
        [Range(0, 1)] public float captureSoftness = .22f;
        [Range(0, 2)] public float captureSaturation = 1.22f;
        [Range(.5f, 1.5f)] public float captureContrast = 1.05f;
        [Range(0, 1)] public float captureInstability = .16f;
        [Range(0, 1)] public float analogArtifacts = .32f;
        public bool legacyVhs;
        [Range(0, 1)] public float tapeStrength = .7f;
        [Range(0, 2)] public float tapeSaturation = 1.45f;
        [Range(.5f, 1.5f)] public float tapeContrast = 1.06f;
        [Header("Audio — ambience and surface-aware footsteps")]
        public AudioClip breezeClip, grassStepClip, dirtStepClip, cementStepClip;
        [Range(0, 1)] public float audioVolume = .7f;
        [Range(0, 1)] public float breezeVolume = .18f;
        [Range(0, 1)] public float footstepVolume = .48f;
        [Header("Future authored locations (IDs must remain stable)")]
        public LandmarkRule[] landmarks =
        {
            new LandmarkRule { id = "open-door-house", kind = PropKind.OpenHouse,
                chance = .22f, spacingMeters = 550, minimumDistance = 200 }
        };
    }

    public enum PropKind { House, OpenHouse, WaterTower, Turbine, Pole }
    public enum TerrainStyle { Perlin, GodotContours, GodotLegacyWaves }
    public enum FogStyle { HorizonOnly, GodotExponential }

    [Serializable]
    public sealed class HillContour
    {
        public Vector3 position;
        public Vector2 width;
        public HillContour(float x, float height, float z, float widthX, float widthZ)
        {
            position = new Vector3(x, height, z); width = new Vector2(widthX, widthZ);
        }
    }

    [Serializable]
    public sealed class LandmarkRule
    {
        public string id = "landmark";
        public PropKind kind = PropKind.OpenHouse;
        public bool exactChunk;
        public Vector2Int chunk;
        public Vector2 localPosition = new Vector2(48, 48);
        public bool includeWaterTower;
        public bool includeTurbine;
        [Min(96)] public float spacingMeters = 550;
        [Min(0)] public float minimumDistance = 200;
        [Range(0, 1)] public float chance = .22f;
    }
}
