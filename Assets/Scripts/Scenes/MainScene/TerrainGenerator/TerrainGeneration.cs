using UnityEngine;

public class TerrainGeneration : MonoBehaviour
{
    // Змінні для спрайтів плиток (трава, грязь, камінь)
    [Header("Tile Atlas")]
    public TileAtlas tileAtlas;

    // Налаштування генерації терену
    [Header("Generation Settings")]
    public const int WORLD_SIZE = 99; // Розмір світу
    public const int DUNGEON_HEIGHT = 250;
    public const int TOTAL_HEIGHT = 254;
    public const float STONE_PROBABILITY = 0.07f;
    private const int MAX_X_OFFSET = 22;
    bool isBackground = false;

    [Header("Noise Settings")]
    public float terrainFreq = 0.05f;
    public float caveFreq = 0.05f;
    public float seed;

    [Header("Ore Settings")]
    public OreClass[] ores;

    // Метод, який викликається при запуску гри
    private void Start()
    {
        // Генеруємо випадковий seed для шуму
        seed = Random.Range(-10000, 10000);

        // Ініціалізуємо текстури розповсюдження руд
        InitializeOreSpreadTextures();

        // Генеруємо терен
        GenerateTerrain();
        //HideLowerTerrain(); // Якщо ця функція потрібна, розкоментуйте
    }

    private void InitializeOreSpreadTextures()
    {
        // Ініціалізуємо текстури розповсюдження руд для кожного типу руди
        foreach (OreClass ore in ores)
        {
            ore.spreadTexture = new Texture2D(WORLD_SIZE, DUNGEON_HEIGHT);
            GenerateNoiseTexture(seed, ore.frequency, ore.size, ore.spreadTexture);
        }
    }

    // Метод для генерації терену
    public void GenerateTerrain()
    {
        for (int y = 0; y <= TOTAL_HEIGHT; y++)
        {
            for (int x = 0; x <= WORLD_SIZE; x++)
            {
                // Визначаємо тип плитки для поточних координат
                Sprite tileSprite = DetermineTileType(x, y);

                // Перевіряємо умови, щоб припинити генерацію терену
                if (y > DUNGEON_HEIGHT + 1 && x > MAX_X_OFFSET - 1)
                    break;

                if (y >= DUNGEON_HEIGHT && x > MAX_X_OFFSET)
                    break;

                if (tileSprite == null)
                    break;

                // Встановлюємо чи плитка належить до фону
                if (tileSprite == tileAtlas.tunnel.tileSprite)
                {
                    isBackground = true;
                }

                if (tileSprite != tileAtlas.tunnel.tileSprite)
                {
                    isBackground = false;
                }

                // Розміщуємо плитку на сцені
                PlaceTile(tileSprite, x, y, isBackground);
            }
        }
    }

    public void GenerateNoiseTexture(float seed, float frequency, float limit, Texture2D noise)
    {
        for (int x = 0; x < noise.width; x++)
        {
            for (int y = 0; y < noise.height; y++)
            {
                // Генеруємо шум за допомогою Perlin Noise
                float v = Mathf.PerlinNoise((x + seed) * frequency, (y + seed) * frequency);

                // Встановлюємо колір пікселя на основі шуму
                if (v > limit)
                    noise.SetPixel(x, y, Color.white);
                else
                    noise.SetPixel(x, y, Color.black);
            }
        }

        noise.Apply();
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
            return tileAtlas.stone.tileSprite;
        }

        // Генерація тунелю
        if (IsTunnel(x, y))
        {
            return tileAtlas.tunnel.tileSprite;
        }

        if (y < TOTAL_HEIGHT && x < WORLD_SIZE)
        {
            for (int i = 0; i < ores.Length; i++)
            {
                if (ores[i].spreadTexture.GetPixel(x, y).r > 0.5f && y < ores[i].maxSpawnHeight && y > ores[i].minSpawnHeight)
                {
                    return GetOreSprite(i);
                }
            }

            // Рандомна генерація каменів на інших частинах терену
            if (y != DUNGEON_HEIGHT + 1 && Random.Range(0.0f, 1.0f) < STONE_PROBABILITY)
                return tileAtlas.stone.tileSprite;
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
    public GameObject PlaceTile(Sprite tileSprite, float x, float y, bool backgroundElement)
    {
        GameObject newTile = new();
        newTile.transform.parent = transform;
        newTile.AddComponent<SpriteRenderer>();

        if (!backgroundElement)
        {
            newTile.AddComponent<BoxCollider2D>();
            newTile.GetComponent<BoxCollider2D>().size = Vector2.one;
            newTile.tag = "Ground";
        }

        newTile.GetComponent<SpriteRenderer>().sprite = tileSprite;
        newTile.name = tileSprite.name;
        newTile.transform.position = new Vector2(x + 0.5f, y + 0.5f);
        return newTile;
    }
}
