using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerLevel : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentExp = 0;
    [SerializeField] private int requiredExp = 10;

    [Header("Growth")]
    [SerializeField] private float requiredExpMultiplier = 1.35f;

    public int Level => level;
    public int CurrentExp => currentExp;
    public int RequiredExp => requiredExp;

    public event System.Action<int, int, int> ExpChanged;
    public event System.Action<int> LevelChanged;
    public event System.Action<int> LevelUpChoicesRequested;

    private void Start()
    {
        NotifyChanged();
    }

    public void AddExp(int amount)
    {
        currentExp += amount;

        Debug.Log($"EXP +{amount} / {currentExp}/{requiredExp}");

        while (currentExp >= requiredExp)
        {
            currentExp -= requiredExp;
            LevelUp();
        }

        NotifyChanged();
    }

    private void LevelUp()
    {
        level++;

        requiredExp = Mathf.CeilToInt(requiredExp * requiredExpMultiplier);
        GameVfx.SpawnLevelUp(transform.position);
        GameAudio.PlayLevelUp(transform.position);

        Debug.Log($"Level Up! Current level: {level}, next required EXP: {requiredExp}");

        LevelChanged?.Invoke(level);
        LevelUpChoicesRequested?.Invoke(level);
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        ExpChanged?.Invoke(level, currentExp, requiredExp);
    }
}
