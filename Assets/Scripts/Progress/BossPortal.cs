using UnityEngine;

public class BossPortal : MonoBehaviour
{
    private GameProgressManager progressManager;
    private Transform visual;
    private Transform ringVisual;
    private Transform portalTwirl;
    private Transform portalCircle;
    private Transform innerGate;
    private Transform player;
    private float enterRadius = 2.2f;

    public static BossPortal Create(Vector3 position, GameProgressManager manager)
    {
        GameObject portalObject = new GameObject("BossPortal");
        portalObject.transform.position = position;

        BossPortal portal = portalObject.AddComponent<BossPortal>();
        portal.progressManager = manager;
        portal.BuildVisual();

        return portal;
    }

    private void BuildVisual()
    {
        SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = enterRadius;

        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        CreateModel("Models/KenneySpace/template-floor-layer-hole", "PortalFloorPad", Vector3.zero, Quaternion.identity, Vector3.one * 2.8f);
        CreateModel("Models/KenneySpace/template-wall-half", "PortalLeftPylon", new Vector3(-1.8f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.2f);
        CreateModel("Models/KenneySpace/template-wall-half", "PortalRightPylon", new Vector3(1.8f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 2.2f);

        GameObject gateFrame = CreateModel("Models/KenneySpace/gate-door-window", "PortalGateFrame", Vector3.up * 0.08f, Quaternion.Euler(0f, 180f, 0f), Vector3.one * 2.55f);
        if (gateFrame == null)
        {
            gateFrame = CreateModel("Models/KenneySpace/gate-door", "PortalGateFrame", Vector3.up * 0.08f, Quaternion.Euler(0f, 180f, 0f), Vector3.one * 2.55f);
        }

        GameObject laserGate = CreateModel("Models/KenneySpace/gate-lasers", "PortalLaserGate", Vector3.up * 0.08f, Quaternion.Euler(0f, 180f, 0f), Vector3.one * 2.55f);
        if (laserGate != null)
        {
            innerGate = laserGate.transform;
        }

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "PortalRing";
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = Vector3.up * 1.1f;
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ring.transform.localScale = new Vector3(2.6f, 0.08f, 2.6f);
        Destroy(ring.GetComponent<Collider>());
        ApplyMaterial(ring, new Color(0.05f, 0.85f, 1f, 0.75f));
        ringVisual = ring.transform;

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "PortalCore";
        core.transform.SetParent(transform, false);
        core.transform.localPosition = Vector3.up * 1.1f;
        core.transform.localScale = Vector3.one * 1.45f;
        Destroy(core.GetComponent<Collider>());
        ApplyMaterial(core, new Color(0.25f, 0.15f, 1f, 0.65f));

        Light light = gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.1f, 0.85f, 1f);
        light.range = 8f;
        light.intensity = 3f;

        visual = core.transform;

        portalTwirl = GameVfx.CreatePersistentVfxQuad(
            "PortalTwirlSprite",
            transform,
            Vector3.up * 1.1f,
            Quaternion.identity,
            Vector3.one * 3.2f,
            "twirl_02",
            new Color(0.2f, 0.95f, 1f, 0.92f)
        );

        portalCircle = GameVfx.CreatePersistentVfxQuad(
            "PortalCircleSprite",
            transform,
            Vector3.up * 1.1f,
            Quaternion.identity,
            Vector3.one * 3.8f,
            "circle_04",
            new Color(0.7f, 0.25f, 1f, 0.82f)
        );
    }

    private void Update()
    {
        transform.Rotate(0f, 45f * Time.deltaTime, 0f);

        if (ringVisual != null)
        {
            ringVisual.Rotate(0f, 0f, 120f * Time.deltaTime, Space.Self);
        }

        if (visual != null)
        {
            float pulse = 1.2f + Mathf.Sin(Time.time * 5f) * 0.15f;
            visual.localScale = Vector3.one * pulse;
        }

        if (portalTwirl != null)
        {
            portalTwirl.localRotation = Quaternion.Euler(0f, 0f, Time.time * 150f);
            portalTwirl.localScale = Vector3.one * (3.1f + Mathf.Sin(Time.time * 4f) * 0.18f);
        }

        if (portalCircle != null)
        {
            portalCircle.localRotation = Quaternion.Euler(0f, 0f, -Time.time * 70f);
        }

        if (innerGate != null)
        {
            float gatePulse = 1f + Mathf.Sin(Time.time * 6f) * 0.035f;
            innerGate.localScale = Vector3.one * (2.55f * gatePulse);
        }

        if (player == null)
        {
            PlayerLevel playerLevel = FindFirstObjectByType<PlayerLevel>();
            if (playerLevel != null)
            {
                player = playerLevel.transform;
            }
        }

        if (player != null && Vector3.Distance(transform.position, player.position) <= enterRadius)
        {
            progressManager.EnterBossPortal();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerLevel>() == null)
            return;

        progressManager.EnterBossPortal();
    }

    private void ApplyMaterial(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = color;
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", color * 1.8f);
    }

    private GameObject CreateModel(string resourcePath, string objectName, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);

        if (prefab == null)
            return null;

        GameObject instance = Instantiate(prefab, transform);
        instance.name = objectName;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        instance.transform.localScale = localScale;

        Collider[] colliders = instance.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }

        return instance;
    }
}
