using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AwesomeTools.Scene
{
    // uses for load certain scene
    public class SceneLoader : MonoBehaviour
    {
        private const string PREVIOUS_SCENE = "previousScene";

        public static bool IsLevelComplied = true; // bool which provide whether scene was fully finished

        [SerializeField] private SceneInfo[] _scenesInfo;
        [SerializeField] private FadeScreenPanel _fadeScreenPanel;
        [SerializeField] private bool _isFadeScreenPanelEnable = true;

        public event Action<float> OnSceneLoadProgress;

        public bool IsFadeScreenPanelEnable => _isFadeScreenPanelEnable;

        // Loads the specified scene with optional delay and fade screen panel
        public void LoadScene(string sceneKey, bool isLevelComplied = true, float delay = 1f)
        {
            IsLevelComplied = isLevelComplied;
            SceneInfo loadSceneInfo = GetScene(sceneKey);

            if (loadSceneInfo == null)
                return;

            if (loadSceneInfo.Key == CurrentScene())
                return;

            PlayerPrefs.SetString(PREVIOUS_SCENE, CurrentScene());
            StartCoroutine(LoadScene(loadSceneInfo, delay));
        }

        public void ReloadCurrentScene()
        {
            StartCoroutine(ReloadCurrentSceneStart(true, 1));
        }

        public void ReloadCurrentScene(bool isLevelComplied = true, float delay = 1f)
        {
            StartCoroutine(ReloadCurrentSceneStart(isLevelComplied, delay));
        }

        private IEnumerator ReloadCurrentSceneStart(bool isLevelComplied = true, float delay = 1f)
        {
            IsLevelComplied = isLevelComplied;

            if (_fadeScreenPanel != null)
                _fadeScreenPanel.FadeIn();
            else
                Debug.LogError("FadeScreenPanel is null");

            yield return new WaitForSeconds(delay);
            SceneManager.LoadSceneAsync(CurrentScene());
        }

        // Retrieves the scene information based on the specified scene type
        private SceneInfo GetScene(string sceneKey)
        {
            if (_scenesInfo != null && _scenesInfo.Length > 0)
            {
                SceneInfo sceneInfo = _scenesInfo.FirstOrDefault(info => info != null && info.Key == sceneKey);

                if (sceneInfo != null)
                {
                    Debug.Log(sceneKey);
                    return sceneInfo;
                }
                else
                {
                    Debug.LogError("Name of Scene in config is not correct");
                    return null;
                }
            }
            else
            {
                Debug.LogError("Array _scenesInfo is empty");
                return null;
            }
        }
            
        // Returns the name of the currently active scene
        private string CurrentScene()
            => SceneManager.GetActiveScene().name;

        //Coroutine to load the scene with optional delay and fade screen panel
        private IEnumerator LoadScene(SceneInfo loadSceneInfo, float delay)
        {
            if (IsFadeScreenPanelEnable)
            {
                if (_fadeScreenPanel != null)
                    _fadeScreenPanel.FadeIn();
                else
                    Debug.LogError("FadeScreenPanel is null");
            }

            yield return new WaitForSeconds(delay);

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(loadSceneInfo.Key);

            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f); // Clamps the progress between 0 and 1
                OnSceneLoadProgress?.Invoke(progress);
                yield return null;
            }
        }
    }
}