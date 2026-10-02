using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class TowerEntity2D : MonoBehaviour {
    [Header("Definição")]
    [SerializeField]
    private TowerDefinition definition;

    [Header("Componentes")]
    [SerializeField]
    private Collider2D gameplayCollider;

    private Health health;

    private TowerAttack2D towerAttack;

    private bool destroyed;

    public TowerDefinition Definition =>
        definition;

    public Health Health =>
        health;

    public bool IsDestroyed =>
        destroyed;

    private void Awake() {
        health =
            GetComponent<Health>();

        towerAttack =
            GetComponent<TowerAttack2D>();

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
        if (definition == null) {
            Debug.LogError(
                $"{name}: TowerDefinition não configurado.",
                this
            );

            enabled = false;
            return;
        }

        health.Initialize(
            definition.MaxHealth
        );

        destroyed = false;
    }

    private void HandleDied() {
        if (destroyed)
            return;

        destroyed = true;

        if (towerAttack != null) {
            towerAttack.enabled =
                false;
        }

        // A torre destruída não deve mais
        // ser encontrada pelos inimigos
        // como alvo de combate.
        if (gameplayCollider != null) {
            gameplayCollider.enabled =
                false;
        }

        Debug.Log(
            $"{name} foi destruída e permanece no campo.",
            this
        );
    }

    public bool Repair() {
        if (!destroyed ||
            health == null ||
            !health.IsDead) {
            return false;
        }

        if (!health.ReviveFull()) {
            return false;
        }

        destroyed = false;

        if (gameplayCollider != null) {
            gameplayCollider.enabled =
                true;
        }

        if (towerAttack != null) {
            towerAttack.enabled =
                true;
        }

        Debug.Log(
            $"{name} foi reparada.",
            this
        );

        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("DEBUG/Repair Tower")]
    private void DebugRepair() {
        if (!Application.isPlaying) {
            Debug.LogWarning(
                "Use este comando durante o Play Mode.",
                this
            );

            return;
        }

        Repair();
    }
#endif

    private void OnDisable() {
        if (health != null) {
            health.Died -=
                HandleDied;
        }
    }
}