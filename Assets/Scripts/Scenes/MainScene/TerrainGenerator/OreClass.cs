using UnityEngine;

[System.Serializable]
public class OreClass
{
    public string name;

    [Range(0f, 0.5f)] public float frequency;

    public int maxSpawnHeight;
    public int minSpawnHeight;
}
