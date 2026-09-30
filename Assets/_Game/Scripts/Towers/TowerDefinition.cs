using UnityEngine;

[CreateAssetMenu(
    fileName = "Tower_",
    menuName = "Game/Towers/Tower Definition"
)]
public sealed class TowerDefinition : ScriptableObject
{
    [Header("Vida")]
    [SerializeField, Min(1)]
    private int maxHealth = 100;

    [Header("Combate")]
    [SerializeField, Min(0.1f)]
    private float attackRange = 3f;

    [SerializeField, Min(1)]
    private int attackDamage = 10;

    [SerializeField, Min(0.05f)]
    private float attackInterval = 1f;

    [Header("Detecção")]
    [SerializeField, Min(0.05f)]
    private float detectionInterval = 0.2f;

    public int MaxHealth => maxHealth;

    public float AttackRange => attackRange;

    public int AttackDamage => attackDamage;

    public float AttackInterval => attackInterval;

    public float DetectionInterval =>
        detectionInterval;
}