using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class BuildingEntity : MonoBehaviour {
    [Header("Dados da construção")]
    [SerializeField]
    private BuildingDefinition definition;

    private Health health;

    public BuildingDefinition Definition => definition;
    public Health Health => health;

    private void Awake() {
        health = GetComponent<Health>();

        if (definition == null) {
            Debug.LogError(
                $"BuildingDefinition não foi configurado em {name}.",
                this
            );

            enabled = false;
            return;
        }

        health.Initialize(definition.MaxHealth);
    }
}