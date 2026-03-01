using UnityEngine;

public class StoneBehaviour : MonoBehaviour
{
    [SerializeField] private float fallDelaySeconds = 0f;

    private Rigidbody2D rb;
    private bool isFalling;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void StartFalling()
    {
        if (isFalling)
        {
            return;
        }

        isFalling = true;
        TriggerStoneAbove();

        if (fallDelaySeconds <= 0f)
        {
            BeginFalling();
            return;
        }

        Invoke(nameof(BeginFalling), fallDelaySeconds);
    }

    private void BeginFalling()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private void TriggerStoneAbove()
    {
        Vector2 rayStart = (Vector2)transform.position + new Vector2(0f, 0.6f);
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.up, 0.5f, LayerMask.GetMask("Default"));

        if (hit.collider == null || hit.collider.gameObject == gameObject)
        {
            return;
        }

        StoneBehaviour stoneAbove = hit.collider.GetComponent<StoneBehaviour>();
        stoneAbove?.StartFalling();
    }
}
