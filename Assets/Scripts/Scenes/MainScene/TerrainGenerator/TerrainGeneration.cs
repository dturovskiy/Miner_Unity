using System.Collections.Generic;
using UnityEngine;

public class TerrainGeneration : MonoBehaviour
{
    // Змінні для спрайтів плиток (трава, грязь, камінь)
    [Header("Tile Atlas")]
    public TileAtlas tileAtlas;

    // Налаштування генерації терену
    [Header("Generation Settings")]
    public const int WORLD_SIZE = 99;
    public const int DUNGEON_HEIGHT = 250;
    public const int TOTAL_HEIGHT = 254;
    public const float STONE_PROBABILITY = 0.07f;
    private const int MAX_X_OFFSET = 22;

    [Header("Ore Settings")]
    public OreClass[] ores;

    private int CHUNK_SIZE = 10;
    private List<Transform> chunks = new();

    private bool isEdge;

    // Метод, який викликається при запуску гри
    private void Start()
    {
        CreateChunk();

        // Генеруємо терен
    }

    // Метод для генерації терену
    public void GenerateTerrain()
    {
        for (int y = 0; y <= TOTAL_HEIGHT; y++)
        {
            for (int x = 0; x <= WORLD_SIZE; x++)
            {
                if (y % CHUNK_SIZE == 0 && y > 0)
                {
                    CreateChunk();
                }
                // Визначаємо тип плитки для поточних координат
                Sprite tileSprite = DetermineTileType(x, y);

                // Перевіряємо умови, щоб припинити генерацію терену
                if (y > DUNGEON_HEIGHT + 1 && x > MAX_X_OFFSET - 1)
                    break;

                if (y >= DUNGEON_HEIGHT && x > MAX_X_OFFSET)
                    break;

                if (tileSprite == null)
                    break;

                // Розміщуємо плитку на сцені
                PlaceTile(tileSprite, x, y);
            }
        }

        for (int i = 0; i < 24; i++)
        {
            if (chunks[i] != null)
                chunks[i].gameObject.SetActive(false);
        }

        System.GC.Collect();
    }

    private void CreateChunk()
    {
        if (chunks.Count < 26)
        {
            GameObject chunkObject = new("Chunk");
            chunkObject.transform.parent = transform;
            chunks.Add(chunkObject.transform);
        }
    }

    // Функція, що перевіряє, чи `(x, y)` на межі терену
    private bool IsTerrainEdge(int x, int y)
    {
        return (x == 0 || x == WORLD_SIZE || y == 0) || (x <= 19 && y == TOTAL_HEIGHT);
    }

    // Функція, що перевіряє, чи `(x, y)` на рівні тунелю
    private bool IsTunnel(int x, int y)
    {
        return y == DUNGEON_HEIGHT && x >= 10 && x <= MAX_X_OFFSET;
    }

    private Sprite DetermineTileType(int x, int y)
    {
        // Генерація меж терену
        if (IsTerrainEdge(x, y))
        {
            isEdge = true;
            return tileAtlas.stone.tileSprite;
        }
        else
        {
            isEdge = false;
        }

        // Генерація тунелю
        if (IsTunnel(x, y))
        {
            return tileAtlas.tunnel.tileSprite;
        }

        if (y < TOTAL_HEIGHT && x < WORLD_SIZE)
        {
            // Рандомна генерація каменів на інших частинах терену
            if (y != DUNGEON_HEIGHT + 1 && Random.Range(0.0f, 1.0f) < STONE_PROBABILITY)
                return tileAtlas.stone.tileSprite;

            for (int i = 0; i < ores.Length; i++)
            {
                if (Random.Range(0.0f, 1.0f) < ores[i].frequency && y > ores[i].minSpawnHeight && y < ores[i].maxSpawnHeight)
                {
                    return GetOreSprite(i);
                }
            }
        }

        return tileAtlas.dirt.tileSprite;
    }

