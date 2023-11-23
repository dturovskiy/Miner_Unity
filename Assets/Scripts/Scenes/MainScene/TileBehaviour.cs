using UnityEngine;

public class TileBehaviour : MonoBehaviour
{
    private bool isBroken;
    private int hitsRemaining = 4;

    private float lastHitTime = 0f;
    private float timeBetweenHits = 1f;

    public bool IsBroken { get { return isBroken; } }

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
        Destroy(gameObject);
        isBroken = true;
    }

    public void HitTile(TileBehaviour tile)
    {
        if (CanHit())
        {
            Debug.Log("Hit: " + hitsRemaining);

            if (hitsRemaining == 4)
            {
                CreateCrack();
            }

            Debug.Log("Can hit? - " + CanHit());

            hitsRemaining--;

            HitAndCrack(hitsRemaining);

            // Перевірка, чи необхідно скинути тригер та вивід інформації у консоль
            if (hitsRemaining <= 0)
            {
                tile.BreakTile();
            }
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
