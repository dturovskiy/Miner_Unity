using UnityEngine;
using MinerUnity.Terrain;

/// <summary>
/// Міст між твоєю поточною генерацією світу і новою логічною сіткою.
/// Цей скрипт не генерує світ сам.
/// Його задача — записати результат завантаження або генерації в WorldGridService.
/// </summary>
public sealed class TerrainToGridBootstrap : MonoBehaviour
{
    [SerializeField] private WorldGridService worldGrid;
    [SerializeField] private ChunkManager chunkManager;

    private bool isSynced = false;

    private void Update()
    {
        if (isSynced) return;
        
        if (chunkManager != null)
        {
            WorldData data = chunkManager.GetWorldData();
            if (data != null && data.Width > 0)
            {
                SyncWorldToGrid();
                isSynced = true;
            }
        }
    }

    public void SyncWorldToGrid()
    {
        WorldData data = chunkManager.GetWorldData();
        if (data == null) return;

        Debug.Log($"[TerrainToGridBootstrap] Syncing {data.Width}x{data.Height} map to grid...");

        // Переініціалізуємо сітку під реальний розмір даних
        worldGrid.Initialize(data.Width, data.Height);

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                TileID id = data.GetTile(x, y);
                WorldCellType type = MapTileIDToWorldCellType(id);
                worldGrid.SetCellType(new Vector2Int(x, y), type);
            }
        }

        worldGrid.MarkReady();
        Debug.Log("[TerrainToGridBootstrap] Sync complete!");
    }

    private WorldCellType MapTileIDToWorldCellType(TileID id)
    {
        switch (id)
        {
            case TileID.Empty: return WorldCellType.Empty;
            case TileID.Tunnel: return WorldCellType.Empty;
            
            case TileID.Dirt: return WorldCellType.Dirt;
            case TileID.Coal: return WorldCellType.Dirt;
            case TileID.Iron: return WorldCellType.Dirt;
            case TileID.Gold: return WorldCellType.Dirt;
            case TileID.Diamond: return WorldCellType.Dirt;
            case TileID.Uranus: return WorldCellType.Dirt;
            case TileID.Topaz: return WorldCellType.Dirt;
            case TileID.Silver: return WorldCellType.Dirt;
            case TileID.Ruby: return WorldCellType.Dirt;
            case TileID.Platinum: return WorldCellType.Dirt;
            case TileID.Opal: return WorldCellType.Dirt;
            case TileID.Nephritis: return WorldCellType.Dirt;
            case TileID.Map: return WorldCellType.Dirt;
            case TileID.Lazurite: return WorldCellType.Dirt;
            case TileID.Emerald: return WorldCellType.Dirt;
            case TileID.Artifact: return WorldCellType.Dirt;
            case TileID.Amethyst: return WorldCellType.Dirt;

            case TileID.Stone: return WorldCellType.Stone;
            case TileID.Edge: return WorldCellType.Stone;

            case TileID.Ladder: return WorldCellType.Ladder;

            default: return WorldCellType.Empty;
        }
    }
}
