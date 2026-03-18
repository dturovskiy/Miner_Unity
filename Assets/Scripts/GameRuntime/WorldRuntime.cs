using System;
using MinerUnity.Terrain;

namespace MinerUnity.Runtime
{
    /// <summary>
    /// Gameplay-facing API over the single mutable world state.
    /// This class is intentionally thin: it should wrap WorldData, not duplicate it.
    /// </summary>
    public sealed class WorldRuntime
    {
        private readonly WorldData worldData;
        private readonly StoneGravityService stoneGravityService;

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

        public bool TryPlaceTile(int x, int y, TileID tileId)
        {
            if (!worldData.IsValidCoordinate(x, y))
            {
                return false;
            }

            worldData.SetTile(x, y, tileId);
            return true;
        }

        public bool TryDestroyTile(int x, int y, out TileID previousTile)
        {
            previousTile = worldData.GetTile(x, y);
            if (previousTile is TileID.Empty or TileID.Edge)
            {
                return false;
            }

            worldData.SetTile(x, y, TileID.Empty);
            return true;
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
