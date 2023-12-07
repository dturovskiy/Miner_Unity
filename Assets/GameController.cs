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
        hero.transform.position = transformSaver.transform.position;
    }

    public void SaveGame()
    {
        // ַבונודעט דנף
        SavingService.SaveGame("SaveGame.json");
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene("MenuScene");
        SavingService.UpdateActiveSceneInfo("SaveGame.json");
    }

    public void LoadGame()
    {
        SavingService.LoadGame("SaveGame.json");
    }
}
