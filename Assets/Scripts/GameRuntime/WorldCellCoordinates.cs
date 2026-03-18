using UnityEngine;

namespace MinerUnity.Runtime
{
    public static class WorldCellCoordinates
    {
        public const float DefaultCellSize = 1f;

        public static Vector2Int WorldToCell(Vector2 worldPosition)
        {
            return WorldToCell(worldPosition, Vector2.zero, DefaultCellSize);
        }

        public static Vector2Int WorldToCell(Vector2 worldPosition, Vector2 origin, float cellSize)
        {
            float safeCellSize = Mathf.Approximately(cellSize, 0f) ? DefaultCellSize : cellSize;

            return new Vector2Int(
                Mathf.FloorToInt((worldPosition.x - origin.x) / safeCellSize),
                Mathf.FloorToInt((worldPosition.y - origin.y) / safeCellSize));
        }

        public static Vector2 CellToWorldCenter(Vector2Int cell, Vector2 origin, float cellSize)
        {
            float safeCellSize = Mathf.Approximately(cellSize, 0f) ? DefaultCellSize : cellSize;

            return origin + new Vector2(
                (cell.x + 0.5f) * safeCellSize,
                (cell.y + 0.5f) * safeCellSize);
        }

        public static float GetCellBottomY(int y, float originY, float cellSize)
        {
            float safeCellSize = Mathf.Approximately(cellSize, 0f) ? DefaultCellSize : cellSize;
            return originY + y * safeCellSize;
        }

        public static float GetCellTopY(int y, float originY, float cellSize)
        {
            float safeCellSize = Mathf.Approximately(cellSize, 0f) ? DefaultCellSize : cellSize;
            return originY + (y + 1) * safeCellSize;
        }
    }
}
