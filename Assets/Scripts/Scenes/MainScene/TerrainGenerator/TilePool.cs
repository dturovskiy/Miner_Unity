using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Scenes.MainScene
{
    public class TilePool : MonoBehaviour
    {
        public GameObject tilePrefab;
        private List<GameObject> tilePool = new List<GameObject>();

        public GameObject GetTile()
        {
            foreach (var tile in  tilePool)
            {
                if (!tile.activeInHierarchy)
                {
                    tile.SetActive(true);
                    return tile;
                }
            }

            GameObject newTile = Instantiate(tilePrefab);
            tilePool.Add(newTile);
            return newTile;
        }

        public void ReturnTile(GameObject tile)
        {
            tile.SetActive(false);
        }
    }
}