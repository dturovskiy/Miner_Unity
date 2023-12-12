using UnityEngine;
using LitJson;

public class TileSaver : SaveableBehavior
{
    private const string POSITION_KEY = "position";
    private const string TILE_TYPE_KEY = "tileType";

    private TileAtlas tileAtlas;

    private void Awake()
    {
        
    }

    public override JsonData SavedData
    {
        get
        {
            JsonData result = new JsonData();
            result[TILE_TYPE_KEY] = tileAtlas.platinum.ToString();
        }
    }

    public override void LoadFromData(JsonData data)
    {
        throw new System.NotImplementedException();
    }
}
