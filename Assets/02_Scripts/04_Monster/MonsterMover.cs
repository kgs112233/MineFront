using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터를 타일 경로를 따라 부드럽게 이동시키는 전담 컴포넌트
/// </summary>
[RequireComponent(typeof(Monster))]
public class MonsterMover : MonoBehaviour
{
    [Header("Move Settings")]
    [Tooltip("전역 속도 배율 (게임 전체 이동 속도 조절용 상수)")]
    public float speedMultiplier = 1.0f;

    [Tooltip("타일 중앙에 도달했다고 판단하는 거리")]
    public float arriveThreshold = 0.05f;

    private Monster monster;                  // 몬스터 기본 정보 (moveSpeed 등)
    private TileManager tileManager;          // 타일 → 월드 좌표 변환용
    private List<Vector2Int> path;            // 따라갈 타일 경로
    private int currentIndex = 0;             // 현재 타겟 타일 인덱스
    private bool hasPath = false;             // 경로 보유 여부

    private void Awake()
    {
        monster = GetComponent<Monster>();

        if (tileManager == null)
        {
            // GameManager에 참조가 있으면 우선 사용
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
            Debug.LogError("[MonsterMover] TileManager를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 외부에서 경로를 설정해 주는 함수
    /// - 경로는 시작 타일 ~ 목표 타일(기지)을 포함하는 순서로 전달
    /// </summary>
    public void SetPath(List<Vector2Int> newPath, TileManager manager)
    {
        if (newPath == null || newPath.Count == 0)
        {
            hasPath = false;
            path = null;
            return;
        }

        path = newPath;
        currentIndex = 0;
        hasPath = true;

        if (manager != null)
            tileManager = manager;

        // 첫 타겟 타일의 중앙으로 Y, X 맞추고 Z는 현재 값 유지
        Vector3 firstPos = GetWorldPos(path[currentIndex]);
        transform.position = new Vector3(firstPos.x, firstPos.y, transform.position.z);
    }

    private void Update()
    {
        if (!hasPath || path == null || tileManager == null)
            return;

        if (currentIndex >= path.Count)
            return;

        // 현재 타겟 타일의 월드 좌표
        Vector3 targetPos = GetWorldPos(path[currentIndex]);

        // Z는 현재 오브젝트의 Z를 유지
        targetPos.z = transform.position.z;

        float speed = monster.moveSpeed * speedMultiplier;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // 타겟 타일에 도달했는지 체크
        if ((transform.position - targetPos).sqrMagnitude <= arriveThreshold * arriveThreshold)
        {
            currentIndex++;

            // 마지막 타일(기지)에 도달한 경우
            if (currentIndex >= path.Count)
            {
                OnArrivedBase();
            }
        }
    }

    // 타일 좌표를 월드 좌표로 변환
    private Vector3 GetWorldPos(Vector2Int gridPos)
    {
        Vector3 world = tileManager.tileView.GetTileWorldPosition(gridPos);
        return world;
    }

    // 기지에 도달했을 때 처리
    private void OnArrivedBase()
    {
        // 기지 HP -1 처리
        if (BaseHealthManager.Instance != null)
        {
            BaseHealthManager.Instance.TakeDamage(1);
        }
        else
        {
            Debug.LogWarning("[MonsterMover] BaseHealthManager 인스턴스를 찾을 수 없습니다.");
        }

        // 도착한 몬스터는 제거
        Destroy(gameObject);
    }
}
