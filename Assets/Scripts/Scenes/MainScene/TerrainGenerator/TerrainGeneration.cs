using System.Collections.Generic;
using UnityEngine;

public class TerrainGeneration : MonoBehaviour
{
    GameObject newTile;

    // Змінні для спрайтів плиток (трава, грязь, камінь)
    [Header("Tile Atlas")]
    public TileAtlas tileAtlas;

    // Налаштування генерації терену
    [Header("Generation Settings")]
    public const int WORLD_SIZE = 100; // Розмір світу
    public const int DUNGEON_HEIGHT = 250; // Додавання до висоти
    public const int TOTAL_HEIGHT = 255;
    public const float STONE_PROBABILITY = 0.07f;
    private const int MAX_X_OFFSET = 22;
    bool isBackground = false;

    [Header("Noise Settings")]
    public float terrainFreq = 0.05f;
    public float caveFreq = 0.05f;
    public float seed;

    [Header("Ore Settings")]
    public OreClass[] ores;

    // Список об'єктів для нижньої частини терену
    [SerializeField] private List<GameObject> lowerTerrainObjects = new List<GameObject>();

    private List<GameObject> worlTileObjects = new List<GameObject>();

    // Метод, який викликається при запуску гри
    private void Start()
    {
        seed = Random.Range(-10000, 10000);

        ores[0].spreadTexture = new Texture2D(WORLD_SIZE, DUNGEON_HEIGHT);
        ores[1].spreadTexture = new Texture2D(WORLD_SIZE, DUNGEON_HEIGHT);
        ores[2].spreadTexture = new Texture2D(WORLD_SIZE, DUNGEON_HEIGHT);
        ores[3].spreadTexture = new Texture2D(WORLD_SIZE, DUNGEON_HEIGHT);

        GenerateNoiseTexture(seed, ores[0].frequency, ores[0].size, ores[0].spreadTexture);
        GenerateNoiseTexture(seed, ores[1].frequency, ores[1].size, ores[1].spreadTexture);
        GenerateNoiseTexture(seed, ores[2].frequency, ores[2].size, ores[2].spreadTexture);
        GenerateNoiseTexture(seed, ores[3].frequency, ores[3].size, ores[3].spreadTexture);

        // Генеруємо терен
        GenerateTerrain();
        //HideLowerTerrain();
    }

    // Метод для генерації терену
    public void GenerateTerrain()
    {
        for (int y = 0; y < TOTAL_HEIGHT; y++)
        {
            for (int x = 0; x < WORLD_SIZE; x++)
            {
                Sprite tileSprite = DetermineTileType(x, y);

                if (tileSprite == null)
                {
                    // Якщо плитка пуста, переходимо до наступної ітерації циклу y.
                    continue;
                }


                if (tileSprite == tileAtlas.tunnel.tileSprite)
                {
                    isBackground = true;
                }
                if (tileSprite != tileAtlas.tunnel.tileSprite)
                {
                    isBackground = false;
                }
                PlaceTile(tileSprite, x, y, isBackground); //todo
                worlTileObjects.Add(newTile);

                if (y < DUNGEON_HEIGHT)
                {
                    lowerTerrainObjects.Add(newTile);
                }
            }
        }
    }

    public void GenerateNoiseTexture(float seed, float frequency, float limit, Texture2D noise)
    {
        for (int x = 0; x < noise.width; x++)
        {
            for (int y = 0; y < noise.height; y++)
            {
                float v = Mathf.PerlinNoise((x + seed) * frequency, (y + seed) * frequency);

                if (v > limit)
                    noise.SetPixel(x, y, Color.white);

                else
                    noise.SetPixel(x, y, Color.black);
            }
        }

        noise.Apply();
    }

    // Метод для приховування (вимикання) нижньої частини терену
    public void HideLowerTerrain()
    {
        foreach (var obj in lowerTerrainObjects)
        {
            obj.SetActive(false); // Приховуємо нижні блоки
        }
    }

    private Sprite DetermineTileType(int x, int y)
    {
        //Генерація меж терену
        if ((x == 0 || x == WORLD_SIZE - 1 || y == 0) && y < DUNGEON_HEIGHT)
        {
            return tileAtlas.stone.tileSprite;
        }

        //Генерація тунелю
        if (y == DUNGEON_HEIGHT)
        {
            if (x == 0)
            {
                return tileAtlas.stone.tileSprite;
            }
            if (x >= 10 && x <= MAX_X_OFFSET)
            {
                return tileAtlas.tunnel.tileSprite;
            }
            if (x > MAX_X_OFFSET)
            {
                return null;
            }
        }

        //Генерація найвищої межі терену
        if (y >= DUNGEON_HEIGHT && y < TOTAL_HEIGHT)
        {
            if (x == 0 || (x <= 19 && y == DUNGEON_HEIGHT + 4))
            {
                return tileAtlas.stone.tileSprite;
            }
        }

        if (y < TOTAL_HEIGHT && x < WORLD_SIZE)
        {
            if (y > DUNGEON_HEIGHT + 1 && x > MAX_X_OFFSET - 1) return null;
            if (y > DUNGEON_HEIGHT && x > MAX_X_OFFSET) return null;

            if (ores[0].spreadTexture.GetPixel(x, y).r > 0.5f && y < ores[0].maxSpawnHeight && y > ores[0].minSpawnHeight)
            {
                return tileAtlas.coal.tileSprite;
            }
            if (ores[1].spreadTexture.GetPixel(x, y).r > 0.5f && y < ores[1].maxSpawnHeight && y > ores[1].minSpawnHeight)
            {
                return tileAtlas.iron.tileSprite;
            }
            if (ores[2].spreadTexture.GetPixel(x, y).r > 0.5f && y < ores[2].maxSpawnHeight && y > ores[2].minSpawnHeight)
            {
                return tileAtlas.gold.tileSprite;
            }
            if (ores[3].spreadTexture.GetPixel(x, y).r > 0.5f && y < ores[3].maxSpawnHeight && y > ores[3].minSpawnHeight)
            {
                return tileAtlas.diamond.tileSprite;
            }

            // Рандомна генерація каменів на інших частинах терену
            if (Random.Range(0f, 1f) < STONE_PROBABILITY)
            {
                if (y == DUNGEON_HEIGHT + 1) return tileAtlas.dirt.tileSprite;
                return tileAtlas.stone.tileSprite;
            }
        }

        return tileAtlas.dirt.tileSprite;
    }

    // Метод для розміщення плитки на сцені
    public GameObject PlaceTile(Sprite tileSprite, float x, float y, bool backgroundElement)
    {
        newTile = new GameObject();
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
