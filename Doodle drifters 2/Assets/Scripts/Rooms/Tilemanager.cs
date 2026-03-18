using Unity.VisualScripting;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 8;
    public int height = 8;
    public float tileSize = 1f;

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject enemyPrefab;
    public GameObject chestPrefab;

    private Tile[,] grid;
    private GameObject[,] visualGrid;

    [SerializeField] private Camera cam;

    public bool reset;

    void Start()
    {
        GenerateNewLevel();
    }

    private void Update()
    {
        if (reset)
        {
            GenerateNewLevel();
            reset = false;
        }
    }

    private void GenerateNewLevel()
    {
        ClearAll();

        GenerateRoom();
        SpawnVisuals();

        // Demo placements
        SetTile(1, Random.Range(1, 6), TileType.Chest);
        SetTile(Random.Range(2, 7), Random.Range(1, height - 1), TileType.Enemy);
    }

    private void ClearAll()
    {
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    void GenerateRoom()
    {
        grid = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileType type;

                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                    type = TileType.Wall;
                else
                    type = TileType.Floor;

                grid[x, y] = new Tile(x, y, type);
            }
        }

        cam.transform.position = new Vector3(width / 2, (height / 2) - 0.5f, -10);
    }

    void SpawnVisuals()
    {
        visualGrid = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SpawnTileVisual(x, y);
            }
        }
    }

    void SpawnTileVisual(int x, int y)
    {
        GameObject prefab = GetPrefab(grid[x, y].type);

        Vector2 position = new Vector2(x * tileSize, y * tileSize);
        visualGrid[x, y] = Instantiate(prefab, position, Quaternion.identity, transform);
    }

    public void SetTile(int x, int y, TileType newType)
    {
        if (!IsInsideGrid(x, y)) return;

        grid[x, y].type = newType;

        Destroy(visualGrid[x, y]);
        SpawnTileVisual(x, y);
    }

    bool IsInsideGrid(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    GameObject GetPrefab(TileType type)
    {
        switch (type)
        {
            case TileType.Wall: return wallPrefab;
            case TileType.Enemy: return enemyPrefab;
            case TileType.Chest: return chestPrefab;
            default: return floorPrefab;
        }
    }
}