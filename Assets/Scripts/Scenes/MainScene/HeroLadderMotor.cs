using UnityEngine;

/// <summary>
/// Окрема, чиста логіка драбини.
///
/// Архітектурні правила:
/// 1. Цей скрипт ЄДИНИЙ керує рухом героя по драбині.
/// 2. HeroMotor не повинен містити логіку climb.
/// 3. Якщо герой стоїть на верхньому блоці останньої драбини,
///    натискання ВГОРУ не повинно знову затягувати його в climb.
/// 4. Рух по драбині виконується через MovePosition, а не через velocity по Y.
///    Це прибирає підскоки і "вистріли" над драбиною.
/// </summary>
[RequireComponent(typeof(HeroStateController))]
[RequireComponent(typeof(HeroInputReader))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class HeroLadderMotor : MonoBehaviour
{
    [Header("Probe References")]
    [SerializeField] private Transform bodyProbe;
    [SerializeField] private Transform feetProbe;

    [Header("Probe Sizes")]
    [SerializeField] private Vector2 bodyProbeSize = new Vector2(0.35f, 0.85f);
    [SerializeField] private Vector2 feetProbeSize = new Vector2(0.35f, 0.20f);

    [Header("Movement")]
    [SerializeField] private float climbSpeed = 3.0f;
    [SerializeField] private float snapToCenterSpeed = 14.0f;

    [Header("Input Thresholds")]
    [SerializeField] private float upThreshold = 0.25f;
    [SerializeField] private float downThreshold = -0.25f;

    [Header("Masks")]
    [SerializeField] private LayerMask ladderMask;
    [SerializeField] private LayerMask groundMask;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.12f;

    [Header("Neighbour Search")]
    [SerializeField] private float neighbourGap = 0.04f;
    [SerializeField] private Vector2 neighbourProbeSize = new Vector2(0.30f, 0.30f);

    private HeroStateController heroState;
    private HeroInputReader inputReader;
    private Rigidbody2D rb;
    private Collider2D heroCollider;

    /// <summary>
    /// Поточна активна драбина, по якій герой реально лізе.
    /// </summary>
    private LadderBehaviour currentLadder;

    private void Awake()
    {
        heroState = GetComponent<HeroStateController>();
        inputReader = GetComponent<HeroInputReader>();
        rb = GetComponent<Rigidbody2D>();
        heroCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        // У цьому методі тільки рішення:
        // входити в climb чи ні.
        if (heroState.CurrentState != HeroState.Normal)
        {
            return;
        }

        float vertical = inputReader.Vertical;

        bool wantsUp = vertical > upThreshold;
        bool wantsDown = vertical < downThreshold;

        LadderBehaviour ladderAtBody = FindLadderAtBody();
        LadderBehaviour ladderBelowFeet = FindLadderBelowFeet();

        bool groundedNow = IsGrounded();

        // -----------------------------
        // КЛЮЧОВЕ ПРАВИЛО №1
        // -----------------------------
        // Якщо герой уже СТОЇТЬ зверху на драбині,
        // і над цією драбиною НЕМАЄ продовження,
        // то натискання ВГОРУ не повинно повторно запускати climb.
        //
        // Саме цього зараз не вистачає поточному репо.
        if (wantsUp && groundedNow && ladderBelowFeet != null)
        {
            bool hasLadderAbove = FindNeighbourAbove(ladderBelowFeet) != null;

            if (!hasLadderAbove)
            {
                // Стоїмо на верхньому блоці останньої драбини.
                // Вгору нічого не робимо.
                return;
            }
        }

        // Початок підйому з тулуба.
        if (wantsUp && ladderAtBody != null)
        {
            EnterLadder(ladderAtBody);
            return;
        }

        // Початок спуску з верхнього блока.
        if (wantsDown && ladderBelowFeet != null)
        {
            EnterLadder(ladderBelowFeet);
            return;
        }
    }

    private void FixedUpdate()
    {
        if (heroState.CurrentState != HeroState.Climbing)
        {
            return;
        }

        // Якщо активна драбина зникла — пробуємо знайти її заново.
        if (currentLadder == null)
        {
            currentLadder = FindLadderAtBody();

            if (currentLadder == null)
            {
                currentLadder = FindLadderBelowFeet();
            }

            if (currentLadder == null)
            {
                ExitLadder();
                return;
            }
        }

        // Важливо:
        // під час climb не накопичуємо фізичну швидкість.
        rb.linearVelocity = Vector2.zero;

        float vertical = inputReader.Vertical;
        Vector2 currentPosition = rb.position;

        // Якщо тулуб уже перетнув наступну драбину,
        // переключаємо активну драбину на неї.
        LadderBehaviour ladderAtBodyNow = FindLadderAtBody();
        if (ladderAtBodyNow != null)
        {
            currentLadder = ladderAtBodyNow;
        }

        LadderBehaviour ladderAbove = FindNeighbourAbove(currentLadder);
        LadderBehaviour ladderBelow = FindNeighbourBelow(currentLadder);

        // Притягуємо героя до центру драбини по X без різкого телепорту.
        float targetX = Mathf.MoveTowards(
            currentPosition.x,
            currentLadder.CenterX,
            snapToCenterSpeed * Time.fixedDeltaTime
        );

        float targetY = currentPosition.y;

        bool wantsUp = vertical > upThreshold;
        bool wantsDown = vertical < downThreshold;

        // --------------------------------------
        // Рух УГОРУ
        // --------------------------------------
        if (wantsUp)
        {
            targetY += vertical * climbSpeed * Time.fixedDeltaTime;

            // Якщо над поточною драбиною продовження НЕМАЄ,
            // то верхня точка строго обмежена.
            if (ladderAbove == null)
            {
                float topStandY = currentLadder.GetTopStandCenterY(heroCollider);

                // Коли доходимо до вершини останньої драбини,
                // жорстко ставимо героя на верхній блок
                // і завершуємо climb БЕЗ підскоку.
                if (targetY >= topStandY)
                {
                    targetY = topStandY;

                    rb.MovePosition(new Vector2(currentLadder.CenterX, targetY));
                    ExitLadder();
                    return;
                }
            }
        }
        // --------------------------------------
        // Рух УНИЗ
        // --------------------------------------
        else if (wantsDown)
        {
            targetY += vertical * climbSpeed * Time.fixedDeltaTime;

            // Якщо нижче цієї драбини вже нема продовження,
            // не даємо піти нижче нижньої межі останньої драбини.
            if (ladderBelow == null)
            {
                float bottomStandY = currentLadder.GetBottomStandCenterY(heroCollider);

                if (targetY <= bottomStandY)
                {
                    targetY = bottomStandY;

                    rb.MovePosition(new Vector2(currentLadder.CenterX, targetY));
                    ExitLadder();
                    return;
                }
            }
        }
        else
        {
            // Якщо вертикального інпуту нема —
            // герой просто висить на драбині без падіння.
            targetY = currentPosition.y;
        }

        rb.MovePosition(new Vector2(targetX, targetY));
    }

    /// <summary>
    /// Початок climb.
    /// Одразу обнуляємо інерцію і запам'ятовуємо поточну драбину.
    /// </summary>
    private void EnterLadder(LadderBehaviour ladder)
    {
        currentLadder = ladder;
        rb.linearVelocity = Vector2.zero;
        heroState.ChangeState(HeroState.Climbing);
    }

    /// <summary>
    /// Завершення climb без залишкового імпульсу.
    /// </summary>
    private void ExitLadder()
    {
        rb.linearVelocity = Vector2.zero;
        heroState.ChangeState(HeroState.Normal);
    }

    /// <summary>
    /// Пошук драбини на рівні тулуба героя.
    /// </summary>
    private LadderBehaviour FindLadderAtBody()
    {
        return FindBestLadderAtBox(GetBodyProbePosition(), bodyProbeSize);
    }

    /// <summary>
    /// Пошук драбини під ногами героя.
    /// Це потрібно для коректного старту спуску зверху.
    /// </summary>
    private LadderBehaviour FindLadderBelowFeet()
    {
        return FindBestLadderAtBox(GetFeetProbePosition(), feetProbeSize);
    }

    /// <summary>
    /// Пошук сусідньої драбини над поточною.
    /// Якщо вона є — можна продовжувати climb угору.
    /// Якщо її немає — це остання вершина.
    /// </summary>
    private LadderBehaviour FindNeighbourAbove(LadderBehaviour source)
    {
        if (source == null)
        {
            return null;
        }

        Vector2 probeCenter = new Vector2(
            source.CenterX,
            source.TopY + neighbourGap + neighbourProbeSize.y * 0.5f
        );

        return FindBestLadderAtBox(probeCenter, neighbourProbeSize, source);
    }

    /// <summary>
    /// Пошук сусідньої драбини знизу.
    /// </summary>
    private LadderBehaviour FindNeighbourBelow(LadderBehaviour source)
    {
        if (source == null)
        {
            return null;
        }

        Vector2 probeCenter = new Vector2(
            source.CenterX,
            source.BottomY - neighbourGap - neighbourProbeSize.y * 0.5f
        );

        return FindBestLadderAtBox(probeCenter, neighbourProbeSize, source);
    }

    /// <summary>
    /// Пошук найближчої драбини у вказаній зоні.
    /// Якщо в зоні кілька драбин — беремо найкращу по близькості до центру probe.
    /// </summary>
    private LadderBehaviour FindBestLadderAtBox(Vector2 center, Vector2 size, LadderBehaviour ignore = null)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, ladderMask);

        LadderBehaviour best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            LadderBehaviour ladder = hit.GetComponent<LadderBehaviour>();
            if (ladder == null)
            {
                continue;
            }

            if (ladder == ignore)
            {
                continue;
            }

            float score =
                Mathf.Abs(ladder.Bounds.center.x - center.x) +
                Mathf.Abs(ladder.Bounds.center.y - center.y);

            if (score < bestScore)
            {
                bestScore = score;
                best = ladder;
            }
        }

        return best;
    }

    /// <summary>
    /// Позиція probe на рівні тулуба.
    /// Якщо Transform не заданий — беремо центр колайдера героя.
    /// </summary>
    private Vector2 GetBodyProbePosition()
    {
        if (bodyProbe != null)
        {
            return bodyProbe.position;
        }

        return heroCollider.bounds.center;
    }

    /// <summary>
    /// Позиція probe під ногами.
    /// Якщо Transform не заданий — беремо точку трохи вище нижньої межі колайдера героя.
    /// </summary>
    private Vector2 GetFeetProbePosition()
    {
        if (feetProbe != null)
        {
            return feetProbe.position;
        }

        Bounds bounds = heroCollider.bounds;
        return new Vector2(bounds.center.x, bounds.min.y + 0.05f);
    }

    /// <summary>
    /// Перевірка, чи стоять ноги героя на землі.
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(GetBodyProbePosition(), bodyProbeSize);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(GetFeetProbePosition(), feetProbeSize);

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (currentLadder != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(
                new Vector3(currentLadder.CenterX, currentLadder.Bounds.center.y, 0f),
                0.05f
            );
        }
    }
}
