using UnityEngine;

public class EnemyRangedAttack : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 1.7f;
    [SerializeField] private float preferredDistance = 11f;
    [SerializeField] private float retreatDistance = 6f;

    [Header("Attack")]
    [SerializeField] private float attackInterval = 2.2f;
    [SerializeField] private float projectileDamage = 8f;

    private Transform player;
    private EnemyVisual visual;
    private float attackTimer;
    private bool chargeStarted;

    public void Configure(float speed, float attackRate, float damage, float preferred, float retreat)
    {
        moveSpeed = speed;
        attackInterval = attackRate;
        projectileDamage = damage;
        preferredDistance = preferred;
        retreatDistance = retreat;
    }

    public void Init(Transform target)
    {
        player = target;
        attackTimer = Random.Range(0.4f, attackInterval);
        visual = GetComponent<EnemyVisual>();
    }

    private void Update()
    {
        if (player == null)
            return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.001f)
            return;

        float distance = toPlayer.magnitude;
        Vector3 direction = toPlayer.normalized;

        Face(direction);
        MoveByDistance(distance, direction);
        TryShoot(direction);
    }

    private void Face(Vector3 direction)
    {
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void MoveByDistance(float distance, Vector3 direction)
    {
        if (distance > preferredDistance)
        {
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
        else if (distance < retreatDistance)
        {
            transform.position -= direction * moveSpeed * Time.deltaTime;
        }
    }

    private void TryShoot(Vector3 direction)
    {
        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f)
        {
            if (!chargeStarted && attackTimer <= 0.45f && visual != null)
            {
                chargeStarted = true;
                visual.PlayAttackCharge(0.45f);
            }

            return;
        }

        attackTimer = attackInterval;
        chargeStarted = false;

        Vector3 origin = visual != null
            ? visual.GetMuzzlePosition()
            : transform.position + Vector3.up * 1.1f + direction * 0.8f;
        Vector3 target = player.position + Vector3.up * 0.75f;
        Vector3 shootDirection = (target - origin).normalized;

        if (visual != null)
        {
            visual.PlayFireKick();
        }

        EnemyProjectile.Create(origin, shootDirection, projectileDamage);
    }
}
