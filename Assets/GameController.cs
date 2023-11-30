using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public void SaveGame()
    {
        SavingService.SaveGame("SaveGame.json");
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene("MenuScene");
    }
}
