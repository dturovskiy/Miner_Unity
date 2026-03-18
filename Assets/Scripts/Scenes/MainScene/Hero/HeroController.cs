using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(HeroCollision))]
[RequireComponent(typeof(HeroState))]
public sealed class HeroController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
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

    private float horizontalInput;
    private float previousInput;
    private bool wasGrounded;
    private bool wasBlocked;

    public float HorizontalInput => horizontalInput;
    public float CurrentSpeedX => rb != null ? rb.linearVelocity.x : 0f;
    public float CurrentSpeedY => rb != null ? rb.linearVelocity.y : 0f;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        collision = GetComponent<HeroCollision>();
        state = GetComponent<HeroState>();
    }

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (collision == null)
        {
            collision = GetComponent<HeroCollision>();
        }

        if (state == null)
        {
            state = GetComponent<HeroState>();
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = Mathf.Max(0.01f, rb.gravityScale);
        rb.freezeRotation = true;
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
        bool grounded = collision.IsGrounded();
        if (grounded != wasGrounded)
        {
            Diag.Event(
                "Hero",
                "GroundedChanged",
                grounded ? "Hero is grounded." : "Hero left ground.",
                this,
                ("grounded", grounded),
                ("velocityY", rb.linearVelocity.y),
                ("footCell", collision.GetFootCell().ToString()),
                ("footType", collision.GetFootCellType().ToString()));

            if (grounded)
            {
                Diag.Event("Hero", "Landed", null, this, ("velocityY", rb.linearVelocity.y));
            }
            else
            {
                Diag.Event("Hero", "FallStarted", null, this, ("velocityY", rb.linearVelocity.y));
            }

            wasGrounded = grounded;
        }

        bool blocked = collision.IsBlockedHorizontally(horizontalInput);
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

        Vector2 velocity = rb.linearVelocity;

        if (Mathf.Abs(horizontalInput) <= inputDeadZone)
        {
            velocity.x = 0f;
        }
        else if (blocked)
        {
            velocity.x = 0f;
        }
        else
        {
            velocity.x = horizontalInput * moveSpeed;
        }

        rb.linearVelocity = velocity;
        UpdateState(grounded, Mathf.Abs(velocity.x) > 0.01f);
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
        if (!flipSpriteByScale || Mathf.Abs(horizontalInput) <= inputDeadZone)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (horizontalInput >= 0f ? 1f : -1f);
        transform.localScale = scale;
    }

    private void UpdateState(bool grounded, bool moving)
    {
        if (!grounded)
        {
            state.SetLocomotion(HeroLocomotionState.Falling);
            return;
        }

        if (moving)
        {
            state.SetLocomotion(HeroLocomotionState.Moving);
            return;
        }

        state.SetLocomotion(HeroLocomotionState.Idle);
    }
}
