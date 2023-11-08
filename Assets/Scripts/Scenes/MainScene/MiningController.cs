using UnityEngine;

public class MiningController : MonoBehaviour
{
    public float raycastDistance = 0.9f;
    private Animator animator;

    public float timeBetweenHits = 0.1f; // Час між ударом (в секундах)
    private float timeSinceLastHit = 0.0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private bool CanHit()
    {
        // Перевірка, чи пройшов достатній час між ударами
        return timeSinceLastHit >= timeBetweenHits;
    }

    private void Update()
    {
        timeSinceLastHit += Time.fixedDeltaTime;

        CheckInput(Vector2.down);
        CheckInput(Vector2.up);
        CheckInput(Vector2.left);
        CheckInput(Vector2.right);
    }

    private void CheckInput(Vector2 direction)
    {
        if (Input.GetKey(GetKeyCodeForDirection(direction)) && CanHit())
        {
            BreakTiles(direction);
        }
    }

    private KeyCode GetKeyCodeForDirection(Vector2 direction)
    {
        if (direction == Vector2.down) return KeyCode.DownArrow;
        if (direction == Vector2.up) return KeyCode.UpArrow;
        if (direction == Vector2.left) return KeyCode.LeftArrow;
        if (direction == Vector2.right) return KeyCode.RightArrow;
        return KeyCode.None;
    }

    private void BreakTiles(Vector2 direction)
    {
        

        Vector2 startPos = (Vector2)transform.position;

        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, raycastDistance, LayerMask.GetMask("Default"));

        Debug.DrawRay(startPos, direction * raycastDistance, Color.green);
        
        if (hit.collider != null)
        {
            GameObject tile = hit.collider.gameObject;

            TileBehaviour tileBehaviour = tile.GetComponent<TileBehaviour>();

            if (tile.CompareTag("Player") || tile.CompareTag("Stone") || tile.CompareTag("Cave")) return;

            if (tileBehaviour != null)
            {
                animator.SetTrigger("IsMining");
                tileBehaviour.HitTile();
                if (tileBehaviour.GetHitsRemaining() <= 0)
                {
                    animator.SetTrigger("IsIdle");
                    timeSinceLastHit = 0.0f; // Скидання лічильника часу при ударі
                }
            }
        }
    }
}
