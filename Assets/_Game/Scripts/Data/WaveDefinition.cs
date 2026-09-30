
using UnityEngine;

[CreateAssetMenu(
    fileName = "Wave_",
    menuName = "Game/Data/Wave Definition"
)]
public sealed class WaveDefinition : ScriptableObject {
    [Header("Identidade")]
    [SerializeField] private string displayName = "Nova Onda";

    [Header("Inimigos")]
    [SerializeField] private EnemyObjectiveController enemyPrefab;

    [SerializeField, Min(1)]
    private int enemyCount = 5;

    [Header("Temporização")]
    [SerializeField, Min(0f)]
    private float initialDelay = 2f;

    [SerializeField, Min(0.1f)]
    private float spawnInterval = 3f;

    public string DisplayName => displayName;

    public EnemyObjectiveController EnemyPrefab => enemyPrefab;

    public int EnemyCount => enemyCount;

    public float InitialDelay => initialDelay;

    public float SpawnInterval => spawnInterval;
}