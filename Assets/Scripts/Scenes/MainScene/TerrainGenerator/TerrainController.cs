using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TerrainController : MonoBehaviour
{
    [SerializeField] Transform hero; // Публічне поле для призначення героя з інтерфейсу Unity.
    [SerializeField] int activationDistance = 1; // Відстань для активації плиток.
    [SerializeField] int sideActivationDistance = 3; // Відстань для активації плиток з боків героя.

    [SerializeField] Tilemap hiddenArea;

    private Vector2Int tileDestroyRadius;

    private void Start()
    {
        tileDestroyRadius = new Vector2Int(sideActivationDistance, activationDistance);
    }

    private void Update()
    {
        
        Vector3 heroPosition = hero.position;
        Vector3Int heroCellPosition = hiddenArea.WorldToCell(heroPosition);

        for (int x = -tileDestroyRadius.x; x <= tileDestroyRadius.x; x++)
        {
            for(int y = -tileDestroyRadius.y; y <= tileDestroyRadius.y; y++)
            {
                Vector3Int cellPosition = new Vector3Int(heroCellPosition.x + x, heroCellPosition.y + y, heroCellPosition.z);
                TileBase tile = hiddenArea.GetTile(cellPosition);

                if( tile != null)
                {
                    hiddenArea.SetTile(cellPosition, null);
                }
            }
        }
    }
}
