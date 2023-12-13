using LitJson;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TerrainBuilder : MonoBehaviour
{
    private const string fileName = "terrain_layout.json";
    private const int WORLD_SIZE = 99;
    private const int TOTAL_HEIGHT = 254;
    private const int DUNGEON_HEIGHT = 250;
    private const int MAX_X_OFFSET = 22;
    private const float STONE_PROBABILITY = 0.07f;
    public OreClass[] ores;
    public TileAtlas tileAtlas;

    private void Awake()
    {
        CreateTerrainJsonFile();
    }

    public void CreateTerrainJsonFile()
    {
        string outputPath = Path.Combine(Application.persistentDataPath, fileName);

        // Create a list to store tile data
        List<TileData> tileDataList = new List<TileData>();

        for (int y = 0; y <= TOTAL_HEIGHT; y++)
        {
            for (int x = 0; x <= WORLD_SIZE; x++)
            {
                if (y > DUNGEON_HEIGHT + 1 && x > MAX_X_OFFSET - 1)
                    break;

                if (y >= DUNGEON_HEIGHT && x > MAX_X_OFFSET)
                    break;

                string tileType = DetermineTileType(x, y);

                // Add tile data to the list
                TileData tileData = new TileData { X = x, Y = y, TileType = tileType };
                tileDataList.Add(tileData);

                // Add debug log for each tile
                Debug.Log($"Tile at X:{x}, Y:{y}, Type:{tileType}");
            }
        }

        WriteTileData(tileDataList, outputPath);
    }

    private void WriteTileData(List<TileData> tileDataList, string outputPath)
    {
        // Write all tile data to the file after the loops
        if (File.Exists(outputPath))
        {
            // Прочитати існуючий JSON з файлу
            var existingData = JsonMapper.ToObject(File.ReadAllText(outputPath));

            // Створити JsonWriter з параметром PrettyPrint
            var writer = new JsonWriter();
            writer.PrettyPrint = true;

            // Записати оновлений JSON-текст назад в файл
            existingData.ToJson(writer);
            File.WriteAllText(outputPath, writer.ToString());
            Debug.Log("Terrain layout has been rewritten to " + outputPath);
        }
        else
        {
            var writer = new JsonWriter();
            writer.PrettyPrint = true;

            JsonMapper.ToJson(tileDataList, writer);
            File.WriteAllText(outputPath, writer.ToString());
            Debug.Log("Terrain layout has been written to " + outputPath);
        }
        System.GC.Collect();
    }

    private string DetermineTileType(int x, int y)
    {
        if (IsTerrainEdge(x, y))
        {
            return tileAtlas.stone.name;
        }
        if (IsTunnel(x, y))
        {
            return tileAtlas.tunnel.name;
        }

        if (y < TOTAL_HEIGHT && x < WORLD_SIZE)
        {
            // Рандомна генерація каменів на інших частинах терену
            if (y != DUNGEON_HEIGHT + 1 && Random.Range(0.0f, 1.0f) < STONE_PROBABILITY)
                return tileAtlas.stone.name;

            for (int i = 0; i < ores.Length; i++)
            {
                if (Random.Range(0.0f, 1.0f) < ores[i].frequency && y > ores[i].minSpawnHeight && y < ores[i].maxSpawnHeight)
                {
                    return GetOreName(i);
                }
            }
        }

        return tileAtlas.dirt.name;
    }

    private string GetOreName(int oreIndex)
    {
        // Визначаємо спрайт для типу руди за індексом
        return oreIndex switch
        {
            0 => tileAtlas.coal.name,
            1 => tileAtlas.iron.name,
            2 => tileAtlas.gold.name,
            3 => tileAtlas.diamond.name,
            4 => tileAtlas.uranus.name,
            5 => tileAtlas.topaz.name,
            6 => tileAtlas.silver.name,
            7 => tileAtlas.ruby.name,
            8 => tileAtlas.platinum.name,
            9 => tileAtlas.opal.name,
            10 => tileAtlas.nephritis.name,
            11 => tileAtlas.map.name,
            12 => tileAtlas.lazurite.name,
            13 => tileAtlas.emerald.name,
            14 => tileAtlas.artifact.name,
            15 => tileAtlas.amethyst.name,
            _ => null, // Обробляємо невідомий індекс руди
        };
    }

    private bool IsTerrainEdge(int x, int y)
    {
        return (x == 0 || x == WORLD_SIZE || y == 0) || (x <= 19 && y == TOTAL_HEIGHT);
    }

    // Функція, що перевіряє, чи `(x, y)` на рівні тунелю
    private bool IsTunnel(int x, int y)
    {
        return y == DUNGEON_HEIGHT && x >= 10 && x <= MAX_X_OFFSET;
    }
}
