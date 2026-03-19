using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(HeroMotor))]
[RequireComponent(typeof(HeroGroundSensor))]
[RequireComponent(typeof(HeroWallSensor))]
[RequireComponent(typeof(HeroCollision))]
[RequireComponent(typeof(HeroState))]
public sealed class HeroController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HeroMotor motor;
    [SerializeField] private HeroGroundSensor groundSensor;
    [SerializeField] private HeroWallSensor wallSensor;
    [SerializeField] private HeroCollision collision;
    [SerializeField] private HeroState state;
    [SerializeField] private Joystick movementJoystick;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField] private bool useKeyboardFallback = true;
    [SerializeField, Min(0f)] private float inputDeadZone = 0.15f;
    [SerializeField] private bool flipSpriteByScale = true;

    [Header("Diagnostics")]
    [SerializeField] private bool logInputChanges = true;
    [SerializeField] private bool logBlockedMovement = true;
    [SerializeField, Min(0)] private int fixedFramesToWaitAfterWorldReady = 1;

    private float horizontalInput;
    private float previousInput;
    private bool wasGrounded;
    private bool wasBlocked;
    private bool locomotionBootstrapApplied;
    private int readyFixedFrames;

    public float HorizontalInput => horizontalInput;
    public float CurrentSpeedX => motor != null ? motor.CurrentSpeedX : 0f;
    public float CurrentSpeedY => motor != null ? motor.CurrentSpeedY : 0f;

    private void Reset()
    {
        motor = GetComponent<HeroMotor>();
        groundSensor = GetComponent<HeroGroundSensor>();
        wallSensor = GetComponent<HeroWallSensor>();
        collision = GetComponent<HeroCollision>();
        state = GetComponent<HeroState>();
    }

    private void Awake()
    {
        if (motor == null)
        {
            motor = GetComponent<HeroMotor>();
        }

        if (motor == null)
        {
            motor = gameObject.AddComponent<HeroMotor>();
        }

        if (groundSensor == null)
        {
            groundSensor = GetComponent<HeroGroundSensor>();
        }

        if (groundSensor == null)
        {
            groundSensor = gameObject.AddComponent<HeroGroundSensor>();
        }

        if (wallSensor == null)
        {
            wallSensor = GetComponent<HeroWallSensor>();
        }

        if (wallSensor == null)
        {
            wallSensor = gameObject.AddComponent<HeroWallSensor>();
        }

        if (collision == null)
        {
            collision = GetComponent<HeroCollision>();
        }

        if (state == null)
        {
            state = GetComponent<HeroState>();
        }

        if (movementJoystick == null)
        {
            movementJoystick = FindFirstObjectByType<Joystick>();
        }

        motor.ConfigureBody();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        EnsureDebugTools();
#endif
    }

    private void Update()
    {
        horizontalInput = ReadHorizontalInput();

        if (logInputChanges && Mathf.Abs(horizontalInput - previousInput) > 0.2f)
        {
            Diag.Event(
                "Hero",
                "MoveInput",
                null,
                this,
                ("x", horizontalInput),
                ("source", movementJoystick != null ? "joystick" : "keyboard"));
        }

        previousInput = horizontalInput;
        UpdateFacing();
    }

    private void FixedUpdate()
    {
        if (!IsGameplayLoopReady())
        {
            ResetBootstrapState();
            return;
        }

        if (!EnsureBootstrappedState())
        {
            return;
        }

        bool grounded = groundSensor != null && groundSensor.IsGrounded();
        if (grounded != wasGrounded)
        {
            Diag.Event(
                "Hero",
                "GroundedChanged",
                grounded ? "Hero is grounded." : "Hero left ground.",
                this,
                ("grounded", grounded),
                ("velocityY", CurrentSpeedY),
                ("footCell", collision.GetFootCell().ToString()),
                ("footType", collision.GetFootCellType().ToString()));

            if (grounded)
            {
                Diag.Event("Hero", "Landed", null, this, ("velocityY", CurrentSpeedY));
            }
            else
            {
                Diag.Event("Hero", "FallStarted", null, this, ("velocityY", CurrentSpeedY));
            }

            wasGrounded = grounded;
        }

        bool blocked = wallSensor != null && wallSensor.IsBlockedHorizontally(horizontalInput);
        if (blocked && logBlockedMovement && Mathf.Abs(horizontalInput) > inputDeadZone && !wasBlocked)
        {
            Diag.Warning(
                "Hero",
                "MoveBlocked",
                "Horizontal movement was blocked.",
                this,
                ("x", horizontalInput),
                ("cell", collision.GetCurrentCell().ToString()),
                ("footCell", collision.GetFootCell().ToString()),
                ("footType", collision.GetFootCellType().ToString()));
        }
        wasBlocked = blocked;

        float velocityX = motor != null
            ? motor.ApplyHorizontalMovement(horizontalInput, blocked, inputDeadZone, moveSpeed)
            : 0f;

        UpdateState(grounded, Mathf.Abs(velocityX) > 0.01f);
    }

    private bool IsGameplayLoopReady()
    {
        if (motor == null || groundSensor == null || wallSensor == null || collision == null || state == null)
        {
            return false;
        }

        return collision.IsWorldReady();
    }

    private bool EnsureBootstrappedState()
    {
        if (locomotionBootstrapApplied)
        {
            return true;
        }

        readyFixedFrames++;
        if (readyFixedFrames <= fixedFramesToWaitAfterWorldReady)
        {
            return false;
        }

        bool grounded = groundSensor.IsGrounded();
        bool blocked = wallSensor.IsBlockedHorizontally(horizontalInput);
        wasGrounded = grounded;
        wasBlocked = blocked;
        state.SetLocomotionSilently(ResolveLocomotionState(grounded, Mathf.Abs(horizontalInput) > inputDeadZone && !blocked));
        locomotionBootstrapApplied = true;
        return true;
    }

    private void ResetBootstrapState()
    {
        locomotionBootstrapApplied = false;
        readyFixedFrames = 0;
        wasGrounded = false;
        wasBlocked = false;
    }

    private float ReadHorizontalInput()
    {
        float x = 0f;

        if (movementJoystick != null)
        {
            x = movementJoystick.Horizontal;
        }
        else if (useKeyboardFallback)
        {
            x = Input.GetAxisRaw("Horizontal");
        }

        if (Mathf.Abs(x) < inputDeadZone)
        {
            return 0f;
        }

        return Mathf.Clamp(x, -1f, 1f);
    }

    private void UpdateFacing()
    {
        motor?.UpdateFacing(horizontalInput, inputDeadZone, flipSpriteByScale);
    }

    private void UpdateState(bool grounded, bool moving)
    {
        state.SetLocomotion(ResolveLocomotionState(grounded, moving));
    }

    private static HeroLocomotionState ResolveLocomotionState(bool grounded, bool moving)
    {
        if (!grounded)
        {
            return HeroLocomotionState.Falling;
        }

        if (moving)
        {
            return HeroLocomotionState.Moving;
        }

        return HeroLocomotionState.Idle;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void EnsureDebugTools()
    {
        if (GetComponent<HeroDebugDigTool>() == null)
        {
            gameObject.AddComponent<HeroDebugDigTool>();
        }
    }
#endif
}
