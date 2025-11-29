using System.Collections.Generic;
using UnityEngine;

// 타일이 어떤 상태인지 표현하는 열거형 (화면에 어떻게 보이는지)
public enum TileState
{
    Closed,   // 아직 열리지 않은 타일
    Open,     // 열린 타일
    Flagged,  // 깃발이 꽂힌 타일
    Buildable // 건물 설치가 가능한 타일 (후반에 사용 예정)
}

// 타일 하나가 가지는 데이터 구조
[System.Serializable]
public class TileData
{
    public Vector2Int position;           // 타일 좌표
    public TileState state;               // 현재 화면 상태
    public bool hasMine;                  // 이 타일에 지뢰가 있는지 여부
    public int surroundingMineCount;      // 주변 지뢰 개수
    public bool isDungeonSpawnPoint;      // 던전 생성 지점 여부
}

public class TileManager : MonoBehaviour
{
    [Header("Tile Settings")]
    public int width = 20;                // 맵 가로 크기
    public int height = 20;               // 맵 세로 크기

    [Header("Mine Settings")]
    [Range(0.05f, 0.3f)]
    public float mineRatio = 0.12f;       // 전체 타일 중 지뢰 비율
    public int safeCenterSize = 4;        // 중앙 안전 구역 한 변 길이 (4 → 4x4)

    // 화면 렌더링용 TileView 참조
    public TileView tileView;

    // 전체 타일 데이터를 저장하는 2차원 배열
    public TileData[,] tiles;
    // -------------------- 2주차용 진행도 계산 필드 --------------------
    // 전체 지뢰 개수
    private int mineCount = 0;

    //  현재 열린 타일 개수
    private int openedTileCount = 0;
    // "시작 안전구역 자동 오픈" 때 열린 타일 개수
    // → 진행도 계산에서 빼기 위해 사용
    private int initialOpenedCount = 0;
    // 외부에서 읽을 수 있는 프로퍼티

    public int OpenedTileCount => openedTileCount;
    public int TotalTileCount => width * height;
    public int NonMineTileCount => TotalTileCount - mineCount;

    // 플레이어 진행도 기준 개방률
    //  - 분자: openedTileCount - initialOpenedCount
    //  - 분모: NonMineTileCount - initialOpenedCount
    public float ProgressOpenRatio
    {
        get
        {
            int progressOpened = Mathf.Max(0, openedTileCount - initialOpenedCount);
            int progressTotal = Mathf.Max(1, NonMineTileCount - initialOpenedCount);
            return (float)progressOpened / progressTotal;
        }
    }

    // 타일이 새로 열릴 때 알리는 이벤트
    // (어떤 타일이 열렸는지, 현재 개방률이 얼마인지 전달)
    public System.Action<TileData, float> OnTileOpened;
    private void Awake()
    {
        // [추가] 혹시나 해서 시작 시 0으로 초기화
        openedTileCount = 0;
        // 타일 배열 생성
        tiles = new TileData[width, height];

        // 1) 타일 기본 데이터 생성 (모두 Closed, hasMine = false)
        InitializeTiles();

        // 2) 지뢰 랜덤 배치 (중앙 safe 영역은 항상 비워둠)
        PlaceMines();

        // 3) 주변 8칸 지뢰 개수 계산
        CalculateSurroundingMineCounts();

        // 4) 중앙 안전 구역 자동 오픈
        OpenSafeCenterRegion();
    }

    // 타일 데이터 초기화 함수
    private void InitializeTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileData tile = new TileData
                {
                    position = new Vector2Int(x, y),
                    state = TileState.Closed,
                    hasMine = false,
                    surroundingMineCount = 0,
                    isDungeonSpawnPoint = false
                };

                tiles[x, y] = tile;

