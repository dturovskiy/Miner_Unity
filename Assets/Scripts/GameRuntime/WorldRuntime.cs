using System;
using System.Collections.Generic;
using MinerUnity.Terrain;

namespace MinerUnity.Runtime
{
    /// <summary>
    /// Gameplay-facing API over the single mutable world state.
    /// This class is intentionally thin: it should wrap WorldData, not duplicate it.
    /// </summary>
    public sealed class WorldRuntime
    {
        public readonly struct MiningHitResult
        {
            public MiningHitResult(int x, int y, TileID tileId, int hitsApplied, int hitsRequired, int crackStage, bool destroyed)
            {
                Cell = new UnityEngine.Vector2Int(x, y);
                TileId = tileId;
                HitsApplied = hitsApplied;
                HitsRequired = hitsRequired;
                CrackStage = crackStage;
                Destroyed = destroyed;
            }

            public UnityEngine.Vector2Int Cell { get; }
            public TileID TileId { get; }
            public int HitsApplied { get; }
            public int HitsRequired { get; }
            public int CrackStage { get; }
            public bool Destroyed { get; }
        }

        private readonly WorldData worldData;
        private readonly StoneGravityService stoneGravityService;
        private readonly Dictionary<UnityEngine.Vector2Int, int> miningHits = new();
        private int? highestTunnelRow;

        public WorldRuntime(WorldData worldData)
        {
            this.worldData = worldData ?? throw new ArgumentNullException(nameof(worldData));
            stoneGravityService = new StoneGravityService(worldData);
        }

        public WorldData WorldData => worldData;
        public StoneGravityService StoneGravityService => stoneGravityService;
        public int Width => worldData.Width;
        public int Height => worldData.Height;

        public bool LoadFromFile(string filePath)
        {
            return worldData.LoadFromFile(filePath);
        }

        public void SaveToFile(string filePath)
        {
            worldData.SaveToFile(filePath);
        }

        public TileID GetTile(int x, int y)
        {
            return worldData.GetTile(x, y);
        }

        public bool IsInsideBounds(int x, int y)
        {
            return worldData.IsValidCoordinate(x, y);
        }

        public bool IsMineable(int x, int y)
        {
            if (!worldData.IsValidCoordinate(x, y))
            {
                return false;
            }

            return WorldCellRules.IsMineable(worldData.GetTile(x, y));
        }

        public bool IsInsideMiningArea(int x, int y)
        {
            if (!worldData.IsValidCoordinate(x, y))
            {
                return false;
            }

            TileID tile = worldData.GetTile(x, y);
            if (tile == TileID.Tunnel || tile == TileID.Ladder)
            {
                return true;
            }

            if (HasAdjacentMiningAccess(x, y))
            {
                return true;
            }

            if (tile != TileID.Empty)
            {
                return false;
            }

            int tunnelRow = GetHighestTunnelRow();
            return tunnelRow >= 0 && y < tunnelRow;
        }

        public bool TryPlaceTile(int x, int y, TileID tileId)
        {
            if (!worldData.IsValidCoordinate(x, y))
            {
                Diag.Warning(
                    "World",
                    "MutationRejected",
                    "Tile placement rejected because the target cell is out of bounds.",
                    null,
                    ("operation", "PlaceTile"),
                    ("cell", new UnityEngine.Vector2Int(x, y)),
                    ("tile", tileId.ToString()),
                    ("reason", "outOfBounds"));
                return false;
            }

            TileID previousTile = worldData.GetTile(x, y);
            worldData.SetTile(x, y, tileId);
            ClearMiningHits(x, y);
            InvalidateCachedRowsIfNeeded(previousTile, tileId);
            return true;
        }

        public bool TryDestroyTile(int x, int y, out TileID previousTile)
        {
            previousTile = worldData.GetTile(x, y);
            if (!worldData.IsValidCoordinate(x, y))
            {
                Diag.Warning(
                    "World",
                    "MutationRejected",
                    "Tile destruction rejected because the target cell is out of bounds.",
                    null,
                    ("operation", "DestroyTile"),
                    ("cell", new UnityEngine.Vector2Int(x, y)),
                    ("reason", "outOfBounds"));
                return false;
            }

            if (previousTile is TileID.Empty or TileID.Edge)
            {
                Diag.Warning(
                    "World",
                    "MutationRejected",
                    "Tile destruction rejected because the target tile is not destroyable.",
                    null,
                    ("operation", "DestroyTile"),
                    ("cell", new UnityEngine.Vector2Int(x, y)),
                    ("tile", previousTile.ToString()),
                    ("reason", previousTile == TileID.Empty ? "emptyTile" : "edgeTile"));
                return false;
            }

            worldData.SetTile(x, y, TileID.Empty);
            ClearMiningHits(x, y);
            InvalidateCachedRowsIfNeeded(previousTile, TileID.Empty);
            return true;
        }

        public int GetMiningHits(int x, int y)
        {
            if (!worldData.IsValidCoordinate(x, y))
            {
                return 0;
            }

            return miningHits.TryGetValue(new UnityEngine.Vector2Int(x, y), out int hitsApplied)
                ? hitsApplied
                : 0;
        }

        public int SetMiningHits(int x, int y, int hitsApplied)
        {
            if (!worldData.IsValidCoordinate(x, y) || !IsMineable(x, y))
            {
                ClearMiningHits(x, y);
                return 0;
            }

            UnityEngine.Vector2Int key = new UnityEngine.Vector2Int(x, y);
            if (hitsApplied <= 0)
            {
                miningHits.Remove(key);
                return 0;
            }

            miningHits[key] = hitsApplied;
            return hitsApplied;
        }

