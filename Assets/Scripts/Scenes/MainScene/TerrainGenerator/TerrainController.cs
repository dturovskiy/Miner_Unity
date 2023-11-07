using UnityEngine;
using UnityEngine.Tilemaps;

public class TerrainController : MonoBehaviour
{
    [SerializeField] GameObject hero; // Публічне поле для призначення героя з інтерфейсу Unity.
    [SerializeField] int activationDistance = 1; // Відстань для активації плиток.
    [SerializeField] int sideActivationDistance = 3; // Відстань для активації плиток з боків героя.

    [SerializeField] Tilemap hiddenArea;

    private Vector2Int tileDestroyRadius;
    bool inCave = false;

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

            for (int x = -tileDestroyRadius.x; x <= tileDestroyRadius.x; x++)
            {
                for (int y = -tileDestroyRadius.y; y <= tileDestroyRadius.y; y++)
                {
                    Vector3Int cellPosition = new Vector3Int(heroCellPosition.x + x, heroCellPosition.y + y, heroCellPosition.z);
                    TileBase tile = hiddenArea.GetTile(cellPosition);

                    if (tile != null)
                    {
                        hiddenArea.SetTile(cellPosition, null);
                    }
                }
            }
        }
    }

    private bool CheckIsPlayerInCave()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(hero.transform.position, 1f);

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
