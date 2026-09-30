
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class EnemySelectionTarget2D : MonoBehaviour {
    private Health targetHealth;

    public Health TargetHealth => targetHealth;

    private void Awake() {
        // Procura o Health no objeto pai.
        targetHealth = GetComponentInParent<Health>();

        if (targetHealth == null) {
            Debug.LogError(
                $"Health não encontrado para {name}.",
                this
            );
        }
    }
}