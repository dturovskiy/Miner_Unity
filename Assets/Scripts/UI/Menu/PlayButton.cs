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
            GameLaunchContext.RequestNewGame();
            StartCoroutine(LoadScene(_sceneData));
        }

        public void ContinueGame()
        {
            GameLaunchContext.RequestContinue();
            StartCoroutine(LoadScene(_sceneData));
        }

        public void InvokeOnButtonIsPressed()
        {
            OnButtonIsPressed?.Invoke();
        }

        private IEnumerator LoadScene(SceneData type)
        {
            yield return new WaitForSeconds(1);
            _sceneLoader.LoadScene(type.Key);
        }

        private IEnumerator LoadScene()
        {
            yield return new WaitForSeconds(1);
        }
    }
}
