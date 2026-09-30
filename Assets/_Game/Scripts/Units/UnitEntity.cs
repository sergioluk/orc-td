
using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class UnitEntity : MonoBehaviour {
    [Header("Dados da unidade")]
    [SerializeField] private UnitDefinition definition;

    private Health health;

    public UnitDefinition Definition => definition;
    public Health Health => health;

    private void Awake() {
        health = GetComponent<Health>();

        ResetForSpawn();
    }

    // Reinicializa os atributos da unidade.
    // Será chamado sempre que ela sair do Pool.
    public bool ResetForSpawn() {
        if (definition == null) {
            Debug.LogError(
                $"UnitDefinition não configurado em {name}.",
                this
            );

            return false;
        }

        if (health == null)
            health = GetComponent<Health>();

        if (health == null)
            return false;

        // Restaura a vida máxima usando o ScriptableObject.
        health.Initialize(definition.MaxHealth);

        return true;
    }
}