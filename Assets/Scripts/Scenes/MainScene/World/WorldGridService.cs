using System;
using UnityEngine;

/// <summary>
/// Логічна сітка світу.
/// Саме тут зберігається, що знаходиться в кожній клітинці.
/// Не prefab-об'єкти, не trigger-колайдери, не HeroState.
/// </summary>
public sealed class WorldGridService : MonoBehaviour
{
    public static WorldGridService Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private int width = 100;
    [SerializeField] private int height = 255;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 origin = Vector2.zero;

    private WorldCellType[,] cells;
    private bool isReady = false;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;
    public Vector2 Origin => origin;
    public bool IsReady => isReady;

    public event Action<Vector2Int, WorldCellType, WorldCellType> OnCellChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (cells == null)
        {
            Initialize(width, height);
        }
    }

    public void Initialize(int w, int h)
    {
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

    public bool IsInsideBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }

    public Vector2Int WorldToCell(Vector2 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x - origin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPosition.y - origin.y) / cellSize);
        return new Vector2Int(x, y);
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

    public float GetCellTopY(int y)
    {
        return origin.y + (y + 1) * cellSize;
    }

    public WorldCellType GetCellType(Vector2Int cell)
    {
        if (!IsInsideBounds(cell))
        {
            // Вище карти — порожньо, боки і низ — камінь.
            if (cell.y >= height)
            {
                return WorldCellType.Empty;
            }

            return WorldCellType.Stone;
        }

        return cells[cell.x, cell.y];
    }

    public void SetCellType(Vector2Int cell, WorldCellType newType)
    {
        if (!IsInsideBounds(cell))
        {
            return;
        }

        WorldCellType oldType = cells[cell.x, cell.y];

        if (oldType == newType)
        {
            return;
        }

        cells[cell.x, cell.y] = newType;
        OnCellChanged?.Invoke(cell, oldType, newType);
    }

    public bool IsSolid(Vector2Int cell)
    {
        return WorldCellRules.IsSolid(GetCellType(cell));
    }

    public bool IsPassable(Vector2Int cell)
    {
        return WorldCellRules.IsPassable(GetCellType(cell));
    }

    public bool IsClimbable(Vector2Int cell)
    {
        return WorldCellRules.IsClimbable(GetCellType(cell));
    }

    public bool IsMineable(Vector2Int cell)
    {
        return WorldCellRules.IsMineable(GetCellType(cell));
    }
}
