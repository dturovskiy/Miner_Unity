using UnityEngine;

public class TileBehaviour : MonoBehaviour
{
    private bool isBroken;
    private bool interacted;
    private float interactionStartTime;
    private float interactionEndTime;
    // Кількість доступних ударів перед скиданням
    private int hitsRemaining = 4;

    public bool IsBroken { get { return isBroken; } }
    public bool Interacted { get { return interacted; } }
    public float InteractionStartTime { get { return interactionStartTime; } }
    public float InteractionEndTime { get { return interactionEndTime; } }

    public GameObject crackPrefab;
    public Crack crackClass;

    private void Awake()
    {
        crackPrefab = Resources.Load<GameObject>("Crack");
    }

    public void Interact()
    {
        interacted = true;
        interactionStartTime = Time.time;
    }

    public void EndInteraction()
    {
        interactionEndTime = Time.time;
    }
    public void BreakTile()
    {
        Destroy(gameObject);
        isBroken = true;

        Debug.Log("Tile is broken!");
    }

    public void HitTile(TileBehaviour tile)
    {
        if (hitsRemaining == 4)
        {
            CreateCrack();
        }

        Debug.Log("Hit: " + hitsRemaining);
        // Зменшення кількості залишених ударів
        hitsRemaining--;

        

        HitAndCrack(hitsRemaining);
        
        // Перевірка, чи необхідно скинути тригер та вивід інформації у консоль
        if (hitsRemaining <= 0)
        {
            tile.BreakTile();
        }
    }

    private void HitAndCrack(int hits)
    {
        crackClass.HitCrack(hits);
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
