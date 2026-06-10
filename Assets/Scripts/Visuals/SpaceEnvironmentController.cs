using UnityEngine;

[DefaultExecutionOrder(-200)]
public class SpaceEnvironmentController : MonoBehaviour
{
    [SerializeField] private Color skyTint = new Color(0.01f, 0.015f, 0.045f);
    [SerializeField] private Color horizonFog = new Color(0.045f, 0.065f, 0.095f);
    [SerializeField] private Color ambientColor = new Color(0.11f, 0.16f, 0.22f);
    [SerializeField] private Color keyLightColor = new Color(0.62f, 0.78f, 1f);
    [SerializeField] private int starCount = 360;
    [SerializeField] private float starRadius = 180f;

    private Transform starField;
    private Camera targetCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeEnvironment()
    {
        if (FindFirstObjectByType<SpaceEnvironmentController>() != null)
            return;

        GameObject environment = new GameObject("SpaceEnvironmentController");
        environment.AddComponent<SpaceEnvironmentController>();
    }

    private void Awake()
    {
        ApplyRenderSettings();
        ConfigureDirectionalLight();
        CreateStarField();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null && starField != null)
        {
            starField.position = targetCamera.transform.position;
        }
    }

    private void ApplyRenderSettings()
    {
        Shader skyShader = Shader.Find("Skybox/Procedural");

        if (skyShader != null)
        {
            Material skybox = new Material(skyShader);
            skybox.SetColor("_SkyTint", skyTint);
            skybox.SetColor("_GroundColor", horizonFog);
            skybox.SetFloat("_Exposure", 0.62f);
            skybox.SetFloat("_AtmosphereThickness", 0.18f);
            RenderSettings.skybox = skybox;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = horizonFog;
        RenderSettings.fogDensity = 0.006f;

        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.Skybox;
            Camera.main.backgroundColor = skyTint;
        }
    }

    private void ConfigureDirectionalLight()
    {
        Light directionalLight = null;
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && lights[i].type == LightType.Directional)
            {
                directionalLight = lights[i];
                break;
            }
        }

        if (directionalLight == null)
        {
            GameObject lightObject = new GameObject("Stargrave Key Light");
            directionalLight = lightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
        }

        directionalLight.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
        directionalLight.color = keyLightColor;
        directionalLight.intensity = 1.15f;
        directionalLight.shadows = LightShadows.Soft;
    }

    private void CreateStarField()
    {
        GameObject starObject = new GameObject("RuntimeStarField");
        starField = starObject.transform;

        ParticleSystem particles = starObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.startLifetime = 999999f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.82f, 1f), Color.white);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = starCount;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = starRadius;
        shape.radiusThickness = 0.08f;

        ParticleSystemRenderer renderer = starObject.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateStarMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        particles.Emit(starCount);
    }

    private Material CreateStarMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader);
        material.color = Color.white;

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.white * 1.8f);
        }

        return material;
    }
}
