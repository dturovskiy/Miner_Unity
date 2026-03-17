using UnityEngine;

/// <summary>
/// НОВА архітектура драбини.
///
/// Важливо:
/// 1. Ми НЕ покладаємося на "recently grounded", "topExitLock", "race condition" тощо.
/// 2. Ми НЕ штовхаємо героя вгору через velocity і не чекаємо, що фізика сама все вирішить.
/// 3. Ми явно:
///    - знаходимо драбину біля тулуба або під ногами
///    - вирівнюємо героя по центру драбини
///    - рухаємо героя по Y через MovePosition
///    - жорстко зупиняємо на верхній точці останньої драбини
///
/// Це прибирає:
/// - підскоки на верхньому краї
/// - випадкове падіння вниз після "майже виліз"
/// - зависання між героєм і драбиною
/// </summary>
[RequireComponent(typeof(HeroStateController))]
[RequireComponent(typeof(HeroInputReader))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class LadderZoneDetector : MonoBehaviour
{
    [Header("Probe References")]
    [SerializeField] private Transform bodyProbe;
    [SerializeField] private Transform feetProbe;

    [Header("Probe Sizes")]
    // Перевірка драбини на рівні тулуба.
    [SerializeField] private Vector2 bodyProbeSize = new Vector2(0.35f, 0.85f);

    // Перевірка драбини під ногами.
    // Потрібна, щоб стоячи зверху можна було натиснути вниз і почати спуск.
    [SerializeField] private Vector2 feetProbeSize = new Vector2(0.35f, 0.20f);

    [Header("Movement")]
    [SerializeField] private float climbSpeed = 3f;

    // Наскільки швидко герой "підтягується" до центру драбини по X.
    [SerializeField] private float snapToCenterSpeed = 14f;

    [Header("Input Thresholds")]
    [SerializeField] private float upThreshold = 0.25f;
    [SerializeField] private float downThreshold = -0.25f;

    [Header("Masks")]
    [SerializeField] private LayerMask ladderMask;

    [Header("Neighbour Search")]
    // Невеликий зазор для пошуку сусідньої драбини зверху/знизу.
    [SerializeField] private float ladderNeighbourGap = 0.04f;

    private HeroStateController heroState;
    private HeroInputReader inputReader;
    private Rigidbody2D rb;
    private Collider2D heroCollider;

    // Поточна активна драбина, по якій герой реально лізе.
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
        // Вхід у драбину керується в Update,
        // бо інпут читається теж в Update.
        if (heroState.CurrentState != HeroState.Normal)
        {
            return;
        }

        float vertical = inputReader.Vertical;

        bool wantsUp = vertical > upThreshold;
        bool wantsDown = vertical < downThreshold;

        // Якщо гравець тисне вгору —
        // шукаємо драбину на рівні тулуба.
        if (wantsUp)
        {
            LadderBehaviour ladderAtBody = FindLadderAtBody();

            if (ladderAtBody != null)
            {
                EnterLadder(ladderAtBody);
                return;
            }
        }

        // Якщо гравець тисне вниз —
        // шукаємо драбину під ногами.
        // Це дозволяє стояти зверху на блоці й почати спуск без ривків.
        if (wantsDown)
        {
            LadderBehaviour ladderBelowFeet = FindLadderBelowFeet();

            if (ladderBelowFeet != null)
            {
                EnterLadder(ladderBelowFeet);
                return;
            }
        }
    }

    private void FixedUpdate()
    {
        if (heroState.CurrentState != HeroState.Climbing)
        {
            return;
        }

        if (currentLadder == null)
        {
            // Якщо з якоїсь причини поточна драбина втрачена —
            // пробуємо перевизначити її по зоні тулуба або ніг.
            currentLadder = FindLadderAtBody();

            if (currentLadder == null)
            {
                currentLadder = FindLadderBelowFeet();
            }

            // Якщо й це не допомогло —
            // завершуємо climb без ривка.
            if (currentLadder == null)
            {
                ExitLadder();
                return;
            }
        }

        // Під час climb фізична швидкість не повинна накопичуватись.
        rb.linearVelocity = Vector2.zero;

        float vertical = inputReader.Vertical;
        Vector2 position = rb.position;

        // Якщо тулуб уже перетнувся з наступною драбиною —
        // переключаємося на неї як на нову активну.
        LadderBehaviour ladderAtBody = FindLadderAtBody();
        if (ladderAtBody != null)
        {
            currentLadder = ladderAtBody;
        }

        // Шукаємо, чи є продовження драбини зверху / знизу.
        LadderBehaviour ladderAbove = FindNeighbourAbove(currentLadder);
        LadderBehaviour ladderBelow = FindNeighbourBelow(currentLadder);

        // Завжди акуратно центруємо героя по осі X до центру активної драбини.
        float targetX = Mathf.MoveTowards(
            position.x,
            currentLadder.CenterX,
            snapToCenterSpeed * Time.fixedDeltaTime
        );

        float targetY = position.y;

        // -------------------------------------------------
        // Рух угору
        // -------------------------------------------------
        if (vertical > upThreshold)
        {
            targetY += vertical * climbSpeed * Time.fixedDeltaTime;

            // Якщо НАД поточною драбиною немає продовження,
            // то верхній рух має завершитися рівно на top stand position.
            if (ladderAbove == null)
            {
                float topStandY = currentLadder.GetTopStandCenterY(heroCollider);

                // Як тільки доходиш до вершини останньої драбини —
                // жорстко ставимо героя на верхній блок,
                // завершуємо climb і НЕ даємо "вистрілити" вище.
                if (targetY >= topStandY)
                {
                    targetY = topStandY;

                    rb.MovePosition(new Vector2(currentLadder.CenterX, targetY));
                    ExitLadder();

                    return;
                }
            }
        }
        // -------------------------------------------------
        // Рух униз
        // -------------------------------------------------
        else if (vertical < downThreshold)
        {
            targetY += vertical * climbSpeed * Time.fixedDeltaTime;

            // Якщо під поточною драбиною немає продовження,
            // нижче bottom stand position опускатися не даємо.
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
        // -------------------------------------------------
        // Немає вертикального інпуту
        // -------------------------------------------------
        else
        {
            // Якщо інпуту по Y нема —
            // герой просто "висить" на поточній позиції драбини без падіння.
            targetY = position.y;
        }

        rb.MovePosition(new Vector2(targetX, targetY));
    }

    /// <summary>
    /// Заходимо в режим climb.
    /// Скидаємо поточну фізичну швидкість і вирівнюємо героя по осі X.
    /// </summary>
    private void EnterLadder(LadderBehaviour ladder)
    {
        currentLadder = ladder;

        rb.linearVelocity = Vector2.zero;

        Vector2 position = rb.position;

        // При вході в драбину одразу притягуємо героя до центру драбини по X.
        // По Y НЕ телепортуємо агресивно, щоб не було ривка.
        rb.position = new Vector2(currentLadder.CenterX, position.y);

        heroState.ChangeState(HeroState.Climbing);
    }

    /// <summary>
    /// Вихід із драбини без інерції.
    /// Після цього HeroMotor знову бере на себе звичайний рух по землі.
    /// </summary>
    private void ExitLadder()
    {
        rb.linearVelocity = Vector2.zero;
        heroState.ChangeState(HeroState.Normal);
    }

    /// <summary>
    /// Знаходимо драбину на рівні тулуба.
    /// Це основний спосіб почати climb угору.
    /// </summary>
    private LadderBehaviour FindLadderAtBody()
    {
        return FindBestLadderAtBox(GetBodyProbePosition(), bodyProbeSize);
    }

    /// <summary>
    /// Знаходимо драбину прямо під ногами.
    /// Це основний спосіб почати спуск зверху вниз.
    /// </summary>
    private LadderBehaviour FindLadderBelowFeet()
    {
        return FindBestLadderAtBox(GetFeetProbePosition(), feetProbeSize);
    }

    /// <summary>
    /// Шукаємо сусідню драбину зверху відносно поточної.
    /// Якщо вона є — герой може лізти далі вгору.
    /// Якщо її нема — це останній верхній сегмент.
    /// </summary>
    private LadderBehaviour FindNeighbourAbove(LadderBehaviour source)
    {
        if (source == null)
        {
            return null;
        }

        Vector2 size = new Vector2(bodyProbeSize.x, Mathf.Max(0.10f, source.Height * 0.45f));
        Vector2 center = new Vector2(
            source.CenterX,
            source.TopY + size.y * 0.5f + ladderNeighbourGap
        );

        return FindBestLadderAtBox(center, size, source);
    }

    /// <summary>
    /// Шукаємо сусідню драбину знизу відносно поточної.
    /// Якщо вона є — герой може лізти далі вниз.
    /// </summary>
    private LadderBehaviour FindNeighbourBelow(LadderBehaviour source)
    {
        if (source == null)
        {
            return null;
        }

        Vector2 size = new Vector2(bodyProbeSize.x, Mathf.Max(0.10f, source.Height * 0.45f));
        Vector2 center = new Vector2(
            source.CenterX,
            source.BottomY - size.y * 0.5f - ladderNeighbourGap
        );

        return FindBestLadderAtBox(center, size, source);
    }

    /// <summary>
    /// Узагальнений пошук "найкращої" драбини в заданій коробці.
    /// Якщо коробка перетинає кілька драбин — беремо ту,
    /// у якої центр найближчий до центру probe.
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

            // Чим ближча драбина по X та Y до probe-центру —
            // тим кращий кандидат.
            float score =
                Mathf.Abs(ladder.WorldBounds.center.x - center.x) +
                Mathf.Abs(ladder.WorldBounds.center.y - center.y);

            if (score < bestScore)
            {
                bestScore = score;
                best = ladder;
            }
        }

        return best;
    }

    /// <summary>
    /// Якщо окремий bodyProbe не заданий —
    /// використовуємо центр колайдера героя.
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
    /// Якщо окремий feetProbe не заданий —
    /// беремо точку трохи вище мінімуму hero collider.
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(GetBodyProbePosition(), bodyProbeSize);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(GetFeetProbePosition(), feetProbeSize);

        // Якщо є активна драбина — малюємо її центр.
        if (currentLadder != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(
                new Vector3(currentLadder.CenterX, currentLadder.WorldBounds.center.y, 0f),
                0.06f
            );
        }
    }
}
