using UnityEngine;

/// <summary>
/// Постановка драбини через логічну сітку.
/// Не інстансить ladder prefab як джерело істини.
/// Спочатку змінює дані grid, а візуал оновлюється окремо.
/// </summary>
public sealed class PlaceItem : MonoBehaviour
{
    [SerializeField] private WorldGridService worldGrid;

    /// <summary>
    /// Ставить драбину в клітинку, де зараз знаходиться точка feet.
    /// Адаптуй цю точку під твою механіку інвентаря / будівництва.
    /// </summary>
    public bool TryPlaceLadderAtFeet(Vector2 feetWorldPosition)
    {
        if (worldGrid == null)
        {
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
        if (worldGrid == null)
        {
            return false;
        }

        Vector2Int cell = worldGrid.WorldToCell(worldPosition);

        if (worldGrid.GetCellType(cell) != WorldCellType.Ladder)
        {
            return false;
        }

        worldGrid.SetCellType(cell, WorldCellType.Empty);
        return true;
    }
}