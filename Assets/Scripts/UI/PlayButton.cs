using AwesomeTools.Scene;
using AwesomeTools.UI;
using System;
using System.Collections;
using UnityEngine;

namespace AwesomeTools.MainScene
{
    public class PlayButton : HudElement
    {
        [Header("PlayButton")]
        [SerializeField] private SceneData _sceneData;
        [SerializeField] private SceneLoader _sceneLoader;

        public event Action OnButtonIsPressed;

        /// <summary>
        /// Показує кнопку для початку гри
        /// </summary>
        //private void Start()
        //{
        //    //Appear();
        //}

        /// <summary>
        /// Запускає процес переходу у сцену "QuestScene"
        /// </summary>
        public void GoToMainScene()
        {
            //Disappear();
            StartCoroutine(LoadScene(_sceneData));
        }
        public void LoadGame()
        {
            LoadScene();
        }

        public void InvokeOnButtonIsPressed()
        {
            OnButtonIsPressed?.Invoke();
            //Disappear();
        }

        /// <summary>
        /// Вводимо тип сцени [type] - переходимо у сцену "type"
        /// </summary>    
        private IEnumerator LoadScene(SceneData type)
        {
            yield return new WaitForSeconds(1);
            Debug.Log(type.Key);
            _sceneLoader.LoadScene(_sceneData.Key);
        }
        private IEnumerator LoadScene()
        {
            yield return new WaitForSeconds(1);
            SavingService.LoadGame("SaveGame.json");
        }
    }
}