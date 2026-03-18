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

        var runtime = chunkManager.GetWorldRuntime();
        worldGrid.Initialize(runtime.Width, runtime.Height);

        worldGrid.MarkReady();

        Diag.Event(
            "World",
            "WorldGridReady",
            "World grid facade is now ready and aligned to runtime dimensions.",
            this,
            ("width", runtime.Width),
            ("height", runtime.Height),
            ("cellSize", worldGrid.CellSize),
            ("origin", worldGrid.Origin));
    }
}
