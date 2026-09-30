using UnityEngine;
using UnityEngine.UI;

public sealed class BattleControlsUI : MonoBehaviour {
    [Header("Referências")]
    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private Button startBattleButton;

    [SerializeField]
    private Button restartButton;

    private void Start() {
        if (gameManager == null) {
            Debug.LogError(
                "BattleControlsUI: GameManager não configurado.",
                this
            );

            enabled = false;
            return;
        }

        if (startBattleButton == null ||
            restartButton == null) {
            Debug.LogError(
                "BattleControlsUI: botões não configurados.",
                this
            );

            enabled = false;
            return;
        }

        startBattleButton.onClick.AddListener(
            HandleStartBattleClicked
        );

        restartButton.onClick.AddListener(
            HandleRestartClicked
        );

        gameManager.PhaseChanged +=
            HandlePhaseChanged;

        // Atualiza imediatamente o estado visual.
        UpdateButtons(
            gameManager.CurrentPhase
        );
    }

    private void HandleStartBattleClicked() {
        gameManager.BeginBattle();
    }

    private void HandleRestartClicked() {
        gameManager.RestartBattle();
    }

    private void HandlePhaseChanged(
        GamePhase phase) {
        UpdateButtons(phase);
    }

    private void UpdateButtons(
    GamePhase phase) {
        // Só aparece durante preparação.
        startBattleButton.gameObject.SetActive(
            phase == GamePhase.Preparation
        );

        // Aparece ao perder ou vencer.
        restartButton.gameObject.SetActive(
            phase == GamePhase.Defeat ||
            phase == GamePhase.Victory
        );
    }

    private void OnDestroy() {
        if (startBattleButton != null) {
            startBattleButton.onClick.RemoveListener(
                HandleStartBattleClicked
            );
        }

        if (restartButton != null) {
            restartButton.onClick.RemoveListener(
                HandleRestartClicked
            );
        }

        if (gameManager != null) {
            gameManager.PhaseChanged -=
                HandlePhaseChanged;
        }
    }
}