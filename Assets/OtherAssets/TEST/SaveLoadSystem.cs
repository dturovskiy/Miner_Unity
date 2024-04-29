using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SaveLoadSystem : MonoBehaviour
{
    // Зберігаємо координати знищених блоків
    [SerializeField] private List<Vector2> destroyedTilesCoordinates = new List<Vector2>();
    [SerializeField] private List<SerializableVector2> spawnedTilesCoordinates = new List<SerializableVector2>();
    [SerializeField] private List<SerializableVector2> hiddenTilesCoordinates = new List<SerializableVector2>();
    [SerializeField] private SerializableVector2 heroPosition;
    private Vector3Int heroDefaultPosition = new Vector3Int(29, 250, 0);

    private const string fileName = "terrain_layout.bin";
    private const string spawnedTilesBinary = "spawned_tiles.bin";
    private const string hiddenTilesBinary = "hiden_tiles.bin";
    private const string heroPositionBinary = "hero_position.bin";

    // Метод для збереження координати знищеного блоку
    public void SaveDestroyedTiles(Vector2 coordinates)
    {
        destroyedTilesCoordinates.Add(coordinates);
    }

    public void SaveSpawnedTiles(Vector2 coordinates)
    {
        spawnedTilesCoordinates.Add(coordinates);
    }

    public void SaveHiddenTiles(Vector3Int coordinates)
    {
        SerializableVector2 serializableCoordinates = new SerializableVector2(coordinates.x, coordinates.y);
        hiddenTilesCoordinates.Add(serializableCoordinates);
    }

    public void SaveHeroPosition(Vector3 position)
    {
        heroPosition.x = position.x;
        heroPosition.y = position.y;
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
        SaveHidenTilesToBinary();
        SaveHeroPositionToBinary();
    }

    public void DeleteTerrainBinaryFile()
    {
        string deletedTiles = Path.Combine(Application.persistentDataPath, fileName);
        string spawnedTiles = Path.Combine(Application.persistentDataPath, spawnedTilesBinary);
        string hidenTiles = Path.Combine(Application.persistentDataPath, hiddenTilesBinary);
        string heroPosition = Path.Combine(Application.persistentDataPath, heroPositionBinary);

        if (File.Exists(deletedTiles))
        {
            File.Delete(deletedTiles);
            File.Delete(spawnedTiles);
            File.Delete(hidenTiles);
            File.Delete(heroPosition);
            Debug.Log("Terrain layout binary file has been deleted.");
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

        System.GC.Collect();
    }

    public void SaveHidenTilesToBinary()
    {
        string outputPath = Path.Combine(Application.persistentDataPath, hiddenTilesBinary);
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
                hiddenTilesCoordinates.Add(vector);
            }
        }

        // Записуємо дані типу List<Vector2Int> у файл
        using (FileStream fileStream = new FileStream(outputPath, FileMode.Create))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, hiddenTilesCoordinates);
        }

        Debug.Log("Hiden tiles have been saved to a binary file.");
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
        }

        foreach (SerializableVector2 tile in loadedData)
        {
            loadedTiles.Add(new Vector2() { x = tile.x, y = tile.y });
        }

        return loadedTiles;
    }
    public List<Vector3Int> LoadHidenTilesFromBinary()
    {
        List<SerializableVector2> loadedData = new List<SerializableVector2>();
        List<Vector3Int> loadedTiles = new List<Vector3Int>();

        string filePath = Path.Combine(Application.persistentDataPath, hiddenTilesBinary);

        if (File.Exists(filePath))
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                loadedData = (List<SerializableVector2>)binaryFormatter.Deserialize(fileStream);
            }
        }

        foreach (SerializableVector2 tile in loadedData)
        {
            loadedTiles.Add(new Vector3Int() { x = (int)tile.x, y = (int)tile.y, z = 0 });
        }

        return loadedTiles;
    }

    public void SaveHeroPositionToBinary()
    {
        string filePath = Path.Combine(Application.persistentDataPath, heroPositionBinary);

        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, heroPosition);
        }
    }

    public Vector3Int LoadHeroPositionFromBinary()
    {
        string filePath = Path.Combine(Application.persistentDataPath, heroPositionBinary);

        if (File.Exists(filePath))
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                SerializableVector2 serializedPosition = (SerializableVector2)binaryFormatter.Deserialize(fileStream);
                return new Vector3Int((int)serializedPosition.x, (int)serializedPosition.y, 0);
            }
        }

        return heroDefaultPosition;
    }
}
