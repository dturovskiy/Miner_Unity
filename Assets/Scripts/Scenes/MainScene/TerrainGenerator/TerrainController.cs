using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TerrainController : MonoBehaviour
{
    [SerializeField] GameObject hero; // Публічне поле для призначення героя з інтерфейсу Unity.
    [SerializeField] int activationDistance = 1; // Відстань для активації плиток зверху.
    [SerializeField] int sideActivationDistance = 3; // Відстань для активації плиток з боків героя.

    [SerializeField] Tilemap hiddenArea;
    [SerializeField] Dictionary<Vector2, TileData> tileData = new Dictionary<Vector2, TileData>();

    [SerializeField] TerrainGeneration terrainGeneration;
    [SerializeField] SaveLoadSystem saveLoadSystem;

    private Vector2Int tileDestroyRadius;
    public bool inCave = false;
    public bool firstContact = true;

    private void Awake()
    {
        terrainGeneration = GetComponent<TerrainGeneration>();
    }

    private void Start()
    {
        tileData = terrainGeneration.GetTileDataDictionary();
        tileDestroyRadius = new Vector2Int(sideActivationDistance, activationDistance);
        GenerateStartingTerrain(tileData);
    }

    private void Update()
    {
        Vector3 heroPosition = hero.transform.position;
        inCave = CheckIsPlayerInCave();

        if (!inCave)
        {
            Vector3Int heroCellPosition = hiddenArea.WorldToCell(heroPosition);

            for (int x = -tileDestroyRadius.x; x <= tileDestroyRadius.x; x++)
            {
                for (int y = -tileDestroyRadius.y; y <= tileDestroyRadius.y; y++)
                {
                    Vector3Int cellPosition = new(heroCellPosition.x + x, heroCellPosition.y + y, heroCellPosition.z);
                    TileBase tile = hiddenArea.GetTile(cellPosition);

                    if (tile != null)
                    {
                        hiddenArea.SetTile(cellPosition, null);
                        saveLoadSystem.SaveHiddenTiles(cellPosition);
                    }
                }
            }

            GenerateTilesAroundPlayer(heroPosition);
        }

        GetHeroPosition();
    }

    public void GetHeroPosition()
    {
        saveLoadSystem.SaveHeroPosition(hero.transform.position);
    }

    private void GenerateTilesAroundPlayer(Vector3 playerPosition)
    {


        int playerX = Mathf.FloorToInt(playerPosition.x);
        int playerY = Mathf.FloorToInt(playerPosition.y);

        for (int x = -tileDestroyRadius.x; x <= tileDestroyRadius.x; x++)
        {
            for (int y = -tileDestroyRadius.y; y <= tileDestroyRadius.y; y++)
            {
                int tileX = playerX + x;
                int tileY = playerY + y;

                Vector2 tilePosition = new Vector2(tileX, tileY);

                GenerateTileIfExist(tilePosition);
            }
        }
    }

    public void GenerateStartingTerrain(Dictionary<Vector2, TileData> tileDataDictionary)
    {
        for (int y = 249; y <= 254; y++)
        {
            for (int x = 0; x <= 100; x++)
            {
                Vector2 key = new Vector2(x, y);
                if (tileDataDictionary.ContainsKey(key))
                {
                    TileData value = tileDataDictionary[key];
                    terrainGeneration.PlaceTileByType(value.TileType, value.X, value.Y);
                    tileDataDictionary.Remove(key);
                }

            }
        }

        foreach (Vector2 tile in saveLoadSystem.LoadSpawnedTilesFromBinary())
        {
            if (tileDataDictionary.ContainsKey(tile))
            {
                TileData value = tileDataDictionary[tile];
                terrainGeneration.PlaceTileByType(value.TileType, value.X, value.Y);
                tileDataDictionary.Remove(tile);
            }
        }

        foreach (Vector3Int tile in saveLoadSystem.LoadHidenTilesFromBinary())
        {
            Vector3Int cellPosition = new Vector3Int(tile.x, tile.y, tile.z);

            hiddenArea.SetTile(cellPosition, null);
        }

        hero.transform.position = saveLoadSystem.LoadHeroPositionFromBinary();
    }

    private void GenerateTileIfExist(Vector2 position)
    {
        // Перевірка наявності ключа в словнику
        if (tileData.TryGetValue(position, out TileData tile))
        {
            // Розміщення блоку за типом та його позицією
            terrainGeneration.PlaceTileByType(tile.TileType, position.x, position.y);

            // Видалення блоку зі словника
            tileData.Remove(position);

            // Збереження позиції блоку в бінарний файл
            saveLoadSystem.SaveSpawnedTiles(position);
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
