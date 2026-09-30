using UnityEngine;

public sealed class PlacedObject2D : MonoBehaviour {
    private BuildItemDefinition definition;

    private PlacementGrid2D placementGrid;

    private Vector3Int anchorCell;

    private Vector2Int footprint;

    private bool configured;

    private bool placementReleased;

    public BuildItemDefinition Definition =>
        definition;

    public Vector3Int AnchorCell =>
        anchorCell;

    public Vector2Int Footprint =>
        footprint;

    public bool IsConfigured =>
        configured;

    public bool PlacementReleased =>
        placementReleased;

    public void Configure(
        BuildItemDefinition newDefinition,
        PlacementGrid2D newPlacementGrid,
        Vector3Int newAnchorCell) {
        definition = newDefinition;

        placementGrid = newPlacementGrid;

        anchorCell = newAnchorCell;

        footprint =
            definition != null
                ? definition.Footprint
                : Vector2Int.one;

        configured = true;

        placementReleased = false;
    }

    // NÃO será chamado quando uma tropa morrer.
    //
    // No futuro poderá ser usado, por exemplo:
    // - ao vender uma torre;
    // - remover definitivamente uma construção;
    // - substituir um objeto.
    public void ReleasePlacement() {
        if (!configured)
            return;

        if (placementReleased)
            return;

        if (placementGrid == null)
            return;

        placementGrid.Release(
            anchorCell,
            footprint
        );

        placementReleased = true;
    }
}