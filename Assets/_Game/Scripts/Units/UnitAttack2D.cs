using System;
using UnityEngine;

[RequireComponent(typeof(UnitEntity))]
public sealed class UnitAttack2D : MonoBehaviour {
    [Header("Alvo")]
    [SerializeField] private Health targetHealth;

    [SerializeField] private Transform attackPoint;

    private UnitEntity unit;

    private float attackTimer;

    public event Action<Vector2> AttackPerformed;

    private Health pendingAttackTarget;

    private Transform pendingAttackPoint;

    private int pendingAttackDamage;

    private bool attackHitPending;

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
        if (targetHealth == null ||
            targetHealth.IsDead) {
            return;
        }

        if (attackPoint == null)
            return;

        Vector2 attackDirection =
            CurrentAttackDirection;

        // Guarda qual alvo deverá receber
        // o golpe quando a animação chegar
        // ao frame de impacto.
        pendingAttackTarget =
            targetHealth;

        pendingAttackPoint =
            attackPoint;

        pendingAttackDamage =
            unit.Definition.AttackDamage;

        attackHitPending = true;

        // Inicia a animação.
        AttackPerformed?.Invoke(
            attackDirection
        );

        // O cooldown começa no início
        // da execução do ataque.
        attackTimer =
            unit.Definition.AttackCooldown;
    }

    public void AnimationEvent_ApplyAttackDamage() {
        if (!attackHitPending)
            return;

        attackHitPending = false;

        Health target =
            pendingAttackTarget;

        Transform targetPoint =
            pendingAttackPoint;

        int damage =
            pendingAttackDamage;

        ClearPendingAttack();

        if (target == null ||
            targetPoint == null ||
            target.IsDead) {
            return;
        }

        // Confere novamente o alcance
        // no momento exato do impacto.
        float attackRange =
            unit.Definition.AttackRange;

        Vector2 difference =
            targetPoint.position -
            transform.position;

        if (difference.sqrMagnitude >
            attackRange * attackRange) {
            return;
        }

        target.TakeDamage(
            damage
        );
    }

    private void ClearPendingAttack() {
        attackHitPending = false;

        pendingAttackTarget = null;

        pendingAttackPoint = null;

        pendingAttackDamage = 0;
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

        ClearPendingAttack();
    }
    private void OnDisable() {
        ClearPendingAttack();
    }

    public Vector2 CurrentAttackDirection {
        get {
            if (attackPoint == null)
                return Vector2.zero;

            Vector2 direction =
                (Vector2)attackPoint.position -
                (Vector2)transform.position;

            if (direction.sqrMagnitude <
                0.0001f) {
                return Vector2.zero;
            }

            return direction.normalized;
        }
    }
}