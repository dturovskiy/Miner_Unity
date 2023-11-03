using Unity.VisualScripting;
using UnityEngine;

public class MiningController : MonoBehaviour
{
    public float raycastDistance = 0.9f;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void Update()
    {
        if (Input.GetKey(KeyCode.DownArrow))
        {
            BreakTiles(Vector2.down);
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            BreakTiles(Vector2.up);
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            BreakTiles(Vector2.left);
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            BreakTiles(Vector2.right);
        }
    }

    private void BreakTiles(Vector2 direction)
    {
        Vector2 startPos = (Vector2)transform.position;

        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, raycastDistance, LayerMask.GetMask("Default"));

        Debug.DrawRay(startPos, direction * raycastDistance, Color.green);
        if (hit.collider == null || !hit.collider.CompareTag("Ground"))
        {
            animator.SetTrigger("IsIdle");
        }

        if (hit.collider != null)
        {
            GameObject tile = hit.collider.gameObject;

            if (tile.CompareTag("Player")) return;
            if (tile.CompareTag("Stone")) return;
            if (tile.CompareTag("Cave"))
            {
                return;
            }

            animator.SetTrigger("IsMining");
            Destroy(tile);

            //Дебаг вивід для відстеження знищення плитки
            Debug.Log("Destroyed a Ground tile!");
        }
    }
}
