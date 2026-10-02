using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(UnitMovement2D))]
[RequireComponent(typeof(UnitAttack2D))]
[RequireComponent(typeof(Health))]
public sealed class UnitAnimator2D : MonoBehaviour {
    private Animator animator;

    private UnitMovement2D movement;

    private UnitAttack2D attack;

    private Health health;

    private string currentState;

    private UnitFacingDirection
        currentDirection =
            UnitFacingDirection.SW;

    private bool isAttacking;

    private float attackEndTime;

    private bool isDead;

    public event Action DeathAnimationCompleted;

    private bool deathAnimationPending;

    private void Awake() {
        CacheReferences();
    }

    private void OnEnable() {
        if (attack != null) {
            attack.AttackPerformed +=
                HandleAttackPerformed;
        }

        if (health != null) {
            health.Died +=
                HandleDied;

            health.Revived +=
                HandleRevived;
        }
    }

    private void Start() {
        ResetForSpawn();
    }



    private void Update() {
        // Unidade morta:
        // deixa a animação Death terminar.
        if (isDead) {
            CheckDeathAnimationCompletion();
            return;
        }

        // Ataque em andamento.
        if (isAttacking) {
            if (Time.time <
                attackEndTime) {
                return;
            }

            isAttacking = false;

            currentState = null;
        }

        UpdateDirectionFromMovement();

        if (movement != null &&
            movement.IsMoving) {
            PlayDirectionalAnimation(
                "Walk"
            );
        } else {
            PlayDirectionalAnimation(
                "Idle"
            );
        }
    }

    private void HandleAttackPerformed(
        Vector2 attackDirection) {
        if (isDead)
            return;

        if (attackDirection.sqrMagnitude >
            0.0001f) {
            currentDirection =
                GetDirectionFromVector(
                    attackDirection
                );
        }

        isAttacking = true;

        PlayDirectionalAnimation(
            "Attack",
            true
        );

        // Força o Animator a entrar
        // imediatamente no estado novo,
        // para podermos ler sua duração.
        animator.Update(0f);

        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(
                0
            );

        float duration =
            Mathf.Max(
                0.05f,
                stateInfo.length
            );

        attackEndTime =
            Time.time + duration;
    }

    private void HandleDied() {
        if (isDead)
            return;

        isDead = true;

        isAttacking = false;

        deathAnimationPending = false;

        currentState = null;

        // Mantém aproximadamente a direção
        // em que a unidade estava olhando.
        UpdateDirectionFromMovement();

        PlayDirectionalAnimation(
            "Death",
            true
        );

        // Se conseguimos entrar em um
        // estado Death, começamos a esperar
        // o término da animação.
        if (!string.IsNullOrEmpty(
                currentState) &&
            currentState.StartsWith(
                "Death_")) {
            deathAnimationPending = true;
        }
    }

    private void HandleRevived() {
        isDead = false;

        isAttacking = false;

        deathAnimationPending = false;

        currentState = null;

        UpdateDirectionFromMovement();

        PlayDirectionalAnimation(
            "Idle",
            true
        );
    }

    private void UpdateDirectionFromMovement() {
        if (movement == null)
            return;

        Vector2 direction =
            movement.IsMoving
                ? movement.CurrentMoveDirection
                : movement.LastMoveDirection;

        if (direction.sqrMagnitude <
            0.0001f) {
            return;
        }

        currentDirection =
            GetDirectionFromVector(
                direction
            );
    }

    private void PlayDirectionalAnimation(
        string action,
        bool restart = false) {
        CacheReferences();

        if (animator == null) {
            return;
        }

        if (animator.runtimeAnimatorController == null) {
            return;
        }
        UnitFacingDirection
            visualDirection =
                ResolveAvailableDirection(
                    action,
                    currentDirection
                );

        string stateName =
            $"{action}_{visualDirection}";

        if (!restart &&
            currentState == stateName) {
            return;
        }

        int stateHash =
            Animator.StringToHash(
                stateName
            );

        if (!animator.HasState(
                0,
                stateHash)) {
            Debug.LogWarning(
                $"{name}: estado " +
                $"{stateName} não existe.",
                this
            );

            return;
        }

        animator.Play(
            stateHash,
            0,
            0f
        );

        currentState =
            stateName;
    }

    private UnitFacingDirection
        GetDirectionFromVector(
            Vector2 direction) {
        direction.Normalize();

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;

        if (angle < 0f) {
            angle += 360f;
        }

        if (angle >= 337.5f ||
            angle < 22.5f) {
            return UnitFacingDirection.E;
        }

        if (angle < 67.5f) {
            return UnitFacingDirection.NE;
        }

        if (angle < 112.5f) {
            return UnitFacingDirection.N;
        }

        if (angle < 157.5f) {
            return UnitFacingDirection.NW;
        }

        if (angle < 202.5f) {
            return UnitFacingDirection.W;
        }

        if (angle < 247.5f) {
            return UnitFacingDirection.SW;
        }

        if (angle < 292.5f) {
            return UnitFacingDirection.S;
        }

        return UnitFacingDirection.SE;
    }

