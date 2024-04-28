using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.VisualScripting;
using UnityEngine;

public class SaveLoadSystem : MonoBehaviour
{
    // Зберігаємо координати знищених блоків
    [SerializeField] private List<Vector2> destroyedTilesCoordinates = new List<Vector2>();
    [SerializeField] private List<SerializableVector2> spawnedTilesCoordinates = new List<SerializableVector2>();
    private const string fileName = "terrain_layout.bin";
    private const string spawnedTilesBinary = "spawned_tiles.bin";

    // Метод для збереження координати знищеного блоку
    public void SaveDestroyedTiles(Vector2 coordinates)
    {
        destroyedTilesCoordinates.Add(coordinates);
    }

    public void SaveSpawnedTiles(Vector2 coordinates)
    {
        spawnedTilesCoordinates.Add(coordinates);
    }

    // Метод для видалення знищених блоків з файлу
    public void RemoveDestroyedBlocksFromBinary()
    {
        string outputPath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(outputPath))
        {
            List<TileData> savedData;

            try
            {
                using (FileStream fileStream = new FileStream(outputPath, FileMode.Open))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    savedData = (List<TileData>)binaryFormatter.Deserialize(fileStream);
                }
            }
            catch (SerializationException e)
            {
                Debug.LogError("Error deserializing data: " + e.Message);
                return;
            }

            // Remove destroyed tiles from the list
            foreach (var coordinate in destroyedTilesCoordinates)
            {
                savedData.RemoveAll(tileData => tileData.X == (int)coordinate.x && tileData.Y == (int)coordinate.y);
            }

            // Rewrite the updated data back to the file
            try
            {
                using (FileStream fileStream = new FileStream(outputPath, FileMode.Create))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Serialize(fileStream, savedData);
                }
            }
            catch (SerializationException e)
            {
                Debug.LogError("Error serializing data: " + e.Message);
                return;
            }

            Debug.Log("Destroyed blocks have been removed from the terrain layout.");
        }
        else
        {
            Debug.LogError("Terrain layout file not found!");
        }

        SaveSpawnedTilesToBinary();
    }

    public void DeleteTerrainBinaryFile()
    {
        string deletedTiles = Path.Combine(Application.persistentDataPath, fileName);
        string spawnedTiles = Path.Combine(Application.persistentDataPath, spawnedTilesBinary);

        if (File.Exists(deletedTiles))
        {
            File.Delete(deletedTiles);
            File.Delete(spawnedTiles);
            Debug.Log("Terrain layout binary file has been deleted.");
        }
        else
        {
            Debug.LogWarning("Terrain layout binary file not found. Nothing to delete.");
        }
    }

    public void SaveSpawnedTilesToBinary()
    {
        string outputPath = Path.Combine(Application.persistentDataPath, spawnedTilesBinary);
        List<SerializableVector2> loadedData = new List<SerializableVector2>();

        if (File.Exists(outputPath))
        {
            using (FileStream fileStream = new FileStream(outputPath, FileMode.Open))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                loadedData = (List<SerializableVector2>)binaryFormatter.Deserialize(fileStream);
            }

            foreach (SerializableVector2 vector in loadedData)
            {
                spawnedTilesCoordinates.Add(vector);
            }
        }

        // Записуємо дані типу List<Vector2Int> у файл
        using (FileStream fileStream = new FileStream(outputPath, FileMode.Create))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, spawnedTilesCoordinates);
        }

        Debug.Log("Spawned tiles have been saved to a binary file.");
        System.GC.Collect();
    }

    public List<Vector2> LoadSpawnedTilesFromBinary()
    {
        List<SerializableVector2> loadedData = new List<SerializableVector2>();
        List<Vector2> loadedTiles = new List<Vector2>();

        string filePath = Path.Combine(Application.persistentDataPath, spawnedTilesBinary);

        if (File.Exists(filePath))
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                loadedData = (List<SerializableVector2>)binaryFormatter.Deserialize(fileStream);
            }

            Debug.Log("Spawned tiles have been loaded from the binary file.");
        }
        else
        {
            Debug.LogError("Spawned tiles binary file not found!");
        }

        foreach (SerializableVector2 tile in loadedData)
        {
            loadedTiles.Add(new Vector2() { x = tile.x, y = tile.y });
        }

        return loadedTiles;
    }
}
