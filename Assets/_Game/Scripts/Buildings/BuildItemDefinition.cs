using UnityEngine;

public enum BuildItemCategory {
    Unit,
    Tower
}

[CreateAssetMenu(
    fileName = "BuildItem_",
    menuName = "Game/Building/Build Item"
)]
public sealed class BuildItemDefinition : ScriptableObject {
    [Header("Informações")]
    [SerializeField]
    private string displayName;

    [SerializeField]
    private BuildItemCategory category;

    [Header("Loja")]
    [SerializeField]
    private Sprite shopIcon;

    [SerializeField, Min(0)]
    private int price = 50;

    [Header("Construção")]
    [SerializeField]
    private GameObject prefab;

    [SerializeField]
    private Sprite placementSprite;

    [SerializeField]
    private Vector2Int footprint =
        Vector2Int.one;

    public string DisplayName => displayName;

    public BuildItemCategory Category =>
        category;

    public Sprite ShopIcon => shopIcon;

    public int Price => price;

    public GameObject Prefab => prefab;

    public Sprite PlacementSprite =>
        placementSprite;

    public Vector2Int Footprint =>
        footprint;

    private void OnValidate() {
        footprint = new Vector2Int(
            Mathf.Max(1,footprint.x),
            Mathf.Max(1,footprint.y)
        );
    }
}