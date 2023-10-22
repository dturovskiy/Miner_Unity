using UnityEngine;

public class MiningController : MonoBehaviour
{
    public float raycastDistance = 1f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Input.GetKey(KeyCode.DownArrow))
                BreakTiles(Vector2.down);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Input.GetKey(KeyCode.UpArrow))
                BreakTiles(Vector2.up);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Input.GetKey(KeyCode.LeftArrow))
                BreakTiles(Vector2.left);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Input.GetKey(KeyCode.RightArrow))
                BreakTiles(Vector2.right);
        }
    }

    private void BreakTiles(Vector2 direction)
    {
        Vector2 startPos = transform.position;
        Vector2 endPos = startPos + direction * raycastDistance;

        RaycastHit2D hit = Physics2D.Raycast(endPos, direction, raycastDistance);

        if (hit.collider != null)
        {
            GameObject tile = hit.collider.gameObject;
            if (tile.CompareTag("Ground"))
            {
                Destroy(tile);

                // Дебаг вивід для відстеження знищення плитки
                Debug.Log("Destroyed a Ground tile!");
            }
            else
            {
                // Дебаг вивід для відстеження інших об'єктів
                Debug.Log("Hit an object with tag: " + tile.tag);
            }
        }
        else
        {
            // Дебаг вивід, якщо зіткнення не відбулось
            Debug.Log("No collision detected.");
        }
    }
}
