using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DiagRecord
{
    public string sessionId;
    public string category;
    public string name;
    public string level;
    public string scene;
    public string source;
    public int frame;
    public float time;
    public string message;
    public List<DiagField> fields = new();
}

[Serializable]
public sealed class DiagField
{
    public string key;
    public string value;

    public DiagField() { }

    public DiagField(string key, string value)
    {
        this.key = key;
        this.value = value;
    }
}

public static class DiagFieldBag
{
    public static List<DiagField> Create(params (string key, object value)[] pairs)
    {
        var list = new List<DiagField>(pairs.Length);
        for (int i = 0; i < pairs.Length; i++)
        {
            list.Add(new DiagField(pairs[i].key, Stringify(pairs[i].value)));
        }
        return list;
    }

    public static string Stringify(object value)
    {
        if (value == null) return "null";
        return value switch
        {
            Vector2 v2 => $"({v2.x:0.###},{v2.y:0.###})",
            Vector3 v3 => $"({v3.x:0.###},{v3.y:0.###},{v3.z:0.###})",
            Bounds b => $"center={b.center} size={b.size}",
            _ => value.ToString()
        };
    }
}
