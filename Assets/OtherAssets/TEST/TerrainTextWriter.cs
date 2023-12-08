using System.IO;
using System.Text;
using UnityEngine;

public class TerrainTextWriter : MonoBehaviour
{
    private const string fileName = "terrain_layout.txt";
    private const int WORLD_SIZE = 99;
    private const int TOTAL_HEIGHT = 254;
    private const int DUNGEON_HEIGHT = 250;
    private const int MAX_X_OFFSET = 22;
    private const float STONE_PROBABILITY = 0.07f;
    public OreClass[] ores;
    public TileAtlas tileAtlas;

    public void CreateTerrainTextFile()
    {
        string filePath = Path.Combine(Application.dataPath, fileName);

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            StringBuilder lineBuilder = new StringBuilder();

            for (int y = 0; y <= TOTAL_HEIGHT; y++)
            {
                lineBuilder.Clear();

                for (int x = 0; x <= WORLD_SIZE; x++)
                {

                    if (y > DUNGEON_HEIGHT + 1 && x > MAX_X_OFFSET - 1)
                        break;

                    if (y >= DUNGEON_HEIGHT && x > MAX_X_OFFSET)
                        break;

                    string tileType = DetermineTileType(x, y);

                    lineBuilder.Append(tileType);
                    lineBuilder.Append("|");
                }

                writer.WriteLine(lineBuilder.ToString());
            }
        }

        Debug.Log("Terrain layout has been written to " + filePath);
    }

    private string DetermineTileType(int x, int y)
    {
        if (IsTerrainEdge(x, y))
        {
            return "ED";
        }
        if (IsTunnel(x, y))
        {
            return "TU";
        }

        if (y < TOTAL_HEIGHT && x < WORLD_SIZE)
        {
            // Рандомна генерація каменів на інших частинах терену
            if (y != DUNGEON_HEIGHT + 1 && Random.Range(0.0f, 1.0f) < STONE_PROBABILITY)
                return "ST";

            for (int i = 0; i < ores.Length; i++)
            {
                if (Random.Range(0.0f, 1.0f) < ores[i].frequency && y > ores[i].minSpawnHeight && y < ores[i].maxSpawnHeight)
                {
                    return GetOreID(i);
                }
            }
        }

        return "DI";
    }

    private string GetOreID(int oreIndex)
    {
        // Визначаємо спрайт для типу руди за індексом
        return oreIndex switch
        {
            0 => "CO",
            1 => "IR",
            2 => "GO",
            3 => "DI",
            4 => "UR",
            5 => "TO",
            6 => "SI",
            7 => "RU",
            8 => "PL",
            9 => "OP",
            10 => "NE",
            11 => "MA",
            12 => "LA",
            13 => "EM",
            14 => "AR",
            15 => "AM",
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
