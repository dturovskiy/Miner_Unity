using UnityEngine;

namespace AwesomeTools.Scene
{
    /// <summary>
    /// Отвечает за ключ к сцене
    /// </summary>
    [CreateAssetMenu(fileName = "SceneInfo", menuName = "Scene")]
    public class SceneInfo : ScriptableObject
    {
        [SerializeField] private string _sceneKey;

        public string Key => _sceneKey;
    }
}