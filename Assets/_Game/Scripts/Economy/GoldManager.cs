using System;
using UnityEngine;

public sealed class GoldManager : MonoBehaviour {
    public static GoldManager Instance { get; private set; }

    [Header("Configuração")]
    [SerializeField, Min(0)]
    private int startingGold = 100;

    private int currentGold;

    public int CurrentGold => currentGold;

    public event Action<int> GoldChanged;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        currentGold = startingGold;
    }

    private void Start() {
        NotifyGoldChanged();
    }

    public void AddGold(int amount) {
        if (amount <= 0)
            return;

        currentGold += amount;

        NotifyGoldChanged();
    }

    public bool CanAfford(int amount) {
        if (amount < 0)
            return false;

        return currentGold >= amount;
    }

    public bool TrySpendGold(int amount) {
        if (amount <= 0)
            return false;

        if (!CanAfford(amount))
            return false;

        currentGold -= amount;

        NotifyGoldChanged();

        return true;
    }

    public void ResetGold() {
        currentGold = startingGold;

        NotifyGoldChanged();
    }

    private void NotifyGoldChanged() {
        GoldChanged?.Invoke(currentGold);
    }

    private void OnDestroy() {
        if (Instance == this)
            Instance = null;
    }

#if UNITY_EDITOR

    [ContextMenu("DEBUG/Add 10 Gold")]
    private void DebugAddGold() {
        AddGold(10);

        Debug.Log(
            $"Ouro atual: {currentGold}",
            this
        );
    }

    [ContextMenu("DEBUG/Spend 25 Gold")]
    private void DebugSpendGold() {
        bool success = TrySpendGold(25);

        Debug.Log(
            success
                ? $"Gastou 25. Ouro atual: {currentGold}"
                : $"Ouro insuficiente. Ouro atual: {currentGold}",
            this
        );
    }

#endif
}