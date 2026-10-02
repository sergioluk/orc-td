using UnityEngine;

public sealed class SelectionVisual2D : MonoBehaviour {
    [Header("Visual")]
    [SerializeField]
    private GameObject selectionVisual;

    private void Awake() {
        SetSelected(false);
    }

    public void SetSelected(bool selected) {
        if (selectionVisual != null) {
            selectionVisual.SetActive(selected);
        }
    }
}