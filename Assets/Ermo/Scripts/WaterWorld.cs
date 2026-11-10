using UnityEngine;

namespace Ermo
{
    public sealed class WaterWorld : MonoBehaviour
    {
        public const string ScenePath = "Assets/Ermo/Scenes/WaterWorld.unity";
        public static WaterWorld Instance { get; private set; }
        public Vector3 Arrival => transform.position+Vector3.down*.55f;
        public float SurfaceY => transform.position.y;
        public Material Sky { get; private set; }
        private GameObject environment;
        private Material water;
        private Mesh waterMesh;
        private Light sun;

        private void Awake()
        {
            Instance = this;
            environment = new GameObject("Water, sky and invisible pool floor");
            environment.transform.SetParent(transform,false);
            water = new Material(Resources.Load<Shader>("Ermo/Water/Water"));
            Sky = new Material(Resources.Load<Shader>("Ermo/Water/WaterSky"));
            waterMesh = new Mesh { name = "Water world surface" };
            waterMesh.vertices = new[] { new Vector3(-2048,0,-2048),new Vector3(-2048,0,2048),
                new Vector3(2048,0,2048),new Vector3(2048,0,-2048) };
            waterMesh.triangles = new[] { 0,1,2,0,2,3 }; waterMesh.RecalculateNormals(); waterMesh.RecalculateBounds();
            var surface = new GameObject("Water without a visible shore",typeof(MeshFilter),typeof(MeshRenderer));
            surface.transform.SetParent(environment.transform,false);
            surface.GetComponent<MeshFilter>().sharedMesh = waterMesh;
            surface.GetComponent<Renderer>().sharedMaterial = water;
            var floor = new GameObject("Submerged walkable floor",typeof(BoxCollider));
            floor.transform.SetParent(environment.transform,false); floor.transform.localPosition = Vector3.down*.65f;
            floor.GetComponent<BoxCollider>().size = new Vector3(4096,.2f,4096);
            var light = new GameObject("Water world afternoon",typeof(Light)); light.transform.SetParent(environment.transform,false);
            sun = light.GetComponent<Light>(); sun.type = LightType.Directional;
            sun.color = new Color(1,.94f,.8f); sun.intensity = 1.15f;
            light.transform.rotation = Quaternion.Euler(38,-35,0);
            SetVisible(false);
        }

        public void SetVisible(bool visible) => environment.SetActive(visible);
        public void ActivateEnvironment()
        {
            SetVisible(true); RenderSettings.skybox = Sky; RenderSettings.sun = sun;
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.24f,.63f,.73f);
            RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = .0015f;
        }
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (water != null) Destroy(water);
            if (Sky != null) Destroy(Sky);
            if (waterMesh != null) Destroy(waterMesh);
        }
    }
}
