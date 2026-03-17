using UnityEngine;

[CreateAssetMenu(fileName = "TileAtlas", menuName = "Tile Atlas")]
public class TileAtlas : ScriptableObject
{
    [Header("Environment")]
    public TileClass grass;
    public TileClass dirt;
    public TileClass stone;
    public TileClass tunnel;
    public TileClass ladder;

    [Header("Ores")]
    public TileClass coal;
    public TileClass iron;
    public TileClass gold;
    public TileClass diamond;
    public TileClass silver;
    public TileClass platinum;
    public TileClass uranus;
    public TileClass lazurite;
    public TileClass topaz;
    public TileClass emerald;
    public TileClass nephritis;
    public TileClass ruby;
    public TileClass amethyst;
    public TileClass opal;
    public TileClass map;
    public TileClass artifact;
}
