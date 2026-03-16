using UnityEngine;

/// <summary>
/// Стабільна логіка драбини:
/// 1. Герой може залазити вгору/вниз.
/// 2. На верхньому краї драбини герой стає у Normal і стоїть як на звичайному блоці.
/// 3. Якщо герой іде вбік у тунель із середини драбини — він просто виходить із драбини.
/// 4. Немає залежності від OnTriggerEnter/Exit, які часто дають ривки на верхньому краї.
/// 5. Є захист від race condition:
///    коли герой тільки почав лізти знизу, ми короткий час ігноруємо recentlyGrounded,
///    щоб його не викинуло назад у Normal на наступному кадрі.
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
    // Мінімальне відхилення джойстика, після якого вважаємо,
    // що гравець справді хоче почати лазіння.
    [SerializeField] private float climbEnterThreshold = 0.25f;

    // Якщо гравець тисне вниз сильніше за цей поріг —
    // починаємо / продовжуємо спуск.
    [SerializeField] private float climbDownThreshold = -0.25f;

    [Header("Stability")]
    // Пам’ять контакту із землею.
    // Допомагає на верхньому краї драбини, коли groundCheck
    // може на 1 кадр втратити або знайти землю.
    [SerializeField] private float groundedMemoryTime = 0.12f;

    // Коротка затримка після виходу на верхню площадку,
    // щоб герой не застрибував назад у Climbing,
    // якщо джойстик ще трохи тисне вгору.
    [SerializeField] private float topExitLockTime = 0.18f;

    [Header("Climb Start Protection")]
    // Після початку лазіння знизу ми короткий час
    // ігноруємо recentlyGrounded.
    // Це прибирає баг, коли герой встигає увійти в Climbing,
    // але на наступному ж кадрі вилітає в Normal,
    // бо ще "пам’ятає", що щойно стояв на землі.
    [SerializeField] private float climbStartLockTime = 0.2f;

    private HeroStateController heroState;
    private HeroInputReader inputReader;

    // Час останнього підтвердженого контакту із землею.
    private float lastGroundedTime = -999f;

    // Час останнього виходу на верхню площадку.
    private float lastTopExitTime = -999f;

    // Час останнього входу у стан Climbing.
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

        // Якщо ноги героя стоять на землі прямо зараз —
        // запам’ятовуємо цей момент.
        if (groundedNow)
        {
            lastGroundedTime = Time.time;
        }

        // "Недавно стояв на землі" — це не обов’язково цей кадр,
        // а ще невелике вікно пам’яті після нього.
        bool recentlyGrounded = Time.time - lastGroundedTime <= groundedMemoryTime;

        // Після виходу на верхню платформу тимчасово блокуємо
        // повторний вхід у climb від випадкового up.
        bool topExitLocked = Time.time - lastTopExitTime <= topExitLockTime;

        // Перші 0.2 сек після входу в Climbing
        // не дозволяємо логіці верхнього виходу одразу ж викинути героя назад.
        bool justStartedClimbing = Time.time - lastClimbStartTime <= climbStartLockTime;

        float vertical = inputReader.Vertical;

        bool wantsUp = vertical > climbEnterThreshold;
        bool wantsDown = vertical < climbDownThreshold;

        // ------------------------------------------------
        // Якщо герой ВЖЕ у стані лазіння
        // ------------------------------------------------
        if (heroState.CurrentState == HeroState.Climbing)
        {
            // Якщо герой уже торкнувся землі і не тисне вниз,
            // значить він доліз до верхньої площадки.
            // Але НЕ робимо цього в перші миті після старту лазіння,
            // інакше отримаємо race condition з нижньою землею.
            if (recentlyGrounded && !wantsDown && !justStartedClimbing)
            {
                lastTopExitTime = Time.time;
                heroState.ChangeState(HeroState.Normal);
                return;
            }

            // Якщо герой більше не торкається драбини тулубом,
            // значить він зійшов убік у тунель — виходимо з climb.
            if (!touchingLadder)
            {
                heroState.ChangeState(HeroState.Normal);
                return;
            }

            // Інакше просто лишаємось у Climbing.
            return;
        }

        // ------------------------------------------------
        // Якщо герой у звичайному стані
        // ------------------------------------------------
        if (heroState.CurrentState == HeroState.Normal)
        {
            // Без контакту з драбиною активувати climb не можна.
            if (!touchingLadder)
            {
                return;
            }

            // Якщо щойно вилізли на верхню площадку,
            // блокуємо повторний вхід по up.
            // Але вниз спускатися дозволяємо завжди.
            if (topExitLocked && !wantsDown)
            {
                return;
            }

            // Спуск зверху вниз дозволяємо завжди,
            // якщо герой торкається драбини і тисне вниз.
            if (wantsDown)
            {
                StartClimbing();
                return;
            }

            // Підйом вгору теж дозволяємо,
            // навіть якщо герой щойно стояв на землі внизу драбини.
            // Саме це виправляє головний баг "не хоче починати лізти знизу".
            if (wantsUp)
            {
                StartClimbing();
                return;
            }
        }
    }

    /// <summary>
    /// Єдина точка входу в Climbing.
    /// Тут фіксуємо час початку лазіння,
    /// щоб тимчасово ігнорувати recentlyGrounded.
    /// </summary>
    private void StartClimbing()
    {
        lastClimbStartTime = Time.time;
        heroState.ChangeState(HeroState.Climbing);
    }

    /// <summary>
    /// Перевіряє, чи тулуб героя зараз торкається драбини.
    /// Це стабільніше за OnTriggerEnter/Exit на верхньому краї.
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
