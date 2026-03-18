using System.Collections.Concurrent;
using UnityEngine;

public sealed class DiagUnityLogRelay : MonoBehaviour
{
    [SerializeField] private bool includeStackTraceForErrorsOnly = true;

    private readonly ConcurrentQueue<PendingUnityLog> pendingLogs = new();

    private readonly struct PendingUnityLog
    {
        public PendingUnityLog(string condition, string stackTrace, LogType type)
        {
            Condition = condition;
            StackTrace = stackTrace;
            Type = type;
        }

        public string Condition { get; }
        public string StackTrace { get; }
        public LogType Type { get; }
    }

    private void OnEnable()
    {
        Application.logMessageReceivedThreaded += OnLogReceived;
    }

    private void OnDisable()
    {
        Application.logMessageReceivedThreaded -= OnLogReceived;
        FlushPendingLogs();
    }

    private void Update()
    {
        FlushPendingLogs();
    }

    private void OnLogReceived(string condition, string stackTrace, LogType type)
    {
        pendingLogs.Enqueue(new PendingUnityLog(condition, stackTrace, type));
    }

    private void FlushPendingLogs()
    {
        var manager = DiagManager.Instance;
        if (manager == null || !manager.IsReady)
        {
            return;
        }

        if (!manager.WriteUnityLogsToEvents)
        {
            while (pendingLogs.TryDequeue(out _))
            {
            }

            return;
        }

        while (pendingLogs.TryDequeue(out var entry))
        {
            string category = "Unity";
            string name = entry.Type.ToString();
            string payload = includeStackTraceForErrorsOnly && entry.Type is LogType.Log or LogType.Warning
                ? entry.Condition
                : entry.Condition + "\n" + entry.StackTrace;

            manager.Emit(
                entry.Type == LogType.Warning ? "Warning" : entry.Type is LogType.Error or LogType.Exception or LogType.Assert ? "Error" : "Info",
                category,
                name,
                payload,
                this,
                ("logType", entry.Type),
                ("hasStackTrace", !string.IsNullOrWhiteSpace(entry.StackTrace)));
        }
    }
}
