using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopItemCardUI : MonoBehaviour {
    [Header("Referências")]
    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text priceText;

    [SerializeField]
    private Button button;

    private BuildItemDefinition definition;

    private Action<BuildItemDefinition> clickCallback;

    public void Configure(
        BuildItemDefinition newDefinition,
        Action<BuildItemDefinition> newClickCallback) {
        definition = newDefinition;

        clickCallback = newClickCallback;

        if (definition == null)
            return;

        if (iconImage != null) {
            iconImage.sprite =
                definition.ShopIcon;

            iconImage.enabled =
                definition.ShopIcon != null;
        }

        if (nameText != null) {
            nameText.text =
                definition.DisplayName;
        }

        if (priceText != null) {
            priceText.text =
                $"{definition.Price} ouro";
        }

        if (button != null) {
            button.onClick.RemoveListener(
                OnButtonClicked
            );

            button.onClick.AddListener(
                OnButtonClicked
            );
        }
    }

    private void OnButtonClicked() {
        if (definition == null)
            return;

        clickCallback?.Invoke(definition);
    }

    private void OnDestroy() {
        if (button != null) {
            button.onClick.RemoveListener(
                OnButtonClicked
            );
        }
    }
}