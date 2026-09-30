
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(UnitMovement2D))]
[RequireComponent(typeof(UnitAttack2D))]
[RequireComponent(typeof(HeroMouseController2D))]
public sealed class HeroDeathController2D : MonoBehaviour {
    [Header("Visual")]
    [SerializeField] private bool hideSpriteOnDeath = true;

    private Health health;
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;

    private UnitMovement2D movement;
    private UnitAttack2D attack;
    private HeroMouseController2D mouseController;
    private CombatSlotManager2D combatSlots;

    private void Awake() {
        health = GetComponent<Health>();

        rb = GetComponent<Rigidbody2D>();

        bodyCollider = GetComponent<Collider2D>();

        spriteRenderer = GetComponent<SpriteRenderer>();

        movement = GetComponent<UnitMovement2D>();

        attack = GetComponent<UnitAttack2D>();

        mouseController =
            GetComponent<HeroMouseController2D>();

        combatSlots =
            GetComponent<CombatSlotManager2D>();
    }

    private void OnEnable() {
        health.Died += HandleDeath;
    }

    private void OnDisable() {
        health.Died -= HandleDeath;
    }

    private void HandleDeath() {
        // Remove os objetivos atuais.
        movement.ClearTarget();

        attack.ClearTarget();

        // Interrompe o movimento físico.
        rb.linearVelocity = Vector2.zero;

        // Impede novos comandos do jogador.
        mouseController.enabled = false;

        // Desativa movimento e ataque.
        movement.enabled = false;

        attack.enabled = false;

        // Deixa de bloquear outras unidades.
        if (bodyCollider != null)
            bodyCollider.enabled = false;

        // Libera as posições de combate reservadas.
        if (combatSlots != null)
            combatSlots.enabled = false;

        // Esconde a sprite, se configurado.
        if (hideSpriteOnDeath && spriteRenderer != null)
            spriteRenderer.enabled = false;

        Debug.Log(
            $"Herói derrotado: {name}",
            this
        );
    }
}