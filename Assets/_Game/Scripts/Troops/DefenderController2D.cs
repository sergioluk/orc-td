using UnityEngine;

[RequireComponent(typeof(UnitEntity))]
[RequireComponent(typeof(UnitMovement2D))]
[RequireComponent(typeof(UnitAttack2D))]
public sealed class DefenderController2D : MonoBehaviour {
    [Header("Posição de defesa")]
    [SerializeField] private Transform guardPoint;

    [Header("Detecção de inimigos")]
    [SerializeField] private LayerMask enemyLayer;

    [SerializeField, Min(0.1f)]
    private float detectionRadius = 2f;

    [SerializeField, Min(0.05f)]
    private float detectionInterval = 0.25f;

    private UnitEntity unit;
    private UnitMovement2D movement;
    private UnitAttack2D attack;

    private Health currentEnemy;

    private float detectionTimer;

    private CombatSlotManager2D currentSlotManager;

    private int currentSlotIndex = -1;

    private float slotRetryTimer;

    private void Awake() {
        unit = GetComponent<UnitEntity>();

        movement = GetComponent<UnitMovement2D>();

        attack = GetComponent<UnitAttack2D>();
    }

    private void Start() {
        if (guardPoint == null) {
            Debug.LogError(
                $"Guard Point não configurado em {name}.",
                this
            );

            enabled = false;
            return;
        }

        ReturnToGuard();
    }

    private void Update() {
        if (unit.Health.IsDead)
            return;

        if (currentEnemy != null) {
            if (currentEnemy.IsDead ||
                !currentEnemy.gameObject.activeInHierarchy) {
                currentEnemy = null;

                ReturnToGuard();
                return;
            }

            if (currentSlotIndex < 0) {
                slotRetryTimer -= Time.deltaTime;

                if (slotRetryTimer <= 0f) {
                    slotRetryTimer = detectionInterval;

                    TryAcquireCombatSlot();
                }
            }

            return;
        }

        // Se já temos um inimigo, não precisamos
        // procurar outro neste momento.
        if (currentEnemy != null)
            return;

        detectionTimer -= Time.deltaTime;

        if (detectionTimer > 0f)
            return;

        detectionTimer = detectionInterval;

        SearchForEnemy();
    }

    private void SearchForEnemy() {
        Collider2D detectedCollider =
            Physics2D.OverlapCircle(
                guardPoint.position,
                detectionRadius,
                enemyLayer
            );

        if (detectedCollider == null)
            return;

        Health enemyHealth =
            detectedCollider.GetComponent<Health>();

        if (enemyHealth == null)
            return;

        if (enemyHealth.IsDead)
            return;

        SetEnemy(enemyHealth);
    }
    private void SetEnemy(Health enemyHealth) {
        currentEnemy = enemyHealth;

        TryAcquireCombatSlot();
    }
    private void TryAcquireCombatSlot() {
        if (currentEnemy == null)
            return;

        CombatSlotManager2D slotManager =
            currentEnemy.GetComponent<CombatSlotManager2D>();

        if (slotManager == null) {
            Debug.LogError(
                $"CombatSlotManager2D ausente em {currentEnemy.name}.",
                currentEnemy
            );

            return;
        }

        bool reserved = slotManager.TryReserve(
            transform,
            unit.Definition.AttackRange,
            out int slotIndex
        );

        if (reserved) {
            currentSlotManager = slotManager;
            currentSlotIndex = slotIndex;

            movement.SetCombatSlot(
                slotManager,
                slotIndex
            );

            attack.SetTarget(
                currentEnemy,
                currentEnemy.transform
            );
        } else {
            movement.SetTarget(
                currentEnemy.transform,
                unit.Definition.AttackRange
            );

            attack.ClearTarget();
        }
    }

    private void ReturnToGuard() {
        if (currentSlotManager != null) {
            currentSlotManager.Release(transform);
        }

        currentSlotManager = null;
        currentSlotIndex = -1;

        attack.ClearTarget();

        movement.SetTarget(guardPoint);
    }

    public void ConfigureGuardPoint(Transform newGuardPoint) {
        guardPoint = newGuardPoint;

        detectionTimer = 0f;
        slotRetryTimer = 0f;
    }

    private void OnDisable() {
        if (currentSlotManager != null) {
            currentSlotManager.Release(transform);
        }

        currentSlotManager = null;
        currentSlotIndex = -1;
    }

    private void OnDrawGizmosSelected() {
        if (guardPoint == null)
            return;

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            guardPoint.position,
            detectionRadius
        );
    }
}