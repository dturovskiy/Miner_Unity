using System.Collections.Generic;
using UnityEngine;

public sealed class DiagMarker : MonoBehaviour, IDiagContextProvider
{
    [SerializeField] private string entityType = "Object";
    [SerializeField] private string entityId = "";
    [SerializeField] private bool includeTransform = true;

    public void AppendContext(List<DiagField> fields)
    {
        fields.Add(new DiagField("entityType", entityType));
        fields.Add(new DiagField("entityId", string.IsNullOrWhiteSpace(entityId) ? gameObject.name : entityId));
        fields.Add(new DiagField("gameObject", gameObject.name));

        if (includeTransform)
        {
            fields.Add(new DiagField("position", DiagFieldBag.Stringify(transform.position)));
        }
    }
}
