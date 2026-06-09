using UnityEngine;

public class EnemyMoveToPlayer : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDistance = 1.2f;

    [Header("Attack")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackInterval = 1f;

    private Transform player;
    private PlayerHealth playerHealth;
    private EnemyVisual visual;
    private float attackTimer;

    public void Configure(float speed, float damage, float interval, float stoppingDistance)
    {
        moveSpeed = speed;
        attackDamage = damage;
        attackInterval = interval;
        stopDistance = stoppingDistance;
    }

    public void Init(Transform target)
    {
        player = target;

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();

            if (playerHealth == null)
            {
                playerHealth = player.gameObject.AddComponent<PlayerHealth>();
            }
        }

        visual = GetComponent<EnemyVisual>();
    }

    private void Update()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance <= stopDistance)
        {
            TryAttack();
            return;
        }

        Vector3 move = direction.normalized * moveSpeed * Time.deltaTime;
        transform.position += move;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void TryAttack()
    {
        if (playerHealth == null)
            return;

        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f)
            return;

        attackTimer = attackInterval;
        if (visual != null)
        {
            visual.PlayMeleePulse();
        }

        playerHealth.TakeDamage(attackDamage);
    }
}
