
using UnityEngine;

public sealed class CombatSlotManager2D : MonoBehaviour {
    [Header("Posições de combate")]
    [SerializeField, Range(4,12)]
    private int slotCount = 6;

    [SerializeField, Min(0.05f)]
    private float slotRadius = 0.27f;

    [SerializeField, Min(0f)]
    private float extraClearance = 0.015f;

    [Header("Verificação de ocupação")]
    [SerializeField] private LayerMask occupancyLayers;

    // Cada posição pode pertencer a um atacante.
    private Transform[] reservedBy;

    private ContactFilter2D occupancyFilter;

    // Buffer reutilizável para consultas de física.
    private readonly Collider2D[] overlapResults =
        new Collider2D[32];

    private void Awake() {
        reservedBy = new Transform[slotCount];

        occupancyFilter = new ContactFilter2D();

        occupancyFilter.SetLayerMask(occupancyLayers);

        occupancyFilter.useTriggers = false;
    }

    // Tenta reservar uma posição para o atacante.
    public bool TryReserve(
        Transform attacker,
        float attackRange,
        out int slotIndex) {
        slotIndex = -1;

        if (attacker == null || !isActiveAndEnabled)
            return false;

        // A posição precisa estar dentro do alcance.
        if (attackRange < slotRadius + 0.03f)
            return false;

        // Se já possui uma reserva, retorna a mesma.
        for (int i = 0; i < reservedBy.Length; i++) {
            if (reservedBy[i] == attacker) {
                slotIndex = i;
                return true;
            }
        }

        Collider2D attackerCollider =
            attacker.GetComponent<Collider2D>();

        if (attackerCollider == null)
            return false;

        float attackerRadius = Mathf.Max(
            attackerCollider.bounds.extents.x,
            attackerCollider.bounds.extents.y
        ) + extraClearance;

        float bestDistance = float.MaxValue;

        // Procura a posição disponível mais próxima.
        for (int i = 0; i < reservedBy.Length; i++) {
            if (reservedBy[i] != null)
                continue;

            Vector2 slotPosition = GetSlotPosition(i);

            if (!IsPositionFree(
                    slotPosition,
                    attackerRadius,
                    attacker)) {
                continue;
            }

            float distanceSquared =
                ((Vector2)attacker.position -
                 slotPosition).sqrMagnitude;

            if (distanceSquared < bestDistance) {
                bestDistance = distanceSquared;
                slotIndex = i;
            }
        }

        if (slotIndex < 0)
            return false;

        reservedBy[slotIndex] = attacker;

        return true;
    }

    // Calcula a posição no mundo.
    public Vector2 GetSlotPosition(int index) {
        float angle =
            (360f / slotCount) *
            index *
            Mathf.Deg2Rad;

        Vector2 offset = new Vector2(
            Mathf.Cos(angle),
            Mathf.Sin(angle)
        ) * slotRadius;

        return (Vector2)transform.position + offset;
    }

    private bool IsPositionFree(
        Vector2 position,
        float radius,
        Transform attacker) {
        int hitCount = Physics2D.OverlapCircle(
            position,
            radius,
            occupancyFilter,
            overlapResults
        );

        // Conservador: não aprova uma consulta
        // se o buffer ficou completamente cheio.
        if (hitCount >= overlapResults.Length)
            return false;

        for (int i = 0; i < hitCount; i++) {
            Collider2D hit = overlapResults[i];

            if (hit == null)
                continue;

            Transform hitTransform = hit.transform;

            // Ignora o próprio alvo.
            if (hitTransform == transform ||
                hitTransform.IsChildOf(transform)) {
                continue;
            }

            // Ignora o próprio atacante.
            if (hitTransform == attacker ||
                hitTransform.IsChildOf(attacker)) {
                continue;
            }

            // Existe outro objeto ocupando a posição.
            return false;
        }

        return true;
    }

    // Libera a posição reservada pelo atacante.
    public void Release(Transform attacker) {
        if (reservedBy == null)
            return;

        for (int i = 0; i < reservedBy.Length; i++) {
            if (reservedBy[i] == attacker) {
                reservedBy[i] = null;
                return;
            }
        }
    }

    private void OnDisable() {
        if (reservedBy == null)
            return;

        for (int i = 0; i < reservedBy.Length; i++) {
            reservedBy[i] = null;
        }
    }

    private void OnDrawGizmosSelected() {
        if (slotCount <= 0)
            return;

        Gizmos.color = Color.green;

        for (int i = 0; i < slotCount; i++) {
            Vector2 position = GetSlotPosition(i);

            Gizmos.DrawWireSphere(
                position,
                0.05f
            );
        }
    }
}