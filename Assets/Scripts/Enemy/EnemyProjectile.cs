using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 9f;
    [SerializeField] private float lifetime = 5f;

    private Vector3 direction;
    private float damage;
    private Transform trailCore;

    public static EnemyProjectile Create(Vector3 position, Vector3 direction, float damage)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "Enemy_EnergyShot";
        projectileObject.transform.position = position;
        projectileObject.transform.localScale = Vector3.one * 0.35f;

        SphereCollider collider = projectileObject.GetComponent<SphereCollider>();
        collider.isTrigger = true;

        Rigidbody rb = projectileObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        Renderer renderer = projectileObject.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = new Color(0.1f, 0.85f, 1f);
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", new Color(0.1f, 0.85f, 1f) * 2f);

        EnemyProjectile projectile = projectileObject.AddComponent<EnemyProjectile>();
        projectile.direction = direction.normalized;
        projectile.damage = damage;
        projectile.BuildTrail();

        return projectile;
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        lifetime -= Time.deltaTime;

        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void BuildTrail()
    {
        GameObject trailObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        trailObject.name = "ProjectileTrailGlow";
        trailObject.transform.SetParent(transform, false);
        trailObject.transform.localPosition = -direction.normalized * 0.45f;
        trailObject.transform.localScale = new Vector3(0.18f, 0.18f, 0.7f);

        Collider collider = trailObject.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = trailObject.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = new Color(0.1f, 0.85f, 1f, 0.6f);
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", new Color(0.1f, 0.85f, 1f) * 1.8f);
        trailCore = trailObject.transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<EnemyHealth>() != null)
            return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            GameVfx.SpawnHitSpark(transform.position, -direction, false);
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Ground"))
            return;

        string objectName = other.name;

        if (objectName.Contains("Ground") || objectName.Contains("Floor"))
            return;

        GameVfx.SpawnHitSpark(transform.position, -direction, false);
        Destroy(gameObject);
    }
}
