using LitJson;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class TerrainGeneration : MonoBehaviour
{
    // Змінні для спрайтів плиток (трава, грязь, камінь)
    [Header("Tile Atlas")]
    public TileAtlas tileAtlas;

    private const string fileName = "terrain_layout.json";
    private int CHUNK_SIZE = 10;
    private List<Transform> chunks = new();
    private Dictionary<Vector2, TileData> tileDataDictionary = new Dictionary<Vector2, TileData>();
    private bool isEdge;

    // Метод, який викликається при запуску гри
    private void Start()
    {
        CreateChunk();
        tileDataDictionary = GenerateTerrainFromJson(Path.Combine(Application.persistentDataPath, fileName));
       
        // Генеруємо терен
        GenerateTerrain(tileDataDictionary); 
    }

    public Dictionary<Vector2, TileData> GetTileDataDictionary()
    {
        tileDataDictionary = GenerateTerrainFromJson(Path.Combine(Application.persistentDataPath, fileName));
        return tileDataDictionary;
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

    public void GenerateTerrain(Dictionary<Vector2, TileData> tileDataDictionary)
    {
        foreach (var tileDataEntry in tileDataDictionary)
        {
            TileData tileData = tileDataEntry.Value;
            Vector2 position = tileDataEntry.Key;

            if (position.y >= 249)
            {
                GameObject newTile = PlaceTileByType(tileData.TileType, tileData.X, tileData.Y);
            }
        }
    }

    public Dictionary<Vector2, TileData> GenerateTerrainFromJson(string filePath)
    {
        Dictionary<Vector2, TileData> tileDataDictionary = new Dictionary<Vector2, TileData>();

        if (File.Exists(filePath))
        {
            var jsonData = JsonMapper.ToObject(File.ReadAllText(filePath));

            foreach (JsonData jsonDataItem in jsonData)
            {
                TileData tileDataObject = new TileData
                {
                    X = (int)jsonDataItem["X"],
                    Y = (int)jsonDataItem["Y"],
                    TileType = (string)jsonDataItem["TileType"]
                };

                Vector2 position = new Vector2(tileDataObject.X, tileDataObject.Y);
                tileDataDictionary[position] = tileDataObject;
            }
        }

        else
        {
            Debug.LogError("Terrain layout file not found!");
        }

        return tileDataDictionary;
    }
}
