using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class CastleController : MonoBehaviour {
    private Health health;

    private void Awake() {
        health = GetComponent<Health>();
    }

    private void OnEnable() {
        health.Died += HandleCastleDestroyed;
    }

    private void OnDisable() {
        health.Died -= HandleCastleDestroyed;
    }

    private void HandleCastleDestroyed() {
        Debug.Log(
            "CASTELO DESTRUÍDO! Condição de derrota detectada.",
            this
        );
    }
}