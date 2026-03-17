using UnityEngine;

/// <summary>
/// Логіка драбини без "стрибків" на верхньому краї.
///
/// Поведінка:
/// 1. Якщо герой лізе і над ним Є драбина — можна лізти далі вгору.
/// 2. Якщо герой лізе, під ногами вже є верхній блок, а над ним драбини НЕМАЄ —
///    він виходить із Climbing і просто стає зверху.
/// 3. Якщо герой стоїть зверху на драбині — він НЕ падає вниз сам по собі.
/// 4. Якщо герой стоїть зверху і тисне вниз — починає спуск.
/// 5. Якщо поставити ще одну драбину вище — герой зможе знову лізти вгору.
/// </summary>
[RequireComponent(typeof(HeroStateController), typeof(HeroInputReader))]
public class LadderZoneDetector : MonoBehaviour
{
    [Header("Main Ladder Check")]
    [SerializeField] private Transform ladderCheck;

    // Основна зона перевірки драбини на рівні тулуба.
    [SerializeField] private Vector2 ladderBodyCheckSize = new Vector2(0.35f, 0.90f);

    [Header("Ladder Continuation Checks")]
    // Перевірка "чи є драбина вище".
    // Важливо: це і є ключ до коректної зупинки на верхньому блоці.
    [SerializeField] private Vector2 ladderAboveOffset = new Vector2(0f, 0.65f);
    [SerializeField] private Vector2 ladderAboveCheckSize = new Vector2(0.28f, 0.28f);

    // Перевірка "чи є драбина нижче".
    // Потрібно, щоб зі стоячого положення зверху можна було натиснути вниз
    // і почати спуск.
    [SerializeField] private Vector2 ladderBelowOffset = new Vector2(0f, -0.65f);
    [SerializeField] private Vector2 ladderBelowCheckSize = new Vector2(0.28f, 0.28f);

    [SerializeField] private LayerMask ladderMask;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [SerializeField] private LayerMask groundMask;

    [Header("Input Thresholds")]
    [SerializeField] private float climbUpThreshold = 0.25f;
    [SerializeField] private float climbDownThreshold = -0.25f;

    [Header("Stability")]
    // Після старту лазіння короткий час не даємо логіці верхнього краю
    // одразу викинути героя назад.
    [SerializeField] private float climbStartLockTime = 0.18f;

    // Коротка поблажка після втрати контакту з драбиною тулубом,
    // щоб на верхньому краї не було випадкового "відвалу".
    [SerializeField] private float ladderLostGraceTime = 0.10f;

    // Коротке блокування повторного входу в драбину після того,
    // як герой уже виліз на верхній блок.
    [SerializeField] private float topExitLockTime = 0.18f;

    private HeroStateController heroState;
    private HeroInputReader inputReader;

    private float lastClimbStartTime = -999f;
    private float lastLadderTouchTime = -999f;
    private float lastTopExitTime = -999f;

    private void Awake()
    {
        // Беремо потрібні компоненти з того ж Hero GameObject.
        heroState = GetComponent<HeroStateController>();
        inputReader = GetComponent<HeroInputReader>();
    }

