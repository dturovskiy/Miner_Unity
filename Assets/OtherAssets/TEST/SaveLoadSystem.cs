using LitJson;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveLoadSystem : MonoBehaviour
{
    private List<Vector2> hiddenTilesCoordinates = new List<Vector2>();
    private const string hiddenTilesFileName = "hiden_tiles.json";

    public void SaveHiddenTiles(Vector3Int coordinates)
    {
        hiddenTilesCoordinates.Add(new Vector2(coordinates.x, coordinates.y));
    }

    public void SaveHidenTilesToJson()
    {
        string outputPath = Path.Combine(Application.persistentDataPath, hiddenTilesFileName);
        string json = JsonMapper.ToJson(hiddenTilesCoordinates);
        File.WriteAllText(outputPath, json);
        Debug.Log("Hidden tiles saved to JSON.");
    }

    public List<Vector3Int> LoadHidenTilesFromJson()
    {
        List<Vector3Int> loadedTiles = new List<Vector3Int>();
        string filePath = Path.Combine(Application.persistentDataPath, hiddenTilesFileName);

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var loadedList = JsonMapper.ToObject<List<Vector2>>(json);
            if (loadedList != null)
            {
                foreach (var tile in loadedList)
                {
                    loadedTiles.Add(new Vector3Int((int)tile.x, (int)tile.y, 0));
                    hiddenTilesCoordinates.Add(tile);
                }
            }
        }
        return loadedTiles;
    }
}