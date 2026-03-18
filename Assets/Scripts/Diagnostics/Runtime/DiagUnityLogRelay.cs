using System;
using UnityEngine;

public sealed class DiagUnityLogRelay : MonoBehaviour
{
    [SerializeField] private bool includeStackTraceForErrorsOnly = true;

    private void OnEnable()
    {
        Application.logMessageReceivedThreaded += OnLogReceived;
    }

    private void OnDisable()
    {
        Application.logMessageReceivedThreaded -= OnLogReceived;
    }

    private void OnLogReceived(string condition, string stackTrace, LogType type)
    {
        var manager = DiagManager.Instance;
        if (manager == null || !manager.IsReady || !manager.WriteUnityLogsToEvents)
        {
            return;
        }

        string category = "Unity";
        string name = type.ToString();
        string payload = includeStackTraceForErrorsOnly && type is LogType.Log or LogType.Warning ? condition : condition + "\n" + stackTrace;

        manager.Emit(type == LogType.Warning ? "Warning" : type is LogType.Error or LogType.Exception or LogType.Assert ? "Error" : "Info",
            category,
            name,
            payload,
            this,
            ("logType", type),
            ("hasStackTrace", !string.IsNullOrWhiteSpace(stackTrace)));
    }
}
