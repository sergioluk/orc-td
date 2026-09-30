using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour {
    [Header("Sistemas")]
    [SerializeField] private EnemyWaveSpawner waveSpawner;
    [SerializeField] private Health castleHealth;

    [Header("Construção")]
    [SerializeField] private ShopUIController shopUI;
    [SerializeField] private PlacementManager2D placementManager;

    [Header("Progressão das Ondas")]
    [SerializeField] private WaveDefinition[] waves;

    [Header("Inicialização")]
    [SerializeField]
    private bool autoStartFirstWave = false;

    [Header("Estado Atual")]
    [SerializeField]
    private GamePhase currentPhase =
        GamePhase.None;

    [SerializeField]
    private int currentWaveIndex = -1;

    public GamePhase CurrentPhase =>
        currentPhase;

    public int CurrentWaveNumber =>
        currentWaveIndex + 1;

    public int TotalWaves =>
        waves != null ? waves.Length : 0;

    public event Action<GamePhase> PhaseChanged;

    private void Awake() {
        Time.timeScale = 1f;

        currentPhase = GamePhase.None;
        currentWaveIndex = -1;
    }

    private void OnEnable() {
        if (waveSpawner != null) {
            waveSpawner.WaveCompleted +=
                HandleWaveCompleted;
        }

        if (castleHealth != null) {
            castleHealth.Died +=
                HandleCastleDestroyed;
        }
    }

    private void Start() {
        if (waveSpawner == null ||
            castleHealth == null ||
            waves == null ||
            waves.Length == 0) {
            Debug.LogError(
                "GameManager não está configurado corretamente.",
                this
            );

            enabled = false;
            return;
        }

        if (shopUI == null) {
            Debug.LogWarning(
                "GameManager: ShopUI não configurado.",
                this
            );
        }

        if (placementManager == null) {
            Debug.LogWarning(
                "GameManager: PlacementManager não configurado.",
                this
            );
        }

        // Prepara a primeira onda.
        if (!PrepareWave(0)) {
            enabled = false;
            return;
        }

        if (autoStartFirstWave) {
            BeginBattle();
        }
    }

    private void Update() {
        if (Keyboard.current == null)
            return;

        // Durante a preparação,
        // Espaço inicia a batalha.
        if (currentPhase == GamePhase.Preparation) {
            if (Keyboard.current
                .spaceKey.wasPressedThisFrame) {
                BeginBattle();
            }
        }

        // Reiniciar após vitória ou derrota.
        if (currentPhase == GamePhase.Victory ||
            currentPhase == GamePhase.Defeat) {
            if (Keyboard.current
                .rKey.wasPressedThisFrame) {
                RestartBattle();
            }
        }
    }

    private bool PrepareWave(int waveIndex) {
        if (waveIndex < 0 ||
            waveIndex >= waves.Length) {
            return false;
        }

        WaveDefinition definition =
            waves[waveIndex];

        if (definition == null) {
            Debug.LogError(
                $"WaveDefinition ausente no índice {waveIndex}.",
                this
            );

            return false;
        }

        bool configured =
            waveSpawner.SetWave(definition);

        if (!configured)
            return false;

        currentWaveIndex = waveIndex;

        ChangePhase(
            GamePhase.Preparation
        );

        Debug.Log(
            $"Preparação: onda " +
            $"{CurrentWaveNumber}/{TotalWaves}",
            this
        );

        return true;
    }

    [ContextMenu("Iniciar batalha")]
    public void BeginBattle() {
        if (!Application.isPlaying)
            return;

        if (currentPhase !=
            GamePhase.Preparation) {
            return;
        }

        if (castleHealth.IsDead)
            return;

        bool started =
            waveSpawner.BeginWave();

        if (!started) {
            Debug.LogError(
                "Não foi possível iniciar a batalha.",
                this
            );

            return;
        }

        ChangePhase(
            GamePhase.Battle
        );
    }

    private void HandleWaveCompleted() {
        if (currentPhase != GamePhase.Battle)
            return;

        if (castleHealth.IsDead) {
            HandleCastleDestroyed();
            return;
        }

        int nextWaveIndex =
            currentWaveIndex + 1;

        // Ainda existem ondas.
        if (nextWaveIndex < waves.Length) {
            PrepareWave(nextWaveIndex);
            return;
        }

        // Todas as ondas concluídas.
        ChangePhase(
            GamePhase.Victory
        );

        Debug.Log(
            "VITÓRIA! Todas as ondas foram concluídas.",
            this
        );

        Time.timeScale = 0f;
    }

    private void HandleCastleDestroyed() {
        if (currentPhase ==
            GamePhase.Defeat ||
            currentPhase ==
            GamePhase.Victory) {
            return;
        }

        ChangePhase(
            GamePhase.Defeat
        );

        waveSpawner.StopWave();

        Debug.Log(
            "DERROTA! O castelo foi destruído.",
            this
        );

        Time.timeScale = 0f;
    }

    private void ChangePhase(
        GamePhase newPhase) {
        if (currentPhase == newPhase)
            return;

        currentPhase = newPhase;

        // Libera ou bloqueia a construção
        // conforme a fase atual.
        UpdateBuildAvailability();

        Debug.Log(
            $"Estado da partida: {currentPhase}",
            this
        );

        PhaseChanged?.Invoke(
            currentPhase
        );
    }

    private void UpdateBuildAvailability() {
        bool canBuild =
            currentPhase ==
            GamePhase.Preparation;

        if (shopUI != null) {
            shopUI.SetShopEnabled(
                canBuild
            );
        }

        if (placementManager != null) {
            placementManager
                .SetPlacementEnabled(
                    canBuild
                );
        }
    }

    [ContextMenu("Reiniciar partida")]
    public void RestartBattle() {
        if (!Application.isPlaying)
            return;

        // Restaura o tempo antes
        // de carregar a Scene.
        Time.timeScale = 1f;

        Scene currentScene =
            SceneManager.GetActiveScene();

        if (currentScene.buildIndex < 0) {
            Debug.LogError(
                "Adicione a Scene à lista de cenas do Build.",
                this
            );

            return;
        }

        SceneManager.LoadScene(
            currentScene.buildIndex
        );
    }

    private void OnDisable() {
        if (waveSpawner != null) {
            waveSpawner.WaveCompleted -=
                HandleWaveCompleted;
        }

        if (castleHealth != null) {
            castleHealth.Died -=
                HandleCastleDestroyed;
        }
    }
}