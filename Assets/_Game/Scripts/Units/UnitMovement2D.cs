
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(UnitEntity))]
public sealed class UnitMovement2D : MonoBehaviour {
    [Header("Destino")]
    [SerializeField] private Transform target;

    [Header("Movimentação")]
    [SerializeField, Min(0.01f)]
    private float stopDistance = 0.15f;

    [Header("Bloqueio de movimento")]
    [SerializeField] private LayerMask blockingLayers;
    [Header("Desvio de unidades")]
    [SerializeField] private bool enableUnitAvoidance = true;

    private int avoidanceSide;

    // Ângulos utilizados para procurar uma passagem lateral.
    private static readonly float[] AvoidanceAngles =
    {
        45f,
        75f,
        90f,
        120f
    };

    [Header("Recuperação de travamento")]
    [SerializeField, Min(0.1f)]
    private float stuckCheckInterval = 1.2f;

    [SerializeField, Min(0.001f)]
    private float stuckMinMovement = 0.04f;

    private Vector2 lastStuckCheckPosition;

    private float stuckCheckTimer;

    private bool monitoringStuck;

    [SerializeField, Min(0f)]
    private float collisionSkin = 0.01f;

    [Header("Navegação A*")]
    [SerializeField] private Pathfinder2D pathfinder;

    [Header("Depuração da navegação")]
    [SerializeField] private bool drawNavigationPath = true;

    [SerializeField, Min(0.05f)]
    private float repathInterval = 0.4f;

    [Header("Destino inacessível")]
    [SerializeField, Min(0.5f)]
    private float failedPathRetryInterval = 2f;

    private bool pathUnavailable;

    [SerializeField, Min(0.01f)]
    private float waypointArrivalDistance = 0.04f;

    [SerializeField, Min(0.01f)]
    private float destinationChangeThreshold = 0.15f;

    [Header("Tamanho do agente")]
    [SerializeField, Min(0f)]
    private float navigationPadding = 0.02f;


    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private UnitEntity unit;
    private UnitAttack2D attack;

    private float currentStopDistance;

    // Posição reservada de combate.
    private CombatSlotManager2D combatSlotManager;
    private int combatSlotIndex = -1;

    // Caminho calculado pelo A*.
    private readonly List<Vector2> path = new(64);

    private int currentWaypointIndex;

    private bool hasPath;
    private bool hasPlannedDestination;

    private Vector2 lastPlannedDestination;

    private float nextRepathTime;

    // Buffer reutilizável de colisões.
    private readonly RaycastHit2D[] castResults =
        new RaycastHit2D[16];

    private ContactFilter2D collisionFilter;

    public bool HasCombatSlot =>
        combatSlotManager != null &&
        combatSlotIndex >= 0;

    public bool IsMoving {
        get;
        private set;
    }

    public Vector2 CurrentMoveDirection {
        get;
        private set;
    }

    public Vector2 LastMoveDirection {
        get;
        private set;
    } = new Vector2(-1f,-1f).normalized;

    public bool IsAtCombatSlot {
        get {
            if (!HasCombatSlot)
                return false;

            Vector2 destination =
                combatSlotManager.GetSlotPosition(
                    combatSlotIndex
                );

            return Vector2.Distance(
                rb.position,
                destination
            ) <= currentStopDistance + 0.005f;
        }
    }

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();

        bodyCollider = GetComponent<Collider2D>();

        unit = GetComponent<UnitEntity>();

        attack = GetComponent<UnitAttack2D>();

        currentStopDistance = stopDistance;

        collisionFilter = new ContactFilter2D();

        collisionFilter.SetLayerMask(blockingLayers);

