using UnityEngine;

// 웨이브가 발생했을 때 던전에서 몬스터를 소환하는 매니저
public class MonsterSpawner : MonoBehaviour
{
    public static MonsterSpawner Instance { get; private set; }


    [Header("References")]
    [SerializeField] private DungeonManager dungeonManager;

    [SerializeField] private AStarPathfinder pathfinder; // ★ 추가
    [Header("Monster Prefabs")]
    [SerializeField] private GameObject normalMonsterPrefab; // 일반형
    [SerializeField] private GameObject fastMonsterPrefab;   // 속행형
    [SerializeField] private GameObject tankMonsterPrefab;   // 단단형

    [Header("Spawn Settings")]
    [SerializeField] private int baseCount = 3;        // 기본 소환 수
    [SerializeField] private int bonusPerNSpawns = 3;  // N번 소환마다 +1마리

    // 지금까지 몇 마리의 몬스터가 생성되었는지 (전체 카운트)
    private int globalSpawnIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dungeonManager == null)
        {
            dungeonManager = FindObjectOfType<DungeonManager>();
        }
        if (pathfinder == null)
        {
            pathfinder = FindObjectOfType<AStarPathfinder>();
        }
    }

    // -------------------------------------------------------------
    // 외부에서 호출: "웨이브 한 번"을 소환
    // 반환값:
    //  - true  : 적어도 한 마리 이상 소환 성공
    //  - false : 던전이 없어서 소환 실패 (나중에 다시 시도)
    // -------------------------------------------------------------
    public bool TrySpawnWave()
    {
        if (dungeonManager == null)
        {
            Debug.LogWarning("[MonsterSpawner] DungeonManager 참조가 없습니다.");
            return false;
        }

        var dungeons = dungeonManager.Dungeons;
        if (dungeons == null || dungeons.Count == 0)
        {
            Debug.Log("[MonsterSpawner] 활성 던전이 없어 웨이브를 소환할 수 없습니다.");
            return false;
        }

        // 이번 웨이브에서 소환할 총 수량 계산
        int bonusCount = globalSpawnIndex / bonusPerNSpawns;
        int spawnCount = baseCount + bonusCount;

        // 난이도 스케일링: 몹이 생성될 때마다 체력 +10%
        // globalSpawnIndex가 증가할수록 곱해지는 비율이 커진다.
        float hpMultiplier = Mathf.Pow(1.1f, globalSpawnIndex);

        Debug.Log($"[MonsterSpawner] 웨이브 소환: {spawnCount}마리, HP 배율 x{hpMultiplier:F2}");

        for (int i = 0; i < spawnCount; i++)
        {
            // 1) 사용할 던전 하나를 랜덤 선택
            var dungeon = dungeonManager.GetRandomDungeon();
            if (dungeon == null)
                break;

            // 2) 생성 위치 (던전 아이콘 위치 사용)
            Vector3 spawnPos = dungeon.iconObject != null
                ? dungeon.iconObject.transform.position
                : dungeonManager.TileManager.tileView.GetTileWorldPosition(dungeon.position);


            // 살짝 Z값 조정 (타일/아이콘보다 위쪽에)
            spawnPos += new Vector3(0f, 0f, -1.5f);

            // 3) 이번 몬스터의 타입 결정
            GameObject prefab = ChooseMonsterPrefab();

            if (prefab == null)
                continue;

            GameObject monsterObj = Instantiate(prefab, spawnPos, Quaternion.identity);

            // 체력 초기화
            Monster monster = monsterObj.GetComponent<Monster>();
            if (monster != null)
            {
                monster.Initialize(hpMultiplier);
            }

            // 이동 컴포넌트에 경로 설정
            if (pathfinder != null && dungeonManager != null)
            {
                // 던전 위치에서 기지 중심까지의 경로 계산
                // DungeonManager에 BaseCenter가 없다면, 새로 public 프로퍼티를 추가해 주세요.
                Vector2Int start = dungeon.position;
                Vector2Int goal = dungeonManager.BaseCenter;  // ★ BaseCenter 프로퍼티 필요

                var path = pathfinder.FindPath(start, goal);

                MonsterMover mover = monsterObj.GetComponent<MonsterMover>();
                if (mover != null && path != null && path.Count > 0)
                {
                    mover.SetPath(path, dungeonManager.TileManager);
                }
                else if (path == null)
                {
                    Debug.LogWarning($"[MonsterSpawner] 경로를 찾지 못해 몬스터가 정지 상태입니다. start={start}, goal={goal}");
                }
            }
            else
            {
                Debug.LogWarning("[MonsterSpawner] pathfinder 또는 dungeonManager 참조가 없어 몬스터 이동을 설정할 수 없습니다.");
            }

            // 전역 스폰 인덱스 증가 (체력/수량 스케일링 기준)
            globalSpawnIndex++;
        }

        return true;
    }

    // -------------------------------------------------------------
    // 현재 globalSpawnIndex를 기준으로 몬스터 종류 비율 결정
    //  - 0~3회   : 일반형 100%
    //  - 4~6회   : 일반형 70% / 속행형 30%
    //  - 7회 이후: 일반형 40% / 속행형 40% / 탱커 20%
    // -------------------------------------------------------------
    private GameObject ChooseMonsterPrefab()
    {
        int index = globalSpawnIndex;

        float r = Random.value;

        if (index < 4)
        {
            // 초기: 일반형만
            return normalMonsterPrefab;
        }
        else if (index < 7)
        {
            // 일반 70%, 빠른 30%
            if (r < 0.7f) return normalMonsterPrefab;
            return fastMonsterPrefab;
        }
        else
        {
            // 일반 40%, 빠른 40%, 탱커 20%
            if (r < 0.4f) return normalMonsterPrefab;
            if (r < 0.8f) return fastMonsterPrefab;
            return tankMonsterPrefab;
        }
    }
}
