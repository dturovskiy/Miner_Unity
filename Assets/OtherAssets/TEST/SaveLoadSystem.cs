using LitJson;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveLoadSystem : MonoBehaviour
{
    // Зберігаємо координати знищених блоків
    [SerializeField] private List<Vector2> destroyedBlockCoordinates = new List<Vector2>();
    private const string fileName = "terrain_layout.json";

    // Метод для збереження координати знищеного блоку
    public void SaveDestroyedBlock(Vector2 coordinates)
    {
        destroyedBlockCoordinates.Add(coordinates);
    }

    // Метод для видалення знищених блоків з файлу
    public void RemoveDestroyedBlocksFromJson()
    {
        string outputPath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(outputPath))
        {
            // Прочитати JSON з файлу
            var jsonData = JsonMapper.ToObject(File.ReadAllText(outputPath));

            // Видаляємо координати знищених блоків із списку
            foreach (var coordinate in destroyedBlockCoordinates)
            {
                // Знайти та видалити блок із списку
                foreach (JsonData jsonDataItem in jsonData)
                {
                    if ((int)jsonDataItem["X"] == (int)coordinate.x && ((int)jsonDataItem["Y"]) == (int)coordinate.y)
                    {
                        jsonData.Remove(jsonDataItem);
                        break; // Виходимо з циклу після видалення блоку
                    }
                }
            }

            // Записати оновлений JSON-об'єкт назад у файл
            var writer = new JsonWriter();
            writer.PrettyPrint = true;
            jsonData.ToJson(writer);
            File.WriteAllText(outputPath, writer.ToString());

            Debug.Log("Destroyed blocks have been removed from the terrain layout.");
        }
        else
        {
            Debug.LogError("Terrain layout file not found!");
        }
    }

    public void DeleteTerrainJsonFile()
    {
        string outputPath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
            Debug.Log("Terrain layout file has been deleted.");
        }
        else
        {
            Debug.LogWarning("Terrain layout file not found. Nothing to delete.");
        }
    }
}
