using System.Collections.Generic;
using UnityEngine;

public class BarracksTower : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private int tilesPerSpawn = 4;
    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private float detectRangeTiles = 8f;

    private TileManager tileManager;
    private Vector2Int tilePos;
    private int openedCount = 0;

    private bool initialized = false;

    public void Initialize(Vector2Int pos, TileManager manager)
    {
        tilePos = pos;
        tileManager = manager;
        initialized = true;
    }

    private void OnEnable()
    {
        if (tileManager == null)
        {
            tileManager = FindObjectOfType<TileManager>();
        }

        if (tileManager != null)
            tileManager.OnTileOpened += HandleTileOpened;
    }

    private void OnDisable()
    {
        if (tileManager != null)
            tileManager.OnTileOpened -= HandleTileOpened;
    }

    private void HandleTileOpened(TileData tile, float ratio)
    {
        if (!initialized)
            return;

        openedCount++;

        if (openedCount >= tilesPerSpawn)
        {
            openedCount -= tilesPerSpawn;
            SpawnUnit();
        }
    }

    private void SpawnUnit()
    {
        Vector2Int[] dirs = {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1)
        };

        List<Vector2Int> candidates = new List<Vector2Int>();

        foreach (var d in dirs)
        {
            Vector2Int pos = tilePos + d;
            TileData t = tileManager.GetTile(pos);

            if (tileManager.IsWalkable(t))
                candidates.Add(pos);
        }

        Vector2Int spawnTile = (candidates.Count > 0)
            ? candidates[Random.Range(0, candidates.Count)]
            : tilePos;

        Vector3 world = tileManager.tileView.GetTileWorldPosition(spawnTile);
        world.z = -2;

        GameObject obj = Instantiate(unitPrefab, world, Quaternion.identity);

        BarracksUnit unit = obj.GetComponent<BarracksUnit>();
        if (unit != null)
        {
            unit.Initialize(tileManager, tilePos, detectRangeTiles);
        }

        Debug.Log($"[BarracksTower] 병영 유닛 생성 at tile {spawnTile}");
    }
}
