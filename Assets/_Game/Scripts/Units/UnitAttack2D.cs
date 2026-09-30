
using UnityEngine;

[RequireComponent(typeof(UnitEntity))]
public sealed class UnitAttack2D : MonoBehaviour {
    [Header("Alvo")]
    [SerializeField] private Health targetHealth;

    [SerializeField] private Transform attackPoint;

    private UnitEntity unit;

    private float attackTimer;

    // Informa se o alvo está dentro do alcance de ataque.
    public bool IsTargetInRange {
        get {
            if (unit == null || unit.Definition == null)
                return false;

            if (unit.Health.IsDead)
                return false;

            if (targetHealth == null || attackPoint == null)
                return false;

            if (targetHealth.IsDead ||
                !targetHealth.gameObject.activeInHierarchy)
                return false;

            float attackRange =
                unit.Definition.AttackRange;

            Vector2 difference =
                (Vector2)attackPoint.position -
                (Vector2)transform.position;

            return difference.sqrMagnitude <=
                   attackRange * attackRange;
        }
    }

    private void Awake() {
        unit = GetComponent<UnitEntity>();
    }

    private void Update() {
        if (unit.Health.IsDead)
            return;

        if (targetHealth == null || attackPoint == null)
            return;

        if (targetHealth.IsDead)
            return;

        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;

        // Agora basta estar dentro do alcance.
        if (!IsTargetInRange)
            return;

        if (attackTimer > 0f)
            return;

        Attack();
    }

    private void Attack() {
        targetHealth.TakeDamage(
            unit.Definition.AttackDamage
        );

        attackTimer =
            unit.Definition.AttackCooldown;
    }

    public void SetTarget(
        Health newTargetHealth,
        Transform newAttackPoint) {
        targetHealth = newTargetHealth;
        attackPoint = newAttackPoint;

        attackTimer = 0f;
    }

    public void ClearTarget() {
        targetHealth = null;
        attackPoint = null;

        attackTimer = 0f;
    }
}