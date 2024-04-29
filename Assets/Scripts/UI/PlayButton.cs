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
        [SerializeField] private SaveLoadSystem _saveLoadSystem;

        public event Action OnButtonIsPressed;

        private void Awake()
        {
            SavingService.UpdateActiveSceneInfo("SaveGame.json");
        }

        public void GoToMainScene()
        {
            //Disappear();
            _saveLoadSystem.DeleteTerrainBinaryFile();
            StartCoroutine(LoadScene(_sceneData));
        }
        public void ContinueGame()
        {
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
            SavingService.UpdateActiveSceneInfo("SaveGame.json");
        }

        private IEnumerator LoadScene()
        {
            yield return new WaitForSeconds(1);
            
        }
    }
}