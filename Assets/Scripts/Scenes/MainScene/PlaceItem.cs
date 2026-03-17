using UnityEngine;

/// <summary>
/// Постановка драбини через логічну сітку.
/// </summary>
public sealed class PlaceItem : MonoBehaviour
{
    [SerializeField] private WorldGridService worldGrid;

    private void Awake()
    {
        if (worldGrid == null) worldGrid = WorldGridService.Instance;
    }

    private void Start()
    {
        if (worldGrid == null) worldGrid = WorldGridService.Instance;
    }

    /// <summary>
    /// Цей метод викликається з UI-кнопки (Unity Event).
    /// </summary>
    public void PlaceLadder()
    {
        // Використовуємо позицію ніг героя
        TryPlaceLadderAtFeet(transform.position);
    }

    public bool TryPlaceLadderAtFeet(Vector2 feetWorldPosition)
    {
        if (worldGrid == null) worldGrid = WorldGridService.Instance;
        
        if (worldGrid == null)
        {
            Debug.LogError("[PlaceItem] WorldGridService is missing!");
            return false;
        }

        Vector2Int cell = worldGrid.WorldToCell(feetWorldPosition);
        WorldCellType currentType = worldGrid.GetCellType(cell);

        // Драбину дозволяємо ставити тільки в passable-простір.
        if (currentType != WorldCellType.Empty && currentType != WorldCellType.Cave)
        {
            return false;
        }

        worldGrid.SetCellType(cell, WorldCellType.Ladder);
        
        var chunkManager = Object.FindFirstObjectByType<MinerUnity.Terrain.ChunkManager>();
        if (chunkManager != null)
        {
            chunkManager.PlaceTileInWorld(cell.x, cell.y, MinerUnity.Terrain.TileID.Ladder);
        }

        return true;
    }

    public bool TryRemoveLadder(Vector2 worldPosition)
    {
        if (worldGrid == null) worldGrid = WorldGridService.Instance;
        
        if (worldGrid == null) return false;

        Vector2Int cell = worldGrid.WorldToCell(worldPosition);

        if (worldGrid.GetCellType(cell) != WorldCellType.Ladder)
        {
            return false;
        }

        worldGrid.SetCellType(cell, WorldCellType.Empty);
        
        var chunkManager = Object.FindFirstObjectByType<MinerUnity.Terrain.ChunkManager>();
        if (chunkManager != null)
        {
            chunkManager.DestroyTileInWorld(cell.x, cell.y);
        }

        return true;
    }
}