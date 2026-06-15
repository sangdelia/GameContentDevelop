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
    [SerializeField] private float contactAttackPadding = 0.18f;

    private Transform player;
    private PlayerHealth playerHealth;
    private EnemyVisual visual;
    private StatusEffectController statusEffects;
    private float attackTimer;

    public void Configure(float speed, float damage, float interval, float stoppingDistance)
    {
        moveSpeed = speed;
        attackDamage = damage;
        attackInterval = interval;
        stopDistance = stoppingDistance;
        bodySeparationDistance = Mathf.Min(bodySeparationDistance, stopDistance + 0.15f);
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
        statusEffects = GetComponent<StatusEffectController>();
    }

    private void Update()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;
        float attackDistance = stopDistance + contactAttackPadding;

        if (distance <= attackDistance)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                Face(direction);
            }

            TryAttack();
        }

        if (distance < bodySeparationDistance)
        {
            PushOutFromPlayer(direction, distance);
        }

        if (distance <= stopDistance)
            return;

        float speedMultiplier = GetCurrentMoveSpeedMultiplier();
        float moveDistance = Mathf.Min(moveSpeed * speedMultiplier * Time.deltaTime, Mathf.Max(0f, distance - stopDistance));
        transform.position += direction.normalized * moveDistance;

        if (direction != Vector3.zero)
        {
            Face(direction);
        }
    }

    private void Face(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
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

    private float GetCurrentMoveSpeedMultiplier()
    {
        if (statusEffects == null)
        {
            statusEffects = GetComponent<StatusEffectController>();
        }

        return statusEffects != null ? statusEffects.MoveSpeedMultiplier : 1f;
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
