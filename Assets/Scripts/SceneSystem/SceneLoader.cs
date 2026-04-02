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

        private struct PendingTransition
        {
            public bool HasValue;
            public string SourceScene;
            public string TargetScene;
            public bool Reload;
            public bool IsLevelComplied;
            public bool FadeEnabled;
            public float Delay;
            public int ParticipantCount;
            public int RequestFrame;
            public float RequestTime;
        }

        private static PendingTransition pendingTransition;
        private static bool sceneLoadedHookRegistered;

        public static bool IsLevelComplied = true; // bool which provide whether scene was fully finished

        [SerializeField] private SceneInfo[] _scenesInfo;
        [SerializeField] private FadeScreenPanel _fadeScreenPanel;
        [SerializeField] private bool _isFadeScreenPanelEnable = true;

        public event Action<float> OnSceneLoadProgress;

        public bool IsFadeScreenPanelEnable => _isFadeScreenPanelEnable;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedHook()
        {
            if (sceneLoadedHookRegistered)
            {
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            sceneLoadedHookRegistered = true;
        }

        // Loads the specified scene with optional delay and fade screen panel
        public void LoadScene(string sceneKey, bool isLevelComplied = true, float delay = 1f)
        {
            IsLevelComplied = isLevelComplied;
            string currentScene = CurrentScene();
            Diag.Event(
                "Scene",
                "TransitionRequested",
                "Scene transition requested.",
                this,
                ("targetScene", sceneKey),
                ("currentScene", currentScene),
                ("isLevelComplied", isLevelComplied),
                ("delay", delay),
                ("fadeEnabled", _isFadeScreenPanelEnable));

            SceneInfo loadSceneInfo = GetScene(sceneKey);

            if (loadSceneInfo == null)
            {
                Diag.Warning(
                    "Scene",
                    "TransitionCancelled",
                    "Scene transition cancelled because target scene config was not found.",
                    this,
                    ("reason", "missingSceneConfig"),
                    ("targetScene", sceneKey),
                    ("currentScene", currentScene));
                return;
            }

            if (loadSceneInfo.Key == currentScene)
            {
                Diag.Event(
                    "Scene",
                    "TransitionCancelled",
                    "Scene transition cancelled because the target scene is already active.",
                    this,
                    ("reason", "alreadyCurrentScene"),
                    ("targetScene", loadSceneInfo.Key),
                    ("currentScene", currentScene));
                return;
            }

            int participantCount = PrepareCurrentSceneForTransition();
            Diag.Event(
                "Scene",
                "TransitionPrepared",
                "Current scene prepared for transition.",
                this,
                ("currentScene", currentScene),
                ("targetScene", loadSceneInfo.Key),
                ("participantCount", participantCount));

            PlayerPrefs.SetString(PREVIOUS_SCENE, currentScene);
            StartCoroutine(LoadScene(loadSceneInfo, delay, currentScene, participantCount, isLevelComplied));
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
            string currentScene = CurrentScene();
            Diag.Event(
                "Scene",
                "ReloadRequested",
                "Current scene reload requested.",
                this,
                ("currentScene", currentScene),
                ("isLevelComplied", isLevelComplied),
                ("delay", delay),
                ("fadeEnabled", _isFadeScreenPanelEnable));

            int participantCount = PrepareCurrentSceneForTransition();
            Diag.Event(
                "Scene",
                "TransitionPrepared",
                "Current scene prepared for reload.",
                this,
                ("currentScene", currentScene),
                ("targetScene", currentScene),
                ("participantCount", participantCount),
                ("reload", true));

            if (_fadeScreenPanel != null)
            {
                _fadeScreenPanel.FadeIn();
            }
            else
            {
                Diag.Error(
                    "Scene",
                    "FadeMissing",
                    "Scene reload requested with fade enabled but FadeScreenPanel is missing.",
                    this,
                    ("currentScene", currentScene));
                Debug.LogError("FadeScreenPanel is null");
            }

            yield return new WaitForSeconds(delay);

            TrackPendingTransition(
                currentScene,
                currentScene,
                true,
                isLevelComplied,
                _isFadeScreenPanelEnable,
                delay,
                participantCount);

            Diag.Event(
                "Scene",
                "TransitionStarted",
                "Scene reload async operation started.",
                this,
                ("currentScene", currentScene),
                ("targetScene", currentScene),
                ("reload", true));

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(currentScene);
            if (asyncLoad == null)
            {
                ClearPendingTransition();
                Diag.Error(
                    "Scene",
                    "TransitionCancelled",
                    "Scene reload async operation failed to start.",
                    this,
                    ("reason", "asyncLoadNull"),
                    ("currentScene", currentScene),
                    ("targetScene", currentScene),
                    ("reload", true));
            }
        }

        // Retrieves the scene information based on the specified scene type
        private SceneInfo GetScene(string sceneKey)
        {
            if (_scenesInfo != null && _scenesInfo.Length > 0)
            {
                SceneInfo sceneInfo = _scenesInfo.FirstOrDefault(info => info != null && info.Key == sceneKey);

                if (sceneInfo != null)
                {
                    return sceneInfo;
                }
                else
                {
                    Diag.Error(
                        "Scene",
                        "TransitionCancelled",
                        "Scene transition failed because the target scene key is not configured.",
                        this,
                        ("reason", "invalidSceneKey"),
                        ("targetScene", sceneKey));
                    Debug.LogError("Name of Scene in config is not correct");
                    return null;
                }
            }
            else
            {
                Diag.Error(
                    "Scene",
                    "TransitionCancelled",
                    "Scene transition failed because the scene config array is empty.",
                    this,
                    ("reason", "emptySceneConfigArray"),
                    ("targetScene", sceneKey));
                Debug.LogError("Array _scenesInfo is empty");
                return null;
            }
        }
            
        // Returns the name of the currently active scene
        private string CurrentScene()
            => SceneManager.GetActiveScene().name;

        //Coroutine to load the scene with optional delay and fade screen panel
        private IEnumerator LoadScene(SceneInfo loadSceneInfo, float delay, string sourceScene, int participantCount, bool isLevelComplied)
        {
            if (IsFadeScreenPanelEnable)
            {
                if (_fadeScreenPanel != null)
                {
                    _fadeScreenPanel.FadeIn();
                }
                else
                {
                    Diag.Error(
                        "Scene",
                        "FadeMissing",
                        "Scene transition requested with fade enabled but FadeScreenPanel is missing.",
                        this,
                        ("currentScene", CurrentScene()),
                        ("targetScene", loadSceneInfo != null ? loadSceneInfo.Key : string.Empty));
                    Debug.LogError("FadeScreenPanel is null");
                }
            }

            yield return new WaitForSeconds(delay);

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(loadSceneInfo.Key);
            if (asyncLoad == null)
            {
                Diag.Error(
                    "Scene",
                    "TransitionCancelled",
                    "Scene async load operation failed to start.",
                    this,
                    ("reason", "asyncLoadNull"),
                    ("currentScene", CurrentScene()),
                    ("targetScene", loadSceneInfo.Key));
                yield break;
            }

            TrackPendingTransition(
                sourceScene,
                loadSceneInfo.Key,
                false,
                isLevelComplied,
                _isFadeScreenPanelEnable,
                delay,
                participantCount);

            Diag.Event(
                "Scene",
                "TransitionStarted",
                "Scene async load operation started.",
                this,
                ("currentScene", CurrentScene()),
                ("targetScene", loadSceneInfo.Key),
                ("delay", delay),
                ("fadeEnabled", _isFadeScreenPanelEnable));

            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f); // Clamps the progress between 0 and 1
                OnSceneLoadProgress?.Invoke(progress);
                yield return null;
            }
        }

        private static void TrackPendingTransition(
            string sourceScene,
            string targetScene,
            bool reload,
            bool isLevelComplied,
            bool fadeEnabled,
            float delay,
            int participantCount)
        {
            RegisterSceneLoadedHook();

            pendingTransition = new PendingTransition
            {
                HasValue = true,
                SourceScene = sourceScene,
                TargetScene = targetScene,
                Reload = reload,
                IsLevelComplied = isLevelComplied,
                FadeEnabled = fadeEnabled,
                Delay = delay,
                ParticipantCount = participantCount,
                RequestFrame = Time.frameCount,
                RequestTime = Time.time
            };
        }

        private static void ClearPendingTransition()
        {
            pendingTransition = default;
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            if (!pendingTransition.HasValue)
            {
                return;
            }

            PendingTransition completedTransition = pendingTransition;
            pendingTransition = default;

            Diag.Event(
                "Scene",
                "TransitionCompleted",
                "Scene transition completed.",
                null,
                ("sourceScene", completedTransition.SourceScene),
                ("targetScene", completedTransition.TargetScene),
                ("loadedScene", scene.name),
                ("reload", completedTransition.Reload),
                ("isLevelComplied", completedTransition.IsLevelComplied),
                ("fadeEnabled", completedTransition.FadeEnabled),
                ("delay", completedTransition.Delay),
                ("participantCount", completedTransition.ParticipantCount),
                ("requestFrame", completedTransition.RequestFrame),
                ("requestTime", completedTransition.RequestTime),
                ("mode", mode.ToString()),
                ("targetMatched", string.Equals(scene.name, completedTransition.TargetScene, StringComparison.Ordinal)));
        }

        private int PrepareCurrentSceneForTransition()
        {
            int participantCount = 0;
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is ISceneTransitionSaveParticipant participant)
                {
                    participant.PrepareForSceneTransition();
                    participantCount++;
                }
            }

            return participantCount;
        }
    }
}
