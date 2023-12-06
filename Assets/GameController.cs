using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public GameObject hero;
    public TransformSaver transformSaver;

    private void Start()
    {
        // Перевірте, чи існує файл збереження гри
        bool isLoadGame = SavingService.CheckForSavedGame("SaveGame.json");

        if (isLoadGame)
        {
            // Завантажте гру з файлу JSON
            SavingService.LoadGame("SaveGame.json");

            // Відновлення позиції героя після завантаження гри
            if (hero != null && transformSaver != null)
            {
                hero.transform.position = transformSaver.transform.position;
            }
            else
            {
                Debug.LogWarning("Hero or TransformSaver is null. Make sure to assign them in the inspector.");
            }
        }
        else
        {
            // Генеруйте новий терен, якщо це нова гра
            GenerateTerrain();
        }
    }

    public void SaveGame()
    {
        // Зберегти гру
        SavingService.SaveGame("SaveGame.json");
    }

    public void LoadMenu()
    {
        // Завантажити меню
        SceneManager.LoadScene("MenuScene");
    }

    // Генерувати новий терен
    private void GenerateTerrain()
    {
        TerrainGeneration terrainGeneration = FindObjectOfType<TerrainGeneration>();
        if (terrainGeneration != null)
        {
            terrainGeneration.GenerateTerrain();
        }
        else
        {
            Debug.LogError("TerrainGeneration script not found in the scene.");
        }
    }
}
