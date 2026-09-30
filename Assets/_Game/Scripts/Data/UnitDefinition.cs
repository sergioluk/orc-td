using UnityEngine;

[CreateAssetMenu(
    fileName = "Unit_",
    menuName = "Game/Data/Unit Definition"
)]
public sealed class UnitDefinition : ScriptableObject {
    [Header("Identidade")]
    [SerializeField] private string displayName = "Nova Unidade";

    [Header("Atributos")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0f)] private float moveSpeed = 1f;

    [Header("Combate")]
    [SerializeField, Min(0)] private int attackDamage = 10;
    [SerializeField, Min(0.05f)] private float attackCooldown = 1f;
    [SerializeField, Min(0f)] private float attackRange = 0.35f;

    [Header("Recompensa")]
    [SerializeField, Min(0)] private int goldReward = 5;

    public string DisplayName => displayName;
    public int MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;

    public int AttackDamage => attackDamage;
    public float AttackCooldown => attackCooldown;
    public float AttackRange => attackRange;

    public int GoldReward => goldReward;
}