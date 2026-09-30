using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(TowerAttack2D))]
public sealed class TowerEntity2D : MonoBehaviour
{
    [Header("Definição")]
    [SerializeField]
    private TowerDefinition definition;

    private Health health;

    private TowerAttack2D towerAttack;

    public TowerDefinition Definition =>
        definition;

    public Health Health =>
        health;

    private void Awake()
    {
        health =
            GetComponent<Health>();

        towerAttack =
            GetComponent<TowerAttack2D>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Died += HandleDied;
        }
    }

    private void Start()
    {
        if (definition == null)
        {
            Debug.LogError(
                $"{name}: TowerDefinition não configurado.",
                this
            );

            enabled = false;
            return;
        }

        // Agora a vida vem dos dados da torre.
        health.Initialize(
            definition.MaxHealth
        );
    }

    private void HandleDied()
    {
        if (towerAttack != null)
        {
            towerAttack.enabled = false;
        }

        Debug.Log(
            $"{name} foi destruída.",
            this
        );
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Died -= HandleDied;
        }
    }
}