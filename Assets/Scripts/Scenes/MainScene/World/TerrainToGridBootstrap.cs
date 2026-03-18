using MinerUnity.Terrain;
using UnityEngine;

/// <summary>
/// Міст між твоєю поточною генерацією світу і новою логічною сіткою.
/// Цей скрипт не генерує світ сам.
/// Його задача — дочекатися появи runtime світу і позначити grid facade як готовий.
/// </summary>
public sealed class TerrainToGridBootstrap : MonoBehaviour
{
    [SerializeField] private WorldGridService worldGrid;
    [SerializeField] private ChunkManager chunkManager;

    private bool isSynced = false;

    private void Update()
    {
        if (isSynced) return;
        
        if (chunkManager != null && chunkManager.GetWorldRuntime() != null)
        {
            SyncWorldToGrid();
            isSynced = true;
        }
    }

    public void SyncWorldToGrid()
    {
        if (worldGrid == null || chunkManager == null || chunkManager.GetWorldRuntime() == null)
        {
            return;
        }

        Debug.Log("[TerrainToGridBootstrap] Runtime world is ready. Marking grid facade ready...");

        worldGrid.Initialize(chunkManager.GetWorldRuntime().Width, chunkManager.GetWorldRuntime().Height);

        worldGrid.MarkReady();
        Debug.Log("[TerrainToGridBootstrap] Grid facade ready.");
    }
}
