using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
/// <summary>
/// 타워/병영 건설을 전담하는 매니저
/// - 건설 모드일 때 숫자키로 건설 타입 선택
/// - 좌클릭으로 TryBuildAt을 호출하도록 연동 예정
/// </summary>
public enum BuildType
{
    None = 0,      // 선택 없음
    ArrowTower = 1, // 화살 타워
    Barracks = 2   // 병영 타워
}

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI buildInfoText;

    [Header("References")]
    [SerializeField] private GameModeManager gameModeManager;   // 현재 게임 모드(탐색/건설)
    [SerializeField] private TileManager tileManager;           // 타일 정보 접근용
    [SerializeField] private ResourceManager resourceManager;   // 자원 소비용

    [Header("Building Prefabs")]
    [SerializeField] private GameObject arrowTowerPrefab;       // 화살 타워 프리팹
    [SerializeField] private GameObject barracksPrefab;         // 병영 타워 프리팹

    [Header("Cost Settings")]
    [Tooltip("기본 건설 비용 (첫 건설 시)")]
    [SerializeField] private int baseBuildCost = 3;

    // 지금까지 성공적으로 건설한 건물 수
    private int buildCount = 0;

    // 현재 선택된 건설 타입
    private BuildType currentBuildType = BuildType.None;
    public BuildType CurrentBuildType => currentBuildType;

    // 이미 건물이 지어진 타일 목록 (중복 건설 방지)
    private Dictionary<Vector2Int, GameObject> builtObjects
        = new Dictionary<Vector2Int, GameObject>();

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 참조 자동 할당 (인스펙터에서 비어 있을 경우 보정)
        if (gameModeManager == null)
            gameModeManager = FindObjectOfType<GameModeManager>();

        if (tileManager == null)
            tileManager = FindObjectOfType<TileManager>();

        if (resourceManager == null)
            resourceManager = ResourceManager.Instance;
    }

    private void Update()
    {
        // 키보드가 없으면 아무 것도 하지 않음
        if (Keyboard.current == null)
            return;

        // 건설 모드가 아니면 숫자키 입력을 무시
        if (gameModeManager == null || gameModeManager.CurrentMode != GameMode.Build)
            return;

        // ----------------------------------------------------
        // 숫자키(1~8)로 건설 타입 선택
        // 1: 화살 타워, 2: 병영, 나머지는 향후 확장용
        // ----------------------------------------------------
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            SetBuildType(BuildType.ArrowTower);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            SetBuildType(BuildType.Barracks);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame ||
                 Keyboard.current.digit4Key.wasPressedThisFrame ||
                 Keyboard.current.digit5Key.wasPressedThisFrame ||
                 Keyboard.current.digit6Key.wasPressedThisFrame ||
                 Keyboard.current.digit7Key.wasPressedThisFrame ||
                 Keyboard.current.digit8Key.wasPressedThisFrame)
        {
            // 아직 구현되지 않은 슬롯
            SetBuildType(BuildType.None);
        }

        // 실제 마우스 좌클릭에 따른 건설 시도는
        // TileInputController에서 BuildManager.TryBuildAt을 호출하는 식으로
        // 다음 단계에서 연동할 예정입니다.
    }

    /// <summary>
    /// 현재 선택된 건설 타입을 변경하고, 디버그 로그/추후 UI 갱신 트리거
    /// </summary>
    private void SetBuildType(BuildType type)
    {
        currentBuildType = type;
        Debug.Log($"[BuildManager] 선택된 건설 타입: {currentBuildType}");
        UpdateBuildInfoUI();
    }

    /// <summary>
    /// 특정 타일 좌표에 건설을 시도
    /// - 건설 가능 타일인지 판정
    /// - 자원 부족 여부 체크
    /// - 프리팹 Instantiate 및 내부 상태 갱신
    /// </summary>
    public void TryBuildAt(Vector2Int tilePos)
    {
        if (currentBuildType == BuildType.None)
        {
            Debug.Log("[BuildManager] 선택된 건설 타입이 없습니다. (숫자키로 먼저 선택 필요)");
            return;
        }

        if (tileManager == null)
        {
            Debug.LogWarning("[BuildManager] TileManager 참조가 없습니다.");
            return;
        }

        TileData tile = tileManager.GetTile(tilePos);
        if (tile == null)
            return;

        // 이미 건물이 있는 타일은 건설 불가
        if (builtObjects.ContainsKey(tilePos))
        {
            Debug.Log("[BuildManager] 이미 건물이 있는 타일입니다.");
            return;
        }

        // 타일이 건설 가능한지 여부 검사
        if (!IsBuildableTile(tile))
        {
            Debug.Log("[BuildManager] 이 타일에는 건설할 수 없습니다.");
            return;
        }

        // 실제 건설 비용 = 기본 3 + 건설 횟수
        int cost = baseBuildCost + buildCount;

        // 자원 매니저 보정
        if (resourceManager == null)
            resourceManager = ResourceManager.Instance;

        if (resourceManager != null)
        {
            // 자원이 부족하면 건설 실패
            if (!resourceManager.TrySpendResource(cost))
            {
                Debug.Log($"[BuildManager] 자원이 부족합니다. 필요: {cost}");
                return;
            }

        }
        else
        {
            Debug.LogWarning("[BuildManager] ResourceManager가 설정되어 있지 않습니다.");
        }

        // 건설할 프리팹 선택
        GameObject prefab = GetPrefabForBuildType(currentBuildType);
        if (prefab == null)
        {
            Debug.LogWarning($"[BuildManager] {currentBuildType} 에 해당하는 프리팹이 설정되지 않았습니다.");
            return;
        }

        // 타일 중심 월드 좌표 계산
        Vector3 worldPos = tileManager.tileView.GetTileWorldPosition(tilePos);
        worldPos.z = -2f;   // 타워 Z 레이어는 추후 필요시 조정

        // 실제 건물 생성
        GameObject building = Instantiate(prefab, worldPos, Quaternion.identity);
        // ---- 병영이면 초기화 ----
        var barracks = building.GetComponent<BarracksTower>();
        if (barracks != null)
        {
            barracks.Initialize(tilePos, tileManager);
        }
        // 상태 갱신
        builtObjects.Add(tilePos, building);
        buildCount++;

        Debug.Log($"[BuildManager] {currentBuildType} 건설 완료. 위치: {tilePos}, 비용: {cost}");
    }

    /// <summary>
    /// 이 타일에 건설이 가능한지 판정하는 규칙
    /// Q3에서 정의한 규칙을 반영
    /// </summary>
    private bool IsBuildableTile(TileData tile)
    {
        // 1) 기본 규칙: 깃발이 꽂힌 지뢰 타일에만 건설 가능
        //    - tile.state == Flagged 이고, 실제로 hasMine == true 인 경우
        if (tile.state == TileState.Flagged && tile.hasMine)
            return true;

        // 2) 특별 케이스: 이벤트/유물로 Buildable 상태로 바뀐 타일
        //    - 50% 이벤트 유물 등에서 타일 상태를 Buildable로 변경해주면,
        //      여기에서 예외적으로 건설 허용
        if (tile.state == TileState.Buildable)
            return true;

        // 그 외에는 건설 불가
        return false;
    }

    /// <summary>
    /// 건설 타입별로 사용할 프리팹을 반환
    /// </summary>
    private GameObject GetPrefabForBuildType(BuildType type)
    {
        switch (type)
        {
            case BuildType.ArrowTower:
                return arrowTowerPrefab;
            case BuildType.Barracks:
                return barracksPrefab;
            default:
                return null;
        }
    }
    private void UpdateBuildInfoUI()
    {
        if (buildInfoText == null)
            return;

        string text = "Build: ";

        switch (currentBuildType)
        {
            case BuildType.ArrowTower:
                text += "1 - Arrow Tower";
                break;
            case BuildType.Barracks:
                text += "2 - Barracks";
                break;
            case BuildType.None:
            default:
                text += "None (Press 1 or 2)";
                break;
        }

        buildInfoText.text = text;
    }
}
