using UnityEngine;

public class EnemyMoveToPlayer : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float bodySeparationDistance = 2.05f;
    [SerializeField] private float bodySeparationSpeed = 18f;

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
        bodySeparationDistance = Mathf.Max(bodySeparationDistance, stopDistance + 0.25f);
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

        if (distance < bodySeparationDistance)
        {
            PushOutFromPlayer(direction, distance);
        }

        if (distance <= stopDistance)
        {
            TryAttack();
            return;
        }

        float moveDistance = Mathf.Min(moveSpeed * Time.deltaTime, Mathf.Max(0f, distance - stopDistance));
        Vector3 move = direction.normalized * moveDistance;
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

    private void PushOutFromPlayer(Vector3 directionToPlayer, float distance)
    {
        Vector3 awayFromPlayer;

        if (distance > 0.001f)
        {
            awayFromPlayer = -directionToPlayer.normalized;
        }
        else
        {
            awayFromPlayer = -transform.forward;
            awayFromPlayer.y = 0f;

            if (awayFromPlayer.sqrMagnitude < 0.001f)
            {
                awayFromPlayer = Vector3.back;
            }

            awayFromPlayer.Normalize();
        }

        Vector3 desiredPosition = player.position + awayFromPlayer * bodySeparationDistance;
        desiredPosition.y = transform.position.y;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-bodySeparationSpeed * Time.deltaTime));
    }
}
