
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class NavigationGrid2D : MonoBehaviour {
    [Header("Mapa")]
    [SerializeField] private Tilemap groundTilemap;

    [Header("Configuração da navegação")]
    [SerializeField, Min(0.1f)]
    private float cellSize = 0.25f;

    [SerializeField, Min(0.01f)]
    private float agentRadius = 0.09f;

    public float DefaultAgentRadius => agentRadius;

    [Header("Obstáculos")]
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Depuração")]
    [SerializeField] private bool drawGrid = true;

    private Vector2 origin;

    private int width;
    private int height;

    public float CellSize => cellSize;

    public int Width => width;
    public int Height => height;

    private void Awake() {
        InitializeGrid();
    }

    private void InitializeGrid() {
        if (groundTilemap == null) {
            Debug.LogError(
                "NavigationGrid2D: Ground Tilemap não configurado.",
                this
            );

            return;
        }

        Renderer tilemapRenderer =
            groundTilemap.GetComponent<Renderer>();

        if (tilemapRenderer == null) {
            Debug.LogError(
                "NavigationGrid2D: Tilemap Renderer não encontrado.",
                this
            );

            return;
        }

        // Obtém os limites do mapa no mundo.
        Bounds bounds = tilemapRenderer.bounds;

        origin = new Vector2(
            bounds.min.x,
            bounds.min.y
        );

        // Calcula a quantidade de células.
        width = Mathf.CeilToInt(
            bounds.size.x / cellSize
        );

        height = Mathf.CeilToInt(
            bounds.size.y / cellSize
        );

        Debug.Log(
            $"NavigationGrid2D inicializado: {width} x {height}",
            this
        );
    }

    // Converte uma posição do mundo
    // para uma célula do grid de navegação.
    public Vector2Int WorldToGrid(Vector2 worldPosition) {
        int x = Mathf.FloorToInt(
            (worldPosition.x - origin.x) / cellSize
        );

        int y = Mathf.FloorToInt(
            (worldPosition.y - origin.y) / cellSize
        );

        return new Vector2Int(x,y);
    }

    // Converte uma célula do grid para
    // uma posição no mundo.
    public Vector2 GridToWorld(Vector2Int cell) {
        float x =
            origin.x + (cell.x + 0.5f) * cellSize;

        float y =
            origin.y + (cell.y + 0.5f) * cellSize;

        return new Vector2(x,y);
    }

    // Verifica se uma célula está dentro do mapa.
    public bool IsInsideGrid(Vector2Int cell) {
        return cell.x >= 0 &&
               cell.y >= 0 &&
               cell.x < width &&
               cell.y < height;
    }

    // Verifica se a célula pode ser percorrida.

    public bool IsWalkable(Vector2Int cell) {
        return IsWalkable(cell,agentRadius);
    }

    public bool IsWalkable(
        Vector2Int cell,
        float radius) {
        if (!IsInsideGrid(cell))
            return false;

        Vector2 worldPosition =
            GridToWorld(cell);

        // Verifica se existe chão.
        Vector3Int tileCell =
            groundTilemap.WorldToCell(worldPosition);

        if (!groundTilemap.HasTile(tileCell))
            return false;

        // Verifica se o agente cabe nesta posição.
        Collider2D obstacle =
            Physics2D.OverlapCircle(
                worldPosition,
                radius,
                obstacleLayers
            );

        return obstacle == null;
    }

    private void OnDrawGizmosSelected() {
        if (!drawGrid || !Application.isPlaying)
            return;

        if (groundTilemap == null)
            return;

        // Evita desenhar uma quantidade excessiva
        // de Gizmos em mapas grandes.
        const int maxDrawnCells = 3000;

        int totalCells = width * height;

        int step = Mathf.Max(
            1,
            Mathf.CeilToInt(
                Mathf.Sqrt(
                    totalCells / (float)maxDrawnCells
                )
            )
        );

        for (int x = 0; x < width; x += step) {
            for (int y = 0; y < height; y += step) {
                Vector2Int cell =
                    new Vector2Int(x,y);

                bool walkable =
                    IsWalkable(cell);

                Gizmos.color = walkable
                    ? Color.green
                    : Color.red;

                Vector2 position =
                    GridToWorld(cell);

                Gizmos.DrawWireCube(
                    position,
                    new Vector3(
                        cellSize * 0.8f,
                        cellSize * 0.8f,
                        0f
                    )
                );
            }
        }
    }


    public bool HasClearPath(
        Vector2 startWorld,
        Vector2 destinationWorld) {
        return HasClearPath(
            startWorld,
            destinationWorld,
            agentRadius
        );
    }

    public bool HasClearPath(
        Vector2 startWorld,
        Vector2 destinationWorld,
        float radius) {
        if (groundTilemap == null || cellSize <= 0f)
            return false;

        Vector2 difference =
            destinationWorld - startWorld;

        float distance = difference.magnitude;

        float sampleSpacing = cellSize * 0.5f;

        int sampleCount = Mathf.Max(
            1,
            Mathf.CeilToInt(distance / sampleSpacing)
        );

        // Verifica a continuidade do chão.
        for (int i = 0; i <= sampleCount; i++) {
            float t = i / (float)sampleCount;

            Vector2 samplePosition = Vector2.Lerp(
                startWorld,
                destinationWorld,
                t
            );

            Vector2Int gridCell =
                WorldToGrid(samplePosition);

            if (!IsInsideGrid(gridCell))
                return false;

            Vector3Int tileCell =
                groundTilemap.WorldToCell(samplePosition);

            if (!groundTilemap.HasTile(tileCell))
                return false;
        }

        // Origem e destino praticamente iguais.
        if (distance <= 0.0001f) {
            return Physics2D.OverlapCircle(
                startWorld,
                radius,
                obstacleLayers
            ) == null;
        }

        Vector2 direction = difference / distance;

        // Verifica a passagem considerando o raio
        // individual do personagem.
        RaycastHit2D hit = Physics2D.CircleCast(
            startWorld,
            radius,
            direction,
            distance,
            obstacleLayers
        );

        return hit.collider == null;
    }
}