using System.Collections.Generic;
using UnityEngine;

public class TerrainController : MonoBehaviour
{
    [SerializeField] private GameObject hero;
    [SerializeField] float activationDistance = 1.0f;
    [SerializeField] float sideActivationDistance = 3.0f;

    public TerrainGeneration terrainGeneration;

    private List<GameObject> tiles; // —писок плиток

    private void Start()
    {
        tiles = terrainGeneration.GetTiles(); // ќтримуЇмо список плиток з TerrainGeneration
    }

    private void Update()
    {
        Vector2 heroPosition = hero.transform.position;
        int currentTileX = Mathf.FloorToInt(heroPosition.x);
        int currentTileY = Mathf.FloorToInt(heroPosition.y);

        ActivateTilesInRow(currentTileX, currentTileY, activationDistance);
        ActivateTilesInRow(currentTileX, currentTileY, sideActivationDistance);
    }

    public void ActivateTilesInRow(int x, int y, float distance)
    {
        foreach (GameObject tile in tiles)
        {
            if (tile != null)
            {
                Vector2 tilePosition = tile.transform.position;
                float tileDistance = Mathf.Abs(tilePosition.x - x); // ¬изначаЇмо в≥дстань до плитки
                if (tileDistance <= distance && Mathf.Abs(tilePosition.y - y) <= 1) // ѕерев≥рка на в≥дстань ≥ позиц≥ю за Y
                {
                    tile.SetActive(true);
                }
            }
        }
    }
}
