using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(HeroGroundCore))]
public sealed class HeroController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HeroGroundCore groundCore;
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

    public float HorizontalInput => horizontalInput;
    public float CurrentSpeedX => groundCore != null ? groundCore.CurrentSpeedX : 0f;
    public float CurrentSpeedY => groundCore != null ? groundCore.CurrentSpeedY : 0f;

    private void Reset()
    {
        groundCore = GetComponent<HeroGroundCore>();
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
        groundCore?.SetDesiredHorizontalInput(horizontalInput);
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

    private void ResolveReferences()
    {
        if (groundCore == null)
        {
            groundCore = GetComponent<HeroGroundCore>();
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
