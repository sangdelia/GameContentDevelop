using UnityEngine;

public class ExpOrb : MonoBehaviour
{
    public static float GlobalAttractBonus { get; private set; }

    [SerializeField] private int expAmount = 5;
    [SerializeField] private float rotateSpeed = 120f;
    [SerializeField] private float collectDistance = 1.3f;
    [SerializeField] private float attractDistance = 5f;
    [SerializeField] private float attractSpeed = 8f;

    private Transform player;
    private PlayerLevel playerLevel;

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
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                attractSpeed * Time.deltaTime
            );
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
