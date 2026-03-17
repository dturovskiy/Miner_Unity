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
        rb.gravityScale = defaultGravityScale;
        rb.linearVelocity = Vector2.zero;
        stateHub.SetLocomotion(nextState);
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

        float topStandY = activeLadder.GetTopStandY(heroCollider);
        float bottomStandY = activeLadder.GetBottomStandY(heroCollider);

        // Вихід зверху.
        if (vertical > 0f && nextY >= topStandY)
        {
            rb.MovePosition(new Vector2(activeLadder.CenterX, topStandY));
            ExitLadder(HeroLocomotionState.Grounded);
            return;
        }

        // Вихід знизу.
        if (vertical < 0f && nextY <= bottomStandY)
        {
            rb.MovePosition(new Vector2(activeLadder.CenterX, bottomStandY));
            ExitLadder(HeroLocomotionState.Airborne);
            return;
        }

        rb.MovePosition(new Vector2(nextX, nextY));
    }
}
