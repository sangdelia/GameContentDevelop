using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerShootTest : MonoBehaviour
{
    private static readonly Color DefaultProjectileRayColor = new Color(1f, 0.82f, 0.18f);
    private static readonly Color DefaultProjectileMuzzleColor = new Color(1f, 0.72f, 0.18f);
    private static readonly Color FireHitMarkerColor = new Color(1f, 0.05f, 0.01f);
    private static readonly Color IceHitMarkerColor = new Color(0.25f, 0.85f, 1f);

    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform xrAimSource;

    [Header("Shoot")]
    [SerializeField] private float damage = 12f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float shotsPerSecond = 4.5f;
    [SerializeField] private bool automaticFire = true;
    [SerializeField] private bool logShotDebug = false;
    [SerializeField] private bool allowMouseInput = true;
    [SerializeField] private bool allowVrInput = false;
    [SerializeField] private float vrTriggerThreshold = 0.55f;

    [Header("Ray Visual")]
    [SerializeField] private float rayVisibleTime = 0.05f;
    [SerializeField] private float rayWidth = 0.04f;
    [SerializeField] private float closeEnemyHitRadius = 1.15f;

    [Header("PC Test Weapon Visual")]
    [SerializeField] private bool createPcWeaponVisual = true;
    [SerializeField] private Vector3 weaponLocalPosition = new Vector3(0.23f, -0.22f, 0.48f);
    [SerializeField] private Vector3 weaponLocalRotation = new Vector3(-4f, 0f, 0f);
    [SerializeField] private float weaponRecoilDistance = 0.085f;
    [SerializeField] private float weaponRecoilAngle = 6.5f;
    [SerializeField] private float cameraKickAngle = 0.32f;

    private LineRenderer lineRenderer;
    private Coroutine rayRoutine;
    private Coroutine recoilRoutine;
    private Transform weaponRoot;
    private Transform muzzlePoint;
    private CameraFollowTarget cameraFollow;
    private PlayerTraitController traitController;
    private float nextShootTime;
    private bool wasVrTriggerPressed;

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

        cameraFollow = playerCamera != null ? playerCamera.GetComponent<CameraFollowTarget>() : null;
        traitController = GetComponent<PlayerTraitController>();
        if (traitController == null)
        {
            traitController = gameObject.AddComponent<PlayerTraitController>();
        }

        CreateLineRenderer();
        CreatePcWeaponVisual();
    }

    private void Update()
    {
        bool wantsToShoot = WantsMouseShoot() || WantsVrShoot();

        if (wantsToShoot && Time.time >= nextShootTime)
        {
            float finalShotsPerSecond = traitController != null ? traitController.GetFinalShotsPerSecond(shotsPerSecond) : shotsPerSecond;
            nextShootTime = Time.time + 1f / Mathf.Max(0.1f, finalShotsPerSecond);
            Shoot();
        }
    }

    public void SetRuntimeMode(StargravePlayMode.Mode mode, Camera activeCamera, Transform aimSource)
    {
        allowVrInput = mode == StargravePlayMode.Mode.VrQuest2;
        allowMouseInput = mode == StargravePlayMode.Mode.Pc;

        if (activeCamera != null)
        {
            playerCamera = activeCamera;
            cameraFollow = playerCamera.GetComponent<CameraFollowTarget>();
        }

        xrAimSource = aimSource;
    }

    private bool WantsMouseShoot()
    {
        if (!allowMouseInput)
            return false;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return false;

        return automaticFire
            ? mouse.leftButton.isPressed
            : mouse.leftButton.wasPressedThisFrame;
    }

    private bool WantsVrShoot()
    {
        if (!allowVrInput)
            return false;

        InputDevice rightController = InputSystem.GetDevice("<XRController>{RightHand}");
        if (rightController == null)
        {
            wasVrTriggerPressed = false;
            return false;
        }

        bool isPressed = false;
        ButtonControl triggerPressed = rightController.TryGetChildControl<ButtonControl>("triggerPressed");
        if (triggerPressed != null)
        {
            isPressed = triggerPressed.isPressed;
        }
        else
        {
            AxisControl trigger = rightController.TryGetChildControl<AxisControl>("trigger");
            isPressed = trigger != null && trigger.ReadValue() >= vrTriggerThreshold;
        }

        bool startedThisFrame = isPressed && !wasVrTriggerPressed;
        wasVrTriggerPressed = isPressed;

        return automaticFire ? isPressed : startedThisFrame;
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
        mat.color = DefaultProjectileRayColor;
        lineRenderer.material = mat;

        lineRenderer.startColor = DefaultProjectileRayColor;
        lineRenderer.endColor = DefaultProjectileRayColor;
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

        Ray ray = GetShootRay();

        Vector3 start = muzzlePoint != null ? muzzlePoint.position : ray.origin;
        Vector3 end = ray.origin + ray.direction * range;
        PlayerProjectileElement projectileElement = traitController != null ? traitController.CurrentProjectileElement : PlayerProjectileElement.Normal;
        Color shotColor = traitController != null ? traitController.GetProjectileRayColor() : DefaultProjectileRayColor;
        Color muzzleColor = traitController != null ? traitController.GetProjectileMuzzleColor() : DefaultProjectileMuzzleColor;
        GameVfx.SpawnMuzzleFlash(start, ray.direction, muzzleColor);
        PlayWeaponRecoil();

        if (TryGetCloseEnemyHit(ray, out EnemyHealth closeEnemy, out Vector3 closeHitPoint, out Vector3 closeHitNormal))
        {
            end = closeHitPoint;
            ApplyEnemyShotHit(closeEnemy, closeHitPoint, closeHitNormal, ray.direction, projectileElement);

            if (logShotDebug)
            {
                Debug.Log("Close enemy hit.");
            }
        }
        else if (TryGetShotHit(ray, out RaycastHit hit))
        {
            end = hit.point;

            if (logShotDebug)
            {
                Debug.Log("Hit object: " + hit.collider.name);
            }

            EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();

            if (enemy != null)
            {
                ApplyEnemyShotHit(enemy, hit.point, hit.normal, ray.direction, projectileElement);

                if (logShotDebug)
                {
                    Debug.Log("Enemy hit.");
                }
            }
            else
            {
                GameVfx.SpawnHitSpark(hit.point, hit.normal, false);

                if (logShotDebug)
                {
                    Debug.Log("Hit object has no EnemyHealth: " + hit.collider.name);
                }
            }
        }
        else if (logShotDebug)
        {
            Debug.Log("Shot missed.");
        }

        ShowRay(start, end, shotColor);
    }

    private void ApplyEnemyShotHit(EnemyHealth enemy, Vector3 hitPoint, Vector3 hitNormal, Vector3 hitDirection, PlayerProjectileElement projectileElement)
    {
        if (enemy == null)
            return;

        float shotDamage = traitController != null ? traitController.GetFinalDamage(damage) : damage;
        GameVfx.SpawnHitSpark(hitPoint, hitNormal, true);

        if (projectileElement == PlayerProjectileElement.Fire)
        {
            GameVfx.SpawnHitMarker(hitPoint, hitNormal, FireHitMarkerColor);
        }
        else if (projectileElement == PlayerProjectileElement.Ice)
        {
            GameVfx.SpawnHitMarker(hitPoint, hitNormal, IceHitMarkerColor);
        }

        DamageInfo damageInfo = new DamageInfo(shotDamage, gameObject, DamageType.Direct, hitPoint, hitDirection);
        enemy.TakeDamage(damageInfo);

        if (traitController != null)
        {
            traitController.HandleDirectEnemyHit(enemy, hitPoint, hitDirection, shotDamage);
        }
    }

    private Ray GetShootRay()
    {
        if (allowVrInput && xrAimSource != null)
        {
            return new Ray(xrAimSource.position, xrAimSource.forward);
        }

        return playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }

    private bool TryGetShotHit(Ray ray, out RaycastHit selectedHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, range, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;

            if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
                continue;

            EnemyHealth enemy = hitCollider.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                selectedHit = hits[i];
                return true;
            }

            if (ShouldIgnoreShotHit(hitCollider))
                continue;

            selectedHit = hits[i];
            return true;
        }

        selectedHit = default;
        return false;
    }

    private bool TryGetCloseEnemyHit(Ray ray, out EnemyHealth selectedEnemy, out Vector3 hitPoint, out Vector3 hitNormal)
    {
        selectedEnemy = null;
        hitPoint = ray.origin + ray.direction * 0.2f;
        hitNormal = -ray.direction;

        Collider[] overlaps = Physics.OverlapSphere(ray.origin, closeEnemyHitRadius, ~0, QueryTriggerInteraction.Collide);
        float bestScore = float.MaxValue;

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider hitCollider = overlaps[i];
            if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
                continue;

            EnemyHealth enemy = hitCollider.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead)
                continue;

            Vector3 enemyAimPoint = enemy.transform.position + Vector3.up * 0.8f;
            Vector3 toEnemy = enemyAimPoint - ray.origin;
            float forwardDistance = Vector3.Dot(ray.direction, toEnemy);
            if (forwardDistance < -0.15f)
                continue;

            Vector3 closestPoint = hitCollider.ClosestPoint(ray.origin);
            Vector3 toClosestPoint = closestPoint - ray.origin;
            float sideDistance = Vector3.Cross(ray.direction, toClosestPoint).magnitude;
            float score = Mathf.Max(0f, forwardDistance) + sideDistance * 0.35f;

            if (score >= bestScore)
                continue;

            bestScore = score;
            selectedEnemy = enemy;
            hitPoint = toClosestPoint.sqrMagnitude > 0.0001f ? closestPoint : ray.origin + ray.direction * 0.2f;
            hitNormal = toClosestPoint.sqrMagnitude > 0.0001f ? -toClosestPoint.normalized : -ray.direction;
        }

        return selectedEnemy != null;
    }

    private bool ShouldIgnoreShotHit(Collider hitCollider)
    {
        if (hitCollider.isTrigger)
            return true;

        string objectName = hitCollider.name;
        if (objectName.Contains("Floor") || objectName.Contains("Circuit") || objectName.Contains("Neon") || objectName.Contains("Decor"))
            return true;

        Transform current = hitCollider.transform;
        while (current != null)
        {
            string currentName = current.name;
            if (currentName.Contains("SpaceKit") || currentName.Contains("TempBossArena"))
            {
                return objectName.Contains("Room") || objectName.Contains("Floor") || objectName.Contains("Circuit") || objectName.Contains("Panel") || objectName.Contains("Gate");
            }

            current = current.parent;
        }

        return false;
    }

    private void ShowRay(Vector3 start, Vector3 end, Color color)
    {
        if (lineRenderer == null)
            return;

        if (rayRoutine != null)
            StopCoroutine(rayRoutine);

        rayRoutine = StartCoroutine(ShowRayRoutine(start, end, color));
    }

    private IEnumerator ShowRayRoutine(Vector3 start, Vector3 end, Color color)
    {
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        if (lineRenderer.material != null)
        {
            lineRenderer.material.color = color;
        }

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
        if (cameraFollow != null)
        {
            float yawJitter = Random.Range(-cameraKickAngle * 0.45f, cameraKickAngle * 0.45f);
            cameraFollow.AddKick(new Vector3(Random.Range(-0.002f, 0.002f), Random.Range(-0.001f, 0.002f), -0.006f), new Vector3(-cameraKickAngle, yawJitter, Random.Range(-0.08f, 0.08f)));
        }

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
        Quaternion baseRotation = Quaternion.Euler(weaponLocalRotation);
        Vector3 recoilPosition = basePosition + new Vector3(Random.Range(-0.006f, 0.006f), -0.018f, -weaponRecoilDistance);
        Quaternion recoilRotation = baseRotation * Quaternion.Euler(-weaponRecoilAngle, Random.Range(-1.4f, 1.4f), Random.Range(-1.1f, 1.1f));
        float timer = 0f;

        while (timer < 0.045f)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / 0.045f);
            weaponRoot.localPosition = Vector3.Lerp(basePosition, recoilPosition, t);
            weaponRoot.localRotation = Quaternion.Slerp(baseRotation, recoilRotation, t);
            yield return null;
        }

        timer = 0f;

        while (timer < 0.095f)
        {
            timer += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(timer / 0.095f), 2f);
            weaponRoot.localPosition = Vector3.Lerp(recoilPosition, basePosition, t);
            weaponRoot.localRotation = Quaternion.Slerp(recoilRotation, baseRotation, t);
            yield return null;
        }

        weaponRoot.localPosition = basePosition;
        weaponRoot.localRotation = baseRotation;
    }
}
