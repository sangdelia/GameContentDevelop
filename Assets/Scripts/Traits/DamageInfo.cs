using UnityEngine;

public enum DamageType
{
    Direct,
    Status,
    Aura,
    InstantDeath
}

public struct DamageInfo
{
    public float Damage;
    public GameObject Source;
    public DamageType DamageType;
    public bool IsCritical;
    public Vector3 HitPoint;
    public Vector3 HitDirection;

    public DamageInfo(float damage, GameObject source, DamageType damageType, Vector3 hitPoint, Vector3 hitDirection)
    {
        Damage = damage;
        Source = source;
        DamageType = damageType;
        IsCritical = false;
        HitPoint = hitPoint;
        HitDirection = hitDirection;
    }
}
