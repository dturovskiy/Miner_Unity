using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(HeroGroundCore))]
public sealed class HeroController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HeroGroundCore groundCore;
    [SerializeField] private HeroState heroState;
    [SerializeField] private Animator animator;
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
    private Vector2 movementInput;
    private Vector2 previousMovementInput;

    public float HorizontalInput => horizontalInput;
    public Vector2 MovementInput => movementInput;
    public bool UsesMovementJoystick => movementJoystick != null;
    public float CurrentSpeedX => groundCore != null ? groundCore.CurrentSpeedX : 0f;
    public float CurrentSpeedY => groundCore != null ? groundCore.CurrentSpeedY : 0f;

    private void Reset()
    {
        groundCore = GetComponent<HeroGroundCore>();
        heroState = GetComponent<HeroState>();
        animator = GetComponent<Animator>();
    }

    private void OnValidate()
    {
        ResolveReferences();
        ApplyGroundCoreConfiguration();
    }

    private void Awake()
    {
        ResolveReferences();
        ApplyGroundCoreConfiguration();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        EnsureDebugTools();
#endif
    }

    private void Update()
    {
        ResolveMovementJoystickIfNeeded();
        movementInput = ReadMovementInput();
        horizontalInput = movementInput.x;

        if (logInputChanges && Vector2.Distance(movementInput, previousMovementInput) > 0.2f)
        {
            Diag.Event(
                "Hero",
                "MoveInput",
                null,
                this,
                ("x", movementInput.x),
                ("y", movementInput.y),
                ("source", movementJoystick != null ? "joystick" : "keyboard"));
        }

        previousMovementInput = movementInput;
        groundCore?.SetDesiredHorizontalInput(horizontalInput);
        SyncAnimator();
    }

    private Vector2 ReadMovementInput()
    {
        Vector2 input = Vector2.zero;

        if (movementJoystick != null)
        {
            input = movementJoystick.Direction;
        }
        else if (useKeyboardFallback)
        {
            input = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));
        }

        if (input.sqrMagnitude < inputDeadZone * inputDeadZone)
        {
            return Vector2.zero;
        }

        return Vector2.ClampMagnitude(input, 1f);
    }

    private void ResolveReferences()
    {
        if (groundCore == null)
        {
            groundCore = GetComponent<HeroGroundCore>();
        }

        if (heroState == null)
        {
            heroState = GetComponent<HeroState>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        groundCore ??= gameObject.AddComponent<HeroGroundCore>();
        ResolveMovementJoystickIfNeeded();
    }

    private void ResolveMovementJoystickIfNeeded()
    {
        if (movementJoystick == null)
        {
            movementJoystick = FindFirstObjectByType<Joystick>();
        }
    }

    private void ApplyGroundCoreConfiguration()
    {
        if (groundCore == null)
        {
            return;
        }

        groundCore.ConfigureMovement(moveSpeed, inputDeadZone, flipSpriteByScale);
        groundCore.ConfigureDiagnostics(logBlockedMovement, fixedFramesToWaitAfterWorldReady);
    }

    private void SyncAnimator()
    {
        if (animator == null || heroState == null)
        {
            return;
        }

        animator.SetBool("IsWalking", heroState.IsMoving);
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
