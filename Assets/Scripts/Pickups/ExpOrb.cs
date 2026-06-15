using UnityEngine;

public class ExpOrb : MonoBehaviour
{
    private const float MinimumCollectDistance = 3.5f;
    private const float MinimumAttractDistance = 40f;
    private const float MinimumAttractSpeed = 28f;

    public static float GlobalAttractBonus { get; private set; }

    [SerializeField] private int expAmount = 5;
    [SerializeField] private float rotateSpeed = 120f;
    [SerializeField] private float collectDistance = 2.4f;
    [SerializeField] private float attractDistance = 14f;
    [SerializeField] private float attractSpeed = 18f;

    private Transform player;
    private PlayerLevel playerLevel;

    private void Awake()
    {
        collectDistance = Mathf.Max(collectDistance, MinimumCollectDistance);
        attractDistance = Mathf.Max(attractDistance, MinimumAttractDistance);
        attractSpeed = Mathf.Max(attractSpeed, MinimumAttractSpeed);
    }

    public static void AddGlobalAttractBonus(float amount)
    {
        GlobalAttractBonus += amount;
    }

    private void Start()
    {
        PlayerLevel foundPlayer = FindFirstObjectByType<PlayerLevel>();

        if (foundPlayer != null)
        {
            playerLevel = foundPlayer;
            player = foundPlayer.transform;
        }
    }

    private void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);

        if (player == null || playerLevel == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        float effectiveAttractDistance = attractDistance + GlobalAttractBonus;

        if (distance <= effectiveAttractDistance)
        {
            Vector3 targetPos = player.position + Vector3.up * 1f;
            float distanceSpeedBonus = Mathf.Clamp(distance * 0.65f, 0f, attractSpeed);
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                (attractSpeed + distanceSpeedBonus) * Time.deltaTime
            );
            distance = Vector3.Distance(transform.position, player.position);
        }

        if (distance <= collectDistance)
        {
            GameVfx.SpawnExpCollect(transform.position);
            GameAudio.PlayPickup(transform.position);
            playerLevel.AddExp(expAmount);
            Destroy(gameObject);
        }
    }
}