        collisionFilter.useTriggers = false;
    }

    private void FixedUpdate() {
        if (unit.Definition == null || unit.Health.IsDead) {
            StopMovement();
            return;
        }

        if (target == null && !HasCombatSlot) {
            StopMovement();
            return;
        }

        // Se existe posição reservada de combate,
        // precisamos alcançá-la antes de parar.
        if (attack != null &&
            attack.IsTargetInRange &&
            (!HasCombatSlot || IsAtCombatSlot)) {
            StopMovement();
            return;
        }

        Vector2 finalDestination = GetDestination();

        // Já chegamos ao objetivo final?
        float distanceToDestination =
            Vector2.Distance(
                rb.position,
                finalDestination
            );

        if (distanceToDestination <= currentStopDistance) {
            StopMovement();
            return;
        }

        // Se não existe Pathfinder configurado,
        // preserva o movimento direto anterior.
        if (pathfinder == null) {
            MoveTowards(
                finalDestination,
                currentStopDistance
            );

            return;
        }

        // Atualiza a rota quando necessário.
        RefreshPathIfNeeded(finalDestination);

        if (!hasPath) {
            StopMovement();
            return;
        }

        // Avança pelos pontos já alcançados.
        AdvanceWaypoints();

        if (!hasPath) {
            StopMovement();
            return;
        }

        // Verifica se o personagem ficou preso.
        if (CheckAndRecoverIfStuck()) {
            StopMovement();
            return;
        }

        Vector2 waypoint =
            path[currentWaypointIndex];

        bool isLastWaypoint =
            currentWaypointIndex == path.Count - 1;

        float arrivalDistance = isLastWaypoint
            ? currentStopDistance
            : waypointArrivalDistance;

        MoveTowards(
            waypoint - GetNavigationOffset(),
            arrivalDistance
        );
    }

    private Vector2 GetDestination() {
        if (HasCombatSlot) {
            return combatSlotManager.GetSlotPosition(
                combatSlotIndex
            );
        }

        return target.position;
    }


    private void RefreshPathIfNeeded(Vector2 destination) {
        float thresholdSquared =
            destinationChangeThreshold *
            destinationChangeThreshold;

        // Verifica se o objetivo mudou de posição.
        bool destinationChanged =
            !hasPlannedDestination ||
            (destination - lastPlannedDestination)
            .sqrMagnitude >= thresholdSquared;

        bool needsNewPath =
            !hasPath || destinationChanged;

        if (!needsNewPath)
            return;

        // Se a rota anterior falhou, mas o destino mudou,
        // não precisamos aguardar o intervalo de falha.
        if (destinationChanged && pathUnavailable) {
            nextRepathTime = 0f;
        }

        // Evita cálculos excessivos.
        if (Time.time < nextRepathTime)
            return;

        lastPlannedDestination = destination;

        hasPlannedDestination = true;

        // Limpa o caminho anterior.
        path.Clear();

        currentWaypointIndex = 0;

        // Calcula as posições considerando
        // o centro físico do Collider.
        Vector2 navigationOffset =
            GetNavigationOffset();

        Vector2 navigationStart =
            (Vector2)bodyCollider.bounds.center;

        Vector2 navigationDestination =
            destination + navigationOffset;

        // Obtém o raio individual da unidade.
        float navigationRadius =
            GetNavigationRadius();

        // Solicita a rota ao Pathfinder.
        hasPath = pathfinder.TryFindPath(
            navigationStart,
            navigationDestination,
            navigationRadius,
            path
        ) && path.Count > 0;

        // CAMINHO ENCONTRADO.
        if (hasPath) {
            pathUnavailable = false;

            nextRepathTime =
                Time.time + repathInterval;

            return;
        }

        // CAMINHO NÃO ENCONTRADO.
        pathUnavailable = true;

        path.Clear();

        currentWaypointIndex = 0;

        // Aguarda mais tempo antes de tentar
        // calcular novamente o mesmo caminho.
        nextRepathTime =
            Time.time + failedPathRetryInterval;
    }

    private void AdvanceWaypoints() {
        while (currentWaypointIndex < path.Count) {
            bool isLastWaypoint =
                currentWaypointIndex == path.Count - 1;

            float arrivalDistance = isLastWaypoint
                ? currentStopDistance
                : waypointArrivalDistance;

            Vector2 waypoint =
                path[currentWaypointIndex];

            float distance =
                Vector2.Distance(
                    bodyCollider.bounds.center,
                    waypoint
                );

            if (distance > arrivalDistance)
                break;

            currentWaypointIndex++;
        }

        // Todos os pontos foram alcançados.
        if (currentWaypointIndex >= path.Count) {
            hasPath = false;

            path.Clear();

            currentWaypointIndex = 0;
        }
    }


    private void MoveTowards(
        Vector2 destination,
        float arrivalDistance) {
        Vector2 currentPosition = rb.position;

        Vector2 direction =
            destination - currentPosition;

        float distance = direction.magnitude;

        float remainingDistance =
            distance - arrivalDistance;

        if (remainingDistance <= 0f) {
            StopMovement();
            return;
        }

        float movementDistance =
            unit.Definition.MoveSpeed *
            Time.fixedDeltaTime;

        movementDistance = Mathf.Min(
            movementDistance,
            remainingDistance
        );

        Vector2 normalizedDirection = direction / distance;

        CurrentMoveDirection = normalizedDirection;

        LastMoveDirection = normalizedDirection;

        IsMoving = true;

        // Verifica quanto podemos andar na direção desejada.
        float allowedDistance = GetAllowedDistance(
            normalizedDirection,
            movementDistance,
            out Collider2D blocker
        );

        // Encontramos um obstáculo que está impedindo
        // boa parte do movimento?
        bool movementBlocked =
            allowedDistance < movementDistance * 0.8f;

        if (movementBlocked &&
            enableUnitAvoidance &&
            blocker != null) {
            // Verifica se o obstáculo é uma unidade.
            UnitEntity blockingUnit =
                blocker.GetComponentInParent<UnitEntity>();

            // Se for uma unidade viva, tenta contorná-la.
            if (blockingUnit != null &&
                !blockingUnit.Health.IsDead &&
                blockingUnit.gameObject != gameObject) {
                if (TryAvoidUnit(
                        normalizedDirection,
                        movementDistance)) {
                    return;
                }
            }
        }

        // Se a direção original está livre,
        // não precisamos manter o lado de desvio.
        if (!movementBlocked) {
            avoidanceSide = 0;
        }

        // Não conseguiu avançar.
        if (allowedDistance <= 0f) {
            StopMovement();
            return;
        }

        // Executa o movimento normal.
        rb.MovePosition(
            currentPosition +
            normalizedDirection * allowedDistance
        );
    }


    private float GetAllowedDistance(
        Vector2 direction,
        float desiredDistance,
        out Collider2D blocker) {
        blocker = null;

        int hitCount = bodyCollider.Cast(
            direction,
            collisionFilter,
            castResults,
            desiredDistance + collisionSkin
        );

        float allowedDistance = desiredDistance;

        for (int i = 0; i < hitCount; i++) {
            RaycastHit2D hit = castResults[i];

            if (hit.collider == null)
                continue;

            float distanceBeforeCollision =
                Mathf.Max(
                    0f,
                    hit.distance - collisionSkin
                );

            // Guarda o obstáculo mais próximo.
            if (distanceBeforeCollision < allowedDistance) {
                allowedDistance = distanceBeforeCollision;

                blocker = hit.collider;
            }
        }

        return allowedDistance;
    }


    private bool TryAvoidUnit(
        Vector2 desiredDirection,
        float movementDistance) {
        // Mantém o lado escolhido anteriormente.
        // Se ainda não existe um lado, tenta esquerda primeiro.
        int firstSide =
            avoidanceSide == 0 ? 1 : avoidanceSide;

        // Primeiro tenta o lado preferido.
        // Se não encontrar passagem, tenta o lado oposto.
        for (int sideAttempt = 0;
             sideAttempt < 2;
             sideAttempt++) {
            int side = sideAttempt == 0
                ? firstSide
                : -firstSide;

            // Testa diferentes ângulos de desvio.
            for (int i = 0;
                 i < AvoidanceAngles.Length;
                 i++) {
                float angle =
                    AvoidanceAngles[i] * side;

                float radians =
                    angle * Mathf.Deg2Rad;

                float cos = Mathf.Cos(radians);
                float sin = Mathf.Sin(radians);

                // Gira a direção desejada.
                Vector2 candidateDirection =
                    new Vector2(
                        desiredDirection.x * cos -
                        desiredDirection.y * sin,

                        desiredDirection.x * sin +
                        desiredDirection.y * cos
                    );

                // Verifica se podemos andar nessa direção.
                float allowedDistance =
                    GetAllowedDistance(
                        candidateDirection,
                        movementDistance,
                        out _
                    );

                // Não utiliza uma direção que também
                // esteja praticamente bloqueada.
                if (allowedDistance <
                    movementDistance * 0.9f) {
                    continue;
                }

                // Encontramos uma direção disponível.
                avoidanceSide = side;

                rb.MovePosition(
                    rb.position +
                    candidateDirection * allowedDistance
                );

                return true;
            }
        }

        // Não existe passagem lateral disponível agora.
        return false;
    }

    private void StopMovement() {
        rb.linearVelocity =
            Vector2.zero;

        IsMoving = false;

        CurrentMoveDirection =
            Vector2.zero;
    }




    private void InvalidatePath() {
        path.Clear();

        currentWaypointIndex = 0;

        hasPath = false;

        hasPlannedDestination = false;

        pathUnavailable = false;

        nextRepathTime = 0f;

        // Limpa o lado de desvio anterior.
        avoidanceSide = 0;

        // Reinicia o monitoramento de travamento.
        ResetStuckMonitor();
    }

    // Permite configurar a navegação por código.
    public void ConfigureNavigation(
        Pathfinder2D newPathfinder) {
        pathfinder = newPathfinder;

        InvalidatePath();
    }

    // Movimento normal.
    public void SetTarget(Transform newTarget) {
        target = newTarget;

        combatSlotManager = null;
        combatSlotIndex = -1;

        currentStopDistance = stopDistance;

        InvalidatePath();
    }

    // Movimento com distância personalizada.
    public void SetTarget(
        Transform newTarget,
        float newStopDistance) {
        SetTarget(newTarget);

        currentStopDistance = Mathf.Max(
            0.01f,
            newStopDistance
        );
    }

    // Movimento para uma posição de combate.
    public void SetCombatSlot(
        CombatSlotManager2D manager,
        int slotIndex) {
        target = null;

        combatSlotManager = manager;
        combatSlotIndex = slotIndex;

        currentStopDistance = 0.03f;

        InvalidatePath();
    }

    public void ClearTarget() {
        target = null;

        combatSlotManager = null;
        combatSlotIndex = -1;

        currentStopDistance = stopDistance;

        InvalidatePath();

        StopMovement();
    }


    private bool CheckAndRecoverIfStuck() {
        // Sem rota ativa, não existe movimento
        // pendente para monitorar.
        if (!hasPath) {
            ResetStuckMonitor();
            return false;
        }

        // Começa uma nova observação.
        if (!monitoringStuck) {
            lastStuckCheckPosition = rb.position;

            stuckCheckTimer = 0f;

            monitoringStuck = true;

            return false;
        }

        stuckCheckTimer += Time.fixedDeltaTime;

        // Ainda não chegou o momento da verificação.
        if (stuckCheckTimer < stuckCheckInterval)
            return false;

        // Mede quanto o personagem realmente andou.
        float movedDistance =
            Vector2.Distance(
                rb.position,
                lastStuckCheckPosition
            );

        // Prepara a próxima observação.
        lastStuckCheckPosition = rb.position;

        stuckCheckTimer = 0f;

        // O personagem está se movimentando.
        if (movedDistance >= stuckMinMovement)
            return false;

        // Existe uma rota, mas não houve movimento
        // suficiente: consideramos um travamento.
        if (drawNavigationPath) {
            Debug.Log(
                $"{name}: movimento bloqueado. " +
                "Solicitando nova rota.",
                this
            );
        }

        // Descarta o caminho anterior.
        InvalidatePath();

        // Aguarda o intervalo normal antes de
        // realizar uma nova busca A*.
        nextRepathTime = Time.time + repathInterval;

        return true;
    }

    private void ResetStuckMonitor() {
        monitoringStuck = false;

        stuckCheckTimer = 0f;

        lastStuckCheckPosition = Vector2.zero;
    }

    private void OnDisable() {
        InvalidatePath();
    }


    private void OnDrawGizmosSelected() {
        if (!drawNavigationPath)
            return;

        if (!Application.isPlaying)
            return;

        // Posição atual do personagem.
        Vector3 currentPosition =
            bodyCollider != null
                ? bodyCollider.bounds.center
                : transform.position;

        // Pequeno ajuste de profundidade para
        // facilitar a visualização dos Gizmos.
        float gizmoZ = -0.1f;

        currentPosition.z = gizmoZ;

        // Marca a posição atual.
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            currentPosition,
            0.05f
        );

        // Desenha o caminho calculado pelo A*.
        if (hasPath && path.Count > 0) {
            Vector3 previousPosition = currentPosition;

            Gizmos.color = Color.magenta;

            // Desenha somente os pontos
            // que ainda precisam ser percorridos.
            for (int i = currentWaypointIndex;
                 i < path.Count;
                 i++) {
                Vector2 point = path[i];

                Vector3 waypointPosition = new Vector3(
                    point.x,
                    point.y,
                    gizmoZ
                );

                Gizmos.DrawLine(
                    previousPosition,
                    waypointPosition
                );

                Gizmos.DrawWireSphere(
                    waypointPosition,
                    0.035f
                );

                previousPosition = waypointPosition;
            }

            // Destaca o próximo waypoint.
            if (currentWaypointIndex < path.Count) {
                Vector2 nextWaypoint =
                    path[currentWaypointIndex];

                Gizmos.color = Color.yellow;

                Gizmos.DrawSphere(
                    new Vector3(
                        nextWaypoint.x,
                        nextWaypoint.y,
                        gizmoZ
                    ),
                    0.065f
                );
            }
        }

        // Desenha o destino final.
        if (target != null || HasCombatSlot) {
            Vector2 destination =
                GetDestination() + GetNavigationOffset();

            Vector3 destinationPosition = new Vector3(
                destination.x,
                destination.y,
                gizmoZ
            );

            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(
                destinationPosition,
                0.09f
            );
        }
    }


    private float GetNavigationRadius() {
        Bounds bounds = bodyCollider.bounds;

        float radius;

        // Para nossos personagens com CircleCollider2D.
        if (bodyCollider is CircleCollider2D) {
            radius = Mathf.Max(
                bounds.extents.x,
                bounds.extents.y
            );
        } else {
            // Aproximação circular conservadora
            // para outros formatos de Collider.
            radius = new Vector2(
                bounds.extents.x,
                bounds.extents.y
            ).magnitude;
        }

        return Mathf.Max(
            0.01f,
            radius + navigationPadding
        );
    }

    private Vector2 GetNavigationOffset() {
        return (Vector2)bodyCollider.bounds.center -
               rb.position;
    }
}