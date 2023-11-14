using UnityEngine;

public class MiningController : MonoBehaviour
{
    // Відстань, на яку викидається промінь для визначення цілі
    public float raycastDistance = 0.9f;

    // Аніматор для управління анімацією
    private Animator animator;

    // Інтервал часу між ударами
    public float timeBetweenHits = 0.1f;

    // Час останнього удару
    private float lastHitTime = 0.0f;

    // Кількість доступних ударів перед скиданням
    private int hitsRemaining = 3;

    private void Awake()
    {
        // Ініціалізація аніматора
        animator = GetComponent<Animator>();
    }

    private bool CanHit()
    {
        // Перевірка, чи пройшов достатній час між ударами
        return Time.time - lastHitTime >= timeBetweenHits;
    }

    private void Update()
    {
        if (Input.touchCount > 0 || Input.GetMouseButton(0))
        {
            // Отримання позиції дотику або кліку мишкою
            Vector2 touchPosition = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
            Vector2 worldPosition = Camera.main.ScreenToWorldPoint(touchPosition);
            
            animator.SetBool("IsMining", true);
            if(CanHit() )
            {
                BreakTiles(worldPosition);
                Debug.Log("Touch Position: " + worldPosition);
            }
        }
        else
        {
            animator.SetBool("IsMining", false);
        }
    }

    private void BreakTiles(Vector2 targetPosition)
    {
        // Оновлення часу останнього удару
        lastHitTime = Time.time;

        Collider2D hitCollider = Physics2D.OverlapPoint(targetPosition);

        

        // Перевірка наявності цілі
        if (hitCollider != null)
        {
            Debug.Log("Collided with: " + hitCollider.name);

            GameObject tile = hitCollider.gameObject;

            // Отримання компонента TileBehaviour
            TileBehaviour tileBehaviour = tile.GetComponent<TileBehaviour>();

            // Перевірка тегів та стану плитки
            if (tile.CompareTag("Player") || tile.CompareTag("Stone") || tile.CompareTag("Cave")) return;

            // Перевірка чи плитка не розбита
            if (tileBehaviour != null && !tileBehaviour.IsBroken && !tileBehaviour.Interacted)
            {
                // Запуск анімації розбиття та виклик методу обробки удару
                
                HitTile(tileBehaviour);
                tileBehaviour.EndInteraction();
            }
        }
    }

    public void HitTile(TileBehaviour tile)
    {
        Debug.Log("Hit: " + hitsRemaining);
        // Зменшення кількості залишених ударів
        hitsRemaining--;

        // Перевірка, чи необхідно скинути тригер та вивід інформації у консоль
        if (hitsRemaining <= 0)
        {
            tile.BreakTile();
            
            hitsRemaining = 4;
        }
    }
}
