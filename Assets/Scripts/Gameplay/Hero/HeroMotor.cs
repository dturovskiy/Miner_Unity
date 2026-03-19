using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class HeroMotor : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;

    public Rigidbody2D Rigidbody => rb;
    public float CurrentSpeedX => rb != null ? rb.linearVelocity.x : 0f;
    public float CurrentSpeedY => rb != null ? rb.linearVelocity.y : 0f;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        rb ??= GetComponent<Rigidbody2D>();
        ConfigureBody();
    }

    public void ConfigureBody()
    {
        if (rb == null)
        {
            return;
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = Mathf.Max(0.01f, rb.gravityScale);
        rb.freezeRotation = true;
    }

    public float ApplyHorizontalMovement(float horizontalInput, bool blocked, float inputDeadZone, float moveSpeed)
    {
        if (rb == null)
        {
            return 0f;
        }

        Vector2 velocity = rb.linearVelocity;

        if (Mathf.Abs(horizontalInput) <= inputDeadZone || blocked)
        {
            velocity.x = 0f;
        }
        else
        {
            velocity.x = horizontalInput * moveSpeed;
        }

        rb.linearVelocity = velocity;
        return velocity.x;
    }

    public void UpdateFacing(float horizontalInput, float inputDeadZone, bool flipSpriteByScale)
    {
        if (!flipSpriteByScale || Mathf.Abs(horizontalInput) <= inputDeadZone)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (horizontalInput >= 0f ? -1f : 1f);
        transform.localScale = scale;
    }
}
