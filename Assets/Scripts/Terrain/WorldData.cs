using System;
using System.IO;
using UnityEngine;

namespace MinerUnity.Terrain
{
    /// <summary>
    /// Core data structure for the game world.
    /// Replaces the BinaryFormatter List<TileData> implementation.
    /// Stores the entire 100x255 map in a single byte array of size 25500.
    /// </summary>
    public class WorldData
    {
        private const int WIDTH = 100; // 0 to 99 inclusive
        private const int HEIGHT = 255; // 0 to 254 inclusive
        
        // 1D Array representing 2D grid: index = y * WIDTH + x
        private readonly byte[] grid;

        public int Width => WIDTH;
        public int Height => HEIGHT;

        public WorldData()
        {
            grid = new byte[WIDTH * HEIGHT];
        }

        /// <summary>
        /// Converts X,Y coordinate to 1D array index.
        /// </summary>
        private int GetIndex(int x, int y)
        {
            return y * WIDTH + x;
        }

        /// <summary>
        /// Checks if the coordinate is within world bounds.
        /// </summary>
        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < WIDTH && y >= 0 && y < HEIGHT;
        }

        /// <summary>
        /// Gets the TileID at the specified coordinate.
        /// Returns TileID.Edge if out of bounds.
        /// </summary>
        public TileID GetTile(int x, int y)
        {
            if (!IsValidCoordinate(x, y))
            {
                return TileID.Edge; 
            }
            return (TileID)grid[GetIndex(x, y)];
        }

        /// <summary>
        /// Sets a tile at the specified coordinate. 
        /// In-game, destroying a tile simply does SetTile(x, y, TileID.Empty)
        /// </summary>
        public void SetTile(int x, int y, TileID tileId)
        {
            if (!IsValidCoordinate(x, y)) return;
            grid[GetIndex(x, y)] = (byte)tileId;
        }

        /// <summary>
        /// Saves the entire world instantly as a raw byte array.
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                File.WriteAllBytes(filePath, grid);
                Debug.Log($"WorldData saved successfully to {filePath} (Size: {grid.Length} bytes)");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save world data: {e.Message}");
            }
        }

        /// <summary>
        /// Loads the world from a raw byte array file.
        /// Returns true if loaded successfully, false if file missing or corrupt.
        /// </summary>
        public bool LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return false;

            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                if (fileData.Length == grid.Length)
                {
                    Array.Copy(fileData, grid, grid.Length);
                    return true;
                }
                else
                {
                    Debug.LogError($"WorldSave size mismatch! Expected {grid.Length}, got {fileData.Length}. File might be from an old version.");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load world data: {e.Message}");
                return false;
            }
        }
    }
}
