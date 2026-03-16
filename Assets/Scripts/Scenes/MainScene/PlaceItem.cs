using System.Collections.Generic;
using UnityEngine;

public class PlaceItem : MonoBehaviour
{
    [SerializeField] private GameObject ladderPrefab;

    private readonly HashSet<Vector2> placedLadderPositions = new HashSet<Vector2>();

    public void PlaceLadder()
    {
        if (ladderPrefab == null)
        {
            return;
        }

        // Keep old gameplay rule: do not place ladder while hero is in cave zone.
        bool inCave = false;
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, 0.4f))
        {
            if (collider.CompareTag("Cave")) inCave = true;
        }
        if (inCave)
        {
            return;
        }

        Vector3 playerPosition = transform.position;
        float roundedX = Mathf.Floor(playerPosition.x) + 0.5f;
        float roundedY = Mathf.Floor(playerPosition.y) + 0.5f;
        Vector2 playerTilePosition = new Vector2(roundedX, roundedY);

        if (placedLadderPositions.Contains(playerTilePosition))
        {
            return;
        }

        Vector3 ladderPosition = new Vector3(playerTilePosition.x, playerTilePosition.y, 0f);
        Instantiate(ladderPrefab, ladderPosition, Quaternion.identity);
        placedLadderPositions.Add(playerTilePosition);
    }
}