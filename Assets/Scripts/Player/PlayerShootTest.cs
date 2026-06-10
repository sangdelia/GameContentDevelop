using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShootTest : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera playerCamera;

    [Header("Shoot")]
    [SerializeField] private float damage = 12f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float shotsPerSecond = 4.5f;
    [SerializeField] private bool automaticFire = true;

    [Header("Ray Visual")]
    [SerializeField] private float rayVisibleTime = 0.05f;
    [SerializeField] private float rayWidth = 0.04f;

    [Header("PC Test Weapon Visual")]
    [SerializeField] private bool createPcWeaponVisual = true;
    [SerializeField] private Vector3 weaponLocalPosition = new Vector3(0.23f, -0.22f, 0.48f);
    [SerializeField] private Vector3 weaponLocalRotation = new Vector3(-4f, 0f, 0f);

    private LineRenderer lineRenderer;
    private Coroutine rayRoutine;
    private Coroutine recoilRoutine;
    private Transform weaponRoot;
    private Transform muzzlePoint;
    private float nextShootTime;

    public float Damage => damage;

    public void AddDamageMultiplier(float multiplier)
    {
        damage *= multiplier;
    }

    public void AddAttackSpeedMultiplier(float multiplier)
    {
        shotsPerSecond *= multiplier;
    }

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        CreateLineRenderer();
        CreatePcWeaponVisual();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        bool wantsToShoot = automaticFire
            ? mouse.leftButton.isPressed
            : mouse.leftButton.wasPressedThisFrame;

        if (wantsToShoot && Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + 1f / Mathf.Max(0.1f, shotsPerSecond);
            Shoot();
        }
    }

    private void CreateLineRenderer()
    {
        GameObject lineObj = new GameObject("ShootRayVisual");
        lineObj.transform.SetParent(transform);

        lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth;
        lineRenderer.useWorldSpace = true;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.red;
        lineRenderer.material = mat;

        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.enabled = false;
    }

    private void Shoot()
    {
        if (playerCamera == null)
        {
            Debug.LogError("Shoot failed: Player Camera is not assigned.");
            return;
        }

        GameAudio.PlayPlayerShoot(transform.position);

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 start = muzzlePoint != null ? muzzlePoint.position : ray.origin;
        Vector3 end = ray.origin + ray.direction * range;
        GameVfx.SpawnMuzzleFlash(start, ray.direction);
        PlayWeaponRecoil();

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            end = hit.point;

            Debug.Log("Hit object: " + hit.collider.name);

            EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();

            if (enemy != null)
            {
                GameVfx.SpawnHitSpark(hit.point, hit.normal, true);
                enemy.TakeDamage(damage);
                Debug.Log("Enemy hit.");
            }
            else
            {
                GameVfx.SpawnHitSpark(hit.point, hit.normal, false);
                Debug.LogWarning("Hit object has no EnemyHealth: " + hit.collider.name);
            }
        }
        else
        {
            Debug.Log("Shot missed.");
        }

        ShowRay(start, end);
    }

    private void ShowRay(Vector3 start, Vector3 end)
    {
        if (lineRenderer == null)
            return;

        if (rayRoutine != null)
            StopCoroutine(rayRoutine);

        rayRoutine = StartCoroutine(ShowRayRoutine(start, end));
    }

    private IEnumerator ShowRayRoutine(Vector3 start, Vector3 end)
    {
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        yield return new WaitForSeconds(rayVisibleTime);

        lineRenderer.enabled = false;
    }

    private void CreatePcWeaponVisual()
    {
        if (!createPcWeaponVisual || playerCamera == null)
            return;

        weaponRoot = new GameObject("PC_Test_Blaster").transform;
        weaponRoot.SetParent(playerCamera.transform, false);
        weaponRoot.localPosition = weaponLocalPosition;
        weaponRoot.localRotation = Quaternion.Euler(weaponLocalRotation);

        if (TryCreateImportedGun())
        {
            muzzlePoint = new GameObject("MuzzlePoint").transform;
            muzzlePoint.SetParent(weaponRoot, false);
            muzzlePoint.localPosition = new Vector3(0f, 0.02f, 0.82f);
            return;
        }

        CreateWeaponPart("Grip", new Vector3(0f, -0.1f, -0.08f), new Vector3(0.08f, 0.18f, 0.08f), new Color(0.06f, 0.08f, 0.1f));
        CreateWeaponPart("Body", new Vector3(0f, 0f, 0.06f), new Vector3(0.16f, 0.12f, 0.34f), new Color(0.1f, 0.14f, 0.17f));
        CreateWeaponPart("Barrel", new Vector3(0f, 0.01f, 0.3f), new Vector3(0.08f, 0.08f, 0.28f), new Color(0.02f, 0.05f, 0.07f));
        CreateWeaponPart("EnergyCell", new Vector3(0f, 0.08f, 0.06f), new Vector3(0.1f, 0.035f, 0.18f), new Color(0.08f, 0.75f, 1f));

        muzzlePoint = new GameObject("MuzzlePoint").transform;
        muzzlePoint.SetParent(weaponRoot, false);
        muzzlePoint.localPosition = new Vector3(0f, 0.01f, 0.48f);
    }

    private bool TryCreateImportedGun()
    {
        GameObject gunPrefab = Resources.Load<GameObject>("Models/Guns/Gun_Rifle");

        if (gunPrefab == null)
            return false;

        GameObject gun = Instantiate(gunPrefab, weaponRoot);
        gun.name = "Gun_Rifle_Model";
        gun.transform.localPosition = Vector3.zero;
        gun.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        gun.transform.localScale = Vector3.one * 0.62f;

        Collider[] colliders = gun.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }

        Renderer[] renderers = gun.GetComponentsInChildren<Renderer>();
        Texture2D baseMap = Resources.Load<Texture2D>("Textures/ImportedSciFi/T_Guns_Batch1_BaseColor");
        Texture2D normalMap = Resources.Load<Texture2D>("Textures/ImportedSciFi/T_Guns_Batch1_Normal");
        Texture2D emissionMap = Resources.Load<Texture2D>("Textures/ImportedSciFi/T_Guns_Batch1_Emissive");

        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            if (baseMap != null)
            {
                material.mainTexture = baseMap;
            }

            if (normalMap != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normalMap);
                material.EnableKeyword("_NORMALMAP");
            }

            if (emissionMap != null && material.HasProperty("_EmissionMap"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetTexture("_EmissionMap", emissionMap);
                material.SetColor("_EmissionColor", new Color(0.2f, 0.85f, 1f) * 1.2f);
            }

            renderers[i].material = material;
        }

        return true;
    }

    private void CreateWeaponPart(string partName, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = partName;
        part.transform.SetParent(weaponRoot, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = color;

        if (renderer.material.HasProperty("_EmissionColor"))
        {
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", color * 0.8f);
        }
    }

    private void PlayWeaponRecoil()
    {
        if (weaponRoot == null)
            return;

        if (recoilRoutine != null)
        {
            StopCoroutine(recoilRoutine);
        }

        recoilRoutine = StartCoroutine(RecoilRoutine());
    }

    private IEnumerator RecoilRoutine()
    {
        Vector3 basePosition = weaponLocalPosition;
        Vector3 recoilPosition = basePosition + new Vector3(0f, -0.015f, -0.065f);
        float timer = 0f;

        while (timer < 0.045f)
        {
            timer += Time.deltaTime;
            weaponRoot.localPosition = Vector3.Lerp(basePosition, recoilPosition, timer / 0.045f);
            yield return null;
        }

        timer = 0f;

        while (timer < 0.08f)
        {
            timer += Time.deltaTime;
            weaponRoot.localPosition = Vector3.Lerp(recoilPosition, basePosition, timer / 0.08f);
            yield return null;
        }

        weaponRoot.localPosition = basePosition;
    }
}
