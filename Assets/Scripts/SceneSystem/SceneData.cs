using UnityEngine;

    /// <summary>
    /// Класс який приймає/зберігає/редагує назву сцени як string
    /// </summary>
    [System.Serializable]
    public class SceneData
    {
        /// <summary>
        /// Використовується для вводу нової назви сцени через інспектор
        /// </summary>
        [SerializeField] private string sceneKey;

        /// <summary>
        /// Використовується для читання поточної назви сцени
        /// </summary>
        public string Key => sceneKey;

        /// <summary>
        /// Використовується для вводу нової назви сцени
        /// </summary>
        public SceneData(string key)
        {
            sceneKey = key;
        }
    }

