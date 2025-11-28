using UnityEngine;

// 타일 개방률을 기준으로 던전/웨이브 트리거를 발생시키는 전담 매니저
public class TileOpenTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TileManager tileManager;

    // [설명]
    // - waveThresholds : 웨이브 발생 비율 (15, 30, 45, 60, 75, 90%)
    // - dungeonThresholds : 던전 생성 비율 (20, 40, 60%)
    // 1.0f == 100%
    private readonly float[] waveThresholds = { 0.15f, 0.30f, 0.45f, 0.60f, 0.75f, 0.90f };
    private readonly float[] dungeonThresholds = { 0.20f, 0.40f, 0.60f };

    // 각 임계치가 이미 발동되었는지 여부를 저장
    private bool[] waveTriggered;
    private bool[] dungeonTriggered;

    private void Awake()
    {
        if (tileManager == null)
        {
            tileManager = FindObjectOfType<TileManager>();
        }

        waveTriggered = new bool[waveThresholds.Length];
        dungeonTriggered = new bool[dungeonThresholds.Length];
    }

    private void OnEnable()
    {
        if (tileManager != null)
        {
            // TileManager에서 타일이 열릴 때마다 콜백을 받음
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

    // 타일이 새로 열릴 때마다 호출되는 함수
    private void HandleTileOpened(TileData tile, float openRatio)
    {
        // 1) 던전 생성 트리거 체크 (20, 40, 60%)
        for (int i = 0; i < dungeonThresholds.Length; i++)
        {
            if (dungeonTriggered[i])
                continue;

            if (openRatio >= dungeonThresholds[i])
            {
                dungeonTriggered[i] = true;
                OnDungeonThresholdReached(dungeonThresholds[i]);
            }
        }

        // 2) 웨이브 트리거 체크 (15, 30, 45, 60, 75, 90%)
        for (int i = 0; i < waveThresholds.Length; i++)
        {
            if (waveTriggered[i])
                continue;

            if (openRatio >= waveThresholds[i])
            {
                waveTriggered[i] = true;
                OnWaveThresholdReached(waveThresholds[i]);
            }
        }
    }

    // 던전 생성 이벤트가 발생했을 때 호출되는 함수
    private void OnDungeonThresholdReached(float threshold)
    {
        Debug.Log($"[TileOpenTrigger] 던전 생성 임계치 도달: {threshold * 100f:F0}%");

        // TODO: 다음 단계에서 DungeonManager.SpawnDungeonAt(...) 호출로 연결
    }

    // 웨이브 발생 이벤트가 발생했을 때 호출되는 함수
    private void OnWaveThresholdReached(float threshold)
    {
        Debug.Log($"[TileOpenTrigger] 웨이브 임계치 도달: {threshold * 100f:F0}%");

        // TODO: 다음 단계에서 WaveManager / MonsterSpawner로 연결
    }
}
