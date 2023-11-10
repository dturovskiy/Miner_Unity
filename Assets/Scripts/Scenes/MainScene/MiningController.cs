using UnityEngine;

public class MiningController : MonoBehaviour
{
    // Відстань, на яку викидається промінь для визначення цілі
    public float raycastDistance = 0.9f;

    // Аніматор для управління анімацією
    private Animator animator;

    // Інтервал часу між ударами
    public float timeBetweenHits = 0.5f;

    // Час останнього удару
    private float lastHitTime = 0.0f;

    // Кількість доступних ударів перед скиданням
    private int hitsRemaining = 4;

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
        // Перевірка введення для різних напрямків
        CheckInput(Vector2.down);
        CheckInput(Vector2.up);
        CheckInput(Vector2.left);
        CheckInput(Vector2.right);
    }

    private void CheckInput(Vector2 direction)
    {
        // Перевірка введення та можливості удару
        if (Input.GetKey(GetKeyCodeForDirection(direction)) && CanHit())
        {
            // Визивання методу для розбивання плиток
            BreakTiles(direction);
        }
    }

    private KeyCode GetKeyCodeForDirection(Vector2 direction)
    {
        // Визначення коду клавіші для конкретного напрямку
        if (direction == Vector2.down) return KeyCode.DownArrow;
        if (direction == Vector2.up) return KeyCode.UpArrow;
        if (direction == Vector2.left) return KeyCode.LeftArrow;
        if (direction == Vector2.right) return KeyCode.RightArrow;
        return KeyCode.None;
    }

    private void BreakTiles(Vector2 direction)
    {
        // Оновлення часу останнього удару
        lastHitTime = Time.time;

        // Визначення початкової позиції променя та визначення цілі
        Vector2 startPos = (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, raycastDistance, LayerMask.GetMask("Default"));

        // Візуалізація променя у сцені
        Debug.DrawRay(startPos, direction * raycastDistance, Color.green);

        // Перевірка наявності цілі
        if (hit.collider != null)
        {
            GameObject tile = hit.collider.gameObject;

            // Отримання компонента TileBehaviour
            TileBehaviour tileBehaviour = tile.GetComponent<TileBehaviour>();

            // Перевірка тегів та стану плитки
            if (tile.CompareTag("Player") || tile.CompareTag("Stone") || tile.CompareTag("Cave")) return;

            // Перевірка чи плитка не розбита
            if (tileBehaviour != null && !tileBehaviour.IsBroken && !tileBehaviour.Interacted)
            {
                // Запуск анімації розбиття та виклик методу обробки удару
                animator.SetTrigger("IsMining");
                HitTile(tileBehaviour);
                tileBehaviour.EndInteraction();
            }
        }
    }

    public void HitTile(TileBehaviour tile)
    {
        // Зменшення кількості залишених ударів
        hitsRemaining--;

        // Перевірка, чи необхідно скинути тригер та вивід інформації у консоль
        if (hitsRemaining <= 0)
        {
            tile.BreakTile();
            animator.ResetTrigger("IsMining");
            hitsRemaining = 3;
        }
    }
}
