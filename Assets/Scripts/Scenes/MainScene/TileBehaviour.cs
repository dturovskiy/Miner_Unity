using UnityEngine;

public class TileBehaviour : MonoBehaviour
{
    private bool isBroken;
    private int hitsRemaining = 4;

    private float lastHitTime = 0f;
    private float timeBetweenHits = 0.8f;

    public bool IsBroken => isBroken;

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
        Vector2 rayStart = (Vector2)transform.position + new Vector2(0f, 0.6f);
        float rayLength = 0.5f;

        // If a stone is above the tile, trigger its fall.
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.up, rayLength, LayerMask.GetMask("Default"));
        if (hit.collider != null)
        {
            StoneBehaviour stone = hit.collider.GetComponent<StoneBehaviour>();
            if (stone != null)
            {
                stone.StartFalling();
            }
        }

        Destroy(gameObject);
        isBroken = true;
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
