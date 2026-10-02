using UnityEngine;

public sealed class TargetHighlight2D : MonoBehaviour {
    [Header("Referências")]
    [SerializeField]
    private SpriteRenderer sourceRenderer;

    [SerializeField]
    private SpriteRenderer highlightRenderer;

    private bool highlighted;

    public bool IsHighlighted =>
        highlighted;

    private void Awake() {
        if (sourceRenderer == null) {
            sourceRenderer =
                GetComponent<SpriteRenderer>();
        }

        if (highlightRenderer != null) {
            highlightRenderer.gameObject
                .SetActive(false);
        }
    }

    private void LateUpdate() {
        if (!highlighted)
            return;

        if (sourceRenderer == null ||
            highlightRenderer == null) {
            return;
        }

        // Copia o frame atual da animação.
        highlightRenderer.sprite =
            sourceRenderer.sprite;

        // Também acompanha flips,
        // caso sejam utilizados futuramente.
        highlightRenderer.flipX =
            sourceRenderer.flipX;

        highlightRenderer.flipY =
            sourceRenderer.flipY;
    }

    public void SetHighlighted(
        bool value) {
        highlighted = value;

        if (highlightRenderer == null)
            return;

        highlightRenderer.gameObject
            .SetActive(value);

        if (value &&
            sourceRenderer != null) {
            highlightRenderer.sprite =
                sourceRenderer.sprite;

            highlightRenderer.flipX =
                sourceRenderer.flipX;

            highlightRenderer.flipY =
                sourceRenderer.flipY;
        }
    }
}