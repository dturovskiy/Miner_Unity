using UnityEngine;

/// <summary>
/// Стабільна логіка драбини:
/// - Герой може почати лізти знизу, навіть якщо ще "пам'ятає" землю під ногами.
/// - Біля верхнього краю є невелика поблажка, якщо тулуб на мить втратив контакт із драбиною.
/// - Вихід на верхню площадку відбувається лише тоді, коли герой реально стоїть на землі,
///   а не в перший же кадр після старту підйому.
/// - Якщо герой відпускає джойстик посеред драбини, він має залишатися у Climbing і не падати.
/// </summary>
[RequireComponent(typeof(HeroStateController), typeof(HeroInputReader))]
public class LadderZoneDetector : MonoBehaviour
{
    [Header("Ladder Detection")]
    [SerializeField] private Transform ladderCheck;

    // Зона перевірки драбини на рівні тулуба.
    // Трохи вища, ніж була, щоб на верхньому краї не було миттєвої втрати контакту.
    [SerializeField] private Vector2 ladderCheckSize = new Vector2(0.35f, 1.05f);

    // Layer, на якому лежать усі драбини.
    [SerializeField] private LayerMask ladderMask;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;

    // Радіус перевірки під ногами героя.
    [SerializeField] private float groundCheckRadius = 0.12f;

    // Layer твердих поверхонь: блоки, земля, платформи.
    [SerializeField] private LayerMask groundMask;

    [Header("Input")]
    // Поріг для початку підйому.
    [SerializeField] private float climbEnterThreshold = 0.25f;

    // Поріг для початку спуску.
    [SerializeField] private float climbDownThreshold = -0.25f;

    [Header("Stability")]
    // Коротка "пам'ять" того, що герой стояв на землі.
    [SerializeField] private float groundedMemoryTime = 0.12f;

    // Після виходу на верхню платформу не даємо миттєво знову зайти в Climbing,
    // якщо гравець усе ще трохи тисне джойстик вгору.
    [SerializeField] private float topExitLockTime = 0.18f;

    // Після старту лазіння короткий час ігноруємо recentlyGrounded,
    // щоб герой не вилетів у Normal одразу з нижньої точки драбини.
    [SerializeField] private float climbStartLockTime = 0.20f;

    // Якщо біля верхнього краю тулуб на 1-2 кадри "втратив" драбину,
    // не виходимо з Climbing миттєво.
    [SerializeField] private float ladderLostGraceTime = 0.12f;

    private HeroStateController heroState;
    private HeroInputReader inputReader;

    // Час останнього контакту із землею.
    private float lastGroundedTime = -999f;

    // Час останнього виходу на верхню площадку.
    private float lastTopExitTime = -999f;

    // Час останнього входу в Climbing.
    private float lastClimbStartTime = -999f;

    // Час останнього контакту з драбиною.
    private float lastLadderTouchTime = -999f;

    private void Awake()
    {
        // Беремо компоненти з того ж об'єкта героя.
        heroState = GetComponent<HeroStateController>();
        inputReader = GetComponent<HeroInputReader>();
    }

    private void Update()
    {
        bool touchingLadderNow = IsTouchingLadder();
        bool groundedNow = IsGrounded();

        // Запам'ятовуємо, що драбина була під тулубом недавно.
        if (touchingLadderNow)
        {
            lastLadderTouchTime = Time.time;
        }

        // Запам'ятовуємо, що під ногами була земля.
        if (groundedNow)
        {
            lastGroundedTime = Time.time;
        }

        // Поблажка після контакту із землею.
        bool recentlyGrounded = Time.time - lastGroundedTime <= groundedMemoryTime;

        // Поблажка після втрати драбини.
        bool recentlyTouchedLadder = Time.time - lastLadderTouchTime <= ladderLostGraceTime;

        // Захист від повторного входу в драбину відразу після виходу зверху.
        bool topExitLocked = Time.time - lastTopExitTime <= topExitLockTime;

        // Захист від миттєвого виходу з драбини в перші миті після старту підйому.
        bool justStartedClimbing = Time.time - lastClimbStartTime <= climbStartLockTime;

        float vertical = inputReader.Vertical;

        bool wantsUp = vertical > climbEnterThreshold;
        bool wantsDown = vertical < climbDownThreshold;

        // ------------------------------------------------------------
        // Якщо герой уже у стані лазіння
        // ------------------------------------------------------------
        if (heroState.CurrentState == HeroState.Climbing)
        {
            // Якщо герой реально дістався верхньої поверхні
            // і не намагається лізти вниз — переводимо в Normal.
            // Але НЕ робимо цього відразу після старту climb знизу,
            // інакше отримаємо race condition із нижньою землею.
            if (groundedNow && !wantsDown && !justStartedClimbing)
            {
                lastTopExitTime = Time.time;
                heroState.ChangeState(HeroState.Normal);
                return;
            }

            // Якщо драбина на мить зникла з-під тулуба біля верхнього краю,
            // не виходимо миттєво.
            // Виходимо тільки якщо контакт із драбиною втрачено вже не коротко
            // і герой не стоїть на землі.
            if (!recentlyTouchedLadder && !groundedNow)
            {
                heroState.ChangeState(HeroState.Normal);
                return;
            }

            // Інакше лишаємося в Climbing.
            return;
        }

        // ------------------------------------------------------------
        // Якщо герой у звичайному стані
        // ------------------------------------------------------------
        if (heroState.CurrentState == HeroState.Normal)
        {
            // Якщо драбини поруч немає навіть із короткою поблажкою,
            // зайти в climb не можна.
            if (!recentlyTouchedLadder)
            {
                return;
            }

            // Після виходу на верхню платформу блокуємо повторний re-enter по up.
            // Але вниз дозволяємо завжди.
            if (topExitLocked && !wantsDown)
            {
                return;
            }

            // Спуск зверху вниз.
            if (wantsDown)
            {
                StartClimbing();
                return;
            }

            // Підйом знизу вгору.
            // Тут НЕ перевіряємо recentlyGrounded,
            // бо саме це ламало старт підйому з нижньої точки.
            if (wantsUp)
            {
                StartClimbing();
                return;
            }
        }
    }

    /// <summary>
    /// Єдина точка входу в Climbing.
    /// Тут оновлюємо службові таймери, щоб не було смикань.
    /// </summary>
    private void StartClimbing()
    {
        lastClimbStartTime = Time.time;
        lastLadderTouchTime = Time.time;
        heroState.ChangeState(HeroState.Climbing);
    }

    /// <summary>
    /// Перевірка контакту тулуба героя з драбиною.
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
    /// Перевірка землі під ногами героя.
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
        // Візуалізація зони тулуба для драбини.
        if (ladderCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(ladderCheck.position, ladderCheckSize);
        }

        // Візуалізація перевірки землі під ногами.
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
