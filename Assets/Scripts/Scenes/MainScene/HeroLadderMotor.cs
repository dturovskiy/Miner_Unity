using UnityEngine;

/// <summary>
/// Окремий мотор для драбини.
/// Логіка:
/// 1) знайти конкретну активну драбину;
/// 2) зафіксувати героя в центр драбини по X;
/// 3) рухати лише по Y;
/// 4) коректно вивести зверху/знизу.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(HeroInputReader))]
[RequireComponent(typeof(HeroStateHub))]
[RequireComponent(typeof(HeroSensors))]
public sealed class HeroLadderMotor : MonoBehaviour
{
    [SerializeField] private float climbSpeed = 2.8f;
    [SerializeField] private float snapSpeed = 20f;
    [SerializeField] private float enterThreshold = 0.25f;
    [SerializeField] private float exitDownThreshold = -0.25f;

    private Rigidbody2D rb;
    private Collider2D heroCollider;
    private HeroInputReader input;
    private HeroStateHub stateHub;
    private HeroSensors sensors;

    private float defaultGravityScale;
    private LadderBehaviour activeLadder;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        heroCollider = GetComponent<Collider2D>();
        input = GetComponent<HeroInputReader>();
        stateHub = GetComponent<HeroStateHub>();
        sensors = GetComponent<HeroSensors>();

        defaultGravityScale = rb.gravityScale;
    }

    private void Update()
    {
        if (!stateHub.IsOnLadder())
        {
            TryEnterLadder();
        }
    }

    private void FixedUpdate()
    {
        if (!stateHub.IsOnLadder() || activeLadder == null)
        {
            return;
        }

        MoveOnLadder();
    }

    private void TryEnterLadder()
    {
        // Поки копаємо або в hurt — не входимо в драбину.
        if (stateHub.ActionState != HeroActionState.None)
        {
            return;
        }

        float vertical = input.Vertical;

        // Вхід знизу/посередині при русі вгору.
        if (vertical > enterThreshold)
        {
            LadderBehaviour ladder = sensors.GetLadderAtBody();
            if (ladder != null)
            {
                // Блокуємо повторний вхід ВГОРУ, тільки якщо ми вже стоїмо НА ВЕРХІВЦІ останнього блоку.
                if (stateHub.LocomotionState == HeroLocomotionState.Grounded)
                {
                    bool isAtTopExit = rb.position.y >= ladder.TopY;
                    if (isAtTopExit && !HasLadderAbove()) return;
                }

                EnterLadder(ladder);
                return;
            }
        }

        // Вхід зверху при русі вниз.
        if (vertical < exitDownThreshold)
        {
            LadderBehaviour ladder = sensors.GetLadderAtFeet();
            if (ladder != null)
            {
                EnterLadder(ladder);
            }
        }
    }

    private void EnterLadder(LadderBehaviour ladder)
    {
        activeLadder = ladder;
        stateHub.SetLocomotion(HeroLocomotionState.Ladder);

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        // Одразу притискаємо героя до осі драбини.
        rb.position = new Vector2(activeLadder.CenterX, rb.position.y);
    }

    private void ExitLadder(HeroLocomotionState nextState)
    {
        activeLadder = null;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = defaultGravityScale;
        stateHub.SetLocomotion(nextState);
    }

    /// <summary>
    /// Перевіряє, чи є драбина над поточною активною драбиною.
    /// Це дозволяє безшовно переходити між сегментами без виходу зі стану Climbing.
    /// </summary>
    private bool HasLadderAbove()
    {
        if (activeLadder == null) return false;
        Vector2 checkPos = new Vector2(activeLadder.CenterX, activeLadder.TopY + 0.1f);
        return Physics2D.OverlapPoint(checkPos, sensors.LadderMask) != null;
    }

    /// <summary>
    /// Перевіряє, чи є драбина під поточною активною драбиною.
    /// </summary>
    private bool HasLadderBelow()
    {
        if (activeLadder == null) return false;
        Vector2 checkPos = new Vector2(activeLadder.CenterX, activeLadder.BottomY - 0.1f);
        return Physics2D.OverlapPoint(checkPos, sensors.LadderMask) != null;
    }

    private void MoveOnLadder()
    {
        float vertical = input.Vertical;
        Vector2 current = rb.position;

        // На драбині X завжди прямує до центра драбини.
        float nextX = Mathf.MoveTowards(
            current.x,
            activeLadder.CenterX,
            snapSpeed * Time.fixedDeltaTime
        );

        float nextY = current.y + vertical * climbSpeed * Time.fixedDeltaTime;

        // Оновлюємо активну драбину, якщо перейшли на наступний сегмент.
        LadderBehaviour ladderAtBody = sensors.GetLadderAtBody();
        if (ladderAtBody != null && ladderAtBody != activeLadder)
        {
            activeLadder = ladderAtBody;
        }

        float topStandY = activeLadder.GetTopStandY(heroCollider);
        float bottomStandY = activeLadder.GetBottomStandY(heroCollider);

        // Вихід ВГОРУ: тільки якщо над поточною драбиною НЕМАЄ продовження.
        if (vertical > 0f && nextY >= topStandY)
        {
            if (!HasLadderAbove())
            {
                rb.MovePosition(new Vector2(activeLadder.CenterX, topStandY));
                ExitLadder(HeroLocomotionState.Grounded);
                return;
            }
        }

        // Вихід УНИЗ: тільки якщо під поточною драбиною НЕМАЄ продовження.
        if (vertical < 0f && nextY <= bottomStandY)
        {
            if (!HasLadderBelow())
            {
                rb.MovePosition(new Vector2(activeLadder.CenterX, bottomStandY));
                ExitLadder(HeroLocomotionState.Airborne);
                return;
            }
        }

        rb.MovePosition(new Vector2(nextX, nextY));
    }
}
