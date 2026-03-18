using System.IO;
using UnityEngine;
using MinerUnity.Runtime;

namespace MinerUnity.Terrain
{
    /// <summary>
    /// Generates the initial world map using WorldData instead of thousands of objects.
    /// Operates only once to populate the byte array and save it.
    /// </summary>
    public class FastTerrainGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private float stoneProbability = 0.07f;
        [SerializeField] private OreClass[] ores; // Re-use the existing OreClass
        
        // Exact constraints imported from the old TerrainLayoutBuilder
        private const int MAX_X = 99; // WORLD_SIZE
        private const int MAX_Y = 254; // TOTAL_HEIGHT
        private const int DUNGEON_HEIGHT = 250;
        private const int MAX_TUNNEL_X = 22; // MAX_X_OFFSET

        public void GenerateIfMissing()
        {
            string path = GamePersistenceService.LegacyWorldFilePath;
            
            if (File.Exists(path))
            {
                Debug.Log($"FastTerrainGenerator: World file already exists at {path}. Skipping generation.");
                return;
            }

            Debug.Log("FastTerrainGenerator: Starting optimized world generation...");
            
            WorldData world = new WorldData();
            
            // Generate the world byte-by-byte
            for (int y = 0; y <= MAX_Y; y++)
            {
                for (int x = 0; x <= MAX_X; x++)
                {
                    // Ignore out of bounds (copied from old logic)
                    if (y > DUNGEON_HEIGHT + 1 && x > MAX_TUNNEL_X - 1)
                        continue;
                    if (y >= DUNGEON_HEIGHT && x > MAX_TUNNEL_X)
                        continue;

                    TileID tileToPlace = DetermineTileID(x, y);
                    world.SetTile(x, y, tileToPlace);
                }
            }
            
            // Save the raw byte array
            world.SaveToFile(path);
            Debug.Log($"FastTerrainGenerator: Generation complete! 25KB map saved to {path}.");
        }

        private TileID DetermineTileID(int x, int y)
        {
            if (IsEdge(x, y)) return TileID.Edge;
            if (IsTunnel(x, y)) return TileID.Tunnel;

            if (y < MAX_Y && x < MAX_X)
            {
                // Top layer surface stone probability 
                if (y != DUNGEON_HEIGHT + 1 && Random.value < stoneProbability)
                    return TileID.Stone;

                // Check ores
                if (ores != null)
                {
                    for (int i = 0; i < ores.Length; i++)
                    {
                        if (y > ores[i].minSpawnHeight && y < ores[i].maxSpawnHeight && Random.value < ores[i].frequency)
                        {
                            return GetOreID(i);
                        }
                    }
                }
            }

            return TileID.Dirt; // Default fill
        }

        private bool IsEdge(int x, int y)
        {
            return (x == 0 || x == MAX_X || y == 0) || (x <= 19 && y == MAX_Y);
        }

        private bool IsTunnel(int x, int y)
        {
            return y == DUNGEON_HEIGHT && x >= 10 && x <= MAX_TUNNEL_X;
        }

        private TileID GetOreID(int oreIndex)
        {
            // Same order as old GetOreName switch block
            return oreIndex switch
            {
                0 => TileID.Coal,
                1 => TileID.Iron,
                2 => TileID.Gold,
                3 => TileID.Diamond,
                4 => TileID.Uranus,
                5 => TileID.Topaz,
                6 => TileID.Silver,
                7 => TileID.Ruby,
                8 => TileID.Platinum,
                9 => TileID.Opal,
                10 => TileID.Nephritis,
                11 => TileID.Map,
                12 => TileID.Lazurite,
                13 => TileID.Emerald,
                14 => TileID.Artifact,
                15 => TileID.Amethyst,
                _ => TileID.Dirt
            };
        }
    }
}
