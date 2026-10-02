using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class AlliedUnitDeathController2D
    : MonoBehaviour {
    [Header("Componentes")]
    [SerializeField]
    private Collider2D gameplayCollider;

    private Health health;

    private UnitMovement2D movement;

    private UnitAttack2D attack;

    private DefenderController2D defenderController;

    private bool dead;

    public bool IsDead =>
        dead;

    private void Awake() {
        health =
            GetComponent<Health>();

        movement =
            GetComponent<UnitMovement2D>();

        attack =
            GetComponent<UnitAttack2D>();

        defenderController =
            GetComponent<DefenderController2D>();

        if (gameplayCollider == null) {
            gameplayCollider =
                GetComponent<Collider2D>();
        }
    }

    private void OnEnable() {
        if (health != null) {
            health.Died +=
                HandleDied;
        }
    }

    private void Start() {
        dead =
            health != null &&
            health.IsDead;
    }

    private void HandleDied() {
        if (dead)
            return;

        dead = true;

        // Limpa qualquer combate ou rota
        // que estivesse ativa antes da morte.
        if (attack != null) {
            attack.ClearTarget();
        }

        if (movement != null) {
            movement.ClearTarget();
        }

        // Ao desativar o DefenderController,
        // ele libera CombatSlots reservados.
        if (defenderController != null) {
            defenderController.enabled =
                false;
        }

        if (attack != null) {
            attack.enabled =
                false;
        }

        if (movement != null) {
            movement.enabled =
                false;
        }

        // O cadáver não participa mais
        // da física de combate.
        if (gameplayCollider != null) {
            gameplayCollider.enabled =
                false;
        }

        Debug.Log(
            $"{name} morreu e permanece no campo.",
            this
        );
    }

    public bool Revive() {
        if (!dead ||
            health == null ||
            !health.IsDead) {
            return false;
        }

        // Primeiro devolvemos a vida.
        if (!health.ReviveFull()) {
            return false;
        }

        dead = false;

        // O collider precisa voltar antes
        // da movimentação.
        if (gameplayCollider != null) {
            gameplayCollider.enabled =
                true;
        }

        if (movement != null) {
            movement.ClearTarget();
            movement.enabled = true;
        }

        if (attack != null) {
            attack.ClearTarget();
            attack.enabled = true;
        }

        if (defenderController != null) {
            defenderController.enabled =
                true;
        }

        Debug.Log(
            $"{name} foi revivido.",
            this
        );

        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("DEBUG/Revive Unit")]
    private void DebugRevive() {
        if (!Application.isPlaying) {
            Debug.LogWarning(
                "Use este comando durante o Play Mode.",
                this
            );

            return;
        }

        Revive();
    }
#endif

    private void OnDisable() {
        if (health != null) {
            health.Died -=
                HandleDied;
        }
    }
}