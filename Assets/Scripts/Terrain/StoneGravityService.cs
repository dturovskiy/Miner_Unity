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
            TileID tile = worldData.GetTile(x, y);
            if (tile != TileID.Stone)
            {
                Diag.Event(
                    "Stone",
                    "StartCheckRejected",
                    "Stone start fall rejected: source tile is not stone.",
                    fields: new (string key, object value)[]
                    {
                        ("x", x),
                        ("y", y),
                        ("tile", tile)
                    }
                );
                return false;
            }

            TileID below = worldData.GetTile(x, y - 1);

            // Камінь може почати падіння лише якщо під ним прохідна клітинка.
            bool canFall = below == TileID.Empty || below == TileID.Tunnel;
            if (!canFall)
            {
                Diag.Event(
                    "Stone",
                    "StartCheckRejected",
                    "Stone start fall rejected: support below is solid.",
                    fields: new (string key, object value)[]
                    {
                        ("x", x),
                        ("y", y),
                        ("below", below)
                    }
                );
            }

            return canFall;
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

            if (y != startY)
            {
                Diag.Event(
                    "Stone",
                    "LandingResolved",
                    "Stone landing cell resolved.",
                    fields: new (string key, object value)[]
                    {
                        ("x", x),
                        ("fromY", startY),
                        ("landingY", y),
                        ("fallDistance", startY - y)
                    }
                );
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
                Diag.Event(
                    "Stone",
                    "MoveRejected",
                    "Stone move rejected: cannot start falling.",
                    fields: new (string key, object value)[]
                    {
                        ("x", x),
                        ("y", y)
                    }
                );
                return false;
            }

            int landingY = FindLandingY(x, y);

            if (landingY == y)
            {
                Diag.Event(
                    "Stone",
                    "MoveRejected",
                    "Stone move rejected: landing cell equals current cell.",
                    fields: new (string key, object value)[]
                    {
                        ("x", x),
                        ("y", y)
                    }
                );
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
