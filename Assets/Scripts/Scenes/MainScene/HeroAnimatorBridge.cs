using UnityEngine;

/// <summary>
/// Повертає анімації та поворот героя.
/// </summary>
[RequireComponent(typeof(Animator))]
public sealed class HeroAnimatorBridge : MonoBehaviour
{
    [SerializeField] private HeroInputReader input;
    [SerializeField] private HeroGridMotor motor;
    
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (input == null) input = GetComponent<HeroInputReader>();
        if (motor == null) motor = GetComponent<HeroGridMotor>();
    }

    private void Update()
    {
        if (input == null || motor == null || animator == null) return;

        float h = input.Horizontal;
        float v = input.Vertical;

        // 1. Анімації
        bool isWalking = Mathf.Abs(h) > 0.01f && motor.IsGrounded && !motor.IsInsideLadder;
        bool isClimbing = motor.IsInsideLadder && (Mathf.Abs(v) > 0.1f || Mathf.Abs(h) > 0.1f);
        
        animator.SetBool("IsWalking", isWalking);
        animator.SetBool("IsClimbing", isClimbing);
        animator.SetBool("IsFalling", motor.IsFalling);

        // 2. Поворот (Flip)
        if (Mathf.Abs(h) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            if (h > 0) scale.x = -Mathf.Abs(scale.x);
            else scale.x = Mathf.Abs(scale.x);
            
            transform.localScale = scale;
        }
    }
}