    private UnitFacingDirection
    ResolveAvailableDirection(
        string action,
        UnitFacingDirection desired) {
        CacheReferences();

        if (animator == null ||
            animator.runtimeAnimatorController == null) {
            return desired;
        }

        // Primeiro tenta exatamente
        // a direção desejada.
        string exactState =
            $"{action}_{desired}";

        if (animator.HasState(
                0,
                Animator.StringToHash(
                    exactState))) {
            return desired;
        }

        // Se a direção desejada não existe,
        // procura entre TODAS as direções
        // disponíveis qual é a mais próxima.
        UnitFacingDirection[] directions =
        {
        UnitFacingDirection.N,
        UnitFacingDirection.NE,
        UnitFacingDirection.E,
        UnitFacingDirection.SE,
        UnitFacingDirection.S,
        UnitFacingDirection.SW,
        UnitFacingDirection.W,
        UnitFacingDirection.NW
    };

        Vector2 desiredVector =
            DirectionToVector(
                desired
            );

        bool foundAny = false;

        UnitFacingDirection bestDirection =
            desired;

        float bestDot =
            float.NegativeInfinity;

        foreach (
            UnitFacingDirection candidate
            in directions) {
            string candidateState =
                $"{action}_{candidate}";

            int candidateHash =
                Animator.StringToHash(
                    candidateState
                );

            // Não existe animação dessa
            // ação nessa direção.
            if (!animator.HasState(
                    0,
                    candidateHash)) {
                continue;
            }

            Vector2 candidateVector =
                DirectionToVector(
                    candidate
                );

            float dot =
                Vector2.Dot(
                    desiredVector,
                    candidateVector
                );

            if (!foundAny ||
                dot > bestDot) {
                foundAny = true;

                bestDot =
                    dot;

                bestDirection =
                    candidate;
            }
        }

        return bestDirection;
    }

    private Vector2 DirectionToVector(
        UnitFacingDirection direction) {
        switch (direction) {
            case UnitFacingDirection.N:
                return Vector2.up;

            case UnitFacingDirection.NE:
                return new Vector2(
                    1f,
                    1f
                ).normalized;

            case UnitFacingDirection.E:
                return Vector2.right;

            case UnitFacingDirection.SE:
                return new Vector2(
                    1f,
                    -1f
                ).normalized;

            case UnitFacingDirection.S:
                return Vector2.down;

            case UnitFacingDirection.SW:
                return new Vector2(
                    -1f,
                    -1f
                ).normalized;

            case UnitFacingDirection.W:
                return Vector2.left;

            case UnitFacingDirection.NW:
                return new Vector2(
                    -1f,
                    1f
                ).normalized;
        }

        return Vector2.down;
    }

    private void OnDisable() {
        if (attack != null) {
            attack.AttackPerformed -=
                HandleAttackPerformed;
        }

        if (health != null) {
            health.Died -=
                HandleDied;

            health.Revived -=
                HandleRevived;
        }
    }
    public void ResetForSpawn() {
        // Importante para Object Pooling:
        // este método pode ser chamado muito cedo,
        // inclusive durante o primeiro OnEnable.
        CacheReferences();

        isDead =
            health != null &&
            health.IsDead;

        isAttacking = false;

        deathAnimationPending = false;

        currentState = null;

        currentDirection =
            UnitFacingDirection.SW;

        if (animator == null) {
            Debug.LogError(
                $"{name}: Animator não encontrado.",
                this
            );

            return;
        }

        if (animator.runtimeAnimatorController == null) {
            Debug.LogError(
                $"{name}: Animator Controller não configurado.",
                this
            );

            return;
        }

        if (!isDead) {
            PlayDirectionalAnimation(
                "Idle",
                true
            );
        }
    }
    private void CheckDeathAnimationCompletion() {
        if (!deathAnimationPending)
            return;

        if (animator == null)
            return;

        if (string.IsNullOrEmpty(
                currentState)) {
            return;
        }

        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(
                0
            );

        // Garante que estamos realmente
        // observando a animação Death atual.
        if (!stateInfo.IsName(
                currentState)) {
            return;
        }

        // normalizedTime:
        //
        // 0.0 = começo
        // 0.5 = metade
        // 1.0 = terminou
        if (stateInfo.normalizedTime < 1f)
            return;

        deathAnimationPending = false;

        DeathAnimationCompleted?.Invoke();
    }

    private void CacheReferences() {
        if (animator == null) {
            animator =
                GetComponent<Animator>();
        }

        if (movement == null) {
            movement =
                GetComponent<UnitMovement2D>();
        }

        if (attack == null) {
            attack =
                GetComponent<UnitAttack2D>();
        }

        if (health == null) {
            health =
                GetComponent<Health>();
        }
    }
}