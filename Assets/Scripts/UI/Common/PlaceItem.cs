using MinerUnity.Runtime;
using MinerUnity.Terrain;
using UnityEngine;

public sealed class PlaceItem : MonoBehaviour
{
    [SerializeField] private HeroCollision heroCollision;
    [SerializeField] private ChunkManager chunkManager;

    public void PlaceLadder()
    {
        ResolveReferences();
        WorldRuntime runtime = chunkManager != null ? chunkManager.GetWorldRuntime() : null;

        if (heroCollision == null || chunkManager == null || runtime == null || !heroCollision.IsWorldReady())
        {
            Diag.Warning(
                "UI",
                "PlaceLadderBlocked",
                "Ladder placement skipped because runtime references are not ready.",
                this,
                ("hasHeroCollision", heroCollision != null),
                ("hasChunkManager", chunkManager != null),
                ("hasWorldRuntime", runtime != null));
            return;
        }

        Vector2Int targetCell = heroCollision.TryGetOpenAnchorCell(out Vector2Int openCell)
            ? openCell
            : heroCollision.GetCurrentCell();

        if (!runtime.IsInsideBounds(targetCell.x, targetCell.y))
        {
            Diag.Warning(
                "UI",
                "PlaceLadderBlocked",
                "Ladder placement target is outside world bounds.",
                this,
                ("targetCell", targetCell));
            return;
        }

        TileID currentTile = runtime.GetTile(targetCell.x, targetCell.y);
        if (currentTile == TileID.Ladder)
        {
            Diag.Event(
                "UI",
                "PlaceLadderIgnored",
                "Ladder placement ignored because a ladder already exists in the hero cell.",
                this,
                ("targetCell", targetCell));
            return;
        }

        if (currentTile != TileID.Empty && currentTile != TileID.Tunnel)
        {
            Diag.Warning(
                "UI",
                "PlaceLadderBlocked",
                "Ladder can only be placed in the hero passable cell.",
                this,
                ("targetCell", targetCell),
                ("tile", currentTile.ToString()));
            return;
        }

        chunkManager.PlaceTileInWorld(targetCell.x, targetCell.y, TileID.Ladder);

        Diag.Event(
            "UI",
            "PlaceLadderCompleted",
            "Ladder was placed in the hero cell.",
            this,
            ("targetCell", targetCell));
    }

    private void ResolveReferences()
    {
        heroCollision ??= FindFirstObjectByType<HeroCollision>();
        chunkManager ??= FindFirstObjectByType<ChunkManager>();
    }
}
