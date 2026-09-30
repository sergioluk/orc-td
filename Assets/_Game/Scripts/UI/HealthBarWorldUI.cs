
using UnityEngine;
using UnityEngine.UI;

public sealed class HealthBarWorldUI : MonoBehaviour {
    [Header("Referências")]
    [SerializeField] private Health targetHealth;

    [SerializeField] private Canvas worldCanvas;

    [SerializeField] private Image fillImage;

    [Header("Comportamento")]
    [SerializeField] private bool hideWhenDead = true;

    private void Awake() {
        if (targetHealth == null)
            targetHealth = GetComponentInParent<Health>();

        if (worldCanvas == null)
            worldCanvas = GetComponent<Canvas>();
    }

    private void OnEnable() {
        if (targetHealth == null || fillImage == null) {
            Debug.LogError(
                $"HealthBarWorldUI não está configurado corretamente em {name}.",
                this
            );

            enabled = false;
            return;
        }

        targetHealth.HealthChanged += HandleHealthChanged;
        targetHealth.Died += HandleDied;

        RefreshNow();
    }

    private void Start() {
        // Segunda atualização após a inicialização
        // dos componentes da Scene.
        RefreshNow();
    }

    private void OnDisable() {
        if (targetHealth != null) {
            targetHealth.HealthChanged -= HandleHealthChanged;
            targetHealth.Died -= HandleDied;
        }
    }

    private void HandleHealthChanged(int currentHealth,int maxHealth) {
        UpdateVisual(currentHealth,maxHealth);
    }

    private void HandleDied() {
        UpdateVisual(0,targetHealth.MaxHealth);
    }

    private void RefreshNow() {
        if (targetHealth == null)
            return;

        UpdateVisual(
            targetHealth.CurrentHealth,
            targetHealth.MaxHealth
        );
    }

    private void UpdateVisual(int currentHealth,int maxHealth) {
        // Health ainda não foi inicializado.
        // Não devemos esconder a barra nesse momento.
        if (maxHealth <= 0)
            return;

        // Atualiza o preenchimento.
        if (fillImage != null) {
            float normalized =
                (float)currentHealth / maxHealth;

            fillImage.fillAmount =
                Mathf.Clamp01(normalized);
        }

        // Atualiza também a visibilidade do Canvas.
        if (worldCanvas != null) {
            worldCanvas.enabled =
                !hideWhenDead || currentHealth > 0;
        }
    }
}