using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Класс, что отвечает за сцены и словарь сцен
/// </summary>
public class ConfigChanger : MonoBehaviour
{
    [SerializeField] private LevelConfig _config;
    private Dictionary<string, SceneData> _sceneCodeDictionary;
    private string _sceneName;


    /// <summary>
    /// Установливет новый тип сцены в конфиге в зависимости от названия сцены
    /// </summary>
    private void Awake()
    {
        _sceneCodeDictionary = new Dictionary<string, SceneData>()
        {
            { "MenuScene", new SceneData("MenuScene") },
            { "MainScene", new SceneData("MainScene") },
        };
    }
    /// <summary>
    /// Получает при старте имена сцен
    /// </summary>
    private void Start()
        => GetSceneName();
    /// <summary>
    /// Получение имени сцены
    /// </summary>
    private void GetSceneName()
    {
        _sceneName = SceneManager.GetActiveScene().name;
        SelectorForCorrectScene();
    }
    /// <summary>
    /// Выбирает конфиг для сцены, если его нет, то ничего не вызывает
    /// </summary>
    private void SelectorForCorrectScene()
    {
        if (_sceneCodeDictionary.ContainsKey(_sceneName))
        {
            SceneData sceneType = _sceneCodeDictionary[_sceneName];
            _config.sceneType = sceneType;
            Debug.Log("Scene type: " + _config.sceneType.Key);
        }
        else
        {
            Debug.Log("This config was not found " + _sceneName);
        }
    }
}