using UnityEngine;

[RequireComponent(typeof(UnitEntity))]
[RequireComponent(typeof(Health))]
public sealed class EnemyGoldReward2D : MonoBehaviour {
    private UnitEntity unit;
    private Health health;

    private bool rewardGiven;

    private void Awake() {
        unit = GetComponent<UnitEntity>();
        health = GetComponent<Health>();
    }

    private void OnEnable() {
        // Como os Orcs são reutilizados pelo Pool,
        // cada nova ativação representa uma nova vida.
        rewardGiven = false;

        health.Died += OnDied;
    }

    private void OnDisable() {
        health.Died -= OnDied;
    }

    private void OnDied() {
        // Proteção contra recompensa duplicada.
        if (rewardGiven)
            return;

        rewardGiven = true;

        if (unit.Definition == null) {
            Debug.LogWarning(
                $"{name}: UnitDefinition não configurado.",
                this
            );

            return;
        }

        int reward = unit.Definition.GoldReward;

        if (reward <= 0)
            return;

        if (GoldManager.Instance == null) {
            Debug.LogError(
                $"{name}: GoldManager não encontrado na cena.",
                this
            );

            return;
        }

        GoldManager.Instance.AddGold(reward);

#if UNITY_EDITOR
        Debug.Log(
            $"{name} derrotado: +{reward} ouro. " +
            $"Total: {GoldManager.Instance.CurrentGold}",
            this
        );
#endif
    }
}