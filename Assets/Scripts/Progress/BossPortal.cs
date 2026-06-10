using UnityEngine;

public class BossPortal : MonoBehaviour
{
    private GameProgressManager progressManager;
    private Transform visual;
    private Transform ringVisual;
    private Transform portalTwirl;
    private Transform portalCircle;
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

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "PortalRing";
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = Vector3.up * 1.1f;
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ring.transform.localScale = new Vector3(2.6f, 0.08f, 2.6f);
        Destroy(ring.GetComponent<Collider>());
        ApplyMaterial(ring, new Color(0.05f, 0.85f, 1f, 0.75f));
        ringVisual = ring.transform;

        GameObject gateModel = Resources.Load<GameObject>("Models/KenneySpace/gate-lasers");
        if (gateModel != null)
        {
            GameObject gate = Instantiate(gateModel, transform);
            gate.name = "PortalGateDecor";
            gate.transform.localPosition = Vector3.up * 0.05f;
            gate.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            gate.transform.localScale = Vector3.one * 2.25f;
        }

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
}
