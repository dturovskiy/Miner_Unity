using System.Collections.Generic;
using UnityEngine;

public class PlaceItem : MonoBehaviour
{
    [SerializeField] private GameObject ladderPrefab;
    [SerializeField] private TerrainController terrainController;

    private readonly HashSet<Vector2> placedLadderPositions = new();

    public void PlaceLadder()
    {
        if (ladderPrefab == null || terrainController == null)
        {
            return;
        }

        // Keep old gameplay rule: do not place ladder while hero is in cave zone.
        if (terrainController.inCave)
        {
            return;
        }

        Vector3 playerPosition = transform.position;
        float roundedX = Mathf.Floor(playerPosition.x) + 0.5f;
        float roundedY = Mathf.Floor(playerPosition.y) + 0.5f;
        Vector2 playerTilePosition = new(roundedX, roundedY);

        if (placedLadderPositions.Contains(playerTilePosition))
        {
            return;
        }

        Vector3 ladderPosition = new(playerTilePosition.x, playerTilePosition.y, 0f);
        Instantiate(ladderPrefab, ladderPosition, Quaternion.identity);
        placedLadderPositions.Add(playerTilePosition);
    }
}
