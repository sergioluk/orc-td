
using System.Collections.Generic;
using UnityEngine;

public sealed class NavigationPathTester2D : MonoBehaviour {
    [Header("Navegação")]
    [SerializeField] private Pathfinder2D pathfinder;

    [Header("Pontos do teste")]
    [SerializeField] private Transform startPoint;

    [SerializeField] private Transform destinationPoint;

    [Header("Visualização")]
    [SerializeField] private bool drawPath = true;

    private readonly List<Vector2> path = new();

    private bool pathFound;

    private void Start() {
        RecalculatePath();
    }

    [ContextMenu("Recalcular rota")]
    public void RecalculatePath() {
        path.Clear();

        if (pathfinder == null ||
            startPoint == null ||
            destinationPoint == null) {
            Debug.LogWarning(
                "NavigationPathTester2D: referências incompletas.",
                this
            );

            return;
        }

        pathFound = pathfinder.TryFindPath(
            startPoint.position,
            destinationPoint.position,
            path
        );

        if (pathFound) {
            Debug.Log(
                $"A*: caminho encontrado com {path.Count} pontos.",
                this
            );
        } else {
            Debug.LogWarning(
                "A*: não foi encontrado caminho entre os pontos.",
                this
            );
        }
    }

    private void OnDrawGizmosSelected() {
        if (!drawPath ||
            !Application.isPlaying ||
            startPoint == null ||
            destinationPoint == null) {
            return;
        }

        // Marca a origem.
        Gizmos.color = Color.cyan;

        Gizmos.DrawSphere(
            startPoint.position,
            0.10f
        );

        // Marca o destino.
        Gizmos.color = Color.yellow;

        Gizmos.DrawSphere(
            destinationPoint.position,
            0.10f
        );

        if (!pathFound)
            return;

        Vector3 previousPosition =
            startPoint.position;

        Gizmos.color = Color.magenta;

        foreach (Vector2 point in path) {
            Gizmos.DrawLine(
                previousPosition,
                point
            );

            Gizmos.DrawSphere(
                point,
                0.035f
            );

            previousPosition = point;
        }
    }
}