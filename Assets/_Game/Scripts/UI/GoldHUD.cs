using TMPro;
using UnityEngine;

public sealed class GoldHUD : MonoBehaviour {
    [Header("Referências")]
    [SerializeField] private TMP_Text goldText;

    private GoldManager goldManager;

    private void Start() {
        goldManager = GoldManager.Instance;

        if (goldManager == null) {
            Debug.LogError(
                "GoldHUD: GoldManager não encontrado na cena.",
                this
            );

            enabled = false;
            return;
        }

        if (goldText == null) {
            Debug.LogError(
                "GoldHUD: campo Gold Text não configurado.",
                this
            );

            enabled = false;
            return;
        }

        // Escuta futuras alterações.
        goldManager.GoldChanged += OnGoldChanged;

        // Atualiza imediatamente com o valor atual.
        OnGoldChanged(goldManager.CurrentGold);
    }

    private void OnGoldChanged(int newGold) {
        goldText.text = $"Ouro: {newGold}";
    }

    private void OnDestroy() {
        if (goldManager != null) {
            goldManager.GoldChanged -= OnGoldChanged;
        }
    }
}