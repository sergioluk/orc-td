
using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class PooledEnemy2D : MonoBehaviour {
    private Health health;

    public Health Health => health;

    // Informa qual instância morreu.
    public event Action<PooledEnemy2D> Died;

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
        Died?.Invoke(this);
    }
}