using UnityEngine;

public sealed class HomingProjectile2D : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField, Min(0.1f)]
    private float moveSpeed = 6f;

    [SerializeField, Min(0.001f)]
    private float hitDistance = 0.08f;

    [Header("Rotação")]
    [SerializeField]
    private bool rotateTowardsTarget = true;

    [SerializeField]
    private float rotationOffset = 0f;

    private Health targetHealth;

    private Transform targetPoint;

    private int damage;

    private bool initialized;

    public void Initialize(
        Health newTargetHealth,
        Transform newTargetPoint,
        int newDamage)
    {
        targetHealth =
            newTargetHealth;

        targetPoint =
            newTargetPoint != null
                ? newTargetPoint
                : newTargetHealth.transform;

        damage =
            Mathf.Max(
                0,
                newDamage
            );

        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        // O alvo morreu ou foi removido.
        if (targetHealth == null ||
            !targetHealth.gameObject.activeInHierarchy ||
            targetHealth.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        if (targetPoint == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 currentPosition =
            transform.position;

        Vector2 targetPosition =
            targetPoint.position;

        Vector2 difference =
            targetPosition -
            currentPosition;

        float distance =
            difference.magnitude;

        // Impacto.
        if (distance <= hitDistance)
        {
            HitTarget();
            return;
        }

        Vector2 direction =
            difference / distance;

        float movement =
            moveSpeed *
            Time.deltaTime;

        movement =
            Mathf.Min(
                movement,
                distance
            );

        transform.position =
            currentPosition +
            direction * movement;

        if (rotateTowardsTarget)
        {
            float angle =
                Mathf.Atan2(
                    direction.y,
                    direction.x
                ) *
                Mathf.Rad2Deg;

            transform.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle + rotationOffset
                );
        }
    }

    private void HitTarget()
    {
        if (targetHealth != null &&
            !targetHealth.IsDead)
        {
            targetHealth.TakeDamage(
                damage
            );
        }

        Destroy(gameObject);
    }
}