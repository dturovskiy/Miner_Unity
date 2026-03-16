using UnityEngine;

/// <summary>
/// Стабільна логіка драбини:
/// 1. Герой може залазити вгору/вниз.
/// 2. На верхньому краї драбини герой стає у Normal і стоїть як на звичайному блоці.
/// 3. Якщо герой іде вбік у тунель із середини драбини — він просто виходить із драбини.
/// 4. Немає залежності від OnTriggerEnter/Exit, які часто дають ривки на верхньому краї.
/// </summary>
[RequireComponent(typeof(HeroStateController), typeof(HeroInputReader))]
public class LadderZoneDetector : MonoBehaviour
{
    [Header("Ladder Detection")]
    [SerializeField] private Transform ladderCheck;

    // Краще ставити зону перевірки на рівні тулуба героя.
    // Вона має бути вужчою за тунель, але достатньою, щоб ловити драбину поруч.
    [SerializeField] private Vector2 ladderCheckSize = new Vector2(0.35f, 0.9f);

    // Layer драбин.
    [SerializeField] private LayerMask ladderMask;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;

    // Точка під ногами героя.
    [SerializeField] private float groundCheckRadius = 0.12f;

    // Layer твердих блоків / землі / платформ, на яких герой може стояти.
    [SerializeField] private LayerMask groundMask;

    [Header("Input")]
    // Мінімальне відхилення джойстика, після якого вважаємо, що гравець реально хоче лізти.
    [SerializeField] private float climbEnterThreshold = 0.25f;

    // Якщо гравець тисне вниз сильніше за цей поріг — починаємо / продовжуємо спуск.
    [SerializeField] private float climbDownThreshold = -0.25f;

    [Header("Stability")]
    // Пам’ять контакту із землею.
    // Допомагає, коли на самому верхньому краї groundCheck іноді на 1 кадр "втрачає" землю.
    [SerializeField] private float groundedMemoryTime = 0.12f;

    // Коротка затримка після виходу на верхній край,
    // щоб герой не застрибував назад у Climbing, якщо джойстик ще трохи тисне вгору.
    [SerializeField] private float topExitLockTime = 0.18f;

    // Скільки часу герой не може ВИЙТИ з драбини після того як заліз знизу.
    // Захищає від того, щоб recentlyGrounded не викидало нас назад в Normal на першому ж кадрі.
    [SerializeField] private float climbStartLockTime = 0.2f;

    private HeroStateController heroState;
    private HeroInputReader inputReader;

    // Час останнього підтвердженого контакту із землею.
    private float lastGroundedTime = -999f;

    // Час останнього "виходу на верхню площадку".
    private float lastTopExitTime = -999f;

    // Час останнього входу в стан Climbing.
    private float lastClimbStartTime = -999f;

    private void Awake()
    {
        heroState = GetComponent<HeroStateController>();
        inputReader = GetComponent<HeroInputReader>();
    }

    private void Update()
    {
        bool touchingLadder = IsTouchingLadder();
        bool groundedNow = IsGrounded();

        // Запам’ятовуємо, що герой недавно стояв на землі.
        if (groundedNow)
        {
            lastGroundedTime = Time.time;
        }

        bool recentlyGrounded = Time.time - lastGroundedTime <= groundedMemoryTime;
        bool topExitLocked = Time.time - lastTopExitTime <= topExitLockTime;

        float vertical = inputReader.Vertical;

        bool wantsUp = vertical > climbEnterThreshold;
        bool wantsDown = vertical < climbDownThreshold;

        // -----------------------------
        // Якщо вже ліземо по драбині
        // -----------------------------
        if (heroState.CurrentState == HeroState.Climbing)
        {
            // 1. Коли ми щойно залізли на драбину (навіть з підлоги), нам треба дати 
            // кілька мілісекунд, щоб піднятись. Інакше 'recentlyGrounded' миттєво
            // повірить, що ми "долізли до верху" і скине нас назад.
            bool justStartedClimbing = Time.time - lastClimbStartTime <= climbStartLockTime;

            // 2. Якщо герой уже дістався верхнього блока (насправді стоїть на землі),
            // і вже пройшов час блокування, і НЕ тисне вниз — переводимо його в Normal.
            // Тоді він стоїть зверху як на звичайному блоці.
            if (recentlyGrounded && !wantsDown && !justStartedClimbing)
            {
                lastTopExitTime = Time.time;
                heroState.ChangeState(HeroState.Normal);
                return;
            }

            // 2. Якщо герой більше не торкається драбини тулубом,
            // значить він вийшов убік у тунель.
            // Переводимо в звичайний режим.
            if (!touchingLadder)
            {
                heroState.ChangeState(HeroState.Normal);
                return;
            }

            // 3. Інакше просто лишаємось у Climbing.
            return;
        }

        // -----------------------------
        // Якщо герой у звичайному стані
        // -----------------------------
        if (heroState.CurrentState == HeroState.Normal)
        {
            // Спершу перевіримо, чи взагалі намагаємося лізти:
            if (wantsUp || wantsDown)
            {
                if (!touchingLadder)
                {
                    Debug.Log($"[Ladder] Cannot climb: touchingLadder is FALSE. (Check ladderCheck position, Size: {ladderCheckSize}, and ladderMask)");
                    return;
                }

                if (topExitLocked && !wantsDown)
                {
                    Debug.Log($"[Ladder] Cannot climb up: topExitLocked is TRUE.");
                    return;
                }

                if (wantsDown)
                {
                    Debug.Log($"[Ladder] Entering climb DOWN");
                    lastClimbStartTime = Time.time;
                    heroState.ChangeState(HeroState.Climbing);
                    return;
                }

                if (wantsUp)
                {
                    Debug.Log($"[Ladder] Entering climb UP");
                    lastClimbStartTime = Time.time;
                    heroState.ChangeState(HeroState.Climbing);
                }
            }
        }
    }

    /// <summary>
    /// Перевіряє, чи тулуб героя зараз торкається драбини.
    /// Це набагато стабільніше за лічильник OnTriggerEnter/Exit на верхньому краї.
    /// </summary>
    private bool IsTouchingLadder()
    {
        if (ladderCheck == null)
        {
            return false;
        }

        return Physics2D.OverlapBox(ladderCheck.position, ladderCheckSize, 0f, ladderMask) != null;
    }

    /// <summary>
    /// Перевіряє, чи ноги героя стоять на твердому блоці.
    /// </summary>
    private bool IsGrounded()
    {
        if (groundCheck == null)
        {
            return false;
        }

        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask) != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (ladderCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(ladderCheck.position, ladderCheckSize);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