    private Sprite GetOreSprite(int oreIndex)
    {
        // Визначаємо спрайт для типу руди за індексом
        return oreIndex switch
        {
            0 => tileAtlas.coal.tileSprite,
            1 => tileAtlas.iron.tileSprite,
            2 => tileAtlas.gold.tileSprite,
            3 => tileAtlas.diamond.tileSprite,
            4 => tileAtlas.uranus.tileSprite,
            5 => tileAtlas.topaz.tileSprite,
            6 => tileAtlas.silver.tileSprite,
            7 => tileAtlas.ruby.tileSprite,
            8 => tileAtlas.platinum.tileSprite,
            9 => tileAtlas.opal.tileSprite,
            10 => tileAtlas.nephritis.tileSprite,
            11 => tileAtlas.map.tileSprite,
            12 => tileAtlas.lazurite.tileSprite,
            13 => tileAtlas.emerald.tileSprite,
            14 => tileAtlas.artifact.tileSprite,
            15 => tileAtlas.amethyst.tileSprite,
            _ => null, // Обробляємо невідомий індекс руди
        };
    }

    // Метод для розміщення плитки на сцені
    public GameObject PlaceTile(Sprite tileSprite, float x, float y)
    {
        int chunkIndex = Mathf.FloorToInt(y / CHUNK_SIZE);
        Transform chunk = GetOrCreateChunk(chunkIndex);
        GameObject newTile = new();
        newTile.transform.parent = chunk;
        newTile.AddComponent<SpriteRenderer>();

        if (isEdge)
        {
            newTile.tag = "Edge";
        }

        if (tileSprite.name != "Tunnel")
        {
            newTile.AddComponent<BoxCollider2D>();
            newTile.GetComponent<BoxCollider2D>().size = Vector2.one;
            newTile.tag = "Ground";
        }

        newTile.GetComponent<SpriteRenderer>().sprite = tileSprite;
        newTile.name = tileSprite.name;

        if (newTile.name == "Stone" && !isEdge)
        {
            newTile.tag = "Stone";
            newTile.AddComponent<Rigidbody2D>().isKinematic = true;
        }

        if (!isEdge)
        {
            newTile.AddComponent<TileBehaviour>();
            newTile.AddComponent<TransformSaver>();
        }


        newTile.transform.position = new Vector2(x + 0.5f, y + 0.5f);

        return newTile;
    }

    private Transform GetOrCreateChunk(int chunkIndex)
    {
        if (chunkIndex < chunks.Count)
        {
            return chunks[chunkIndex];
        }

        else
        {
            CreateChunk();
            return chunks[chunkIndex - 1];
        }
    }

    public List<Transform> GetChunks()
    {
        return chunks;
    }

    internal GameObject PlaceTileByType(string tileType, int x, int y)
    {
        Sprite tileSprite = DetermineTileTypeByString(tileType);
        GameObject newTile = PlaceTile(tileSprite, x, y);

        return newTile;
    }

    private Sprite DetermineTileTypeByString(string tileType)
    {
        switch (tileType)
        {
            case "CO":
                return tileAtlas.coal.tileSprite;
            case "IR":
                return tileAtlas.iron.tileSprite;
            case "GO":
                return tileAtlas.gold.tileSprite;
            case "DI":
                return tileAtlas.diamond.tileSprite;
            case "UR":
                return tileAtlas.uranus.tileSprite;
            case "TO":
                return tileAtlas.topaz.tileSprite;
            case "SI":
                return tileAtlas.silver.tileSprite;
            case "RU":
                return tileAtlas.ruby.tileSprite;
            case "PL":
                return tileAtlas.platinum.tileSprite;
            case "OP":
                return tileAtlas.opal.tileSprite;
            case "NE":
                return tileAtlas.nephritis.tileSprite;
            case "MA":
                return tileAtlas.map.tileSprite;
            case "LA":
                return tileAtlas.lazurite.tileSprite;
            case "EM":
                return tileAtlas.emerald.tileSprite;
            case "AR":
                return tileAtlas.artifact.tileSprite;
            case "AM":
                return tileAtlas.amethyst.tileSprite;
            case "DR":
                return tileAtlas.dirt.tileSprite;
            case "ST":
                return tileAtlas.stone.tileSprite;
            case "ED":
                return tileAtlas.stone.tileSprite;
            case "TU":
                return tileAtlas.tunnel.tileSprite;

            default: return tileAtlas.dirt.tileSprite;
        }
    }
}
