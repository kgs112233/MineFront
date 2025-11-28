using System.Collections.Generic;
using UnityEngine;

// 던전(스폰 포인트)과 중앙 기지 아이콘을 관리하는 매니저
public class DungeonManager : MonoBehaviour
{
    // 싱글톤 (간단한 전역 접근용)
    public static DungeonManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TileManager tileManager;       // 타일 정보 접근용
    [SerializeField] private GameObject dungeonIconPrefab;  // 1x1 던전 아이콘 프리팹
    [SerializeField] private GameObject baseIconPrefab;     // 중앙 기지 아이콘 프리팹

    [Header("Settings")]
    [SerializeField] private int minDistanceFromBase = 9;   // 기지로부터 최소 거리(맨해튼)

    // 던전 하나를 표현하는 내부 데이터 구조
    [System.Serializable]
    public class DungeonInstance
    {
        public Vector2Int position;     // 던전이 위치한 타일 좌표
        public GameObject iconObject;   // 화면에 보이는 아이콘 오브젝트
    }

    // 현재 활성화된 던전 목록
    private readonly List<DungeonInstance> dungeons = new List<DungeonInstance>();
    //외부에서 읽기 전용으로 던전 목록을 접근할 수 있게 하는 프로퍼티
    public IReadOnlyList<DungeonInstance> Dungeons => dungeons;
    public TileManager TileManager => tileManager;

    // 중앙 기지(2x2) 영역 좌표 집합
    private readonly HashSet<Vector2Int> baseArea = new HashSet<Vector2Int>();
    private Vector2Int baseCenter;          // 기지 중심(대략적인 기준점)

    private void Awake()
    {
        // 간단한 싱글톤 처리
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (tileManager == null)
        {
            tileManager = FindObjectOfType<TileManager>();
        }
    }

    private void Start()
    {
        if (tileManager == null)
        {
            Debug.LogError("[DungeonManager] TileManager 참조가 없습니다.");
            return;
        }

        if (baseIconPrefab == null)
        {
            Debug.LogWarning("[DungeonManager] Base Icon Prefab이 지정되지 않았습니다.");
        }

        if (dungeonIconPrefab == null)
        {
            Debug.LogWarning("[DungeonManager] Dungeon Icon Prefab이 지정되지 않았습니다.");
        }

        InitializeBaseArea();
        SpawnBaseIcons();
    }

    // -------------------------------------------------------------
    // 중앙 기지 2x2 영역(예: 9,9 ~ 10,10)을 계산하고 저장
    //  - width/height에 의존하도록 만들어 두어서, 나중에 맵 크기가 달라져도 동작
    // -------------------------------------------------------------
    private void InitializeBaseArea()
    {
        int centerX = tileManager.width / 2;
        int centerY = tileManager.height / 2;

        // centerX-1, centerX / centerY-1, centerY → 2x2 영역
        for (int x = centerX - 1; x <= centerX; x++)
        {
            for (int y = centerY - 1; y <= centerY; y++)
            {
                baseArea.Add(new Vector2Int(x, y));
            }
        }

        // 기준점은 중앙 타일 하나로 사용
        baseCenter = new Vector2Int(centerX, centerY);
    }

    // -------------------------------------------------------------
    // 중앙 기지 2x2 타일 위에 BaseIconPrefab을 배치
    // -------------------------------------------------------------
    private void SpawnBaseIcons()
    {
        if (baseIconPrefab == null)
            return;

        foreach (Vector2Int pos in baseArea)
        {
            TileData tile = tileManager.GetTile(pos);
            if (tile == null)
                continue;

            Vector3 worldPos = tileManager.tileView.GetTileWorldPosition(pos);
            worldPos += new Vector3(0f, 0f, -2f);
            GameObject icon = Instantiate(baseIconPrefab, worldPos, Quaternion.identity, transform);

            // 필요하다면 icon의 Z 좌표를 살짝 앞으로 빼줄 수 있음
            // icon.transform.position += new Vector3(0f, 0f, -0.2f);
        }

        Debug.Log("[DungeonManager] 중앙 기지 아이콘 생성 완료.");
    }

    // -------------------------------------------------------------
    // 외부에서 호출: "지금 조건에서 던전 하나를 생성할 수 있으면 생성"
    //  - 생성에 성공하면 true, 아직 후보 타일이 없으면 false
    // -------------------------------------------------------------
    public bool TrySpawnDungeon()
    {
        if (tileManager == null || dungeonIconPrefab == null)
        {
            Debug.LogWarning("[DungeonManager] 던전 생성 불가: 참조 또는 프리팹이 없습니다.");
            return false;
        }

        List<Vector2Int> candidates = new List<Vector2Int>();

        // 1. 전체 타일을 훑으면서 던전 후보 타일 수집
        for (int x = 0; x < tileManager.width; x++)
        {
            for (int y = 0; y < tileManager.height; y++)
            {
                TileData tile = tileManager.tiles[x, y];
                if (tile == null)
                    continue;

                // 열린 타일만 후보 (현재 열린 타일 중에서만 던전 생성)
                if (tile.state != TileState.Open)
                    continue;

                // 지뢰가 있는 타일은 제외
                if (tile.hasMine)
                    continue;

                // 이미 던전으로 사용 중인 타일은 제외
                if (tile.isDungeonSpawnPoint)
                    continue;

                Vector2Int pos = tile.position;

                // 중앙 기지 2x2 영역은 던전 후보에서 제외
                if (baseArea.Contains(pos))
                    continue;

                // 기지 중심에서의 맨해튼 거리 계산
                int dist = Mathf.Abs(pos.x - baseCenter.x) + Mathf.Abs(pos.y - baseCenter.y);
                if (dist < minDistanceFromBase)
                    continue;

                candidates.Add(pos);
            }
        }

        if (candidates.Count == 0)
        {
            // 아직 충분히 멀리 떨어진 열린 타일이 없음 → 나중에 다시 시도
            Debug.Log("[DungeonManager] 던전 후보 타일이 아직 없습니다. 다음에 다시 시도합니다.");
            return false;
        }

        // 2. 후보들 중에서 랜덤으로 1곳 선택
        int index = Random.Range(0, candidates.Count);
        Vector2Int dungeonPos = candidates[index];

        SpawnDungeonAt(dungeonPos);
        return true;
    }

    // 실제로 던전을 생성하는 내부 함수
    private void SpawnDungeonAt(Vector2Int pos)
    {
        TileData tile = tileManager.GetTile(pos);
        if (tile == null)
            return;

        tile.isDungeonSpawnPoint = true;

        Vector3 worldPos = tileManager.tileView.GetTileWorldPosition(pos);
        worldPos += new Vector3(0f, 0f, -2f);
        GameObject icon = Instantiate(dungeonIconPrefab, worldPos, Quaternion.identity, transform);

        DungeonInstance instance = new DungeonInstance
        {
            position = pos,
            iconObject = icon
        };
        dungeons.Add(instance);

        Debug.Log($"[DungeonManager] 던전 생성: {pos}");
    }
    // [추가] 활성 던전 중 하나를 랜덤으로 반환 (없으면 null)
    public DungeonInstance GetRandomDungeon()
    {
        if (dungeons == null || dungeons.Count == 0)
            return null;

        int index = Random.Range(0, dungeons.Count);
        return dungeons[index];
    }
}
