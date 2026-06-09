using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 5f;

    private Vector3 direction;
    private float damage;

    public static BossProjectile Create(Vector3 position, Vector3 direction, float damage)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "BossProjectile";
        projectileObject.transform.position = position;
        projectileObject.transform.localScale = Vector3.one * 0.55f;

        SphereCollider collider = projectileObject.GetComponent<SphereCollider>();
        collider.isTrigger = true;

        Rigidbody rb = projectileObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        Renderer renderer = projectileObject.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = new Color(1f, 0.2f, 0.08f);
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", new Color(1f, 0.15f, 0.05f) * 2f);

        BossProjectile projectile = projectileObject.AddComponent<BossProjectile>();
        projectile.direction = direction.normalized;
        projectile.damage = damage;

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<TempBossController>() != null)
            return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Ground"))
            return;

        string objectName = other.name;
        if (objectName.Contains("Ground") || objectName.Contains("Floor"))
            return;

        Destroy(gameObject);
    }
}
