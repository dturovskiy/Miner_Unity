using System.Collections.Generic;
using UnityEngine;

public sealed class DiagAutoSnapshot : MonoBehaviour
{
    [SerializeField] private string snapshotName = "Heartbeat";
    [SerializeField] private float intervalSeconds = 0.5f;
    [SerializeField] private bool emitOnEnable = true;

    private float nextTime;
    private IDiagSnapshotProvider[] providers;

    private void Awake()
    {
        providers = GetComponents<IDiagSnapshotProvider>();
    }

    private void OnEnable()
    {
        nextTime = Time.unscaledTime;
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

        nextTime = Time.unscaledTime + intervalSeconds;
        EmitSnapshot(snapshotName);
    }

    public void EmitSnapshot(string nameOverride = null)
    {
        var fields = new List<DiagField>();
        for (int i = 0; i < providers.Length; i++)
        {
            providers[i].AppendSnapshot(fields);
        }

        // Use the GameObject as the event source so the record keeps a readable source name
        // without re-appending all component-level context providers on snapshot events.
        Diag.Event("Snapshot", nameOverride ?? snapshotName, null, gameObject, Convert(fields));
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
}
