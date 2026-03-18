using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class DiagAutoSnapshot : MonoBehaviour
{
    [SerializeField] private string snapshotName = "Heartbeat";
    [SerializeField] private float intervalSeconds = 0.5f;
    [SerializeField] private bool emitOnEnable = true;
    [SerializeField] private float minimumHeartbeatIntervalSeconds = 2f;
    [SerializeField] private bool suppressUnchangedHeartbeats = true;

    private float nextTime;
    private IDiagSnapshotProvider[] providers;
    private string lastHeartbeatSignature;

    private void Awake()
    {
        providers = GetComponents<IDiagSnapshotProvider>();
    }

    private void OnEnable()
    {
        nextTime = Time.unscaledTime + GetEffectiveIntervalSeconds();
        if (emitOnEnable)
        {
            EmitSnapshot("OnEnable");
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < nextTime)
        {
            return;
        }

        nextTime = Time.unscaledTime + GetEffectiveIntervalSeconds();
        EmitSnapshot(snapshotName);
    }

    public void EmitSnapshot(string nameOverride = null)
    {
        string eventName = nameOverride ?? snapshotName;
        var fields = new List<DiagField>();
        for (int i = 0; i < providers.Length; i++)
        {
            providers[i].AppendSnapshot(fields);
        }

        string signature = BuildSignature(fields);
        if (ShouldSkipHeartbeat(eventName, signature))
        {
            return;
        }

        if (IsOnEnableHeartbeat(eventName))
        {
            lastHeartbeatSignature = signature;
        }

        // Use the GameObject as the event source so the record keeps a readable source name
        // without re-appending all component-level context providers on snapshot events.
        Diag.Event("Snapshot", eventName, null, gameObject, Convert(fields));
    }

    private static (string key, object value)[] Convert(List<DiagField> fields)
    {
        var result = new (string key, object value)[fields.Count];
        for (int i = 0; i < fields.Count; i++)
        {
            result[i] = (fields[i].key, fields[i].value);
        }
        return result;
    }

    private float GetEffectiveIntervalSeconds()
    {
        if (IsHeartbeatConfigured())
        {
            return Mathf.Max(intervalSeconds, minimumHeartbeatIntervalSeconds);
        }

        return intervalSeconds;
    }

    private bool ShouldSkipHeartbeat(string eventName, string signature)
    {
        if (!IsConfiguredHeartbeatEvent(eventName) || !suppressUnchangedHeartbeats)
        {
            return false;
        }

        if (string.Equals(lastHeartbeatSignature, signature, StringComparison.Ordinal))
        {
            return true;
        }

        lastHeartbeatSignature = signature;
        return false;
    }

    private bool IsOnEnableHeartbeat(string eventName)
    {
        return IsHeartbeatConfigured() && string.Equals(eventName, "OnEnable", StringComparison.Ordinal);
    }

    private bool IsConfiguredHeartbeatEvent(string eventName)
    {
        return IsHeartbeatConfigured() && string.Equals(eventName, snapshotName, StringComparison.Ordinal);
    }

    private bool IsHeartbeatConfigured()
    {
        return string.Equals(snapshotName, "Heartbeat", StringComparison.Ordinal);
    }

    private static string BuildSignature(List<DiagField> fields)
    {
        var builder = new StringBuilder(fields.Count * 24);
        for (int i = 0; i < fields.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('|');
            }

            builder.Append(fields[i].key);
            builder.Append('=');
            builder.Append(fields[i].value);
        }

        return builder.ToString();
    }
}
