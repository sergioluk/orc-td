using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class SelectionManager2D : MonoBehaviour {
    [Header("Referências")]
    [SerializeField]
    private Camera worldCamera;

    [SerializeField]
    private PlacementManager2D placementManager;

    [Header("Detecção")]
    [SerializeField]
    private LayerMask selectableLayer;

    private PlacedObject2D selectedObject;

    public PlacedObject2D SelectedObject =>
        selectedObject;

    public event Action<PlacedObject2D>
        SelectionChanged;

    private void Update() {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton
            .wasPressedThisFrame) {
            return;
        }

        // Não seleciona enquanto estamos
        // posicionando uma compra.
        if (placementManager != null &&
            placementManager.IsPlacing) {
            return;
        }

        // Clique em UI não seleciona objetos
        // do mundo.
        if (EventSystem.current != null &&
            EventSystem.current
                .IsPointerOverGameObject()) {
            return;
        }

        TrySelectUnderMouse();
    }

    private void TrySelectUnderMouse() {
        if (worldCamera == null)
            return;

        Vector2 screenPosition =
            Mouse.current.position.ReadValue();

        Vector3 worldPosition =
            worldCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    -worldCamera.transform.position.z
                )
            );

        worldPosition.z = 0f;

        Collider2D[] hits =
            Physics2D.OverlapPointAll(
                worldPosition,
                selectableLayer
            );

        for (int i = 0;
             i < hits.Length;
             i++) {
            if (hits[i] == null)
                continue;

            PlacedObject2D placedObject =
                hits[i].GetComponentInParent<
                    PlacedObject2D>();

            if (placedObject == null)
                continue;

            Select(placedObject);

            return;
        }

        ClearSelection();
    }

    private void Select(
    PlacedObject2D newSelection) {
        if (selectedObject == newSelection)
            return;

        // Desliga o marcador do objeto anterior.
        SetSelectionVisual(
            selectedObject,
            false
        );

        selectedObject =
            newSelection;

        // Liga o marcador do novo objeto.
        SetSelectionVisual(
            selectedObject,
            true
        );

        Debug.Log(
            $"Selecionado: {selectedObject.name}",
            selectedObject
        );

        SelectionChanged?.Invoke(
            selectedObject
        );
    }

    public void ClearSelection() {
        if (selectedObject == null)
            return;

        SetSelectionVisual(
            selectedObject,
            false
        );

        selectedObject = null;

        Debug.Log(
            "Seleção removida.",
            this
        );

        SelectionChanged?.Invoke(null);
    }

    private void SetSelectionVisual(
    PlacedObject2D placedObject,
    bool selected) {
        if (placedObject == null)
            return;

        SelectionVisual2D visual =
            placedObject.GetComponent<
                SelectionVisual2D>();

        if (visual == null)
            return;

        visual.SetSelected(selected);
    }
}