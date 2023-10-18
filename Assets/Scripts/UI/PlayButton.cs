using AwesomeTools.Scene;
using System.Collections;
using AwesomeTools.UI;
using UnityEngine;
using System;

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
    }
}