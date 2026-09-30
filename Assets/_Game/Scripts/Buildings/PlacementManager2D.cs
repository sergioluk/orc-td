using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class PlacementManager2D : MonoBehaviour {
    [Header("Referências")]
    [SerializeField]
    private ShopUIController shopUI;

    [SerializeField]
    private PlacementGrid2D placementGrid;

    [SerializeField]
    private Camera worldCamera;

    [SerializeField]
    private Pathfinder2D pathfinder;

    [SerializeField]
    private Transform placedObjectsParent;

    [Header("Preview")]
    [SerializeField]
    private string previewSortingLayer = "Default";

    [SerializeField]
    private int previewOrderInLayer = 1000;

    [SerializeField]
    private Color validColor =
        new Color(0.35f,1f,0.35f,0.65f);

    [SerializeField]
    private Color invalidColor =
        new Color(1f,0.25f,0.25f,0.65f);

    [Header("Comportamento")]
    [SerializeField]
    private bool keepPlacingAfterBuild = false;

    private BuildItemDefinition selectedItem;

    private GameObject previewObject;

    private SpriteRenderer previewRenderer;

    private Vector3Int currentCell;

    private bool currentPlacementValid;
    private bool placementEnabled = true;

    private void Start() {
        if (shopUI == null) {
            Debug.LogError(
                "PlacementManager2D: ShopUI não configurado.",
                this
            );

            enabled = false;
            return;
        }

        if (placementGrid == null) {
            Debug.LogError(
                "PlacementManager2D: PlacementGrid não configurado.",
                this
            );

            enabled = false;
            return;
        }

        if (worldCamera == null) {
            worldCamera = Camera.main;
        }

        shopUI.ItemSelected += BeginPlacement;
    }

    private void Update() {
        if (selectedItem == null)
            return;

        if (Mouse.current == null)
            return;

        UpdatePreviewPosition();

        // ESC cancela.
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame) {
            CancelPlacement();
            return;
        }

        // Botão direito também cancela.
        if (Mouse.current.rightButton.wasPressedThisFrame) {
            CancelPlacement();
            return;
        }

        // Clique esquerdo tenta construir.
        if (Mouse.current.leftButton.wasPressedThisFrame) {
            // Não constrói ao clicar na interface.
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject()) {
                return;
            }

            TryPlaceCurrentItem();
        }
    }

    private void BeginPlacement(
    BuildItemDefinition item) {
        if (!placementEnabled)
            return;

        if (item == null)
            return;

        selectedItem = item;

        DestroyPreview();

        CreatePreview();

        placementGrid.ShowAllowedMarkers();

        Debug.Log(
            $"Modo de construção iniciado: " +
            $"{selectedItem.DisplayName}",
            this
        );
    }

    private void CreatePreview() {
        if (selectedItem == null)
            return;

        if (selectedItem.PlacementSprite == null) {
            Debug.LogError(
                $"{selectedItem.DisplayName}: " +
                "Placement Sprite não configurado.",
                selectedItem
            );

            selectedItem = null;
            return;
        }

        previewObject =
            new GameObject(
                $"Preview_{selectedItem.DisplayName}"
            );

        previewRenderer =
            previewObject.AddComponent<SpriteRenderer>();

        previewRenderer.sprite =
            selectedItem.PlacementSprite;

        previewRenderer.sortingLayerName =
            previewSortingLayer;

        previewRenderer.sortingOrder =
            previewOrderInLayer;

        if (selectedItem.Prefab != null) {
            previewObject.transform.localScale =
                selectedItem.Prefab.transform.localScale;
        }
    }

    private void UpdatePreviewPosition() {
        if (previewObject == null ||
            selectedItem == null ||
            worldCamera == null) {
            return;
        }

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

        // Descobre a célula sob o mouse.
        currentCell =
            placementGrid.WorldToCell(
                worldPosition
            );

        // Faz a sprite encaixar no grid.
        Vector3 placementPosition =
            placementGrid.GetPlacementPosition(
                currentCell,
                selectedItem.Footprint
            );

        previewObject.transform.position =
            placementPosition;

        // Verifica grid + ocupação.
        bool gridAllowsPlacement =
            placementGrid.CanPlace(
                currentCell,
                selectedItem.Footprint
            );

        // Verifica dinheiro.
        bool canAfford =
            GoldManager.Instance != null &&
            GoldManager.Instance.CanAfford(
                selectedItem.Price
            );

        currentPlacementValid =
            gridAllowsPlacement &&
            canAfford;

        previewRenderer.color =
            currentPlacementValid
                ? validColor
                : invalidColor;
    }

    private void TryPlaceCurrentItem() {
        if (selectedItem == null)
            return;

        if (!currentPlacementValid) {
            Debug.Log(
                "Não é possível construir nesta posição.",
                this
            );

            return;
        }

        if (selectedItem.Prefab == null) {
            Debug.LogError(
                $"{selectedItem.DisplayName}: " +
                "Prefab não configurado.",
                selectedItem
            );

            return;
        }

        if (GoldManager.Instance == null) {
            Debug.LogError(
                "PlacementManager2D: GoldManager não encontrado.",
                this
            );

            return;
        }

        // Primeiro confirma o pagamento.
        bool paid =
            GoldManager.Instance.TrySpendGold(
                selectedItem.Price
            );

        if (!paid) {
            UpdatePreviewPosition();
            return;
        }

        Vector3 placementPosition =
            placementGrid.GetPlacementPosition(
                currentCell,
                selectedItem.Footprint
            );

        // Cria a unidade ou torre.
        GameObject placedObject =
        Instantiate(
            selectedItem.Prefab,
            placementPosition,
            Quaternion.identity,
            placedObjectsParent
        );

        placedObject.name =
            selectedItem.DisplayName;

        // Reserva as células do grid.
        placementGrid.Occupy(
            currentCell,
            selectedItem.Footprint
        );

        // Registra de onde esta construção veio
        // e quais células ela ocupa.
        PlacedObject2D placedObjectData =
            placedObject.GetComponent<PlacedObject2D>();

        if (placedObjectData == null) {
            placedObjectData =
                placedObject.AddComponent<PlacedObject2D>();
        }

        placedObjectData.Configure(
            selectedItem,
            placementGrid,
            currentCell
        );

        // Configura comportamentos específicos
        // da unidade ou torre criada.
        ConfigurePlacedObject(
            placedObject,
            placementPosition
        );

        Debug.Log(
            $"{selectedItem.DisplayName} construído. " +
            $"Custo: {selectedItem.Price}",
            placedObject
        );

        if (keepPlacingAfterBuild) {
            // Continua com o mesmo item selecionado.
            UpdatePreviewPosition();
        } else {
            CancelPlacement();
        }
    }

    private void ConfigurePlacedObject(
        GameObject placedObject,
        Vector3 placementPosition) {
        // -------------------------------------------------
        // CONFIGURA NAVEGAÇÃO DE UNIDADES
        // -------------------------------------------------

        UnitMovement2D movement =
            placedObject.GetComponent<UnitMovement2D>();

        if (movement != null &&
            pathfinder != null) {
            movement.ConfigureNavigation(
                pathfinder
            );
        }

        // -------------------------------------------------
        // CONFIGURA PONTO DE DEFESA
        // -------------------------------------------------

        DefenderController2D defender =
            placedObject.GetComponent<
                DefenderController2D>();

        if (defender != null) {
            GameObject guardPointObject =
                new GameObject(
                    $"{placedObject.name}_GuardPoint"
                );

            guardPointObject.transform.position =
                placementPosition;

            if (placedObjectsParent != null) {
                guardPointObject.transform.SetParent(
                    placedObjectsParent
                );
            }

            defender.ConfigureGuardPoint(
                guardPointObject.transform
            );
        }
    }

    private void CancelPlacement() {
        selectedItem = null;

        currentPlacementValid = false;

        DestroyPreview();

        // Saiu do modo de construção.
        placementGrid.HideAllowedMarkers();
    }

    private void DestroyPreview() {
        if (previewObject != null) {
            Destroy(previewObject);
        }

        previewObject = null;
        previewRenderer = null;
    }

    public void SetPlacementEnabled(bool enabled) {
        placementEnabled = enabled;

        // Se a fase de construção terminou enquanto
        // o jogador estava segurando uma unidade,
        // cancela imediatamente.
        if (!enabled && selectedItem != null) {
            CancelPlacement();
        }
    }

    private void OnDestroy() {
        if (shopUI != null) {
            shopUI.ItemSelected -= BeginPlacement;
        }
    }
}