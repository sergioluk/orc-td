using UnityEngine;

[CreateAssetMenu(
    fileName = "Building_",
    menuName = "Game/Data/Building Definition"
)]
public sealed class BuildingDefinition : ScriptableObject {
    [Header("Identidade")]
    [SerializeField]
    private string displayName = "Nova Construção";

    [Header("Resistência")]
    [SerializeField, Min(1)]
    private int maxHealth = 500;

    public string DisplayName => displayName;
    public int MaxHealth => maxHealth;
}