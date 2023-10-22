using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickaxe : MonoBehaviour
{
    float damage = 10f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject tile = other.gameObject;

        Debug.Log(tile.name);
        if (tile.CompareTag("Ground"))
        {
            Destroy(tile.gameObject);
        }
    }
}
