using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileView : MonoBehaviour
{
    [Header("Tile Sprites")]
    [SerializeField] private Sprite closedTileSprite;
    [SerializeField] private Sprite openTileSprite;
    [SerializeField] private Sprite flagTileSprite;   // ★ 추가된 플래그 타일 스프라이트

    [Header("Number Prefab")]
    [SerializeField] private TileNumberView numberPrefab;

    private Tilemap tilemap;

    private Tile closedTile;
    private Tile openTile;
    private Tile flagTile;                           // ★ 추가된 플래그 Tile 객체

    // 숫자 오브젝트 관리
    private Dictionary<Vector2Int, TileNumberView> numberViews =
        new Dictionary<Vector2Int, TileNumberView>();

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();

        // 닫힌 타일
        closedTile = ScriptableObject.CreateInstance<Tile>();
        closedTile.sprite = closedTileSprite;

        // 열린 타일
        openTile = ScriptableObject.CreateInstance<Tile>();
        openTile.sprite = openTileSprite;

        // ★ 플래그 타일 생성
        flagTile = ScriptableObject.CreateInstance<Tile>();
        flagTile.sprite = flagTileSprite;
    }

    public void RenderTile(TileData data)
    {
        Tile tileToRender = closedTile;
        Color tileColor = Color.white;

        // 숫자 숨기기
        HideNumberIfExists(data.position);

        switch (data.state)
        {
            case TileState.Closed:
                tileToRender = closedTile;
                tileColor = Color.white;
                break;

            case TileState.Flagged:
                tileToRender = flagTile;
                tileColor = Color.white;
                break;

            case TileState.Open:
                if (!data.hasMine)
                {
                    tileToRender = openTile;
                    tileColor = new Color(0.8f, 0.8f, 0.8f, 1f);

                    if (data.surroundingMineCount > 0)
                        ShowNumber(data);
                }
                else
                {
                    tileToRender = openTile;
                    tileColor = Color.red;
                }
                break;
        }

        Vector3Int cellPos = new Vector3Int(data.position.x, data.position.y, 0);

        // 1) 먼저 타일을 놓고
        tilemap.SetTile(cellPos, tileToRender);

        // 2) 그 다음 LockColor 풀고
        tilemap.SetTileFlags(cellPos, TileFlags.None);

        // 3) 마지막에 색 적용
        tilemap.SetColor(cellPos, tileColor);

    }

    private void ShowNumber(TileData data)
    {
        Vector2Int pos = data.position;

        if (!numberViews.TryGetValue(pos, out TileNumberView numberView) || numberView == null)
        {
            Vector3Int cellPos = new Vector3Int(pos.x, pos.y, 0);
            Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);

            numberView = Instantiate(numberPrefab, worldPos, Quaternion.identity, transform);
            numberView.transform.position += new Vector3(0f, 0f, -0.1f);

            numberViews[pos] = numberView;
        }

        numberView.SetNumber(data.surroundingMineCount);
    }

    private void HideNumberIfExists(Vector2Int pos)
    {
        if (numberViews.TryGetValue(pos, out TileNumberView numberView) && numberView != null)
        {
            Destroy(numberView.gameObject);
            numberViews[pos] = null;
        }
    }
    public Vector3 GetTileWorldPosition(Vector2Int gridPos)
    {
        Vector3Int cellPos = new Vector3Int(gridPos.x, gridPos.y, 0);
        return tilemap.GetCellCenterWorld(cellPos);
    }
    // ----------------------------------------------
    // 월드 좌표 → 타일 그리드 좌표 변환 함수 (TileManager에서 사용)
    // ----------------------------------------------
    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        if (tilemap == null)
            return Vector3Int.zero;

        return tilemap.WorldToCell(worldPos);
    }

    public Vector3 GetCellCenterWorld(Vector3Int cellPos)
    {
        if (tilemap == null)
            return Vector3.zero;

        return tilemap.GetCellCenterWorld(cellPos);
    }
}
