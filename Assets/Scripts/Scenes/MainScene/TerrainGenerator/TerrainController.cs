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

    private void Update()
    {
        
        DestroyHiddenTiles();
    }

    public void DestroyHiddenTiles()
    {
        Vector2Int heroPosition = (Vector2Int)hiddenArea.WorldToCell(hero.position);
        int currentTileX = Mathf.FloorToInt(heroPosition.x); // Знаходимо номер плитки, на якій стоїть герой за координатою X.
        int currentTileY = Mathf.FloorToInt(heroPosition.y); // Знаходимо номер плитки, на якій стоїть герой за координатою Y.

        for (int x = currentTileX - sideActivationDistance; x  <= currentTileX + sideActivationDistance; x++)
        {
            hiddenArea.DeleteCells(new Vector3Int(x, heroPosition.y - activationDistance), new Vector3Int(x, heroPosition.y - activationDistance));
        }
    }
}
