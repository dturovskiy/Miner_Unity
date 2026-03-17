using UnityEngine;

/// <summary>
/// Міст між логікою руху і Animator.
/// Animator не має вирішувати gameplay.
/// Він тільки відображає вже готовий стан мотора.
/// </summary>
[RequireComponent(typeof(Animator))]
public sealed class HeroAnimatorBridge : MonoBehaviour
{
    [SerializeField] private HeroInputReader inputReader;
    [SerializeField] private HeroGridMotor gridMotor;
    [SerializeField] private MiningController miningController; // Використовуємо MiningController, оскільки він у нас уже є

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (inputReader == null)
        {
            inputReader = GetComponent<HeroInputReader>();
        }

        if (gridMotor == null)
        {
            gridMotor = GetComponent<HeroGridMotor>();
        }

        if (miningController == null)
        {
            miningController = GetComponent<MiningController>();
        }
    }

    private void Update()
    {
        if (animator == null || inputReader == null || gridMotor == null)
        {
            return;
        }

        bool isWalking =
            Mathf.Abs(inputReader.Horizontal) > 0.01f &&
            gridMotor.IsGrounded &&
            !gridMotor.IsInsideLadder;

        bool isClimbing =
            (Mathf.Abs(inputReader.Vertical) > 0.01f || Mathf.Abs(inputReader.Horizontal) > 0.01f) &&
            gridMotor.IsInsideLadder;

        bool isMining =
            miningController != null && miningController.IsMining;

        animator.SetBool("IsWalking", isWalking);
        animator.SetBool("IsClimbing", isClimbing);
        animator.SetBool("IsMining", isMining);
        animator.SetBool("IsFalling", gridMotor.IsFalling);

        // Фліп по X.
        if (Mathf.Abs(inputReader.Horizontal) > 0.01f)
        {
            Vector3 scale = transform.localScale;

            if (inputReader.Horizontal > 0f)
            {
                scale.x = -Mathf.Abs(scale.x);
            }
            else
            {
                scale.x = Mathf.Abs(scale.x);
            }

            transform.localScale = scale;
        }
    }
}
