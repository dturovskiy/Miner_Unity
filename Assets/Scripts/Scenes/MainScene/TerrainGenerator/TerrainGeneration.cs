using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[System.Serializable]
public class TerrainGeneration : MonoBehaviour
{
    [Header("Tile Atlas")]
    public TileAtlas tileAtlas;

    private const string fileName = "terrain_layout.bin";
    private int CHUNK_SIZE = 10;
    private List<Transform> chunks = new();
    private bool isEdge;

    // Метод, який викликається при запуску гри
    private void Start()
    {
        CreateChunk();
    }

    public Dictionary<Vector2, TileData> GetTileDataDictionary()
    {
        return LoadTerrainFromBinary(Path.Combine(Application.persistentDataPath, fileName));
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
            newTile.AddComponent<Rigidbody2D>();
            newTile.AddComponent<StoneBehaviour>();
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
        while (chunkIndex >= chunks.Count)
        {
            CreateChunk();
        }
        return chunks[chunkIndex];
    }

    public List<Transform> GetChunks()
    {
        return chunks;
    }

    internal GameObject PlaceTileByType(string tileType, float x, float y)
    {
        Sprite tileSprite = DetermineTileTypeByString(tileType);
        GameObject newTile = PlaceTile(tileSprite, x, y);

        return newTile;
    }

    private Sprite DetermineTileTypeByString(string tileType)
    {
        switch (tileType)
        {
            case "Coal":
                return tileAtlas.coal.tileSprite;
            case "Iron":
                return tileAtlas.iron.tileSprite;
            case "Gold":
                return tileAtlas.gold.tileSprite;
            case "Diamond":
                return tileAtlas.diamond.tileSprite;
            case "Uranus":
                return tileAtlas.uranus.tileSprite;
            case "Topaz":
                return tileAtlas.topaz.tileSprite;
            case "Silver":
                return tileAtlas.silver.tileSprite;
            case "Ruby":
                return tileAtlas.ruby.tileSprite;
            case "Platinum":
                return tileAtlas.platinum.tileSprite;
            case "Opal":
                return tileAtlas.opal.tileSprite;
            case "Nephritis":
                return tileAtlas.nephritis.tileSprite;
            case "Map":
                return tileAtlas.map.tileSprite;
            case "Lazurite":
                return tileAtlas.lazurite.tileSprite;
            case "Emerald":
                return tileAtlas.emerald.tileSprite;
            case "Artifact":
                return tileAtlas.artifact.tileSprite;
            case "Amethyst":
                return tileAtlas.amethyst.tileSprite;
            case "Dirt":
                return tileAtlas.dirt.tileSprite;
            case "Stone":
                return tileAtlas.stone.tileSprite;
            case "Tunnel":
                return tileAtlas.tunnel.tileSprite;

            default: return tileAtlas.stone.tileSprite;
        }
    }

    public Dictionary<Vector2, TileData> LoadTerrainFromBinary(string filePath)
    {
        Dictionary<Vector2, TileData> tileDataDictionary = new Dictionary<Vector2, TileData>();

        if (File.Exists(filePath))
        {
            // Використовуємо BinaryFormatter для десеріалізації об'єктів TileData з файлу
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            // Відкриваємо файловий потік для читання з бінарного файлу
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                // Десеріалізуємо дані з файлу із використанням BinaryFormatter
                List<TileData> tileDataList = (List<TileData>)binaryFormatter.Deserialize(fileStream);

                // Заповнюємо словник з даними про блоки
                foreach (var tileData in tileDataList)
                {
                    Vector2 tileCoordinates = new Vector2();
                    tileCoordinates.x = tileData.X;
                    tileCoordinates.y = tileData.Y;

                    tileDataDictionary.Add(tileCoordinates, tileData);
                }
            }

            System.GC.Collect();
        }
        else
        {
            Debug.LogError("Terrain layout file not found!");
        }

        return tileDataDictionary;
    }
}
