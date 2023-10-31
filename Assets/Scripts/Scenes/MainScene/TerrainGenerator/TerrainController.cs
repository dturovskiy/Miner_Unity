using System.Collections.Generic;
using UnityEngine;

public class TerrainController : MonoBehaviour
{
    [SerializeField] private GameObject hero; // Публічне поле для призначення героя з інтерфейсу Unity.
    [SerializeField] float activationDistance = 1.0f; // Відстань для активації плиток.
    [SerializeField] float sideActivationDistance = 1.0f; // Відстань для активації плиток з боків героя.

    public TerrainGeneration terrainGeneration; // Посилання на іншу скрипту "TerrainGeneration".

    private List<GameObject> tiles; // Список плиток на мапі.

    private void Start()
    {
        tiles = terrainGeneration.GetTiles(); // Отримуємо список плиток з іншого скрипту "TerrainGeneration" при запуску гри.
    }

    private void Update()
    {
        Vector2 heroPosition = hero.transform.position; // Отримуємо позицію героя.
        int currentTileX = Mathf.FloorToInt(heroPosition.x); // Знаходимо номер плитки, на якій стоїть герой за координатою X.
        int currentTileY = Mathf.FloorToInt(heroPosition.y); // Знаходимо номер плитки, на якій стоїть герой за координатою Y.

        ActivateTilesInRow(currentTileX, currentTileY, sideActivationDistance); // Активуємо плитки в ряду біля героя.
        //ActivateTilesInRow(currentTileX, currentTileY, sideActivationDistance); // (Закоментований рядок, що не впливає на код).
    }

    public void ActivateTilesInRow(int x, int y, float distance)
    {
        foreach (GameObject tile in tiles) // Перебираємо всі плитки на мапі.
        {
            if (tile != null) // Перевіряємо, чи плитка існує.
            {
                Vector2 tilePosition = tile.transform.position; // Отримуємо позицію плитки.
                float tileDistance = Mathf.Abs(tilePosition.x - x); // Розраховуємо відстань між плиткою та позицією героя за координатою X.
                if (tileDistance <= distance && Mathf.Abs(tilePosition.y - y) <= 1 && tilePosition.x != x)
                {
                    // Якщо плитка знаходиться на відстані, заданій параметром "distance", від героя (по X),
                    // і відстань по Y не більше 1, і плитка не є тією, на якій стоїть герой,
                    // то активуємо цю плитку.
                    tile.SetActive(true);
                }
            }
        }
    }
}
