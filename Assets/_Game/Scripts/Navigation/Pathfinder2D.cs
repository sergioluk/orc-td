
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NavigationGrid2D))]
public sealed class Pathfinder2D : MonoBehaviour {
    [Header("Referências")]
    [SerializeField] private NavigationGrid2D grid;

    [Header("Configuração")]
    [SerializeField, Min(100)]
    private int maxExpandedNodes = 10000;

    private const int StraightCost = 10;
    private const int DiagonalCost = 14;

    // Oito direções de navegação.
    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int( 0,  1),
        new Vector2Int( 1,  0),
        new Vector2Int( 0, -1),
        new Vector2Int(-1,  0),

        new Vector2Int( 1,  1),
        new Vector2Int( 1, -1),
        new Vector2Int(-1, -1),
        new Vector2Int(-1,  1)
    };

    // Buffers reutilizáveis do A*.
    private int[] gCosts = Array.Empty<int>();

    private int[] parents = Array.Empty<int>();

    private bool[] closed = Array.Empty<bool>();

    // Binary Heap: guarda os índices das células.
    private int[] heap = Array.Empty<int>();

    // Informa a posição de cada célula dentro do Heap.
    // -1 significa que a célula não está na fila.
    private int[] heapPositions = Array.Empty<int>();

    private int heapCount;

    // Lista reutilizável para suavização.
    private readonly List<Vector2> smoothedPath = new(64);

    private void Awake() {
        if (grid == null)
            grid = GetComponent<NavigationGrid2D>();
    }

    // Versão utilizada pelo teste visual.
    public bool TryFindPath(
        Vector2 startWorld,
        Vector2 destinationWorld,
        List<Vector2> result) {
        if (grid == null) {
            result?.Clear();
            return false;
        }

        return TryFindPath(
            startWorld,
            destinationWorld,
            grid.DefaultAgentRadius,
            result
        );
    }

    // Versão utilizada pelas unidades.
    // Recebe o raio individual do Collider.
    public bool TryFindPath(
        Vector2 startWorld,
        Vector2 destinationWorld,
        float agentRadius,
        List<Vector2> result) {
        if (result == null)
            return false;

        result.Clear();

        if (grid == null)
            return false;

        int width = grid.Width;
        int height = grid.Height;

        if (width <= 0 || height <= 0)
            return false;

        Vector2Int startCell =
            grid.WorldToGrid(startWorld);

        Vector2Int destinationCell =
            grid.WorldToGrid(destinationWorld);

        // Verifica os limites do mapa.
        if (!grid.IsInsideGrid(startCell) ||
            !grid.IsInsideGrid(destinationCell)) {
            return false;
        }

        // Primeiro verifica se existe passagem direta.
        if (grid.HasClearPath(
                startWorld,
                destinationWorld,
                agentRadius)) {
            result.Add(destinationWorld);
            return true;
        }

        // Verifica as células inicial e final.
        bool startWalkable =
            grid.IsWalkable(startCell,agentRadius);

        bool destinationWalkable =
            grid.IsWalkable(destinationCell,agentRadius);

        if (!startWalkable || !destinationWalkable) {
            Debug.LogWarning(
                $"A*: rota rejeitada. " +
                $"Origem livre: {startWalkable} | " +
                $"Destino livre: {destinationWalkable} | " +
                $"Raio: {agentRadius:F3} | " +
                $"Origem: {startWorld} | " +
                $"Destino: {destinationWorld}",
                this
            );

            return false;
        }

        int totalNodes = width * height;

        // Prepara os buffers reutilizáveis.
        EnsureBuffers(totalNodes);

        // Esvazia o Heap da busca anterior.
        heapCount = 0;

        for (int i = 0; i < totalNodes; i++) {
            gCosts[i] = int.MaxValue;

            parents[i] = -1;

            closed[i] = false;

            heapPositions[i] = -1;
        }

        int startIndex =
            ToIndex(startCell,width);

        int destinationIndex =
            ToIndex(destinationCell,width);

        gCosts[startIndex] = 0;

        // Coloca a origem no Heap.
        HeapPush(
            startIndex,
            destinationCell,
            width
        );

        int expandedNodes = 0;

        // Busca A*.
        while (heapCount > 0) {
            // Retira a célula com menor custo F.
            int currentIndex =
                HeapPopMin(destinationCell,width);

            if (currentIndex == destinationIndex) {
                BuildPath(
                    startIndex,
                    destinationIndex,
                    parents,
                    width,
                    destinationWorld,
                    result
                );

                return SmoothPath(
                    startWorld,
                    result,
                    agentRadius
                );
            }

            closed[currentIndex] = true;

            expandedNodes++;

            if (expandedNodes >= maxExpandedNodes)
                return false;

            Vector2Int currentCell =
                ToCell(currentIndex,width);

            // Examina os oito vizinhos.
            for (int i = 0; i < Directions.Length; i++) {
                Vector2Int direction = Directions[i];

                Vector2Int neighborCell =
                    currentCell + direction;

                // Respeita o raio individual.
                if (!grid.IsWalkable(
                        neighborCell,
                        agentRadius)) {
                    continue;
                }

                // Impede atravessar cantos diagonalmente.
                if (direction.x != 0 &&
                    direction.y != 0) {
                    Vector2Int sideA =
                        currentCell +
                        new Vector2Int(direction.x,0);

                    Vector2Int sideB =
                        currentCell +
                        new Vector2Int(0,direction.y);

                    if (!grid.IsWalkable(
                            sideA,
                            agentRadius) ||
                        !grid.IsWalkable(
                            sideB,
                            agentRadius)) {
                        continue;
                    }
                }

                int neighborIndex =
                    ToIndex(neighborCell,width);

                if (closed[neighborIndex])
                    continue;

                bool isDiagonal =
                    direction.x != 0 &&
                    direction.y != 0;

                int movementCost = isDiagonal
                    ? DiagonalCost
                    : StraightCost;

                int tentativeG =
                    gCosts[currentIndex] + movementCost;

                // O caminho anterior é melhor?
                if (tentativeG >= gCosts[neighborIndex])
                    continue;

                // Encontramos um caminho melhor.
                parents[neighborIndex] = currentIndex;

                gCosts[neighborIndex] = tentativeG;

                // A célula ainda não está no Heap?
                if (heapPositions[neighborIndex] < 0) {
                    HeapPush(
                        neighborIndex,
                        destinationCell,
                        width
                    );
                } else {
                    // A célula já está no Heap,
                    // mas seu custo diminuiu.
                    // Precisamos atualizar sua posição.
                    HeapDecreaseKey(
                        neighborIndex,
                        destinationCell,
                        width
                    );
                }
            }
        }

        // Não existe caminho.
        return false;
    }

    // =====================================================
    // BUFFERS REUTILIZÁVEIS
    // =====================================================

    private void EnsureBuffers(int requiredCount) {
        if (gCosts.Length >= requiredCount)
            return;

        gCosts = new int[requiredCount];

        parents = new int[requiredCount];

        closed = new bool[requiredCount];

        heap = new int[requiredCount];

        heapPositions = new int[requiredCount];

        Debug.Log(
            $"Pathfinder2D: buffers preparados para " +
            $"{requiredCount} células.",
            this
        );
    }

    // =====================================================
    // BINARY HEAP
    // =====================================================

    private void HeapPush(
        int node,
        Vector2Int destination,
        int width) {
        int position = heapCount;

        heap[position] = node;

        heapPositions[node] = position;

        heapCount++;

        HeapSiftUp(position,destination,width);
    }

    private int HeapPopMin(
        Vector2Int destination,
        int width) {
        // A primeira posição contém o menor custo.
        int minimum = heap[0];

        heapCount--;

        heapPositions[minimum] = -1;

        if (heapCount > 0) {
            int lastNode = heap[heapCount];

            heap[0] = lastNode;

            heapPositions[lastNode] = 0;

            HeapSiftDown(0,destination,width);
        }

        return minimum;
    }

    private void HeapDecreaseKey(
        int node,
        Vector2Int destination,
        int width) {
        int position = heapPositions[node];

        if (position < 0)
            return;

        // O custo diminuiu: a célula pode precisar
        // subir na fila de prioridade.
        HeapSiftUp(position,destination,width);
    }

    private void HeapSiftUp(
        int position,
        Vector2Int destination,
        int width) {
        while (position > 0) {
            int parentPosition = (position - 1) / 2;

            int currentNode = heap[position];

            int parentNode = heap[parentPosition];

            if (CompareNodes(
                    currentNode,
                    parentNode,
                    destination,
                    width) >= 0) {
                break;
            }

            HeapSwap(position,parentPosition);

            position = parentPosition;
        }
    }

    private void HeapSiftDown(
        int position,
        Vector2Int destination,
        int width) {
        while (true) {
            int left = position * 2 + 1;

            int right = left + 1;

            int best = position;

            if (left < heapCount &&
                CompareNodes(
                    heap[left],
                    heap[best],
                    destination,
                    width) < 0) {
                best = left;
            }

            if (right < heapCount &&
                CompareNodes(
                    heap[right],
                    heap[best],
                    destination,
                    width) < 0) {
                best = right;
            }

            if (best == position)
                break;

            HeapSwap(position,best);

            position = best;
        }
    }

    private void HeapSwap(int a,int b) {
        int nodeA = heap[a];

        int nodeB = heap[b];

        heap[a] = nodeB;

        heap[b] = nodeA;

        heapPositions[nodeA] = b;

        heapPositions[nodeB] = a;
    }

    // Compara duas células.
    // Prioridade: menor F, depois menor H.
    private int CompareNodes(
        int a,
        int b,
        Vector2Int destination,
        int width) {
        int hA = GetHeuristic(
            ToCell(a,width),
            destination
        );

        int hB = GetHeuristic(
            ToCell(b,width),
            destination
        );

        int fA = gCosts[a] + hA;

        int fB = gCosts[b] + hB;

        if (fA < fB)
            return -1;

        if (fA > fB)
            return 1;

        if (hA < hB)
            return -1;

        if (hA > hB)
            return 1;

        // Desempate determinístico.
        return a.CompareTo(b);
    }

    // =====================================================
    // CUSTOS E COORDENADAS
    // =====================================================

    private static int GetHeuristic(
        Vector2Int from,
        Vector2Int to) {
        int dx = Mathf.Abs(from.x - to.x);

        int dy = Mathf.Abs(from.y - to.y);

        int diagonalSteps = Mathf.Min(dx,dy);

        int straightSteps =
            Mathf.Max(dx,dy) - diagonalSteps;

        return diagonalSteps * DiagonalCost +
               straightSteps * StraightCost;
    }

    private static int ToIndex(
        Vector2Int cell,
        int width) {
        return cell.y * width + cell.x;
    }

    private static Vector2Int ToCell(
        int index,
        int width) {
        return new Vector2Int(
            index % width,
            index / width
        );
    }

    // =====================================================
    // RECONSTRUÇÃO DO CAMINHO
    // =====================================================

    private void BuildPath(
        int startIndex,
        int destinationIndex,
        int[] parents,
        int width,
        Vector2 destinationWorld,
        List<Vector2> result) {
        int current = destinationIndex;

        while (current != startIndex) {
            Vector2Int cell =
                ToCell(current,width);

            result.Add(
                grid.GridToWorld(cell)
            );

            current = parents[current];

            if (current < 0) {
                result.Clear();
                return;
            }
        }

        result.Reverse();

        // Inclui a posição exata do destino.
        if (result.Count == 0 ||
            Vector2.Distance(
                result[result.Count - 1],
                destinationWorld
            ) > 0.001f) {
            result.Add(destinationWorld);
        }
    }

    // =====================================================
    // SUAVIZAÇÃO DO CAMINHO
    // =====================================================

    private bool SmoothPath(
        Vector2 startWorld,
        List<Vector2> originalPath,
        float agentRadius) {
        if (originalPath.Count == 0)
            return false;

        smoothedPath.Clear();

        Vector2 currentPosition = startWorld;

        int nextIndex = 0;

        while (nextIndex < originalPath.Count) {
            int bestIndex = -1;

            // Procura o ponto mais distante
            // que podemos alcançar diretamente.
            for (int i = originalPath.Count - 1;
                 i >= nextIndex;
                 i--) {
                if (grid.HasClearPath(
                        currentPosition,
                        originalPath[i],
                        agentRadius)) {
                    bestIndex = i;
                    break;
                }
            }

            if (bestIndex < 0) {
                originalPath.Clear();
                return false;
            }

            Vector2 nextPoint = originalPath[bestIndex];

            smoothedPath.Add(nextPoint);

            currentPosition = nextPoint;

            nextIndex = bestIndex + 1;
        }

        originalPath.Clear();

        originalPath.AddRange(smoothedPath);

        return true;
    }
}