using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TileAtlas", menuName = "Tile Atlas")]
public class TileAtlas : ScriptableObject
{
    [Header("Environment")]
    public TileClass grass;
    public TileClass dirt;
    public TileClass stone;
    public TileClass tunnel;

    [Header("Ores")]
    public TileClass coal;
    public TileClass iron;
    public TileClass gold;
    public TileClass diamond;
}
