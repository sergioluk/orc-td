
using UnityEngine;

[RequireComponent(typeof(UnitEntity))]
[RequireComponent(typeof(UnitMovement2D))]
[RequireComponent(typeof(UnitAttack2D))]
public sealed class EnemyObjectiveController : MonoBehaviour {
    [Header("Objetivo: Portão")]
    [SerializeField] private Health gateHealth;
    [SerializeField] private Transform gateAttackPoint;

    [Header("Entrada da Fortaleza")]
    [SerializeField] private Transform insideTarget;

    [Header("Objetivo: Castelo")]
    [SerializeField] private Health castleHealth;
    [SerializeField] private Transform castleAttackPoint;

    [Header("Detecção de tropas")]
    [SerializeField] private LayerMask friendlyLayer;

    [SerializeField, Min(0.1f)]
    private float detectionRadius = 1.5f;

    [SerializeField, Min(0.05f)]
    private float detectionInterval = 0.25f;

    [Header("Navegação")]
    [SerializeField, Min(0.01f)]
    private float waypointArrivalDistance = 0.18f;

    private UnitEntity unit;
    private UnitMovement2D movement;
    private UnitAttack2D attack;

    private CombatSlotManager2D currentSlotManager;

    private int currentSlotIndex = -1;

    private float slotRetryTimer;

    private Health currentSoldier;

    private float detectionTimer;

    // Indica se o Orc já alcançou o ponto
    // intermediário dentro da fortaleza.
    private bool reachedInsideTarget;

    private void Awake() {
        unit = GetComponent<UnitEntity>();

        movement = GetComponent<UnitMovement2D>();

        attack = GetComponent<UnitAttack2D>();
    }

    // Chamado pelo EnemyWaveSpawner.

    public void Configure(
        Health newGateHealth,
        Transform newGateAttackPoint,
        Transform newInsideTarget,
        Health newCastleHealth,
        Transform newCastleAttackPoint) {
        // Remove inscrições antigas.
        if (isActiveAndEnabled && gateHealth != null)
            gateHealth.Died -= HandleGateDestroyed;

        if (isActiveAndEnabled && castleHealth != null)
            castleHealth.Died -= HandleCastleDestroyed;

        // Limpa o combate anterior, caso exista.
        ReleaseSoldier();

        gateHealth = newGateHealth;
        gateAttackPoint = newGateAttackPoint;
        insideTarget = newInsideTarget;

        castleHealth = newCastleHealth;
        castleAttackPoint = newCastleAttackPoint;

        // Reinicializa os estados da invasão.
        reachedInsideTarget = false;

        detectionTimer = 0f;
        slotRetryTimer = 0f;

        currentSlotManager = null;
        currentSlotIndex = -1;

        if (isActiveAndEnabled && gateHealth != null)
            gateHealth.Died += HandleGateDestroyed;

        if (isActiveAndEnabled && castleHealth != null)
            castleHealth.Died += HandleCastleDestroyed;
    }

    private void OnEnable() {
        if (gateHealth != null)
            gateHealth.Died += HandleGateDestroyed;

        if (castleHealth != null)
            castleHealth.Died += HandleCastleDestroyed;
    }


    public void BeginInvasion() {
        if (gateHealth == null ||
            gateAttackPoint == null ||
            insideTarget == null ||
            castleHealth == null ||
            castleAttackPoint == null) {
            Debug.LogError(
                $"Objetivos não configurados em {name}.",
                this
            );

            enabled = false;
            return;
        }

        ResumeInvasion();
    }

