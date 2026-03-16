using UnityEngine;

namespace MinerUnity.Terrain
{
    /// <summary>
    /// Результат фактичного переміщення одного каменю.
    /// </summary>
    public readonly struct StoneFallResult
    {
        public readonly Vector2Int From;
        public readonly Vector2Int To;

        public StoneFallResult(Vector2Int from, Vector2Int to)
        {
            From = from;
            To = to;
        }
    }

    /// <summary>
    /// Unity 6.3.
    /// Чиста логіка гріда без MonoBehaviour.
    /// Тут немає ні таймерів, ні візуалу, ні камери.
    /// </summary>
    public sealed class StoneGravityService
    {
        private readonly WorldData worldData;

        public StoneGravityService(WorldData worldData)
        {
            this.worldData = worldData;
        }

        /// <summary>
        /// Перевіряє, чи стоїть у клітинці камінь і чи втратив він опору.
        /// </summary>
        public bool CanStoneStartFalling(int x, int y)
        {
            if (worldData.GetTile(x, y) != TileID.Stone)
            {
                return false;
            }

            TileID below = worldData.GetTile(x, y - 1);

            // Камінь може почати падіння лише якщо під ним прохідна клітинка.
            return below == TileID.Empty || below == TileID.Tunnel;
        }

        /// <summary>
        /// Шукає найнижчу клітинку, куди може впасти камінь.
        /// </summary>
        public int FindLandingY(int x, int startY)
        {
            int y = startY;

            while (y > 0)
            {
                TileID below = worldData.GetTile(x, y - 1);

                if (below != TileID.Empty && below != TileID.Tunnel)
                {
                    break;
                }

                y--;
            }

            return y;
        }

        /// <summary>
        /// Реально зсуває один камінь у гріді вниз.
        /// Повертає false, якщо камінь уже не повинен падати.
        /// </summary>
        public bool TryMoveStoneToLanding(int x, int y, out StoneFallResult result)
        {
            result = default;

            if (!CanStoneStartFalling(x, y))
            {
                return false;
            }

            int landingY = FindLandingY(x, y);

            if (landingY == y)
            {
                return false;
            }

            worldData.SetTile(x, y, TileID.Empty);
            worldData.SetTile(x, landingY, TileID.Stone);

            result = new StoneFallResult(
                new Vector2Int(x, y),
                new Vector2Int(x, landingY)
            );

            return true;
        }
    }
}
