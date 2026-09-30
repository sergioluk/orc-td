
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyPool2D : MonoBehaviour {
    [Header("Prefab")]
    [SerializeField] private EnemyObjectiveController enemyPrefab;

    [Header("Configuração")]
    [SerializeField, Min(0)]
    private int initialSize = 8;

    // Orcs disponíveis para reutilização.
    private readonly Queue<PooledEnemy2D> available =
        new();

    // Orcs atualmente utilizados na batalha.
    private readonly HashSet<PooledEnemy2D> activeEnemies =
        new();

    public EnemyObjectiveController EnemyPrefab => enemyPrefab;

    public int AvailableCount => available.Count;

    public int ActiveCount => activeEnemies.Count;

    public int TotalCount =>
        available.Count + activeEnemies.Count;

    private void Awake() {
        if (enemyPrefab == null) {
            Debug.LogError(
                "EnemyPool2D: Prefab não configurado.",
                this
            );

            return;
        }

        if (enemyPrefab.GetComponent<PooledEnemy2D>() == null) {
            Debug.LogError(
                "O Prefab precisa possuir PooledEnemy2D.",
                this
            );

            return;
        }

        // Cria antecipadamente as instâncias.
        for (int i = 0; i < initialSize; i++) {
            CreateEnemy();
        }
    }

    private PooledEnemy2D CreateEnemy() {
        EnemyObjectiveController instance =
            Instantiate(
                enemyPrefab,
                transform.position,
                Quaternion.identity,
                transform
            );

        PooledEnemy2D pooledEnemy =
            instance.GetComponent<PooledEnemy2D>();

        if (pooledEnemy == null) {
            Destroy(instance.gameObject);
            return null;
        }

        // Guarda o inimigo desativado.
        instance.gameObject.SetActive(false);

        available.Enqueue(pooledEnemy);

        return pooledEnemy;
    }

    // Solicita uma instância disponível.
    // Ela é devolvida DESATIVADA para permitir
    // que o Spawner configure tudo antes do Play.
    public PooledEnemy2D Acquire() {
        if (enemyPrefab == null)
            return null;

        if (available.Count == 0) {
            CreateEnemy();
        }

        if (available.Count == 0)
            return null;

        PooledEnemy2D enemy = available.Dequeue();

        activeEnemies.Add(enemy);

        return enemy;
    }

    // Devolve o inimigo ao Pool.
    public void Release(PooledEnemy2D enemy) {
        if (enemy == null)
            return;

        // Evita devolver a mesma instância duas vezes.
        if (!activeEnemies.Remove(enemy))
            return;

        enemy.gameObject.SetActive(false);

        // Mantém os Orcs guardados dentro do Pool.
        enemy.transform.SetParent(transform,true);

        available.Enqueue(enemy);
    }
}