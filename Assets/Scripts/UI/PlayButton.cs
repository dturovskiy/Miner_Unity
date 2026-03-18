using AwesomeTools.Scene;
using MinerUnity.Runtime;
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

        public void GoToMainScene()
        {
            GamePersistenceService.ResetForNewGame();
            StartCoroutine(LoadScene(_sceneData));
        }

        public void ContinueGame()
        {
            StartCoroutine(LoadScene(_sceneData));
        }

        public void InvokeOnButtonIsPressed()
        {
            OnButtonIsPressed?.Invoke();
        }

        private IEnumerator LoadScene(SceneData type)
        {
            yield return new WaitForSeconds(1);
            Debug.Log(type.Key);
            _sceneLoader.LoadScene(_sceneData.Key);
        }

        private IEnumerator LoadScene()
        {
            yield return new WaitForSeconds(1);
        }
    }
}
