using MinerUnity.Runtime;
using MinerUnity.Terrain;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HeroDebugDigTool : MonoBehaviour
{
    [Header("Debug Dig Keys")]
    [SerializeField] private KeyCode digUpKey = KeyCode.I;
    [SerializeField] private KeyCode digDownKey = KeyCode.K;
    [SerializeField] private KeyCode digLeftKey = KeyCode.J;
    [SerializeField] private KeyCode digRightKey = KeyCode.L;

    private HeroCollision heroCollision;
    private ChunkManager chunkManager;
    private WorldGridService worldGrid;

    private void Awake()
    {
        heroCollision = GetComponent<HeroCollision>();
        chunkManager = FindFirstObjectByType<ChunkManager>();
        worldGrid = WorldGridService.Instance;
    }

    private void Update()
    {
        if (!IsDebugToolEnabled())
        {
            return;
        }

        if (!TryGetDirectionInput(out Vector2Int direction, out string directionName))
        {
            return;
        }

        TryDestroyNeighbor(direction, directionName);
    }

    private void TryDestroyNeighbor(Vector2Int direction, string directionName)
    {
        heroCollision ??= GetComponent<HeroCollision>();
        chunkManager ??= FindFirstObjectByType<ChunkManager>();
        worldGrid ??= WorldGridService.Instance;

        if (heroCollision == null || chunkManager == null || worldGrid == null || !worldGrid.IsReady)
        {
            Diag.Warning(
                "Debug",
                "DigBlocked",
                "Debug dig skipped because runtime references are not ready.",
                this,
                ("direction", directionName),
                ("hasHeroCollision", heroCollision != null),
                ("hasChunkManager", chunkManager != null),
                ("hasWorldGrid", worldGrid != null),
                ("worldReady", worldGrid != null && worldGrid.IsReady));
            return;
        }

        Vector2Int currentCell = heroCollision.GetCurrentCell();
        Vector2Int targetCell = currentCell + direction;
        if (!worldGrid.IsInsideBounds(targetCell))
        {
            Diag.Warning(
                "Debug",
                "DigBlocked",
                "Debug dig target is outside world bounds.",
                this,
                ("direction", directionName),
                ("currentCell", currentCell),
                ("targetCell", targetCell));
            return;
        }

        WorldRuntime runtime = chunkManager.GetWorldRuntime();
        if (runtime == null)
        {
            Diag.Warning(
                "Debug",
                "DigBlocked",
                "Debug dig skipped because world runtime is missing.",
                this,
                ("direction", directionName),
                ("targetCell", targetCell));
            return;
        }

        TileID targetTile = runtime.GetTile(targetCell.x, targetCell.y);
        if (targetTile is TileID.Empty or TileID.Edge)
        {
            Diag.Warning(
                "Debug",
                "DigBlocked",
                "Debug dig target is not destructible.",
                this,
                ("direction", directionName),
                ("targetCell", targetCell),
                ("tile", targetTile.ToString()));
            return;
        }

        Diag.Event(
            "Debug",
            "DigRequested",
            "Debug dig requested for a neighbor tile.",
            this,
            ("direction", directionName),
            ("currentCell", currentCell),
            ("targetCell", targetCell),
            ("tile", targetTile.ToString()));

        chunkManager.DestroyTileInWorld(targetCell.x, targetCell.y);

        Diag.Event(
            "Debug",
            "DigCompleted",
            "Debug dig destroyed a neighbor tile through runtime.",
            this,
            ("direction", directionName),
            ("targetCell", targetCell),
            ("tile", targetTile.ToString()));
    }

    private bool TryGetDirectionInput(out Vector2Int direction, out string directionName)
    {
        if (Input.GetKeyDown(digUpKey))
        {
            direction = Vector2Int.up;
            directionName = "Up";
            return true;
        }

        if (Input.GetKeyDown(digDownKey))
        {
            direction = Vector2Int.down;
            directionName = "Down";
            return true;
        }

        if (Input.GetKeyDown(digLeftKey))
        {
            direction = Vector2Int.left;
            directionName = "Left";
            return true;
        }

        if (Input.GetKeyDown(digRightKey))
        {
            direction = Vector2Int.right;
            directionName = "Right";
            return true;
        }

        direction = Vector2Int.zero;
        directionName = string.Empty;
        return false;
    }

    private static bool IsDebugToolEnabled()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return true;
#else
        return false;
#endif
    }
}
