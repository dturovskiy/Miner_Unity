using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TerrainController : MonoBehaviour
{
    [SerializeField] GameObject hero; // Публічне поле для призначення героя з інтерфейсу Unity.
    [SerializeField] int activationDistance = 1; // Відстань для активації плиток.
    [SerializeField] int sideActivationDistance = 3; // Відстань для активації плиток з боків героя.

    [SerializeField] Tilemap hiddenArea;

    [SerializeField] TerrainGeneration terrainGeneration;

    private Vector2Int tileDestroyRadius;
    public bool inCave = false;

    private void Start()
    {
        tileDestroyRadius = new Vector2Int(sideActivationDistance, activationDistance);
    }

    private void Update()
    {
        inCave = CheckIsPlayerInCave();

        if (!inCave)
        {
            Vector3 heroPosition = hero.transform.position;
            Vector3Int heroCellPosition = hiddenArea.WorldToCell(heroPosition);

            int CHUNK_SIZE = 10;
            int chunkIndex = Mathf.FloorToInt(heroPosition.y / CHUNK_SIZE);

            List<Transform> chunks = terrainGeneration.GetChunks();

            if (chunkIndex < chunks.Count)
            {
                if (chunkIndex != 0)
                chunks[chunkIndex - 1].gameObject.SetActive(true);
            }

            for (int x = -tileDestroyRadius.x; x <= tileDestroyRadius.x; x++)
            {
                for (int y = -tileDestroyRadius.y; y <= tileDestroyRadius.y; y++)
                {
                    Vector3Int cellPosition = new(heroCellPosition.x + x, heroCellPosition.y + y, heroCellPosition.z);
                    TileBase tile = hiddenArea.GetTile(cellPosition);

                    if (tile != null)
                    {
                        hiddenArea.SetTile(cellPosition, null);
                    }
                }
            }
        }
    }

    public bool CheckIsPlayerInCave()
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
