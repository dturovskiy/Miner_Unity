using System.Collections.Generic;
using UnityEngine;

public class TerrainController : MonoBehaviour
{
    [SerializeField] private GameObject hero;
    [SerializeField] float activationDistance = 1.0f;
    [SerializeField] int previousTileY;

    public TerrainGeneration terrainGeneration;



    private void Update()
    {
        List<GameObject> tiles = terrainGeneration.GetTiles();

        Vector2 heroPosition = hero.transform.position;
        int currentTileX = Mathf.FloorToInt(heroPosition.x);
        int currentTileY = Mathf.FloorToInt(heroPosition.y);

        ActivateTilesInRow(tiles, currentTileX, currentTileY);
    }

    public void ActivateTilesInRow(List<GameObject> tiles, int startX, int y)
    {
        for (int x = startX - 3; x <= startX + 3; x++)
        {
            foreach (GameObject tile in tiles)
            {
                if (tile != null)
                {
                    Vector2 tilePosition = tile.transform.position;
                    int tileX = Mathf.FloorToInt(tilePosition.x);

                    if (tileX == x && Mathf.FloorToInt(tilePosition.y) == y - 1)
                    {
                        tile.SetActive(true);
                    }
                }
            }
        }
    }
}
