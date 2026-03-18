using System.Collections.Generic;
using UnityEngine;

public static class Diag
{
    public static bool IsReady => DiagManager.Instance != null && DiagManager.Instance.IsReady;

    public static void Event(string category, string name, string message = null, Object context = null, params (string key, object value)[] fields)
    {
        if (DiagManager.Instance == null) return;
        DiagManager.Instance.Emit("Info", category, name, message, context, fields);
    }

    public static void Warning(string category, string name, string message = null, Object context = null, params (string key, object value)[] fields)
    {
        if (DiagManager.Instance == null) return;
        DiagManager.Instance.Emit("Warning", category, name, message, context, fields);
    }

    public static void Error(string category, string name, string message = null, Object context = null, params (string key, object value)[] fields)
    {
        if (DiagManager.Instance == null) return;
        DiagManager.Instance.Emit("Error", category, name, message, context, fields);
    }

    public static void Snapshot(string name, Object context = null, params (string key, object value)[] fields)
    {
        if (DiagManager.Instance == null) return;
        DiagManager.Instance.Emit("Info", "Snapshot", name, null, context, fields);
    }

    public static string GetSessionFolderPath()
    {
        return DiagManager.Instance == null ? string.Empty : DiagManager.Instance.SessionFolderPath;
    }

    public static string[] GetTail()
    {
        if (DiagManager.Instance == null) return System.Array.Empty<string>();
        return DiagManager.Instance.GetTail();
    }
}
