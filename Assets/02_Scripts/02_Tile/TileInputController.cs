using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

// 마우스 입력을 받아 타일을 열거나(좌클릭), 플래그를 토글(우클릭)하는 컨트롤러
public class TileInputController : MonoBehaviour
{
    [SerializeField] private MineWarningUI mineWarningUI;  // ★ 경고 UI 참조
    [SerializeField] private Camera mainCamera;   // 메인 카메라
    [SerializeField] private Tilemap tilemap;     // 타일이 그려지는 Tilemap
    [SerializeField] private TileManager tileManager;

    // "유효하지 않은 좌표"를 나타내기 위한 상수
    private static readonly Vector2Int InvalidPos = new Vector2Int(int.MinValue, int.MinValue);



    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        // 에디터/플랫폼에 따라 Mouse.current 가 없을 수도 있으니 방어 코드
        if (Mouse.current == null)
            return;

        // 1) GameManager가 존재하는지 확인
        var gm = GameManager.Instance;

        // -----------------------------
        // A. 건설 모드 처리
        // -----------------------------
        if (gm != null && gm.CurrentMode == GameMode.Build)
        {
            // 좌클릭 → 건설 시도
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleBuildClick();
            }

            // 우클릭은 건설 모드에서는 사용하지 않음
            // (원하면 여기서 "건설 모드에서는 깃발 설치 불가" 안내 로그 출력 가능)
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                Debug.Log("[TileInputController] 건설 모드에서는 깃발을 설치할 수 없습니다. 탐색 모드에서 우클릭하세요.");
            }

            return;
        }

        // -----------------------------
        // B. 탐색 모드 처리 (기존 로직)
        // -----------------------------

        // 좌클릭 → 타일 열기
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleLeftClick();
        }

        // 우클릭 → 플래그 토글
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }
    }

    // --------------------------------------------------------------------
    // 공통: 현재 마우스가 가리키는 타일 좌표 얻기
    // --------------------------------------------------------------------
    private Vector2Int GetClickedTilePos()
    {
        if (mainCamera == null || tilemap == null || tileManager == null)
        {
            Debug.LogWarning("[TileInputController] 참조가 설정되어 있지 않습니다.");
            return InvalidPos;
        }

        // 1) 화면 좌표(픽셀)
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        // 2) 화면 → 월드
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        worldPos.z = 0f;

        // 3) 월드 → 타일 좌표
        Vector3Int cellPos = tilemap.WorldToCell(worldPos);
        return new Vector2Int(cellPos.x, cellPos.y);
    }
    // --------------------------------------------------------------------
    // 건설 모드에서 좌클릭: 선택된 타워/병영 건설 시도
    // --------------------------------------------------------------------
    private void HandleBuildClick()
    {
        Vector2Int tilePos = GetClickedTilePos();
        if (tilePos == InvalidPos)
            return;

        if (BuildManager.Instance == null)
        {
            Debug.LogWarning("[TileInputController] BuildManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        BuildManager.Instance.TryBuildAt(tilePos);
    }

    // --------------------------------------------------------------------
    // 좌클릭: 타일 열기
    // --------------------------------------------------------------------
    private void HandleLeftClick()
    {
        Vector2Int tilePos = GetClickedTilePos();
        if (tilePos == InvalidPos)
            return;

        TileData tile = tileManager.GetTile(tilePos);
        if (tile == null)
            return;

        // 플래그가 꽂힌 칸은 열지 않음
        if (tile.state == TileState.Flagged)
            return;

        // ★ 지뢰를 클릭한 경우: 경고 UI만 표시, 게임은 계속 진행
        if (tile.hasMine)
        {
            // 지뢰 칸은 붉게 열어줌 (시각적 피드백)
            tile.state = TileState.Open;
            tileManager.tileView.RenderTile(tile);

            // 경고 UI 호출
            if (mineWarningUI != null)
            {
                mineWarningUI.ShowOnce();
            }

            Debug.Log("[TileInputController] 지뢰를 밟았습니다! (경고 UI 표시, 게임은 계속)");

            // 자원 패널티를 주고 싶으면 여기에서 ResourceManager 호출
            // 예) resourceManager.AddResource(-5);

            return;
        }

        // 안전 칸 → TileManager에게 열어달라고 요청
        // (자원 +1 지급이 이 함수 안에서 처리되도록 구현하셨을 것이라 가정)
        tileManager.OpenTile(tilePos, true);
    }

    // --------------------------------------------------------------------
    // 우클릭: 플래그 토글
    // --------------------------------------------------------------------
    private void HandleRightClick()
    {
        Vector2Int tilePos = GetClickedTilePos();
        if (tilePos == InvalidPos)
            return;

        tileManager.ToggleFlag(tilePos);
    }
}
