using System;
using UnityEngine;

/// <summary>
/// Контролер копання.
/// 
/// Важлива зміна:
/// - якщо герой копає, стоячи на драбині, ми НЕ переводимо його в HeroState.Mining;
/// - отже HeroMotor не блокує йому боковий рух і не "викидає" його з логіки драбини.
/// </summary>
public class MiningController : MonoBehaviour
{
    private Animator animator;
    private HeroStateController stateController;
    private TileBehaviour tileBehaviour;

    [SerializeField] private float maxMiningDistance = 0.5f;
    [SerializeField] private Joystick miningJoystick;
    [SerializeField] private float miningDelay = 0.4f;

    private float startTime;
    private bool isMiningStarted = false;

    // Чи саме цей контролер "володіє" станом Mining.
    // Якщо копаємо на землі — true.
    // Якщо копаємо на драбині — false, стан Climbing не чіпаємо.
    private bool miningOwnsState = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        stateController = GetComponent<HeroStateController>();
    }

    private void Update()
    {
        if (miningJoystick == null)
        {
            StopMiningAnimation();
            return;
        }

        float horizontalInput = miningJoystick.Horizontal;
        float verticalInput = miningJoystick.Vertical;

        int roundedHorizontalInput = Mathf.RoundToInt(horizontalInput);
        int roundedVerticalInput = Mathf.RoundToInt(verticalInput);

        // Починаємо копання тільки якщо джойстик достатньо відхилений.
        if (Mathf.Abs(horizontalInput) >= 0.5f || Mathf.Abs(verticalInput) >= 0.5f)
        {
            // Ключова частина:
            // ми НЕ дозволяємо одночасно X і Y для mining-напрямку.
            // Беремо лише домінуючу вісь.
            Vector2 miningDirection;
            if (Mathf.Abs(verticalInput) > Mathf.Abs(horizontalInput))
            {
                miningDirection = new Vector2(0, verticalInput > 0f ? 1 : -1);
            }
            else
            {
                miningDirection = new Vector2(horizontalInput > 0f ? 1 : -1, 0);
            }

            // Вгору копаємо трохи далі, ніж убік/вниз.
            if (miningDirection.y > 0)
            {
                maxMiningDistance = 0.7f;
            }
            else
            {
                maxMiningDistance = 0.4f;
            }

            Vector2 miningPosition = (Vector2)transform.position + miningDirection * maxMiningDistance;
            CheckTile(miningPosition);
        }
        else
        {
            StopMiningAnimation();
        }
    }

    private void StartMiningAnimation()
    {
        if (!isMiningStarted)
        {
            isMiningStarted = true;
            startTime = Time.time;
        }

        animator.SetBool("IsMining", true);

        if (stateController == null)
        {
            miningOwnsState = false;
            return;
        }

        // Якщо герой уже на драбині — НЕ переводимо його в Mining.
        // Інакше він втратить climb-поведінку.
        if (stateController.CurrentState == HeroState.Climbing)
        {
            miningOwnsState = false;
            return;
        }

        // Якщо герой не в climb і не hurt — дозволяємо режим Mining.
        if (stateController.CurrentState != HeroState.Hurt)
        {
            if (stateController.CurrentState != HeroState.Mining)
            {
                stateController.ChangeState(HeroState.Mining);
            }

            miningOwnsState = true;
        }
        else
        {
            miningOwnsState = false;
        }
    }

    private void StopMiningAnimation()
    {
        if (isMiningStarted)
        {
            isMiningStarted = false;
            startTime = 0f;
        }

        animator.SetBool("IsMining", false);

        if (stateController != null && miningOwnsState && stateController.CurrentState == HeroState.Mining)
        {
            stateController.ChangeState(HeroState.Normal);
        }

        miningOwnsState = false;
    }

    private void CheckTile(Vector2 targetPosition)
    {
        Collider2D hitCollider = Physics2D.OverlapPoint(targetPosition);

        if (hitCollider == null)
        {
            StopMiningAnimation();
            return;
        }

        GameObject tile = hitCollider.gameObject;
        tileBehaviour = tile.GetComponent<TileBehaviour>();

        // Ці теги не копаємо.
        if (tile.CompareTag("Edge") || tile.CompareTag("Stone") || tile.CompareTag("Cave"))
        {
            StopMiningAnimation();
            return;
        }

        StartMiningAnimation();

        if (Time.time - startTime >= miningDelay)
        {
            if (tileBehaviour != null && !tileBehaviour.IsBroken)
            {
                tileBehaviour.HitTile(tileBehaviour);
            }

            if (tileBehaviour != null && tileBehaviour.IsBroken)
            {
                StopMiningAnimation();
            }
        }
    }
}
