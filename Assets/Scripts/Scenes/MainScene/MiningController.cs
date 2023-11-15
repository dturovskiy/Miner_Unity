using UnityEngine;

public class MiningController : MonoBehaviour
{
    // Аніматор для управління анімацією
    private Animator animator;

    // Інтервал часу між ударами
    public float timeBetweenHits = 0.05f;

    // Час останнього удару
    private float lastHitTime = 0.0f;

    // Кількість доступних ударів перед скиданням
    private int hitsRemaining = 3;

    private float maxMiningDistance = 1f;

    public Joystick miningJoystick;

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
        float horizontalInput = miningJoystick.Horizontal;
        float verticalInput = miningJoystick.Vertical;

        if (horizontalInput != 0 || verticalInput != 0)
        {
            Vector2 miningDirection = new Vector2(horizontalInput, verticalInput).normalized;
            Vector2 miningPosition = (Vector2)transform.position + miningDirection * maxMiningDistance;
            
            
            if (CanHit())
            {
                Debug.Log("Mining position: " + miningPosition);
                BreakTiles(miningPosition);
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
            animator.SetBool("IsMining", true);

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

            hitsRemaining = 3;
        }
    }
}
