using System;
using UnityEngine;

public sealed class Health : MonoBehaviour
{
    [Header("Estado atual")]
    [SerializeField]
    private int maxHealth;

    [SerializeField]
    private int currentHealth;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;

    public event Action<int,int> HealthChanged;
    public event Action Died;
    public event Action Revived;

    public void Initialize(int newMaxHealth)
    {
        maxHealth =
            Mathf.Max(
                1,
                newMaxHealth
            );

        currentHealth =
            maxHealth;

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );
    }

    public void TakeDamage(int amount)
    {
        if (IsDead ||
            amount <= 0)
        {
            return;
        }

        currentHealth =
            Mathf.Max(
                0,
                currentHealth - amount
            );

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        if (currentHealth == 0)
        {
            Died?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead ||
            amount <= 0)
        {
            return;
        }

        currentHealth =
            Mathf.Min(
                maxHealth,
                currentHealth + amount
            );

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );
    }

    public bool ReviveFull() {
        if (!IsDead)
            return false;

        currentHealth =
            maxHealth;

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        Revived?.Invoke();

        return true;
    }
}