using UnityEngine;

[RequireComponent(typeof(HeroStateController), typeof(HeroInputReader), typeof(Rigidbody2D))]
public class HeroMotor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float climbSpeed = 3f;

    private Rigidbody2D rb;
    private HeroStateController stateController;
    private HeroInputReader inputReader;
    private Animator animator;

    private float defaultGravityScale;
    private bool isWalking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stateController = GetComponent<HeroStateController>();
        inputReader = GetComponent<HeroInputReader>();
        animator = GetComponent<Animator>();

        defaultGravityScale = rb.gravityScale;
        stateController.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (stateController != null)
        {
            stateController.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(HeroState oldState, HeroState newState)
    {
        if (newState == HeroState.Climbing)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }
    }

    private void FixedUpdate()
    {
        HeroState currentState = stateController.CurrentState;

        // If mining or hurt, hard lock horizontal movement but allow falling (unless climbing)
        if (currentState == HeroState.Mining || currentState == HeroState.Hurt)
        {
            rb.linearVelocity = new Vector2(0f, currentState == HeroState.Climbing ? 0f : rb.linearVelocity.y);
            UpdateAnimatorWalking(false);
            return;
        }

        float speedX = inputReader.Horizontal * walkSpeed;

        if (currentState == HeroState.Climbing)
        {
            float speedY = inputReader.Vertical * climbSpeed;
            rb.linearVelocity = new Vector2(speedX, speedY);
        }
        else
        {
            // Normal Walking
            rb.linearVelocity = new Vector2(speedX, rb.linearVelocity.y);
        }

        UpdateFacingAndAnimation();
    }

    private void UpdateFacingAndAnimation()
    {
        float actualSpeedX = rb.linearVelocity.x;

        if (actualSpeedX > 0.01f)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (actualSpeedX < -0.01f)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        UpdateAnimatorWalking(Mathf.Abs(actualSpeedX) > 0.01f);
    }

    private void UpdateAnimatorWalking(bool shouldWalk)
    {
        if (animator == null) return;
        
        if (isWalking != shouldWalk)
        {
            isWalking = shouldWalk;
            animator.SetBool("IsWalking", isWalking);
        }
    }
}
