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

    private LineRenderer lineRenderer;
    private Coroutine rayRoutine;
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

        Vector3 start = ray.origin;
        Vector3 end = ray.origin + ray.direction * range;

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            end = hit.point;

            Debug.Log("Hit object: " + hit.collider.name);

            EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log("Enemy hit.");
            }
            else
            {
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
}
