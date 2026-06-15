using UnityEngine;

[CreateAssetMenu(menuName = "Stargrave/Status Effect Data", fileName = "StatusEffectData")]
public class StatusEffectData : ScriptableObject
{
    public string effectId;
    public StatusEffectType effectType;
    public float duration;
    public float tickInterval;
    public float value;
    public int maxStacks = 1;
    public StackingRule stackingRule;
    public GameObject vfxPrefab;
    public Sprite icon;
}
