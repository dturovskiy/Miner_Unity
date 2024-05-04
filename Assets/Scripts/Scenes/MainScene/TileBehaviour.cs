using UnityEngine;

public class TileBehaviour : MonoBehaviour
{
    private bool isBroken;
    private int hitsRemaining = 4;

    private float lastHitTime = 0f;
    private float timeBetweenHits = 0.8f;

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
        Vector2 rayStart = transform.position + new Vector3 (0f, 0.6f, 0f);
        Vector2 rayDirection = Vector2.up;

        float rayLength = 0.5f;

        Debug.DrawRay(rayStart, rayDirection * rayLength, Color.red, 600f);
        // ѕерев≥р€Їмо, чи Ї об'Їкт п≥д ц≥Їю плиткою
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.up, rayLength, LayerMask.GetMask("Default"));
        if (hit.collider != null)
        {
            Debug.Log(hit.collider);
            // якщо Ї об'Їкт п≥д ц≥Їю плиткою, активуЇмо пад≥нн€ каменю
            StoneBehaviour stone = hit.collider.GetComponent<StoneBehaviour>();
            if (stone != null)
            {
                Debug.Log($"Stone found: {stone}");
                stone.StartFalling();
            }
        }
        else
        {
            Debug.Log("Not found collider!");
        }

        Destroy(gameObject);
        isBroken = true;
        Debug.Log("Tile destroyed");
    }

    public void HitTile(TileBehaviour tile)
    {
        if (CanHit())
        {
            if (hitsRemaining == 4)
            {
                CreateCrack();
            }

            hitsRemaining--;

            HitAndCrack(hitsRemaining);

            // ѕерев≥рка, чи необх≥дно скинути тригер та вив≥д ≥нформац≥њ у консоль
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
