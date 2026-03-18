using AwesomeTools.Scene;
using UnityEngine;
using System.Collections;

namespace AwesomeTools.UI
{
    public class ExitButton : HudElement
    {
        [Header("ExitButton")]
        [SerializeField] public SceneData _data; // here you can write the name of the scene you want to move
        [SerializeField] private SceneLoader _sceneLoader;

        // Starts the panel animation by making it appear
        //private void Start()
        //{
        //    Appear();
        //}

        // Navigates to name of scene which you have written
        public void GoToOtherScene()
        {
            //Click();
            //EndCycle();
            StartCoroutine(LoadScene(_data));
        }

        public void ChangeSceneData(string nameOfScene)
        {
            SceneData newSceneData = new SceneData(nameOfScene);
            _data = newSceneData;
        }

        /// <summary>
        /// Вводимо тип сцени [type] - переходимо у сцену "type"
        /// </summary>    
        private IEnumerator LoadScene(SceneData type, bool isComplied = false)
        {
            yield return new WaitForSeconds(1);
            
            if(type.Key == "")
            {
                Debug.LogError("SceneData is empty, please write the name of the scene");
            }
            else
            {
                if(_sceneLoader != null)
                    _sceneLoader.LoadScene(type.Key, isComplied);
                else
                    Debug.LogError("In ExitButton SceneLoader is null");
            }
        }

        // Exits the game
        public void ExitGame()
        {
            Debug.Log("Quit");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif

#if UNITY_ANDROID
            Application.Quit();
#endif
        }

        // Ends the panel animation by making it disappear
        //private void EndCycle()
        //{
        //    Disappear();
        //}
    }
}