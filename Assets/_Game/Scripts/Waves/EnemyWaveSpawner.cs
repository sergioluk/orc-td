
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyWaveSpawner : MonoBehaviour {
    [Header("Configuração da Onda")]
    [SerializeField] private WaveDefinition wave;

    [Header("Spawn")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform enemyContainer;

    [Header("Object Pooling")]
    [SerializeField] private EnemyPool2D enemyPool;

    [Header("Objetivo: Portão")]
    [SerializeField] private Health gateHealth;
    [SerializeField] private Transform gateAttackPoint;

    [Header("Entrada da Fortaleza")]
    [SerializeField] private Transform insideTarget;

    [Header("Objetivo: Castelo")]
    [SerializeField] private Health castleHealth;
    [SerializeField] private Transform castleAttackPoint;

    [Header("Navegação")]
    [SerializeField] private Pathfinder2D pathfinder;

    public event Action WaveCompleted;
    public event Action CountsChanged;

    private readonly List<PooledEnemy2D> spawnedEnemies = new();

    private Coroutine spawnCoroutine;

    private int spawnedCount;
    private int aliveCount;

    private bool waveStarted;
    private bool finishedSpawning;
    private bool waveCompleted;
    private bool waveCancelled;

    public int SpawnedCount => spawnedCount;
    public int AliveCount => aliveCount;

    public int TotalCount =>
        wave != null ? wave.EnemyCount : 0;

    public int DefeatedCount =>
        Mathf.Max(0,spawnedCount - aliveCount);

    public int RemainingCount =>
        Mathf.Max(0,TotalCount - DefeatedCount);

    public string WaveDisplayName =>
        wave != null ? wave.DisplayName : "-";

    // Prepara uma nova onda.
    public bool SetWave(WaveDefinition newWave) {
        // Não permite substituir uma batalha em andamento.
        if (waveStarted &&
            !waveCompleted &&
            !waveCancelled) {
            Debug.LogWarning(
                "Não é possível substituir uma onda em andamento.",
                this
            );

            return false;
        }

        if (newWave == null || newWave.EnemyPrefab == null) {
            Debug.LogError(
                "WaveDefinition inválido.",
                this
            );

            return false;
        }

        // Remove os objetos da onda anterior.
        ClearPreviousWave();

        wave = newWave;

        spawnedCount = 0;
        aliveCount = 0;

        waveStarted = false;
        finishedSpawning = false;
        waveCompleted = false;
        waveCancelled = false;

        CountsChanged?.Invoke();

        Debug.Log(
            $"Onda preparada: {wave.DisplayName}",
            this
        );

        return true;
    }

    public bool BeginWave() {
        if (waveStarted)
            return false;

        if (!ValidateConfiguration())
            return false;

        waveStarted = true;

        CountsChanged?.Invoke();

        spawnCoroutine = StartCoroutine(SpawnWave());

        return true;
    }

    private bool ValidateConfiguration() {
        if (wave == null || wave.EnemyPrefab == null) {
            Debug.LogError(
                "Onda ou Prefab não configurado.",
                this
            );

            return false;
        }

        if (spawnPoint == null ||
            gateHealth == null ||
            gateAttackPoint == null ||
            insideTarget == null ||
            castleHealth == null ||
            castleAttackPoint == null) {
            Debug.LogError(
                "Referências do EnemyWaveSpawner incompletas.",
                this
            );

            return false;
        }

        if (enemyPool == null) {
            Debug.LogError(
                "EnemyPool2D não configurado no Spawner.",
                this
            );

            return false;
        }

        if (wave.EnemyPrefab != enemyPool.EnemyPrefab) {
            Debug.LogError(
                "O Prefab da onda é diferente do Prefab configurado no Pool.",
                this
            );

            return false;
        }

        if (pathfinder == null) {
            Debug.LogError(
                "Pathfinder2D não configurado no Spawner.",
                this
            );

            return false;
        }

        return true;
    }

    private IEnumerator SpawnWave() {
        Debug.Log($"Iniciando {wave.DisplayName}");

        if (wave.InitialDelay > 0f) {
            yield return new WaitForSeconds(
                wave.InitialDelay
            );
        }

        WaitForSeconds intervalWait =
            new WaitForSeconds(wave.SpawnInterval);

        for (int i = 0; i < wave.EnemyCount; i++) {
            if (waveCancelled)
                yield break;

            SpawnEnemy();

            if (i < wave.EnemyCount - 1)
                yield return intervalWait;
        }

        finishedSpawning = true;

        spawnCoroutine = null;

        CheckWaveCompletion();
    }


    private void SpawnEnemy() {
        // Solicita um Orc desativado ao Pool.
        PooledEnemy2D pooledEnemy =
            enemyPool.Acquire();

        if (pooledEnemy == null) {
            Debug.LogError(
                "Não foi possível obter um inimigo do Pool.",
                this
            );

            return;
        }

        // Organiza a instância na Hierarchy.
        pooledEnemy.transform.SetParent(
            enemyContainer,
            true
        );

        // Posiciona no ponto de Spawn.
        pooledEnemy.transform.SetPositionAndRotation(
            spawnPoint.position,
            Quaternion.identity
        );

        // Recupera os componentes da unidade.
        UnitEntity unit =
            pooledEnemy.GetComponent<UnitEntity>();

        EnemyObjectiveController enemy =
            pooledEnemy.GetComponent<EnemyObjectiveController>();

        UnitMovement2D movement =
            pooledEnemy.GetComponent<UnitMovement2D>();

        movement.ConfigureNavigation(pathfinder);

        // Reinicializa a vida.
        if (!unit.ResetForSpawn()) {
            enemyPool.Release(pooledEnemy);
            return;
        }

        // Configura os objetivos da invasão.
        enemy.Configure(
            gateHealth,
            gateAttackPoint,
            insideTarget,
            castleHealth,
            castleAttackPoint
        );

        // Registra o evento de morte.
        pooledEnemy.Died += HandleEnemyDied;

        spawnedEnemies.Add(pooledEnemy);

        spawnedCount++;
        aliveCount++;

        // Ativa a unidade somente depois de configurada.
        pooledEnemy.gameObject.SetActive(true);

        // Inicia seu comportamento.
        enemy.BeginInvasion();

        CountsChanged?.Invoke();
    }


    private void HandleEnemyDied(PooledEnemy2D enemy) {
        if (enemy == null)
            return;

        // Remove a inscrição para evitar notificações duplicadas.
        enemy.Died -= HandleEnemyDied;

        // Remove da lista de inimigos em uso.
        spawnedEnemies.Remove(enemy);

        // Devolve a instância ao Pool.
        enemyPool.Release(enemy);

        if (waveCancelled || waveCompleted)
            return;

        aliveCount = Mathf.Max(
            0,
            aliveCount - 1
        );

        CountsChanged?.Invoke();

        CheckWaveCompletion();
    }

    private void CheckWaveCompletion() {
        if (waveCancelled || waveCompleted)
            return;

        if (!finishedSpawning)
            return;

        if (aliveCount > 0)
            return;

        waveCompleted = true;

        Debug.Log(
            $"Onda concluída: {wave.DisplayName}"
        );

        WaveCompleted?.Invoke();
    }

    public void StopWave() {
        if (!waveStarted || waveCancelled || waveCompleted)
            return;

        waveCancelled = true;

        if (spawnCoroutine != null) {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        CountsChanged?.Invoke();
    }


    private void ClearPreviousWave() {
        foreach (PooledEnemy2D enemy in spawnedEnemies) {
            if (enemy == null)
                continue;

            enemy.Died -= HandleEnemyDied;

            enemyPool.Release(enemy);
        }

        spawnedEnemies.Clear();
    }


    private void OnDisable() {
        StopWave();

        foreach (PooledEnemy2D enemy in spawnedEnemies) {
            if (enemy != null) {
                enemy.Died -= HandleEnemyDied;
            }
        }
    }
}