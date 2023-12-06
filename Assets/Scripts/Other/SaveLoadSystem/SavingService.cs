using LitJson;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Any MonoBehaviour that implements the ISaveable interface will be saved
// in the scene, and loaded back
public interface ISaveable
{
    // The Save ID is a unique string that identifies a component in the
    // save data. It's used for finding that object again when the game is loaded.
    string SaveID { get; }

    // The SavedData is the content that will be written to disk. It's
    // asked for when the game is saved.
    JsonData SavedData { get; }

    /// <summary>
    /// LoadFromData is called when the game is being loaded. The object is provided with the data that was read, and is expected to use that information to restore its previous state.
    /// </summary>
    /// <param name="data"></param>
    void LoadFromData(JsonData data);
}

public static class SavingService
{
    private const string ACTIVE_SCENE_KEY = "activeScene";
    private const string SCENES_KEY = "scenes";
    private const string OBJECTS_KEY = "object";
    private const string SAVEID_KEY = "$saveID";

    // A reference to the delegate that runs after the scene loads, which performs the object state restoration.
    static UnityEngine.Events.UnityAction<Scene, LoadSceneMode> LoadObjectsAfterSceneLoad;

    /// <summary>
    /// Saves the game, and writes it to a file called fileName in the app's persistent data directory.
    /// </summary>
    /// <param name="fileName"></param>
    public static void SaveGame(string fileName)
    {
        var result = new JsonData();

        var allSaveableObject = Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();

        if (allSaveableObject.Count() > 0)
        {
            var savedOjects = new JsonData();

            foreach (var saveableObject in allSaveableObject)
            {
                var data = saveableObject.SavedData;

                if (data.IsObject)
                {
                    data[SAVEID_KEY] = saveableObject.SaveID;
                    savedOjects.Add(data);
                }

                else
                {
                    var behaviour = saveableObject as MonoBehaviour;

                    Debug.LogWarningFormat(behaviour, "{0}'s save data is not a dictionary. The " + "object was not saved.", behaviour.name);
                }
            }

            result[OBJECTS_KEY] = savedOjects;
        }

        else
        {
            Debug.LogWarningFormat("The scene did not include any saveable objects.");
        }

        var openScenes = new JsonData();

        var sceneCount = SceneManager.sceneCount;

        for (int i = 0; i < sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);

            openScenes.Add(scene.name);
        }

        result[SCENES_KEY] = openScenes;
        result[ACTIVE_SCENE_KEY] = SceneManager.GetActiveScene().name;

        var outputPath = Path.Combine(Application.persistentDataPath, fileName);

        var writer = new JsonWriter();
        writer.PrettyPrint = true;

        result.ToJson(writer);

        File.WriteAllText(outputPath, writer.ToString());

        Debug.LogFormat("Wrote saved game to {0}", outputPath);

        result = null;
        System.GC.Collect();
    }

    public static bool LoadGame(string fileName)
    {
        var dataPath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(dataPath) == false)
        {
            Debug.LogErrorFormat("No file exists at {0}", dataPath); 
            return false;
        }

        var text = File.ReadAllText(dataPath);
        var data = JsonMapper.ToObject(text);

        if(data == null || data.IsObject == false)
        {
            Debug.LogErrorFormat("Data at {0} is not a JSON object", dataPath);
            return false;
        }

        if (!data.ContainsKey("scenes"))
        {
            Debug.LogWarningFormat("Data at {0} does not contain any scenes; not " + "loading any!", dataPath);
            return false;
        }

        var scenes = data[SCENES_KEY];
        int sceneCount = scenes.Count;

        if (sceneCount == 0)
        {
            Debug.LogWarningFormat("Data at {0} doesn't specify any scenes to load.", dataPath);
            return false;
        }

        for (int i = 0; i < sceneCount; i++)
        {
            var scene = (string)scenes[i];

            if(i == 0)
            {
                SceneManager.LoadScene(scene, LoadSceneMode.Single);
            }

            else
            {
                SceneManager.LoadScene(scene, LoadSceneMode.Additive);
            }
        }

        if (data.ContainsKey(ACTIVE_SCENE_KEY))
        {
            var activeSceneName = (string)data[ACTIVE_SCENE_KEY];
            var activeScene = SceneManager.GetSceneByName(activeSceneName);

            if(activeScene.IsValid() == false)
            {
                Debug.LogErrorFormat("Data at {0} specifies an active scene that " + "doesn't exist. Stopping loading here.", dataPath);
                return false;
            }

            SceneManager.SetActiveScene(activeScene);
        }

        else
        {
            Debug.LogWarningFormat("Data at {0} does not specify an " + "active scene.", dataPath);
        }

        if (data.ContainsKey(OBJECTS_KEY))
        {
            var objects = data[OBJECTS_KEY];

            LoadObjectsAfterSceneLoad = (scene, loadSceneMode) =>
            {
                var allLoadableObjects = Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToDictionary(o => o.SaveID, o => o);
                var objectsCount = objects.Count;

                for (int i = 0; i < objectsCount; i++)
                {
                    var objectData = objects[i];

                    if (objectData.ContainsKey(SAVEID_KEY) && objectData[SAVEID_KEY] != null)
                    {
                        // Get the Save ID from that data
                        var saveID = (string)objectData[SAVEID_KEY];

                        // Attempt to find the object in the scene(s) that has
                        // that Save ID
                        if (allLoadableObjects.ContainsKey(saveID))
                        {
                            var loadableObject = allLoadableObjects[saveID];

                            // Ask the object to load from this data
                            loadableObject.LoadFromData(objectData);
                        }
                    }
                }

                SceneManager.sceneLoaded -= LoadObjectsAfterSceneLoad;
                LoadObjectsAfterSceneLoad = null;
                System.GC.Collect();
            };

            SceneManager.sceneLoaded += LoadObjectsAfterSceneLoad;
        }

        return true;
    }

    public static void UpdateActiveSceneInfo(string fileName)
    {
        var outputPath = Path.Combine(Application.persistentDataPath, fileName);

        // Перевірити, чи існує файл
        if (File.Exists(outputPath))
        {
            // Прочитати існуючий JSON з файлу
            var existingData = JsonMapper.ToObject(File.ReadAllText(outputPath));

            // Отримати поточну сцену
            var activeScene = SceneManager.GetActiveScene().name;

            // Перевірити, чи є активна сцена в існуючому JSON
            if (existingData.ContainsKey(ACTIVE_SCENE_KEY))
            {
                // Оновити інформацію про поточну сцену
                existingData[ACTIVE_SCENE_KEY] = activeScene;

                // Створити JsonWriter з параметром PrettyPrint
                var writer = new JsonWriter();
                writer.PrettyPrint = true;

                // Записати оновлений JSON-текст назад в файл
                existingData.ToJson(writer);
                File.WriteAllText(outputPath, writer.ToString());
                Debug.LogFormat("Updated active scene info in {0}", outputPath);
            }
            else
            {
                // Якщо в існуючому JSON немає необхідного ключа для активної сцени, вивести попередження
                Debug.LogWarningFormat("Existing JSON data is missing active scene information. Unable to update active scene info in {0}", outputPath);
            }
        }
        else
        {
            // Якщо файл не існує, вивести відповідне повідомлення
            Debug.LogWarningFormat("File does not exist at {0}. Cannot update active scene info.", outputPath);
        }
    }

    // Метод для перевірки існування файлу збереження гри
    public static bool CheckForSavedGame(string fileName)
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        return File.Exists(filePath);
    }
}
