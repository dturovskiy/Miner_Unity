using UnityEngine;

/// <summary>
/// Грід-базована постановка драбин.
/// </summary>
public sealed class PlaceItem : MonoBehaviour
{
    private WorldGridService worldGrid;

    private void Awake()
    {
        worldGrid = WorldGridService.Instance;
    }

    /// <summary>
    /// Метод для виклику з UI Кнопки через Unity Event.
    /// </summary>
    public void PlaceLadder()
    {
        if (worldGrid == null) worldGrid = WorldGridService.Instance;
        if (worldGrid == null) return;

        // Ставимо драбину там, де стоїть герой
        Vector2Int cell = worldGrid.WorldToCell(transform.position);
        
        // Можна ставити тільки в пустоту
        if (worldGrid.GetCellType(cell) == WorldCellType.Empty)
        {
            worldGrid.SetCellType(cell, WorldCellType.Ladder);
            
            // Синхронізація з візуалом
            var chunkManager = Object.FindFirstObjectByType<MinerUnity.Terrain.ChunkManager>();
            if (chunkManager != null)
            {
                chunkManager.PlaceTileInWorld(cell.x, cell.y, MinerUnity.Terrain.TileID.Ladder);
            }
        }
    }
}