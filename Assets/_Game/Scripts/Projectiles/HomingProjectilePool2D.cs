using System.Collections.Generic;
using UnityEngine;

public sealed class HomingProjectilePool2D : MonoBehaviour {
    public static HomingProjectilePool2D Instance {
        get;
        private set;
    }

    [Header("Prefab")]
    [SerializeField]
    private HomingProjectile2D projectilePrefab;

    [Header("Pool")]
    [SerializeField, Min(1)]
    private int initialSize = 20;

    private readonly Queue<HomingProjectile2D> available =
        new();

    private void Awake() {
        if (Instance != null &&
            Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (projectilePrefab == null) {
            Debug.LogError(
                "HomingProjectilePool2D: Projectile Prefab não configurado.",
                this
            );

            enabled = false;
            return;
        }

        for (int i = 0; i < initialSize; i++) {
            HomingProjectile2D projectile =
                CreateProjectile();

            available.Enqueue(projectile);
        }
    }

    private HomingProjectile2D CreateProjectile() {
        HomingProjectile2D projectile =
            Instantiate(
                projectilePrefab,
                transform
            );

        projectile.gameObject.SetActive(false);

        projectile.ConfigurePool(this);

        return projectile;
    }

    public HomingProjectile2D Spawn(
        Vector3 position,
        Quaternion rotation,
        Health targetHealth,
        Transform targetPoint,
        int damage) {
        HomingProjectile2D projectile;

        if (available.Count > 0) {
            projectile =
                available.Dequeue();
        } else {
            // Se todas estiverem em uso,
            // o pool cresce automaticamente.
            projectile =
                CreateProjectile();
        }

        projectile.transform.SetPositionAndRotation(
            position,
            rotation
        );

        projectile.Initialize(
            targetHealth,
            targetPoint,
            damage
        );

        projectile.gameObject.SetActive(true);

        return projectile;
    }

    public void Release(
        HomingProjectile2D projectile) {
        if (projectile == null)
            return;

        projectile.ResetProjectile();

        projectile.gameObject.SetActive(false);

        available.Enqueue(projectile);
    }
    private void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }
    }
}