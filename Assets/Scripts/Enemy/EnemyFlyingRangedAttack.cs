using UnityEngine;

public class EnemyFlyingRangedAttack : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 2.4f;
    [SerializeField] private float preferredDistance = 13f;
    [SerializeField] private float hoverHeight = 4.2f;
    [SerializeField] private float hoverBobAmount = 0.35f;
    [SerializeField] private float hoverBobSpeed = 2.2f;

    [Header("Attack")]
    [SerializeField] private float attackInterval = 1.8f;
    [SerializeField] private float projectileDamage = 7f;

    private Transform player;
    private float attackTimer;
    private float bobSeed;

    public void Configure(float speed, float attackRate, float damage, float preferred, float height)
    {
        moveSpeed = speed;
        attackInterval = attackRate;
        projectileDamage = damage;
        preferredDistance = preferred;
        hoverHeight = height;
    }

    public void Init(Transform target)
    {
        player = target;
        attackTimer = Random.Range(0.25f, attackInterval);
        bobSeed = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (player == null)
            return;

        Vector3 flatToPlayer = player.position - transform.position;
        flatToPlayer.y = 0f;

        if (flatToPlayer.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(flatToPlayer.normalized);
        }

        Move(flatToPlayer.magnitude);
        TryShoot();
    }

    private void Move(float flatDistance)
    {
        Vector3 desiredPosition = player.position + Vector3.up * hoverHeight;

        if (flatDistance < preferredDistance)
        {
            Vector3 away = transform.position - player.position;
            away.y = 0f;

            if (away.sqrMagnitude > 0.001f)
            {
                desiredPosition += away.normalized * (preferredDistance - flatDistance);
            }
        }

        desiredPosition.y += Mathf.Sin(Time.time * hoverBobSpeed + bobSeed) * hoverBobAmount;
        transform.position = Vector3.MoveTowards(transform.position, desiredPosition, moveSpeed * Time.deltaTime);
    }

    private void TryShoot()
    {
        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f)
            return;

        attackTimer = attackInterval;

        Vector3 origin = transform.position + transform.forward * 0.7f;
        Vector3 target = player.position + Vector3.up * 0.85f;
        Vector3 direction = (target - origin).normalized;

        EnemyProjectile.Create(origin, direction, projectileDamage);
    }
}
