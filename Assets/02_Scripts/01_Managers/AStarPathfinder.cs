using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타일 기반 A* 경로 탐색 전담 매니저
/// - TileManager의 IsWalkable / GetWalkableNeighbors 를 사용
/// - 던전 → 기지까지의 최단 경로를 계산할 때 사용
/// </summary>
public class AStarPathfinder : MonoBehaviour
{
    // 간단한 싱글톤 (던전/몬스터에서 쉽게 접근하기 위함)
    public static AStarPathfinder Instance { get; private set; }

    private TileManager tileManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // TileManager 참조 찾기
        if (tileManager == null)
        {
            // GameManager에 연결되어 있으면 우선 사용
            if (GameManager.Instance != null && GameManager.Instance.tileManager != null)
            {
                tileManager = GameManager.Instance.tileManager;
            }
            else
            {
                tileManager = FindObjectOfType<TileManager>();
            }
        }

        if (tileManager == null)
        {
            Debug.LogError("[AStarPathfinder] TileManager를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// start -> goal 까지의 경로를 A*로 계산
    /// - 성공 시: start~goal을 포함하는 좌표 리스트 반환
    /// - 실패 시: null 반환
    /// </summary>
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (tileManager == null)
        {
            Debug.LogWarning("[AStarPathfinder] TileManager가 없어 경로를 계산할 수 없습니다.");
            return null;
        }

        // goal이 이동 불가라면 바로 실패 (기지는 항상 Open이라 정상 동작할 것)
        if (!tileManager.IsWalkable(goal))
        {
            Debug.LogWarning($"[AStarPathfinder] 목표 타일 {goal} 이(가) 이동 불가 상태입니다.");
            return null;
        }

        // A* 자료구조
        var openSet = new List<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int>();
        var fScore = new Dictionary<Vector2Int, int>();

        openSet.Add(start);
        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);

        while (openSet.Count > 0)
        {
            // fScore가 가장 작은 노드 선택
            Vector2Int current = openSet[0];
            int bestF = GetScore(fScore, current);

            for (int i = 1; i < openSet.Count; i++)
            {
                var node = openSet[i];
                int f = GetScore(fScore, node);
                if (f < bestF)
                {
                    bestF = f;
                    current = node;
                }
            }

            // 목표 도달
            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);

            // 이웃 노드 검사
            foreach (var neighbor in tileManager.GetWalkableNeighbors(current))
            {
                int tentativeG = GetScore(gScore, current) + 1; // 이동 비용 = 1

                int oldG = GetScore(gScore, neighbor);
                if (tentativeG < oldG)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        // 경로 없음
        return null;
    }

    // 맨해튼 거리 휴리스틱
    private int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // Dictionary에서 값 가져오기 (없으면 int.MaxValue)
    private int GetScore(Dictionary<Vector2Int, int> dict, Vector2Int key)
    {
        if (dict.TryGetValue(key, out int value))
            return value;
        return int.MaxValue;
    }

    // A* 결과를 실제 경로 리스트로 재구성
    private List<Vector2Int> ReconstructPath(
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Vector2Int current)
    {
        var path = new List<Vector2Int>();
        path.Add(current);

        while (cameFrom.TryGetValue(current, out Vector2Int prev))
        {
            current = prev;
            path.Add(current);
        }

        // start → goal 순서가 되도록 뒤집기
        path.Reverse();
        return path;
    }
}
