using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DiagManager : MonoBehaviour
{
    public static DiagManager Instance { get; private set; }

    [Header("Storage")]
    [SerializeField] private string rootFolderPath = @"E:\Logs\Miner";
    [SerializeField] private bool fallbackToPersistentDataPath = true;
    [SerializeField] private bool writeUnityLogsToEvents = true;

    [Header("Output")]
    [SerializeField] private bool duplicateToConsole = false;
    [SerializeField] private int tailCapacity = 500;

    private readonly Queue<string> tail = new();
    private readonly Dictionary<string, int> eventCounts = new();
    private readonly object gate = new();

    private string sessionId;
    private string eventsFilePath;
    private string summaryFilePath;
    private string latestSessionPathFile;
    private string sessionFolderPath;
    private string sessionFolderName;
    private DateTime startedUtc;
    private bool shuttingDown;

    public bool IsReady { get; private set; }
    public string SessionFolderPath => sessionFolderPath;
    public bool WriteUnityLogsToEvents => writeUnityLogsToEvents;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeSession();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.quitting += OnApplicationQuitting;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.quitting -= OnApplicationQuitting;
    }

    public void Emit(string level, string category, string name, string message, UnityEngine.Object context, params (string key, object value)[] fields)
    {
        if (!IsReady) return;

        var record = new DiagRecord
        {
            sessionId = sessionId,
            category = category,
            name = name,
            level = level,
            scene = SceneManager.GetActiveScene().name,
            source = context != null ? context.name : string.Empty,
            frame = Time.frameCount,
            time = Time.time,
            message = message,
            fields = DiagFieldBag.Create(fields)
        };

        TryAppendObjectContext(context, record.fields);
        WriteRecord(record);
    }

    public string[] GetTail()
    {
        lock (gate)
        {
            return tail.ToArray();
        }
    }

    private void InitializeSession()
    {
        startedUtc = DateTime.UtcNow;
        sessionId = Guid.NewGuid().ToString("N");

        string root = rootFolderPath;
        try
        {
            Directory.CreateDirectory(root);
        }
        catch
        {
            if (!fallbackToPersistentDataPath)
            {
                throw;
            }

            root = Path.Combine(Application.persistentDataPath, "DiagnosticsSessions");
            Directory.CreateDirectory(root);
        }

        sessionFolderName = "session-" + startedUtc.ToString("yyyyMMdd-HHmmss") + "-" + sessionId.Substring(0, 6);
        sessionFolderPath = Path.Combine(root, sessionFolderName);
        Directory.CreateDirectory(sessionFolderPath);

        eventsFilePath = Path.Combine(sessionFolderPath, "events.jsonl");
        summaryFilePath = Path.Combine(sessionFolderPath, "summary.txt");
        latestSessionPathFile = Path.Combine(root, "latest-session-path.txt");

        File.WriteAllText(latestSessionPathFile, sessionFolderPath, Encoding.UTF8);
        WriteSessionJson();

        IsReady = true;
        Emit("Info", "General", "SessionStarted", "Diagnostics session started.", this,
            ("sessionFolder", sessionFolderPath),
            ("rootFolder", root));
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Emit("Info", "General", "SceneLoaded", $"Scene loaded: {scene.name}", this,
            ("scene", scene.name),
            ("mode", mode));
    }

    private void OnApplicationQuitting()
    {
        shuttingDown = true;
        WriteSummary();
        WriteSessionJson();
    }

    private void WriteRecord(DiagRecord record)
    {
        string json = JsonUtility.ToJson(record);

        lock (gate)
        {
            File.AppendAllText(eventsFilePath, json + Environment.NewLine, Encoding.UTF8);

            if (tail.Count >= tailCapacity)
            {
                tail.Dequeue();
            }

            tail.Enqueue(json);

            string key = record.category + "/" + record.name;
            if (!eventCounts.TryAdd(key, 1))
            {
                eventCounts[key]++;
            }
        }

        if (duplicateToConsole)
        {
            Debug.Log($"[DIAG][{record.category}/{record.name}] {record.message}");
        }
    }

    private void TryAppendObjectContext(UnityEngine.Object context, List<DiagField> fields)
    {
        if (context is not Component component)
        {
            return;
        }

        var providers = component.GetComponents<IDiagContextProvider>();
        for (int i = 0; i < providers.Length; i++)
        {
            providers[i].AppendContext(fields);
        }
    }

    private void WriteSessionJson()
    {
        var session = new DiagSessionInfo
        {
            sessionId = sessionId,
            projectName = Application.productName,
            companyName = Application.companyName,
            productName = Application.productName,
            unityVersion = Application.unityVersion,
            platform = Application.platform.ToString(),
            appVersion = Application.version,
            buildType = Debug.isDebugBuild ? "Development" : "Release",
            startedUtc = startedUtc.ToString("O"),
            endedUtc = shuttingDown ? DateTime.UtcNow.ToString("O") : null,
            sessionFolderName = sessionFolderName,
            sessionFolderPath = sessionFolderPath,
            eventsFilePath = eventsFilePath,
            summaryFilePath = summaryFilePath
        };

        string sessionJson = JsonUtility.ToJson(session, true);
        File.WriteAllText(Path.Combine(sessionFolderPath, "session.json"), sessionJson, Encoding.UTF8);
    }

    private void WriteSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Miner Diagnostics Summary");
        sb.AppendLine("========================");
        sb.AppendLine($"SessionId: {sessionId}");
        sb.AppendLine($"Project: {Application.productName}");
        sb.AppendLine($"Unity: {Application.unityVersion}");
        sb.AppendLine($"Platform: {Application.platform}");
        sb.AppendLine($"BuildType: {(Debug.isDebugBuild ? "Development" : "Release")}");
        sb.AppendLine($"StartedUtc: {startedUtc:O}");
        sb.AppendLine($"EndedUtc: {DateTime.UtcNow:O}");
        sb.AppendLine($"SessionFolder: {sessionFolderPath}");
        sb.AppendLine($"EventsFile: {eventsFilePath}");
        sb.AppendLine();
        sb.AppendLine("Event Counts");
        sb.AppendLine("------------");

        foreach (var pair in eventCounts)
        {
            sb.AppendLine($"{pair.Key}: {pair.Value}");
        }

        sb.AppendLine();
        sb.AppendLine("How to share");
        sb.AppendLine("------------");
        sb.AppendLine("Zip the whole session folder and send it together with reproduction steps.");

        File.WriteAllText(summaryFilePath, sb.ToString(), Encoding.UTF8);
    }
}
