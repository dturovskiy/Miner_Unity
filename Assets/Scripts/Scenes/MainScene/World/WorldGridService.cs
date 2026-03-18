using MinerUnity.Runtime;
using MinerUnity.Terrain;
using UnityEngine;

public sealed class WorldGridService : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int width = 100;
    [SerializeField] private int height = 255;

    // Для цього проєкту в MainScene тайлова сітка фактично 1x1.
    // Тому логічна сітка героя теж повинна бути 1f,
    // інакше колізії читатимуться з неправильних клітин.
    [SerializeField] private float cellSize = 1f;

    [SerializeField] private Vector2 origin = Vector2.zero;
    [SerializeField] private ChunkManager chunkManager;

    private WorldCellType[,] cells;
    private bool isReady = false;
    private WorldRuntime worldRuntime;

    public static WorldGridService Instance { get; private set; }

    public bool IsReady => isReady && GetWorldRuntime() != null;
    public float CellSize => cellSize;
    public Vector2 Origin => origin;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        chunkManager ??= FindFirstObjectByType<ChunkManager>();

        if (cells == null)
        {
            Initialize(width, height);
        }
    }

    public void Initialize(int w, int h)
    {
        // Важливо: під час повторного заповнення сітки
        // не даємо motor читати напівготові дані.
        isReady = false;

        width = w;
        height = h;
        cells = new WorldCellType[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = WorldCellType.Empty;
            }
        }
    }

    public void MarkReady()
    {
        isReady = true;
    }

    public Vector2Int WorldToCell(Vector2 worldPosition)
    {
        // При cellSize = 1f ця логіка збігається з Tilemap/Grid і ChunkManager.
        int x = Mathf.FloorToInt((worldPosition.x - origin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPosition.y - origin.y) / cellSize);

        return new Vector2Int(x, y);
    }

    public float GetCellTopY(int y)
    {
        return origin.y + (y + 1) * cellSize;
    }

    public bool IsInsideBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }

    public Vector2 CellToWorldCenter(Vector2Int cell)
    {
        return origin + new Vector2(
            (cell.x + 0.5f) * cellSize,
            (cell.y + 0.5f) * cellSize
        );
    }

    public float GetCellBottomY(int y)
    {
        return origin.y + y * cellSize;
    }

    public WorldCellType GetCellType(Vector2Int cell)
    {
        if (!IsInsideBounds(cell))
        {
            if (cell.y >= height) return WorldCellType.Empty;
            return WorldCellType.Stone;
        }

        WorldRuntime runtime = GetWorldRuntime();
        if (isReady && runtime != null)
        {
            return MapTileIdToCellType(runtime.GetTile(cell.x, cell.y));
        }

        return cells[cell.x, cell.y];
    }

    public void SetCellType(Vector2Int cell, WorldCellType newType)
    {
        if (!IsInsideBounds(cell)) return;

        WorldRuntime runtime = GetWorldRuntime();
        if (isReady && runtime != null && TryMapCellTypeToTileId(newType, out TileID tileId))
        {
            runtime.TryPlaceTile(cell.x, cell.y, tileId);
        }

        cells[cell.x, cell.y] = newType;
    }

    public bool IsSolid(Vector2Int cell) => WorldCellRules.IsSolid(GetCellType(cell));
    public bool IsPassable(Vector2Int cell) => WorldCellRules.IsPassable(GetCellType(cell));
    public bool IsClimbable(Vector2Int cell) => WorldCellRules.IsClimbable(GetCellType(cell));
    public bool IsMineable(Vector2Int cell) => WorldCellRules.IsMineable(GetCellType(cell));

    private WorldRuntime GetWorldRuntime()
    {
        if (worldRuntime != null)
        {
            return worldRuntime;
        }

        if (chunkManager == null)
        {
            chunkManager = FindFirstObjectByType<ChunkManager>();
        }

        if (chunkManager != null)
        {
            worldRuntime = chunkManager.GetWorldRuntime();
        }

        return worldRuntime;
    }

    private static WorldCellType MapTileIdToCellType(TileID id)
    {
        switch (id)
        {
            case TileID.Empty:
            case TileID.Tunnel:
                return WorldCellType.Empty;

            case TileID.Dirt:
            case TileID.Coal:
            case TileID.Iron:
            case TileID.Gold:
            case TileID.Diamond:
            case TileID.Uranus:
            case TileID.Topaz:
            case TileID.Silver:
            case TileID.Ruby:
            case TileID.Platinum:
            case TileID.Opal:
            case TileID.Nephritis:
            case TileID.Map:
            case TileID.Lazurite:
            case TileID.Emerald:
            case TileID.Artifact:
            case TileID.Amethyst:
                return WorldCellType.Dirt;

            case TileID.Stone:
            case TileID.Edge:
                return WorldCellType.Stone;

            case TileID.Ladder:
                return WorldCellType.Ladder;

            default:
                return WorldCellType.Empty;
        }
    }

    private static bool TryMapCellTypeToTileId(WorldCellType type, out TileID tileId)
    {
        switch (type)
        {
            case WorldCellType.Empty:
                tileId = TileID.Empty;
                return true;

            case WorldCellType.Dirt:
                tileId = TileID.Dirt;
                return true;

            case WorldCellType.Stone:
                tileId = TileID.Stone;
                return true;

            case WorldCellType.Ladder:
                tileId = TileID.Ladder;
                return true;

            default:
                tileId = TileID.Empty;
                return false;
        }
    }
}
