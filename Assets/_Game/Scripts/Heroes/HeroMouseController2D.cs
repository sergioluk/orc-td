
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(UnitEntity))]
[RequireComponent(typeof(UnitMovement2D))]
[RequireComponent(typeof(UnitAttack2D))]
public sealed class HeroMouseController2D : MonoBehaviour {
    [Header("Referências")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameManager gameManager;

    [SerializeField] private Tilemap groundTilemap;

    [SerializeField] private Transform moveTarget;

    [Header("Seleção de inimigos")]
    [SerializeField] private LayerMask enemySelectionLayer;

    private UnitEntity unit;
    private UnitMovement2D movement;
    private UnitAttack2D attack;

    // Inimigo selecionado atualmente.
    private Health currentEnemy;

    private TargetHighlight2D
    currentTargetHighlight;

    private void Awake() {
        unit = GetComponent<UnitEntity>();

        movement = GetComponent<UnitMovement2D>();

        attack = GetComponent<UnitAttack2D>();
    }

    private void Update() {
        if (unit.Health.IsDead)
            return;
        // Não aceita comandos após o resultado final.
        if (gameManager != null) {
            GamePhase phase = gameManager.CurrentPhase;

            if (phase == GamePhase.Victory ||
                phase == GamePhase.Defeat) {
                return;
            }
        }

        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (mainCamera == null ||
            groundTilemap == null ||
            moveTarget == null) {
            Debug.LogError(
                "HeroMouseController2D não está configurado corretamente.",
                this
            );

            return;
        }

        HandleMouseClick();
    }

    private void HandleMouseClick() {
        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        float cameraDistance =
            transform.position.z -
            mainCamera.transform.position.z;

        Vector3 worldPosition =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    mousePosition.x,
                    mousePosition.y,
                    cameraDistance
                )
            );

        worldPosition.z = transform.position.z;

        // Primeiro verifica se clicamos em um inimigo.
        Collider2D selectedCollider =
            Physics2D.OverlapPoint(
                worldPosition,
                enemySelectionLayer
            );

        if (selectedCollider != null) {
            EnemySelectionTarget2D selection =
                selectedCollider.GetComponent<EnemySelectionTarget2D>();

            if (selection != null &&
                selection.TargetHealth != null &&
                !selection.TargetHealth.IsDead) {
                SelectEnemy(selection.TargetHealth);
                return;
            }
        }

        // Se não selecionamos um inimigo,
        // verificamos se o clique foi no chão.
        Vector3Int cellPosition =
            groundTilemap.WorldToCell(worldPosition);

        if (!groundTilemap.HasTile(cellPosition))
            return;

        MoveToPosition(worldPosition);
    }

    private void SelectEnemy(
    Health enemyHealth) {
        if (enemyHealth == null ||
            enemyHealth.IsDead) {
            return;
        }

        // Evita selecionar novamente
        // o mesmo inimigo.
        if (currentEnemy == enemyHealth)
            return;

        // Remove o destaque do alvo anterior.
        ClearEnemyTarget();

        currentEnemy =
            enemyHealth;

        currentEnemy.Died +=
            HandleEnemyDied;

        // Procura o sistema visual no Orc.
        currentTargetHighlight =
            currentEnemy.GetComponent<
                TargetHighlight2D>();

        if (currentTargetHighlight != null) {
            currentTargetHighlight
                .SetHighlighted(true);
        }

        float approachDistance =
            unit.Definition.AttackRange *
            0.8f;

        movement.SetTarget(
            currentEnemy.transform,
            approachDistance
        );

        attack.SetTarget(
            currentEnemy,
            currentEnemy.transform
        );
    }

    private void MoveToPosition(Vector3 worldPosition) {
        // Cancela qualquer ataque anterior.
        ClearEnemyTarget();

        // Atualiza o destino.
        moveTarget.position = worldPosition;

        // Inicia o movimento normal.
        movement.SetTarget(moveTarget,0.03f);
    }

    private void HandleEnemyDied() {
        ClearEnemyTarget();

        // Para de perseguir o inimigo morto.
        movement.ClearTarget();
    }

    private void ClearEnemyTarget() {
        // Primeiro remove o visual.
        if (currentTargetHighlight != null) {
            currentTargetHighlight
                .SetHighlighted(false);
        }

        currentTargetHighlight =
            null;

        if (currentEnemy != null) {
            currentEnemy.Died -=
                HandleEnemyDied;
        }

        currentEnemy = null;

        attack.ClearTarget();
    }

    private void OnDisable() {
        ClearEnemyTarget();
    }
}