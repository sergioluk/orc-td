using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public sealed class PlacementGrid2D : MonoBehaviour {
    [Header("Visualização")]
    [SerializeField]
    private bool hideMarkersAtRuntime = true;

    [SerializeField]
    private Color availableCellColor =
        new Color(0.25f,1f,0.25f,0.55f);

    private Tilemap tilemap;

    private TilemapRenderer tilemapRenderer;

    // Células ocupadas continuam sendo consideradas
    // válidas no mapa original, mas não podem receber
    // outra construção.
    private readonly HashSet<Vector3Int> occupiedCells =
        new();

    private bool markersVisible;

    private void Awake() {
        tilemap = GetComponent<Tilemap>();

        tilemapRenderer =
            GetComponent<TilemapRenderer>();

        if (hideMarkersAtRuntime) {
            HideAllowedMarkers();
        }
    }

    public Vector3Int WorldToCell(Vector3 worldPosition) {
        return tilemap.WorldToCell(worldPosition);
    }

    public Vector3 GetCellCenter(Vector3Int cell) {
        Vector3 position =
            tilemap.GetCellCenterWorld(cell);

        position.z = 0f;

        return position;
    }

    public Vector3 GetPlacementPosition(
        Vector3Int anchorCell,
        Vector2Int footprint) {
        Vector3 first =
            tilemap.GetCellCenterWorld(anchorCell);

        Vector3Int lastCell =
            anchorCell +
            new Vector3Int(
                footprint.x - 1,
                footprint.y - 1,
                0
            );

        Vector3 last =
            tilemap.GetCellCenterWorld(lastCell);

        Vector3 position =
            (first + last) * 0.5f;

        position.z = 0f;

        return position;
    }

    public bool IsAllowed(Vector3Int cell) {
        return tilemap.HasTile(cell);
    }

    public bool IsOccupied(Vector3Int cell) {
        return occupiedCells.Contains(cell);
    }

    public bool CanPlace(
        Vector3Int anchorCell,
        Vector2Int footprint) {
        for (int x = 0; x < footprint.x; x++) {
            for (int y = 0; y < footprint.y; y++) {
                Vector3Int cell =
                    anchorCell +
                    new Vector3Int(x,y,0);

                if (!IsAllowed(cell))
                    return false;

                if (IsOccupied(cell))
                    return false;
            }
        }

        return true;
    }

    public void Occupy(
        Vector3Int anchorCell,
        Vector2Int footprint) {
        for (int x = 0; x < footprint.x; x++) {
            for (int y = 0; y < footprint.y; y++) {
                Vector3Int cell =
                    anchorCell +
                    new Vector3Int(x,y,0);

                occupiedCells.Add(cell);
            }
        }

        RefreshMarkers();
    }

    public void Release(
        Vector3Int anchorCell,
        Vector2Int footprint) {
        for (int x = 0; x < footprint.x; x++) {
            for (int y = 0; y < footprint.y; y++) {
                Vector3Int cell =
                    anchorCell +
                    new Vector3Int(x,y,0);

                occupiedCells.Remove(cell);
            }
        }

        RefreshMarkers();
    }

    // =====================================================
    // VISUALIZAÇÃO
    // =====================================================

    public void ShowAllowedMarkers() {
        markersVisible = true;

        if (tilemapRenderer != null) {
            tilemapRenderer.enabled = true;
        }

        RefreshMarkers();
    }

    public void HideAllowedMarkers() {
        markersVisible = false;

        if (tilemapRenderer != null) {
            tilemapRenderer.enabled = false;
        }
    }

    private void RefreshMarkers() {
        if (!markersVisible)
            return;

        BoundsInt bounds =
            tilemap.cellBounds;

        foreach (Vector3Int cell in bounds.allPositionsWithin) {
            if (!tilemap.HasTile(cell))
                continue;

            // Precisamos liberar a alteração de cor individual.
            tilemap.SetTileFlags(
                cell,
                TileFlags.None
            );

            if (occupiedCells.Contains(cell)) {
                // Célula ocupada:
                // não mostra como disponível.
                tilemap.SetColor(
                    cell,
                    Color.clear
                );
            } else {
                // Célula livre:
                // mostra em verde.
                tilemap.SetColor(
                    cell,
                    availableCellColor
                );
            }
        }
    }
}