using System.Collections.Generic;
using UnityEngine;

public class TerrainController : MonoBehaviour
{
    [SerializeField] GameObject hero; // Публічне поле для призначення героя з інтерфейсу Unity.
    [SerializeField] int activationDistance = 1; // Відстань для активації плиток.
    [SerializeField] int sideActivationDistance = 3; // Відстань для активації плиток з боків героя.

    //[SerializeField] TileMap hiddenArea;
    [SerializeField] Dictionary<Vector2, TileData> tileData = new Dictionary<Vector2, TileData>();
    [SerializeField] TerrainGeneration terrainGeneration;

    private Vector2Int tileDestroyRadius;
    public bool inCave = false;

    private void Awake()
    {
        terrainGeneration = GetComponent<TerrainGeneration>();
    }

    private void Start()
    {
        tileData = terrainGeneration.GetTileDataDictionary();
        tileDestroyRadius = new Vector2Int(sideActivationDistance, activationDistance);
        Debug.Log($"Count of tileData: {terrainGeneration.GetTileDataDictionary().Count}");
    }

    private void Update()
    {
        Vector3 heroPosition = hero.transform.position;
        inCave = CheckIsPlayerInCave();

        //if (!inCave)
        //{

        //    Vector3Int heroCellPosition = hiddenArea.WorldToCell(heroPosition);

        //    for (int x = -tileDestroyRadius.x; x <= tileDestroyRadius.x; x++)
        //    {
        //        for (int y = -tileDestroyRadius.y; y <= tileDestroyRadius.y; y++)
        //        {
        //            Vector3Int cellPosition = new(heroCellPosition.x + x, heroCellPosition.y + y, heroCellPosition.z);
        //            TileBase tile = hiddenArea.GetTile(cellPosition);

        //            if (tile != null)
        //            {
        //                hiddenArea.SetTile(cellPosition, null);
        //            }
        //        }
        //    }
        //}

        GenerateTilesAroundPlayer(heroPosition);
    }

    private void GenerateTilesAroundPlayer(Vector3 playerPosition)
    {
        int playerX = Mathf.FloorToInt(playerPosition.x);
        int playerY = Mathf.FloorToInt(playerPosition.y);

        Debug.Log("Player position: " + playerPosition + "intX = " + playerX + "intY = " + playerY);
        for (int x = -tileDestroyRadius.x; x <= tileDestroyRadius.y + 2; x++)
        {
            for (int y = -tileDestroyRadius.y; y <= tileDestroyRadius.y; y++)
            {
                int tileX = playerX + x;
                int tileY = playerY + y;

                Vector2 tilePosition = new Vector2(tileX, tileY);
                Debug.Log("Checking tile at position: " + tilePosition);
                GenerateTileIfExitsts(tilePosition);
            }
        }
    }

    private void GenerateTileIfExitsts(Vector2 position)
    {
        if (tileData.ContainsKey(position))
        {
            TileData tile = tileData[position];
            Debug.Log("Tile exists at position " + position + " with type: " + tile.TileType);

            terrainGeneration.PlaceTileByType(tile.TileType, (int)position.x, (int)position.y);
            tileData.Remove(position);

            Debug.Log("Placed tile of type " + tile.TileType + " at position " + position);
        }

        else
        {
            Debug.Log("No tile at position " + position);
        }
    }

    private bool CheckIsPlayerInCave()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(hero.transform.position, 0.4f);

        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Cave"))
            {
                return true;
            }
        }

        return false;
    }
}
