using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(UnitAnimator2D))]
public sealed class PooledEnemy2D : MonoBehaviour {
    private Health health;

    private UnitAnimator2D unitAnimator;

    private UnitMovement2D movement;

    private UnitAttack2D attack;

    private EnemyObjectiveController
        enemyController;

    private Collider2D gameplayCollider;

    private bool deathInProgress;

    public Health Health =>
        health;

    // Só será disparado DEPOIS
    // da animação de morte terminar.
    public event Action<PooledEnemy2D> Died;

    private void Awake() {
        health =
            GetComponent<Health>();

        unitAnimator =
            GetComponent<UnitAnimator2D>();

        movement =
            GetComponent<UnitMovement2D>();

        attack =
            GetComponent<UnitAttack2D>();

        enemyController =
            GetComponent<
                EnemyObjectiveController>();

        gameplayCollider =
            GetComponent<Collider2D>();
    }

    private void OnEnable() {
        deathInProgress = false;

        if (health != null) {
            health.Died +=
                HandleDeath;
        }

        if (unitAnimator != null) {
            unitAnimator
                .DeathAnimationCompleted +=
                HandleDeathAnimationCompleted;
        }

        RestoreForSpawn();
    }

    private void RestoreForSpawn() {
        // Reativa os componentes que foram
        // desligados na morte anterior.

        if (gameplayCollider != null) {
            gameplayCollider.enabled =
                true;
        }

        if (movement != null) {
            movement.enabled =
                true;

            movement.ClearTarget();
        }

        if (attack != null) {
            attack.enabled =
                true;

            attack.ClearTarget();
        }

        if (enemyController != null) {
            enemyController.enabled =
                true;
        }

        if (unitAnimator != null) {
            unitAnimator.ResetForSpawn();
        }
    }

    private void HandleDeath() {
        if (deathInProgress)
            return;

        deathInProgress = true;

        // Para imediatamente qualquer
        // comportamento de combate.

        if (attack != null) {
            attack.ClearTarget();
        }

        if (movement != null) {
            movement.ClearTarget();
        }

        // Desligar o controller também
        // libera possíveis Combat Slots.
        if (enemyController != null) {
            enemyController.enabled =
                false;
        }

        if (attack != null) {
            attack.enabled =
                false;
        }

        if (movement != null) {
            movement.enabled =
                false;
        }

        // O cadáver não deve continuar
        // bloqueando outras unidades.
        if (gameplayCollider != null) {
            gameplayCollider.enabled =
                false;
        }

        // NÃO fazemos:
        //
        // Died?.Invoke(this);
        //
        // aqui.
        //
        // Primeiro esperamos a animação.
    }

    private void
        HandleDeathAnimationCompleted() {
        if (!deathInProgress)
            return;

        if (health == null ||
            !health.IsDead) {
            return;
        }

        deathInProgress = false;

        // Só agora avisamos o Spawner.
        Died?.Invoke(this);
    }

    private void OnDisable() {
        if (health != null) {
            health.Died -=
                HandleDeath;
        }

        if (unitAnimator != null) {
            unitAnimator
                .DeathAnimationCompleted -=
                HandleDeathAnimationCompleted;
        }
    }
}