using UnityEngine;

/// <summary>
/// Звичайний рух героя по землі.
/// УВАГА:
/// Цей скрипт більше НЕ керує драбиною.
/// Усе climb-поведінка тепер живе в HeroLadderMotor.
/// </summary>
[RequireComponent(typeof(HeroStateController))]
[RequireComponent(typeof(HeroInputReader))]
[RequireComponent(typeof(Rigidbody2D))]
public class HeroMotor : MonoBehaviour
{
    [Header("Ground Movement")]
    [SerializeField] private float walkSpeed = 3f;

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

    /// <summary>
    /// Перемикаємо фізику між ground-режимом і climb-режимом.
    /// </summary>
    private void HandleStateChanged(HeroState oldState, HeroState newState)
    {
        if (newState == HeroState.Climbing)
        {
            // На драбині вимикаємо гравітацію,
            // щоб герой не падав униз без інпуту.
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;

            UpdateAnimatorWalking(false);
            return;
        }

        // При виході з драбини повністю прибираємо інерцію,
        // щоб не було ні підскоку, ні залишкового дрейфу.
        if (oldState == HeroState.Climbing)
        {
            rb.linearVelocity = Vector2.zero;
        }

        rb.gravityScale = defaultGravityScale;
    }

    private void FixedUpdate()
    {
        HeroState currentState = stateController.CurrentState;

        // Під час climb цей скрипт не рухає героя взагалі.
        if (currentState == HeroState.Climbing)
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAnimatorWalking(false);
            return;
        }

        // Під час mining / hurt блокуємо активний рух по X,
        // але не чіпаємо звичайне падіння від гравітації.
        if (currentState == HeroState.Mining || currentState == HeroState.Hurt)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            UpdateAnimatorWalking(false);
            return;
        }

        float speedX = inputReader.Horizontal * walkSpeed;
        rb.linearVelocity = new Vector2(speedX, rb.linearVelocity.y);

        UpdateFacingAndAnimation();
    }

    private void UpdateFacingAndAnimation()
    {
        float actualSpeedX = rb.linearVelocity.x;

        if (actualSpeedX > 0.01f)
        {
            transform.localScale = new Vector3(
                -Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
        else if (actualSpeedX < -0.01f)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }

        UpdateAnimatorWalking(Mathf.Abs(actualSpeedX) > 0.01f);
    }

    private void UpdateAnimatorWalking(bool shouldWalk)
    {
        if (animator == null)
        {
            return;
        }

        if (isWalking != shouldWalk)
        {
            isWalking = shouldWalk;
            animator.SetBool("IsWalking", isWalking);
        }
    }
}
