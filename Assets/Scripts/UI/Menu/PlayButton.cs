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
            Diag.Event(
                "UI",
                "NewGameRequested",
                "New game was requested from the play button.",
                this,
                ("targetScene", _sceneData != null ? _sceneData.Key : string.Empty));
            GameLaunchContext.RequestNewGame();
            StartCoroutine(LoadScene(_sceneData));
        }

        public void ContinueGame()
        {
            Diag.Event(
                "UI",
                "ContinueRequested",
                "Continue was requested from the play button.",
                this,
                ("targetScene", _sceneData != null ? _sceneData.Key : string.Empty));
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
            if (type == null)
            {
                Diag.Error(
                    "UI",
                    "SceneNavigationRejected",
                    "Play button navigation failed because SceneData is missing.",
                    this,
                    ("reason", "missingSceneData"));
                yield break;
            }

            if (_sceneLoader == null)
            {
                Diag.Error(
                    "UI",
                    "SceneNavigationRejected",
                    "Play button navigation failed because SceneLoader is missing.",
                    this,
                    ("reason", "missingSceneLoader"),
                    ("targetScene", type.Key));
                yield break;
            }

            _sceneLoader.LoadScene(type.Key);
        }

        private IEnumerator LoadScene()
        {
            yield return new WaitForSeconds(1);
        }
    }
}
