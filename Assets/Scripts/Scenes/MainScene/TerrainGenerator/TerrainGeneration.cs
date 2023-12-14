using LitJson;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TerrainGeneration : MonoBehaviour
{
    // Змінні для спрайтів плиток (трава, грязь, камінь)
    [Header("Tile Atlas")]
    public TileAtlas tileAtlas;

    private int CHUNK_SIZE = 10;
    private List<Transform> chunks = new();
    private List<TileData> tileDataList;
    private bool isEdge;

    // Метод, який викликається при запуску гри
    private void Start()
    {
        CreateChunk();
        tileDataList = GenerateTerrainFromJson(Path.Combine(Application.persistentDataPath));
        // Генеруємо терен
        GenerateTerrain(tileDataList);
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

    public void GenerateTerrain(List<TileData> tileDataList)
    {

        foreach (var tileData in tileDataList)
        {
            GameObject newTile = PlaceTileByType(tileData.TileType, tileData.X, tileData.Y);
        }
    }

    public List<TileData> GenerateTerrainFromJson(string filePath)
    {
        List<TileData> tileDataList = new List<TileData>();

        if (File.Exists(filePath))
        {
            var jsonData = JsonMapper.ToObject(File.ReadAllText(filePath));

            foreach (var tileData in jsonData)
            {
                JsonData jsonDataItem = tileData as JsonData;

                if (jsonDataItem != null)
                {
                    TileData tileDataObject = new TileData
                    {
                        X = (int)jsonDataItem["X"],
                        Y = (int)jsonDataItem["Y"],
                        TileType = (string)jsonDataItem["TileType"]
                    };

                    tileDataList.Add(tileDataObject);
                }
            }
        }

        else
        {
            Debug.LogError("Terrain layout file not found!");
        }

        return tileDataList;
    }
}