                // 초기에는 모두 닫힌 상태로 렌더링
                tileView.RenderTile(tile);
            }
        }

        Debug.Log($"[TileManager] {width}x{height} 타일 데이터 초기화 완료.");
    }

    // 특정 좌표의 타일을 안전하게 가져오는 함수
    public TileData GetTile(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= width ||
            pos.y < 0 || pos.y >= height)
            return null;

        return tiles[pos.x, pos.y];
    }
    #region Pathfinding helpers (A*에서 사용)

    // 그리드 범위 안에 있는지 검사하는 함수
    public bool IsInsideBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width &&
               pos.y >= 0 && pos.y < height;
    }

    // 타일 데이터 기준으로 이동 가능한지 여부 판단
    // - 지뢰가 있으면 이동 불가
    // - 열린 타일(Open)만 이동 가능
    public bool IsWalkable(TileData tile)
    {
        if (tile == null)
            return false;

        if (tile.hasMine)
            return false;

        return tile.state == TileState.Open;
    }
    public bool IsWalkableWorldPosition(Vector3 worldPos)
    {
        if (tileView == null)
            return false;

        Vector3Int cell = tileView.WorldToCell(worldPos);
        TileData tile = GetTile(new Vector2Int(cell.x, cell.y));
        return IsWalkable(tile);
    }

    // 좌표 기준 오버로드
    public bool IsWalkable(Vector2Int pos)
    {
        return IsWalkable(GetTile(pos));
    }

    // 4방향 이동에 사용할 방향 벡터
    private static readonly Vector2Int[] fourDirections =
    {
    new Vector2Int(1, 0),
    new Vector2Int(-1, 0),
    new Vector2Int(0, 1),
    new Vector2Int(0, -1)
};

    // A*에서 사용할 "이동 가능한 이웃 타일" 목록 반환
    public IEnumerable<Vector2Int> GetWalkableNeighbors(Vector2Int pos)
    {
        foreach (var dir in fourDirections)
        {
            Vector2Int next = pos + dir;

            if (!IsInsideBounds(next))
                continue;

            if (IsWalkable(next))
                yield return next;
        }
    }

    #endregion
    // 지뢰를 랜덤으로 배치하는 함수 (중앙 safe 영역은 항상 비워둠)
    private void PlaceMines()
    {
        int totalTiles = width * height;

        // 목표 지뢰 개수 = 전체 타일 수 × 비율
        int targetMineCount = Mathf.RoundToInt(totalTiles * mineRatio);

        int placedMines = 0;
        int safetyGuard = totalTiles * 10; // 무한 루프 방지용

        while (placedMines < targetMineCount && safetyGuard > 0)
        {
            safetyGuard--;

            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            // 중앙 안전 구역이면 스킵
            if (IsInsideSafeCenter(x, y))
                continue;

            TileData tile = tiles[x, y];

            // 이미 지뢰면 스킵
            if (tile.hasMine)
                continue;

            // 지뢰로 설정
            tile.hasMine = true;
            placedMines++;
        }
        // 실제 배치된 지뢰 개수를 기록
        mineCount = placedMines;

        Debug.Log($"[TileManager] 지뢰 배치 완료: {placedMines}개 / 목표 {targetMineCount}개");
    }
    // 중앙 안전 구역의 최소/최대 좌표를 계산하는 함수
    // safeCenterSize가 짝수인지/홀수인지에 따라 범위를 다르게 계산
    private void GetSafeCenterBounds(out int minX, out int maxX, out int minY, out int maxY)
    {
        int centerX = width / 2;
        int centerY = height / 2;
        int half = safeCenterSize / 2;

        if (safeCenterSize % 2 == 0)
        {
            // 짝수 크기 (예: 4x4)
            // 예: width=20, centerX=10, half=2 → 8~11 (8,9,10,11 = 4칸)
            minX = centerX - half;
            maxX = centerX + half - 1;
            minY = centerY - half;
            maxY = centerY + half - 1;
        }
        else
        {
            // 홀수 크기 (예: 5x5)
            // 예: width=20, centerX=10, half=2 → 8~12 (5칸)
            minX = centerX - half;
            maxX = centerX + half;
            minY = centerY - half;
            maxY = centerY + half;
        }
    }


    // 해당 좌표가 중앙 안전 구역 안인지 판단하는 함수
    private bool IsInsideSafeCenter(int x, int y)
    {
        GetSafeCenterBounds(out int minX, out int maxX, out int minY, out int maxY);

        return (x >= minX && x <= maxX && y >= minY && y <= maxY);
    }


    // 각 타일 주변 8칸의 지뢰 개수를 계산하는 함수
    private void CalculateSurroundingMineCounts()
    {
        // 모든 타일에 대해 검사
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileData tile = tiles[x, y];

                int mineCount = 0;

                // 주변 8칸 순회 (dx, dy: -1 ~ 1)
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        // 자기 자신은 제외
                        if (dx == 0 && dy == 0)
                            continue;

                        int nx = x + dx;
                        int ny = y + dy;

                        // 범위 체크
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                            continue;

                        TileData neighbor = tiles[nx, ny];
                        if (neighbor.hasMine)
                        {
                            mineCount++;
                        }
                    }
                }

                tile.surroundingMineCount = mineCount;
            }
        }

        Debug.Log("[TileManager] 주변 지뢰 개수 계산 완료.");
    }
    // 외부에서 호출하는 "타일 열기" 함수
    // - 지뢰면 무시 (지뢰 처리 로직은 TileInputController에서 담당)
    // - 이미 열린 칸이면 무시
    // - 주변 지뢰가 0이면 주변 칸들을 재귀적으로 계속 열어줌
    public void OpenTile(Vector2Int pos, bool giveResource = true, bool countForProgress = true)
    {
        TileData tile = GetTile(pos);
        if (tile == null)
            return;
        // ★ 플래그가 꽂혀 있으면 열지 않음
        if (tile.state == TileState.Flagged)
            return;
        // 지뢰는 여기서 열지 않음 (안전장치)
        if (tile.hasMine)
            return;

        // 이미 열린 칸이면 다시 처리하지 않음
        if (tile.state == TileState.Open)
            return;

        // 이 칸을 연다
        tile.state = TileState.Open;
        tileView.RenderTile(tile);
        // -------------------- 진행도/트리거 처리 --------------------
        // 지뢰는 애초에 열리지 않으므로 openedTileCount는 "지뢰가 아닌 열린 칸"만 센다.
        openedTileCount++;

        // 시작 이후 플레이어가 연 타일만 진행도에 포함
        if (countForProgress)
        {
            float ratio = ProgressOpenRatio;
            OnTileOpened?.Invoke(tile, ratio);
        }
        // ---------------------------------------------------------
        
        
        // 주변에 지뢰가 하나라도 있으면 여기까지만 열고 종료
        if (tile.surroundingMineCount > 0)
            return;

        // 주변 지뢰가 0이면, 주변 8칸을 재귀적으로 열기
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                // 자기 자신은 제외
                if (dx == 0 && dy == 0)
                    continue;

                Vector2Int neighborPos = new Vector2Int(pos.x + dx, pos.y + dy);

                TileData neighbor = GetTile(neighborPos);
                if (neighbor == null)
                    continue;

                // 지뢰가 있으면 열지 않음
                if (neighbor.hasMine)
                    continue;

                // 재귀 호출: 이미 열린 칸이면 내부에서 바로 return 되므로 무한루프 없음
                OpenTile(neighborPos, giveResource, countForProgress);
            }
        }
    }
    
    // 중앙 안전 구역(예: 4x4, 5x5)을 자동으로 여는 함수
    private void OpenSafeCenterRegion()
    {
        GetSafeCenterBounds(out int minX, out int maxX, out int minY, out int maxY);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                // 범위 밖 보호 (혹시 width/height가 작을 때 대비)
                if (x < 0 || x >= width || y < 0 || y >= height)
                    continue;

                TileData tile = tiles[x, y];

                // 안전 구역에는 지뢰가 없도록 PlaceMines에서 이미 막았지만,
                // 혹시 모를 상황에 대비해 hasMine이 true면 열지 않음
                if (tile.hasMine)
                    continue;

                // [중요] 시작 안전구역은
                //  - 자원 지급 X (giveResource = false)
                //  - 진행도/트리거에 포함 X (countForProgress = false)
                OpenTile(new Vector2Int(x, y), giveResource: false, countForProgress: false);
            }
        }
        // [중요] 여기까지 열린 타일 수를 "초기 오픈"으로 기록
        initialOpenedCount = openedTileCount;

        Debug.Log("[TileManager] 중앙 안전 구역 자동 오픈 완료.");
    }
    public void ToggleFlag(Vector2Int pos)
    {
        // 1. 좌표에 해당하는 타일 데이터 가져오기
        TileData tile = GetTile(pos);
        if (tile == null)
            return;

        // 2. 이미 열린 칸(Open)은 플래그 불가
        if (tile.state == TileState.Open)
            return;

        // 3. 상태 토글: Closed <-> Flagged
        if (tile.state == TileState.Flagged)
        {
            // 이미 플래그가 있으면 제거 → 다시 닫힌 상태
            tile.state = TileState.Closed;
        }
        else
        {
            // 닫혀 있거나 Buildable 등의 상태라면 플래그로 표시
            tile.state = TileState.Flagged;
        }

        // 4. 화면 갱신
        tileView.RenderTile(tile);
    }
    

}
