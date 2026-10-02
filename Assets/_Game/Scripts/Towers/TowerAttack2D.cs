using UnityEngine;

[RequireComponent(typeof(TowerEntity2D))]
public sealed class TowerAttack2D : MonoBehaviour
{
    [Header("Detecção")]
    [SerializeField]
    private LayerMask enemyLayer;

    [Header("Projétil")]
    [SerializeField]
    private HomingProjectilePool2D projectilePool;

    [SerializeField]
    private Transform firePoint;

    [Header("Depuração")]
    [SerializeField]
    private bool drawAttackRange = true;

    private TowerEntity2D tower;

    private Health currentTarget;

    private float detectionTimer;

    private float attackTimer;

    private void Awake() {
        tower =
            GetComponent<TowerEntity2D>();

        if (projectilePool == null) {
            projectilePool =
                HomingProjectilePool2D.Instance;
        }
    }

    private void Update()
    {
        if (tower == null)
            return;

        if (tower.Definition == null)
            return;

        if (tower.Health == null ||
            tower.Health.IsDead)
        {
            return;
        }

        detectionTimer -=
            Time.deltaTime;

        attackTimer -=
            Time.deltaTime;

        ValidateCurrentTarget();

        if (currentTarget == null &&
            detectionTimer <= 0f)
        {
            detectionTimer =
                tower.Definition
                    .DetectionInterval;

            FindTarget();
        }

        if (currentTarget == null)
            return;

        if (attackTimer > 0f)
            return;

        Attack();
    }

    private void ValidateCurrentTarget()
    {
        if (currentTarget == null)
            return;

        if (currentTarget.IsDead ||
            !currentTarget.gameObject
                .activeInHierarchy)
        {
            currentTarget = null;
            return;
        }

        float distance =
            Vector2.Distance(
                transform.position,
                currentTarget
                    .transform.position
            );

        if (distance >
            tower.Definition.AttackRange)
        {
            currentTarget = null;
        }
    }

    private void FindTarget()
    {
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                tower.Definition.AttackRange,
                enemyLayer
            );

        Health bestTarget = null;

        float bestDistanceSquared =
            float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            Health health =
                hit.GetComponent<Health>();

            if (health == null)
            {
                health =
                    hit.GetComponentInParent<
                        Health>();
            }

            if (health == null ||
                health.IsDead)
            {
                continue;
            }

            float distanceSquared =
                (
                    (Vector2)
                    health.transform.position -
                    (Vector2)
                    transform.position
                ).sqrMagnitude;

            if (distanceSquared >=
                bestDistanceSquared)
            {
                continue;
            }

            bestDistanceSquared =
                distanceSquared;

            bestTarget =
                health;
        }

        currentTarget =
            bestTarget;
    }

    private void Attack()
    {
        if (currentTarget == null ||
            currentTarget.IsDead)
        {
            return;
        }

        if (projectilePool == null) {
            Debug.LogError(
                $"{name}: Projectile Pool não configurado.",
                this
            );

            return;
        }

        Vector3 spawnPosition =
            firePoint != null
                ? firePoint.position
                : transform.position;

        Transform projectileTargetPoint =
        GetProjectileTargetPoint(
            currentTarget
        );

        projectilePool.Spawn(
            spawnPosition,
            Quaternion.identity,
            currentTarget,
            projectileTargetPoint,
            tower.Definition.AttackDamage
        );

        attackTimer =
            tower.Definition.AttackInterval;
    }

private Transform GetProjectileTargetPoint(
    Health targetHealth)
{
    if (targetHealth == null)
        return null;

    ProjectileTargetPoint2D point =
        targetHealth.GetComponentInChildren<
            ProjectileTargetPoint2D>();

    if (point != null)
    {
        return point.transform;
    }

    // Caso o objeto ainda não possua
    // um ponto específico, usa o Transform principal.
    return targetHealth.transform;
}
    private void OnDrawGizmosSelected()
    {
        if (!drawAttackRange)
            return;

        TowerEntity2D entity =
            GetComponent<TowerEntity2D>();

        if (entity == null ||
            entity.Definition == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(
            transform.position,
            entity.Definition.AttackRange
        );
    }
}