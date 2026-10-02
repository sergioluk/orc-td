using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SelectionPanelUI : MonoBehaviour {
    [Header("Sistemas")]
    [SerializeField]
    private SelectionManager2D selectionManager;

    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private GoldManager goldManager;

    [Header("Painel")]
    [SerializeField]
    private GameObject panelRoot;

    [Header("Textos")]
    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text healthText;

    [SerializeField]
    private TMP_Text stateText;

    [SerializeField]
    private TMP_Text statsText;

    [Header("Recuperação")]
    [SerializeField]
    private Button recoveryButton;

    [SerializeField]
    private TMP_Text recoveryButtonText;

    [Header("Custos")]
    [SerializeField, Range(0f,1f)]
    private float revivePriceMultiplier = 0.5f;

    [SerializeField, Range(0f,1f)]
    private float repairPriceMultiplier = 0.5f;

    private PlacedObject2D selectedObject;

    private Health selectedHealth;

    private AlliedUnitDeathController2D
        selectedAlliedDeath;

    private TowerEntity2D selectedTower;

    private void Start() {
        if (selectionManager == null) {
            Debug.LogError(
                "SelectionPanelUI: SelectionManager não configurado.",
                this
            );

            enabled = false;
            return;
        }

        if (gameManager == null) {
            Debug.LogError(
                "SelectionPanelUI: GameManager não configurado.",
                this
            );

            enabled = false;
            return;
        }

        if (goldManager == null) {
            Debug.LogError(
                "SelectionPanelUI: GoldManager não configurado.",
                this
            );

            enabled = false;
            return;
        }

        selectionManager.SelectionChanged +=
            HandleSelectionChanged;

        gameManager.PhaseChanged +=
            HandlePhaseChanged;

        goldManager.GoldChanged +=
            HandleGoldChanged;

        if (recoveryButton != null) {
            recoveryButton.onClick.AddListener(
                HandleRecoveryPressed
            );
        }

        HidePanel();

        RefreshRecoveryButton();
    }

    private void HandleSelectionChanged(
        PlacedObject2D newSelection) {
        UnsubscribeFromHealth();

        selectedObject =
            newSelection;

        selectedAlliedDeath = null;
        selectedTower = null;

        if (selectedObject == null) {
            HidePanel();
            return;
        }

        selectedHealth =
            selectedObject.GetComponent<Health>();

        selectedAlliedDeath =
            selectedObject.GetComponent<
                AlliedUnitDeathController2D>();

        selectedTower =
            selectedObject.GetComponent<
                TowerEntity2D>();

        if (selectedHealth != null) {
            selectedHealth.HealthChanged +=
                HandleHealthChanged;
        }

        ShowPanel();

        RefreshAll();
    }

    private void HandleHealthChanged(
        int currentHealth,
        int maxHealth) {
        RefreshHealth(
            currentHealth,
            maxHealth
        );

        RefreshState();

        RefreshRecoveryButton();
    }

    private void HandlePhaseChanged(
        GamePhase phase) {
        RefreshRecoveryButton();
    }

    private void HandleGoldChanged(
        int currentGold) {
        RefreshRecoveryButton();
    }

    private void RefreshAll() {
        RefreshName();

        RefreshHealth();

        RefreshState();

        RefreshStats();

        RefreshRecoveryButton();
    }

    private void RefreshName() {
        if (nameText == null ||
            selectedObject == null) {
            return;
        }

        BuildItemDefinition definition =
            selectedObject.Definition;

        if (definition != null) {
            nameText.text =
                definition.DisplayName;

            return;
        }

        nameText.text =
            selectedObject.name;
    }

    private void RefreshHealth() {
        if (healthText == null)
            return;

        if (selectedHealth == null) {
            healthText.text =
                "Vida: --";

            return;
        }

        RefreshHealth(
            selectedHealth.CurrentHealth,
            selectedHealth.MaxHealth
        );
    }

    private void RefreshHealth(
        int currentHealth,
        int maxHealth) {
        if (healthText == null)
            return;

        healthText.text =
            $"Vida: {currentHealth} / {maxHealth}";
    }

    private void RefreshState() {
        if (stateText == null)
            return;

        if (selectedHealth == null) {
            stateText.text =
                "Estado: --";

            return;
        }

        if (!selectedHealth.IsDead) {
            stateText.text =
                "Estado: Vivo";

            return;
        }

        if (selectedTower != null) {
            stateText.text =
                "Estado: Destruída";

            return;
        }

        stateText.text =
            "Estado: Morto";
    }

    private void RefreshStats() {
        if (statsText == null ||
            selectedObject == null) {
            return;
        }

        UnitEntity unit =
            selectedObject.GetComponent<
                UnitEntity>();

        if (unit != null &&
            unit.Definition != null) {
            ShowUnitStats(unit);
            return;
        }

        if (selectedTower != null &&
            selectedTower.Definition != null) {
            ShowTowerStats(
                selectedTower
            );

            return;
        }

        statsText.text = "";
    }

    private void ShowUnitStats(
        UnitEntity unit) {
        var definition =
            unit.Definition;

        statsText.text =
            $"Dano: {definition.AttackDamage}\n" +
            $"Alcance: {definition.AttackRange:0.##}\n" +
            $"Velocidade: {definition.MoveSpeed:0.##}\n" +
            $"Ataque: {definition.AttackCooldown:0.##} s";
    }

    private void ShowTowerStats(
        TowerEntity2D tower) {
        TowerDefinition definition =
            tower.Definition;

        statsText.text =
            $"Dano: {definition.AttackDamage}\n" +
            $"Alcance: {definition.AttackRange:0.##}\n" +
            $"Ataque: {definition.AttackInterval:0.##} s";
    }

    private void RefreshRecoveryButton() {
        if (recoveryButton == null)
            return;

        RecoveryType recoveryType =
            GetRecoveryType();

        bool showButton =
            recoveryType !=
            RecoveryType.None;

        recoveryButton.gameObject.SetActive(
            showButton
        );

        if (!showButton)
            return;

        int cost =
            GetRecoveryCost(
                recoveryType
            );

        if (recoveryButtonText != null) {
            switch (recoveryType) {
                case RecoveryType.Revive:
                    recoveryButtonText.text =
                        $"REVIVER - {cost}";
                    break;

                case RecoveryType.Repair:
                    recoveryButtonText.text =
                        $"REPARAR - {cost}";
                    break;
            }
        }

        recoveryButton.interactable =
            goldManager != null &&
            goldManager.CanAfford(cost);
    }

    private RecoveryType GetRecoveryType() {
        if (selectedObject == null ||
            selectedHealth == null ||
            gameManager == null) {
            return RecoveryType.None;
        }

        // Recuperação só existe
        // durante a preparação.
        if (gameManager.CurrentPhase !=
            GamePhase.Preparation) {
            return RecoveryType.None;
        }

        if (!selectedHealth.IsDead) {
            return RecoveryType.None;
        }

        // Torre destruída.
        if (selectedTower != null &&
            selectedTower.IsDestroyed) {
            return RecoveryType.Repair;
        }

        // Tropa aliada morta.
        if (selectedAlliedDeath != null &&
            selectedAlliedDeath.IsDead) {
            return RecoveryType.Revive;
        }

        return RecoveryType.None;
    }

    private int GetRecoveryCost(
        RecoveryType recoveryType) {
        if (selectedObject == null ||
            selectedObject.Definition == null) {
            return 0;
        }

        int originalPrice =
            selectedObject.Definition.Price;

        float multiplier =
            recoveryType ==
            RecoveryType.Repair
                ? repairPriceMultiplier
                : revivePriceMultiplier;

        return Mathf.Max(
            0,
            Mathf.CeilToInt(
                originalPrice *
                multiplier
            )
        );
    }

    private void HandleRecoveryPressed() {
        RecoveryType recoveryType =
            GetRecoveryType();

        if (recoveryType ==
            RecoveryType.None) {
            return;
        }

        int cost =
            GetRecoveryCost(
                recoveryType
            );

        if (!goldManager.TrySpendGold(
                cost)) {
            return;
        }

        bool success = false;

        switch (recoveryType) {
            case RecoveryType.Revive:

                if (selectedAlliedDeath != null) {
                    success =
                        selectedAlliedDeath.Revive();
                }

                break;

            case RecoveryType.Repair:

                if (selectedTower != null) {
                    success =
                        selectedTower.Repair();
                }

                break;
        }

        // Caso alguma condição tenha mudado,
        // devolve o ouro.
        if (!success) {
            goldManager.AddGold(
                cost
            );

            return;
        }

        RefreshAll();
    }

    private void ShowPanel() {
        if (panelRoot != null) {
            panelRoot.SetActive(true);
        }
    }

    private void HidePanel() {
        if (panelRoot != null) {
            panelRoot.SetActive(false);
        }
    }

    private void UnsubscribeFromHealth() {
        if (selectedHealth != null) {
            selectedHealth.HealthChanged -=
                HandleHealthChanged;
        }

        selectedHealth = null;
    }

    private void OnDestroy() {
        UnsubscribeFromHealth();

        if (selectionManager != null) {
            selectionManager.SelectionChanged -=
                HandleSelectionChanged;
        }

        if (gameManager != null) {
            gameManager.PhaseChanged -=
                HandlePhaseChanged;
        }

        if (goldManager != null) {
            goldManager.GoldChanged -=
                HandleGoldChanged;
        }

        if (recoveryButton != null) {
            recoveryButton.onClick.RemoveListener(
                HandleRecoveryPressed
            );
        }
    }

    private enum RecoveryType {
        None,
        Revive,
        Repair
    }
}