    private void Update()
    {
        // Поточний інпут із джойстика / клавіатури.
        float vertical = inputReader.Vertical;

        bool wantsUp = vertical > climbUpThreshold;
        bool wantsDown = vertical < climbDownThreshold;

        // Перевірка основної драбини на рівні тулуба.
        bool ladderAtBody = HasLadderAt(GetBodyCheckPosition(), ladderBodyCheckSize);

        // Перевірка, чи є наступний сегмент драбини вище.
        bool ladderAbove = HasLadderAt(GetAboveCheckPosition(), ladderAboveCheckSize);

        // Перевірка, чи є драбина під ногами.
        // Це потрібно для спуску з верхнього блока.
        bool ladderBelow = HasLadderAt(GetBelowCheckPosition(), ladderBelowCheckSize);

        // Чи герой реально стоїть ногами на блоці.
        bool groundedNow = IsGrounded();

        // Запам'ятовуємо "нещодавній контакт із драбиною",
        // щоб не втрачати її через 1 кадр на краю.
        if (ladderAtBody || ladderAbove || ladderBelow)
        {
            lastLadderTouchTime = Time.time;
        }

        bool recentlyTouchedLadder = Time.time - lastLadderTouchTime <= ladderLostGraceTime;
        bool justStartedClimbing = Time.time - lastClimbStartTime <= climbStartLockTime;
        bool topExitLocked = Time.time - lastTopExitTime <= topExitLockTime;

        // ============================================================
        // Якщо герой уже у стані лазіння
        // ============================================================
        if (heroState.CurrentState == HeroState.Climbing)
        {
            // КЛЮЧОВА ЛОГІКА:
            // Якщо герой уже стоїть на верхньому блоці
            // І над ним драбина НЕ продовжується,
            // І він не тисне вниз,
            // то ми завершуємо climb і ставимо героя у Normal.
            //
            // Це і є правильна поведінка для "драбина висотою 1 блок":
            // герой виліз -> зупинився зверху -> не стрибає -> не падає.
            if (groundedNow && !ladderAbove && !wantsDown && !justStartedClimbing)
            {
                lastTopExitTime = Time.time;
                heroState.ChangeState(HeroState.Normal);
                return;
            }

            // Якщо герой іде вниз із верхнього блока —
            // лишаємо його у Climbing.
            if (wantsDown)
            {
                return;
            }

            // Якщо герой ще на драбині або щойно був на ній —
            // нічого не міняємо.
            if (ladderAtBody || ladderBelow || recentlyTouchedLadder)
            {
                return;
            }

            // Якщо драбина повністю втрачена і землі під ногами нема —
            // значить герой реально зійшов / звалився з неї.
            if (!groundedNow)
            {
                heroState.ChangeState(HeroState.Normal);
                return;
            }

            return;
        }

        // ============================================================
        // Якщо герой у звичайному стані
        // ============================================================
        if (heroState.CurrentState != HeroState.Normal)
        {
            return;
        }

        // Спуск із верхнього блока:
        // навіть якщо тулуб уже не перетинає драбину,
        // але під ногами є драбина — вниз має працювати.
        if (wantsDown && ladderBelow)
        {
            StartClimbing();
            return;
        }

        // Після щойно завершеного climb не даємо миттєво
        // знову зайти у нього від утримання "вверх".
        if (topExitLocked && !wantsDown)
        {
            return;
        }

        // Підйом угору:
        // якщо тулуб знаходиться в зоні драбини — починаємо climb.
        if (wantsUp && ladderAtBody)
        {
            StartClimbing();
            return;
        }

        // Якщо герой уже стоїть на верхньому блоці і над ним тепер з'явилася
        // наступна драбина — дозволяємо знову почати підйом.
        if (wantsUp && ladderAbove)
        {
            StartClimbing();
            return;
        }
    }

    /// <summary>
    /// Єдина точка входу у стан лазіння.
    /// Тут фіксуємо час старту, щоб уникнути миттєвого "самоскидання".
    /// </summary>
    private void StartClimbing()
    {
        lastClimbStartTime = Time.time;
        lastLadderTouchTime = Time.time;
        heroState.ChangeState(HeroState.Climbing);
    }

    /// <summary>
    /// Позиція головної перевірки драбини на рівні тулуба.
    /// </summary>
    private Vector2 GetBodyCheckPosition()
    {
        if (ladderCheck != null)
        {
            return ladderCheck.position;
        }

        return transform.position;
    }

    /// <summary>
    /// Позиція перевірки сегмента драбини над героєм.
    /// </summary>
    private Vector2 GetAboveCheckPosition()
    {
        return GetBodyCheckPosition() + ladderAboveOffset;
    }

    /// <summary>
    /// Позиція перевірки сегмента драбини під героєм.
    /// </summary>
    private Vector2 GetBelowCheckPosition()
    {
        return GetBodyCheckPosition() + ladderBelowOffset;
    }

    /// <summary>
    /// Універсальна перевірка драбини в заданій зоні.
    /// </summary>
    private bool HasLadderAt(Vector2 position, Vector2 size)
    {
        return Physics2D.OverlapBox(position, size, 0f, ladderMask) != null;
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
        // Основна зона тулуба.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(GetBodyCheckPosition(), ladderBodyCheckSize);

        // Перевірка драбини над героєм.
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(GetAboveCheckPosition(), ladderAboveCheckSize);

        // Перевірка драбини під героєм.
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(GetBelowCheckPosition(), ladderBelowCheckSize);

        // Перевірка землі під ногами.
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