        public bool TryApplyMiningHit(int x, int y, int hitsRequired, out MiningHitResult result)
        {
            result = default;

            if (!worldData.IsValidCoordinate(x, y))
            {
                Diag.Warning(
                    "World",
                    "MiningHitRejected",
                    "Mining hit rejected because the target cell is out of bounds.",
                    null,
                    ("cell", new UnityEngine.Vector2Int(x, y)),
                    ("hitsRequired", hitsRequired),
                    ("reason", "outOfBounds"));
                return false;
            }

            if (!IsMineable(x, y))
            {
                Diag.Warning(
                    "World",
                    "MiningHitRejected",
                    "Mining hit rejected because the target tile is not mineable.",
                    null,
                    ("cell", new UnityEngine.Vector2Int(x, y)),
                    ("tile", worldData.GetTile(x, y).ToString()),
                    ("hitsRequired", hitsRequired),
                    ("reason", "notMineable"));
                return false;
            }

            TileID tileId = worldData.GetTile(x, y);
            int normalizedHitsRequired = UnityEngine.Mathf.Max(1, hitsRequired);
            int hitsApplied = UnityEngine.Mathf.Min(GetMiningHits(x, y) + 1, normalizedHitsRequired);

            bool destroyed = hitsApplied >= normalizedHitsRequired;
            int crackStage = destroyed ? 0 : GetCrackStage(hitsApplied, normalizedHitsRequired);

            if (destroyed)
            {
                if (!TryDestroyTile(x, y, out _))
                {
                    Diag.Warning(
                        "World",
                        "MiningHitRejected",
                        "Mining hit could not finish because the target tile failed to destroy.",
                        null,
                        ("cell", new UnityEngine.Vector2Int(x, y)),
                        ("tile", tileId.ToString()),
                        ("hitsRequired", normalizedHitsRequired),
                        ("reason", "destroyFailed"));
                    return false;
                }
            }
            else
            {
                SetMiningHits(x, y, hitsApplied);
            }

            result = new MiningHitResult(x, y, tileId, hitsApplied, normalizedHitsRequired, crackStage, destroyed);
            return true;
        }

        public void ClearMiningHits(int x, int y)
        {
            miningHits.Remove(new UnityEngine.Vector2Int(x, y));
        }

        public List<MiningDamageData> CreateMiningDamageSnapshot()
        {
            var snapshot = new List<MiningDamageData>(miningHits.Count);
            foreach (var pair in miningHits)
            {
                if (!worldData.IsValidCoordinate(pair.Key.x, pair.Key.y) || !IsMineable(pair.Key.x, pair.Key.y) || pair.Value <= 0)
                {
                    continue;
                }

                snapshot.Add(new MiningDamageData
                {
                    cellX = pair.Key.x,
                    cellY = pair.Key.y,
                    hitsApplied = pair.Value
                });
            }

            return snapshot;
        }

        public void RestoreMiningDamage(IReadOnlyList<MiningDamageData> savedDamage)
        {
            miningHits.Clear();
            if (savedDamage == null)
            {
                return;
            }

            for (int i = 0; i < savedDamage.Count; i++)
            {
                MiningDamageData entry = savedDamage[i];
                if (entry == null || entry.hitsApplied <= 0)
                {
                    continue;
                }

                if (!worldData.IsValidCoordinate(entry.cellX, entry.cellY) || !IsMineable(entry.cellX, entry.cellY))
                {
                    continue;
                }

                miningHits[new UnityEngine.Vector2Int(entry.cellX, entry.cellY)] = entry.hitsApplied;
            }
        }

        public static int GetCrackStage(int hitsApplied, int hitsRequired)
        {
            if (hitsApplied <= 0 || hitsRequired <= 0)
            {
                return 0;
            }

            if (hitsRequired <= 1)
            {
                return 3;
            }

            return UnityEngine.Mathf.Clamp(UnityEngine.Mathf.CeilToInt(hitsApplied * 3f / hitsRequired), 1, 3);
        }

        private int GetHighestTunnelRow()
        {
            if (highestTunnelRow.HasValue)
            {
                return highestTunnelRow.Value;
            }

            int highest = -1;
            for (int y = 0; y < worldData.Height; y++)
            {
                for (int x = 0; x < worldData.Width; x++)
                {
                    if (worldData.GetTile(x, y) == TileID.Tunnel && y > highest)
                    {
                        highest = y;
                    }
                }
            }

            highestTunnelRow = highest;
            return highest;
        }

        private bool HasAdjacentMiningAccess(int x, int y)
        {
            return IsMiningAccessTile(x - 1, y)
                || IsMiningAccessTile(x + 1, y)
                || IsMiningAccessTile(x, y - 1)
                || IsMiningAccessTile(x, y + 1);
        }

        private bool IsMiningAccessTile(int x, int y)
        {
            if (!worldData.IsValidCoordinate(x, y))
            {
                return false;
            }

            TileID tile = worldData.GetTile(x, y);
            return tile == TileID.Tunnel || tile == TileID.Ladder;
        }

        private void InvalidateCachedRowsIfNeeded(TileID previousTile, TileID nextTile)
        {
            if (previousTile == TileID.Tunnel || nextTile == TileID.Tunnel)
            {
                highestTunnelRow = null;
            }
        }

        public bool CanStoneStartFalling(int x, int y)
        {
            return stoneGravityService.CanStoneStartFalling(x, y);
        }

        public bool TryMoveStoneToLanding(int x, int y, out StoneFallResult result)
        {
            return stoneGravityService.TryMoveStoneToLanding(x, y, out result);
        }
    }
}
