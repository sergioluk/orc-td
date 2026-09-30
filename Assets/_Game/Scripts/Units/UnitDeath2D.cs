using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class UnitDeath2D : MonoBehaviour {
    private Health health;

    private void Awake() {
        health = GetComponent<Health>();
    }

    private void OnEnable() {
        health.Died += HandleDeath;
    }

    private void OnDisable() {
        health.Died -= HandleDeath;
    }

    private void HandleDeath() {
        gameObject.SetActive(false);
    }
}