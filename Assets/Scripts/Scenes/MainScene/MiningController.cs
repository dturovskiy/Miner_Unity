using UnityEngine;

public class MiningController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Joystick miningJoystick;
    [SerializeField] private float inputDeadZone = 0.5f;

    [Header("Distances")]
    [SerializeField] private float horizontalMiningDistance = 0.4f;
    [SerializeField] private float upwardMiningDistance = 0.7f;
    [SerializeField] private float downwardMiningDistance = 0.4f;

    [Header("Timing")]
    [SerializeField] private float miningDelay = 0.4f;

    private Animator animator;
    private HeroStateController stateController;
    private float startTime;
    private bool isMiningStarted;

    private void Awake()
    {
        // Беремо посилання один раз.
        animator = GetComponent<Animator>();
        stateController = GetComponent<HeroStateController>();
    }

    private void Update()
    {
        // Захист від null, щоб не ловити помилки в рантаймі.
        if (miningJoystick == null)
        {
            StopMiningAnimation();
            return;
        }

        // Сирий вектор вводу з джойстика.
        Vector2 rawInput = new Vector2(miningJoystick.Horizontal, miningJoystick.Vertical);

        // Якщо джойстик майже в центрі — копання нема.
        if (rawInput.magnitude < inputDeadZone)
        {
            StopMiningAnimation();
            return;
        }

        // Ключова частина:
        // ми НЕ дозволяємо одночасно X і Y для mining-напрямку.
        // Беремо лише домінуючу вісь.
        Vector2Int snappedDirection = GetSnappedDirection(rawInput);

        // Для різних напрямків можна тримати різну дистанцію.
        float miningDistance = GetMiningDistance(snappedDirection);

        // Цільова точка для OverlapPoint.
        Vector2 miningPosition = (Vector2)transform.position + (Vector2)snappedDirection * miningDistance;

        CheckTile(miningPosition);
    }

    private Vector2Int GetSnappedDirection(Vector2 rawInput)
    {
        // Якщо вертикаль домінує — копаємо тільки вгору або вниз.
        if (Mathf.Abs(rawInput.y) > Mathf.Abs(rawInput.x))
        {
            return new Vector2Int(0, rawInput.y > 0f ? 1 : -1);
        }

        // Інакше копаємо тільки вліво або вправо.
        return new Vector2Int(rawInput.x > 0f ? 1 : -1, 0);
    }

    private float GetMiningDistance(Vector2Int direction)
    {
        // Окрема дистанція для копання вгору.
        if (direction.y > 0)
            return upwardMiningDistance;

        // Окрема дистанція для копання вниз.
        if (direction.y < 0)
            return downwardMiningDistance;

        // Горизонтальне копання.
        return horizontalMiningDistance;
    }

    private void StartMiningAnimation()
    {
        if (!isMiningStarted)
        {
            isMiningStarted = true;
            startTime = Time.time;
        }

        // MiningController має керувати ТІЛЬКИ анімацією копання.
        animator.SetBool("IsMining", true);

        // Не чіпаємо IsWalking тут.
        // Ходьбою нехай керує лише HeroMotor.
        if (stateController != null)
        {
            stateController.ChangeState(HeroState.Mining);
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

        if (stateController != null && stateController.CurrentState == HeroState.Mining)
        {
            stateController.ChangeState(HeroState.Normal);
        }
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
        TileBehaviour tileBehaviour = tile.GetComponent<TileBehaviour>();

        // Ці теги, як і раніше, не копаємо.
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
