using UnityEngine;

// 타일 개방률을 기준으로 던전/웨이브 트리거를 발생시키는 전담 매니저
public class TileOpenTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TileManager tileManager;
    [SerializeField] private DungeonManager dungeonManager;

    // 웨이브 임계치 (진행도 기준 비율)
    private readonly float[] waveThresholds = { 0.15f, 0.30f, 0.45f, 0.60f, 0.75f, 0.90f };

    // 던전 생성 임계치
    private readonly float[] dungeonThresholds = { 0.20f, 0.40f, 0.60f };

    private bool[] waveTriggered;
    private bool[] dungeonTriggered;

    // 아직 생성되지 못한 "보류 던전 개수"
    private int pendingDungeonSpawnCount = 0;

    private void Awake()
    {
        if (tileManager == null)
        {
            tileManager = FindObjectOfType<TileManager>();
        }

        if (dungeonManager == null)
        {
            dungeonManager = FindObjectOfType<DungeonManager>();
        }

        waveTriggered = new bool[waveThresholds.Length];
        dungeonTriggered = new bool[dungeonThresholds.Length];
    }

    private void OnEnable()
    {
        if (tileManager != null)
        {
            tileManager.OnTileOpened += HandleTileOpened;
        }
    }

    private void OnDisable()
    {
        if (tileManager != null)
        {
            tileManager.OnTileOpened -= HandleTileOpened;
        }
    }

    // 타일이 "진행도에 포함되는 방식으로" 새로 열릴 때마다 호출
    private void HandleTileOpened(TileData tile, float progressRatio)
    {
        // 1) 던전 생성 임계치 체크 (20, 40, 60%)
        for (int i = 0; i < dungeonThresholds.Length; i++)
        {
            if (dungeonTriggered[i])
                continue;

            if (progressRatio >= dungeonThresholds[i])
            {
                dungeonTriggered[i] = true;
                OnDungeonThresholdReached(dungeonThresholds[i]);
            }
        }

        // 2) 웨이브 임계치 체크 (15, 30, 45, 60, 75, 90%)
        for (int i = 0; i < waveThresholds.Length; i++)
        {
            if (waveTriggered[i])
                continue;

            if (progressRatio >= waveThresholds[i])
            {
                waveTriggered[i] = true;
                OnWaveThresholdReached(waveThresholds[i]);
            }
        }

        // 3) 타일이 새로 열릴 때마다, 보류 중인 던전 생성 시도
        TrySpawnPendingDungeons();
    }

    private void OnDungeonThresholdReached(float threshold)
    {
        Debug.Log($"[TileOpenTrigger] 던전 생성 임계치 도달: {threshold * 100f:F0}%");

        // 이 타이밍에 던전 생성 1회를 예약
        pendingDungeonSpawnCount++;

        // 바로 시도해 보고, 실패하면 보류 카운트만 남아 있게 된다.
        TrySpawnPendingDungeons();
    }

    private void OnWaveThresholdReached(float threshold)
    {
        Debug.Log($"[TileOpenTrigger] 웨이브 임계치 도달: {threshold * 100f:F0}%");

        // TODO: 다음 단계에서 WaveManager / MonsterSpawner 연결
    }

    // -------------------------------------------------------------
    // 아직 생성되지 못한 던전들을 실제로 생성 시도
    //  - 후보 타일이 없으면 즉시 중단하고, 남은 보류 수는 그대로 유지
    // -------------------------------------------------------------
    private void TrySpawnPendingDungeons()
    {
        if (dungeonManager == null || pendingDungeonSpawnCount <= 0)
            return;

        // 한 번에 여러 개가 보류될 수 있으므로 while 로 처리
        while (pendingDungeonSpawnCount > 0)
        {
            bool success = dungeonManager.TrySpawnDungeon();

            if (!success)
            {
                // 더 이상 생성할 수 있는 후보가 없으므로
                // 나머지 보류 요청은 다음 타일이 열릴 때 다시 시도
                break;
            }

            pendingDungeonSpawnCount--;
        }
    }
}