    private void Update() {
        if (unit.Health.IsDead)
            return;

        // O objetivo final foi destruído.
        if (castleHealth == null || castleHealth.IsDead)
            return;

        // PRIORIDADE: combate contra tropas/heróis.
        if (currentSoldier != null) {
            if (currentSoldier.IsDead ||
                !currentSoldier.gameObject.activeInHierarchy) {
                ReleaseSoldier();
                ResumeInvasion();
                return;
            }

            // Ainda não conseguiu reservar uma posição?
            if (currentSlotIndex < 0) {
                slotRetryTimer -= Time.deltaTime;

                if (slotRetryTimer <= 0f) {
                    slotRetryTimer = detectionInterval;

                    TryAcquireCombatSlot();
                }
            }

            return;
        }

        // Após destruir o portão, verifica se
        // o Orc alcançou o ponto dentro da fortaleza.
        if (gateHealth.IsDead && !reachedInsideTarget) {
            Vector2 difference =
                (Vector2)insideTarget.position -
                (Vector2)transform.position;

            float arrivalDistanceSquared =
                waypointArrivalDistance *
                waypointArrivalDistance;

            if (difference.sqrMagnitude <=
                arrivalDistanceSquared) {
                reachedInsideTarget = true;

                ResumeInvasion();
                return;
            }
        }

        // Procura tropas inimigas em intervalos.
        detectionTimer -= Time.deltaTime;

        if (detectionTimer > 0f)
            return;

        detectionTimer = detectionInterval;

        SearchForSoldier();
    }

    private void SearchForSoldier() {
        Collider2D detectedCollider =
            Physics2D.OverlapCircle(
                transform.position,
                detectionRadius,
                friendlyLayer
            );

        if (detectedCollider == null)
            return;

        Health soldierHealth =
            detectedCollider.GetComponent<Health>();

        if (soldierHealth == null || soldierHealth.IsDead)
            return;

        EngageSoldier(soldierHealth);
    }

    private void EngageSoldier(Health soldierHealth) {
        currentSoldier = soldierHealth;

        currentSoldier.Died += HandleSoldierDied;

        TryAcquireCombatSlot();
    }

    private void TryAcquireCombatSlot() {
        if (currentSoldier == null)
            return;

        CombatSlotManager2D slotManager =
            currentSoldier.GetComponent<CombatSlotManager2D>();

        if (slotManager == null) {
            Debug.LogError(
                $"CombatSlotManager2D ausente em {currentSoldier.name}.",
                currentSoldier
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
                currentSoldier,
                currentSoldier.transform
            );
        } else {
            // Não encontrou posição livre.
            // Aproxima-se respeitando os Colliders.
            movement.SetTarget(
                currentSoldier.transform,
                unit.Definition.AttackRange
            );

            attack.ClearTarget();
        }
    }

    private void HandleSoldierDied() {
        ReleaseSoldier();

        ResumeInvasion();
    }

    private void ReleaseSoldier() {
        if (currentSoldier != null)
            currentSoldier.Died -= HandleSoldierDied;

        if (currentSlotManager != null)
            currentSlotManager.Release(transform);

        currentSlotManager = null;
        currentSlotIndex = -1;

        currentSoldier = null;

        attack.ClearTarget();
        movement.ClearTarget();
    }

    private void HandleGateDestroyed() {
        // Se estiver lutando, termina o combate primeiro.
        if (currentSoldier != null)
            return;

        ResumeInvasion();
    }

    private void HandleCastleDestroyed() {
        ReleaseSoldier();

        attack.ClearTarget();
        movement.ClearTarget();
    }

    private void ResumeInvasion() {
        if (gateHealth == null || castleHealth == null)
            return;

        attack.ClearTarget();

        // O castelo já foi destruído?
        if (castleHealth.IsDead) {
            movement.ClearTarget();
            return;
        }

        // PRIMEIRO OBJETIVO: PORTÃO.
        if (!gateHealth.IsDead) {
            movement.SetTarget(gateAttackPoint);

            attack.SetTarget(
                gateHealth,
                gateAttackPoint
            );

            return;
        }

        // SEGUNDO OBJETIVO: ENTRAR NA FORTALEZA.
        if (!reachedInsideTarget) {
            movement.SetTarget(insideTarget);
            return;
        }

        // TERCEIRO OBJETIVO: CASTELO.
        movement.SetTarget(castleAttackPoint);

        attack.SetTarget(
            castleHealth,
            castleAttackPoint
        );
    }

    private void OnDisable() {
        if (gateHealth != null)
            gateHealth.Died -= HandleGateDestroyed;

        if (castleHealth != null)
            castleHealth.Died -= HandleCastleDestroyed;

        ReleaseSoldier();
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );
    }
}