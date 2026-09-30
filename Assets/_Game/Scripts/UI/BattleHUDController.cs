using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleHUDController : MonoBehaviour {
    [Header("Sistemas")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private EnemyWaveSpawner waveSpawner;

    [SerializeField] private Health gateHealth;
    [SerializeField] private Health castleHealth;

    [Header("Topo da HUD")]
    [SerializeField] private TextMeshProUGUI phaseLabel;
    [SerializeField] private TextMeshProUGUI waveLabel;
    [SerializeField] private TextMeshProUGUI enemiesLabel;

    [SerializeField] private Image gateFillImage;
    [SerializeField] private TextMeshProUGUI gateValueLabel;

    [SerializeField] private Image castleFillImage;
    [SerializeField] private TextMeshProUGUI castleValueLabel;

    [Header("Tela de resultado")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleLabel;
    [SerializeField] private TextMeshProUGUI resultSubtitleLabel;

    private void OnEnable() {
        if (gameManager != null)
            gameManager.PhaseChanged += HandlePhaseChanged;

        if (waveSpawner != null)
            waveSpawner.CountsChanged += HandleCountsChanged;

        if (gateHealth != null)
            gateHealth.HealthChanged += HandleGateHealthChanged;

        if (castleHealth != null)
            castleHealth.HealthChanged += HandleCastleHealthChanged;
    }

    private void Start() {
        ValidateReferences();

        RefreshAll();
    }

    private void OnDisable() {
        if (gameManager != null)
            gameManager.PhaseChanged -= HandlePhaseChanged;

        if (waveSpawner != null)
            waveSpawner.CountsChanged -= HandleCountsChanged;

        if (gateHealth != null)
            gateHealth.HealthChanged -= HandleGateHealthChanged;

        if (castleHealth != null)
            castleHealth.HealthChanged -= HandleCastleHealthChanged;
    }

    private void ValidateReferences() {
        if (gameManager == null ||
            waveSpawner == null ||
            gateHealth == null ||
            castleHealth == null) {
            Debug.LogError(
                "BattleHUDController não está configurado corretamente.",
                this
            );
        }
    }

    private void RefreshAll() {
        if (gameManager != null) {
            UpdatePhaseLabel(gameManager.CurrentPhase);
            UpdateResultPanel(gameManager.CurrentPhase);
        }

        UpdateWaveInfo();

        if (gateHealth != null) {
            UpdateBar(
                gateFillImage,
                gateValueLabel,
                "Portão",
                gateHealth.CurrentHealth,
                gateHealth.MaxHealth
            );
        }

        if (castleHealth != null) {
            UpdateBar(
                castleFillImage,
                castleValueLabel,
                "Castelo",
                castleHealth.CurrentHealth,
                castleHealth.MaxHealth
            );
        }
    }

    private void HandlePhaseChanged(GamePhase newPhase) {
        UpdatePhaseLabel(newPhase);
        UpdateResultPanel(newPhase);
    }

    private void HandleCountsChanged() {
        UpdateWaveInfo();
    }

    private void HandleGateHealthChanged(int currentHealth,int maxHealth) {
        UpdateBar(
            gateFillImage,
            gateValueLabel,
            "Portão",
            currentHealth,
            maxHealth
        );
    }

    private void HandleCastleHealthChanged(int currentHealth,int maxHealth) {
        UpdateBar(
            castleFillImage,
            castleValueLabel,
            "Castelo",
            currentHealth,
            maxHealth
        );
    }

    private void UpdatePhaseLabel(GamePhase phase) {
        if (phaseLabel == null)
            return;

        phaseLabel.text = $"Estado: {phase}";
    }

    private void UpdateWaveInfo() {
        if (waveSpawner == null)
            return;

        if (waveLabel != null) {
            waveLabel.text =
                $"Onda: {waveSpawner.WaveDisplayName}";
        }

        if (enemiesLabel != null) {
            enemiesLabel.text =
                $"Inimigos restantes: {waveSpawner.RemainingCount}/{waveSpawner.TotalCount}";
        }
    }

    private void UpdateBar(
        Image fillImage,
        TextMeshProUGUI valueLabel,
        string label,
        int currentHealth,
        int maxHealth) {
        float normalized = 0f;

        if (maxHealth > 0)
            normalized = (float)currentHealth / maxHealth;

        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(normalized);

        if (valueLabel != null)
            valueLabel.text = $"{label}: {currentHealth}/{maxHealth}";
    }

    private void UpdateResultPanel(GamePhase phase) {
        if (resultPanel == null)
            return;

        bool showVictory = phase == GamePhase.Victory;
        bool showDefeat = phase == GamePhase.Defeat;

        resultPanel.SetActive(showVictory || showDefeat);

        if (!showVictory && !showDefeat)
            return;

        if (resultTitleLabel != null) {
            resultTitleLabel.text =
                showVictory ? "VITÓRIA!" : "DERROTA!";
        }

        if (resultSubtitleLabel != null) {
            resultSubtitleLabel.text =
                showVictory
                ? "Todos os inimigos foram eliminados."
                : "O castelo foi destruído.";
        }
    }
}