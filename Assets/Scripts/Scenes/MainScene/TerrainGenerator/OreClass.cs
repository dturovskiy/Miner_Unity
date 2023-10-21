using UnityEngine;

[System.Serializable]
public class OreClass
{
    public string name;

    [Range(0f, 1f)] public float frequency;
    [Range(0f, 1f)] public float size;
    public int maxSpawnHeight;
    public int minSpawnHeight;
    public Texture2D spreadTexture;
}
