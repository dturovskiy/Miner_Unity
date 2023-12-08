using UnityEngine;
using System.IO;
using System;

public class TerrainTextReader : MonoBehaviour
{
    private const string fileName = "terrain_layout.txt";
    private const char separator = '|';

    public TerrainGeneration terrainGeneration;

    void Start()
    {
        GenerateTerrainFromTextFile();
    }

    private void GenerateTerrainFromTextFile()
    {
        string filePath = Path.Combine(Application.dataPath, fileName);

        if(File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);

            for(int y = 0; y < lines.Length; y++)
            {

                string[] tiles = lines[y].Split(separator);

                for(int x = 0; x < tiles.Length - 1; x++)
                {
                    string tileType = tiles[x];

                    GeterateTile(tileType, x, y);
                }
            }
        }
    }

    private void GeterateTile(string tileType, int x, int y)
    {
        GameObject newTile = terrainGeneration.PlaceTileByType(tileType, x, y);
    }
}
