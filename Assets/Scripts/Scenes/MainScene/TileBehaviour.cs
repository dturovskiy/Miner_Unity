using System;
using UnityEngine;

public class TileBehaviour : MonoBehaviour
{
    private int hitsRemaining = 3;

    public void HitTile()
    {
        hitsRemaining--;

        if (hitsRemaining <= 0)
        {
            BreakTile();
        }
    }

    private void BreakTile()
    {
        Destroy(gameObject);
    }

    public int GetHitsRemaining()
    {
        return hitsRemaining;
    }
}
