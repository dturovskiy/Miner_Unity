using UnityEngine;

public class TileBehaviour : MonoBehaviour
{
    private bool isBroken;
    private int hitsRemaining = 4;

    private float lastHitTime = 0f;
    private float timeBetweenHits = 0.8f;

    public bool IsBroken => isBroken;

    // Added for new Terrain Architecture
    public int gridX;
    public int gridY;

    public GameObject crackPrefab;
    public Crack crackClass;

    private void Awake()
    {
        crackPrefab = Resources.Load<GameObject>("Crack");
    }

    private bool CanHit()
    {
        return Time.time - lastHitTime >= timeBetweenHits;
    }

    public void BreakTile()
    {
        isBroken = true;
        
        // Notify the new terrain system to permanently remove this block
        var chunkManager = GetComponentInParent<MinerUnity.Terrain.ChunkManager>();
        if (chunkManager != null)
        {
            chunkManager.DestroyTileInWorld(gridX, gridY);
        }
    }

    public void HitTile(TileBehaviour tile)
    {
        if (!CanHit())
        {
            return;
        }

        if (hitsRemaining == 4)
        {
            CreateCrack();
        }

        hitsRemaining--;
        HitAndCrack(hitsRemaining);

        // Break after the required number of hits.
        if (hitsRemaining <= 0)
        {
            tile.BreakTile();
        }
    }

    private void HitAndCrack(int hits)
    {
        crackClass.HitCrack(hits);
        lastHitTime = Time.time;
    }

    private void CreateCrack()
    {
        if (crackPrefab != null)
        {
            GameObject crack = Instantiate(crackPrefab, transform.position, Quaternion.identity);
            crack.transform.parent = transform;
            crackClass = crack.GetComponent<Crack>();
        }
        else
        {
            Debug.LogError("CrackPrefab not found in Resources!");
        }
    }
}