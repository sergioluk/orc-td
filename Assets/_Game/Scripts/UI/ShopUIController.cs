using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopUIController : MonoBehaviour {
    [Header("Painel")]
    [SerializeField]
    private GameObject shopPanel;

    [SerializeField]
    private Button shopButton;

    [Header("Lista")]
    [SerializeField]
    private Transform content;

    [SerializeField]
    private ShopItemCardUI itemCardPrefab;

    [Header("Itens disponíveis")]
    [SerializeField]
    private List<BuildItemDefinition> items =
        new();

    public event Action<BuildItemDefinition>
        ItemSelected;

    private bool isOpen;
    private bool shopEnabled = true;

    private void Start() {
        if (shopButton != null) {
            shopButton.onClick.AddListener(
                ToggleShop
            );
        }

        BuildShop();

        SetShopOpen(false);
    }

    private void BuildShop() {
        if (content == null ||
            itemCardPrefab == null) {
            Debug.LogError(
                "ShopUIController: referências da lista não configuradas.",
                this
            );

            return;
        }

        // Remove cards antigos.
        for (int i = content.childCount - 1;
             i >= 0;
             i--) {
            Destroy(
                content.GetChild(i).gameObject
            );
        }

        // Cria um card para cada item.
        foreach (
            BuildItemDefinition item
            in items) {
            if (item == null)
                continue;

            ShopItemCardUI card =
                Instantiate(
                    itemCardPrefab,
                    content
                );

            card.Configure(
                item,
                OnItemClicked
            );
        }
    }

    private void OnItemClicked(
        BuildItemDefinition item) {
        if (item == null)
            return;

        Debug.Log(
            $"Item selecionado: " +
            $"{item.DisplayName} | " +
            $"Preço: {item.Price}",
            this
        );

        ItemSelected?.Invoke(item);
    }

    private void ToggleShop() {
        if (!shopEnabled)
            return;

        SetShopOpen(!isOpen);
    }

    private void SetShopOpen(bool open) {
        isOpen = open;

        if (shopPanel != null) {
            shopPanel.SetActive(open);
        }
    }

    public void SetShopEnabled(bool enabled) {
        shopEnabled = enabled;

        if (shopButton != null) {
            shopButton.interactable = enabled;
        }

        // Se começou uma batalha com a loja aberta,
        // fecha imediatamente.
        if (!enabled) {
            SetShopOpen(false);
        }
    }

    private void OnDestroy() {
        if (shopButton != null) {
            shopButton.onClick.RemoveListener(
                ToggleShop
            );
        }
    }
}