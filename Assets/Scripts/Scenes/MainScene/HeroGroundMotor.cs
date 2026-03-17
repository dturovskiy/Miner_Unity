using UnityEngine;

/// <summary>
/// Мотор землі/повітря.
/// Він НЕ відповідає за драбину.
/// Якщо герой на драбині, цей мотор просто не втручається.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(HeroInputReader))]
[RequireComponent(typeof(HeroStateHub))]
[RequireComponent(typeof(HeroSensors))]
public sealed class HeroGroundMotor : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 3f;

    private Rigidbody2D rb;
    private HeroInputReader input;
    private HeroStateHub stateHub;
    private HeroSensors sensors;
    private Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<HeroInputReader>();
        stateHub = GetComponent<HeroStateHub>();
        sensors = GetComponent<HeroSensors>();
        animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        // Якщо герой на драбині — наземний мотор не працює.
        if (stateHub.IsOnLadder())
        {
            SetWalking(false);
            return;
        }

        // Якщо герой сильно заблокований дією — теж не рухаємо.
        if (stateHub.ActionState == HeroActionState.Hurt)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            SetWalking(false);
            return;
        }

        float targetX = input.Horizontal * walkSpeed;
        rb.linearVelocity = new Vector2(targetX, rb.linearVelocity.y);

        // Оновлюємо локомоційний стан.
        if (sensors.IsGrounded())
        {
            stateHub.SetLocomotion(HeroLocomotionState.Grounded);
        }
        else
        {
            stateHub.SetLocomotion(HeroLocomotionState.Airborne);
        }

        UpdateFacing(targetX);
        SetWalking(Mathf.Abs(targetX) > 0.01f && sensors.IsGrounded());
    }

    private void UpdateFacing(float speedX)
    {
        if (speedX > 0.01f)
        {
            transform.localScale = new Vector3(
                -Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
        else if (speedX < -0.01f)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
    }

    private void SetWalking(bool value)
    {
        if (animator != null)
        {
            animator.SetBool("IsWalking", value);
        }
    }
}
