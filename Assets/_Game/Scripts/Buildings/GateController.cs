using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class GateController : MonoBehaviour {
    [Header("Componentes do Portão")]
    [SerializeField] private Collider2D blockingCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Health health;

    private void Awake() {
        health = GetComponent<Health>();

        if (blockingCollider == null)
            blockingCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable() {
        health.Died += HandleDeath;
    }

    private void OnDisable() {
        health.Died -= HandleDeath;
    }

    private void HandleDeath() {
        if (blockingCollider != null)
            blockingCollider.enabled = false;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }
}