using UnityEngine;

/// <summary>
/// Контролер копання під нову архітектуру.
///
/// Головна ідея:
/// - цей компонент більше НЕ керує locomotion-станом героя;
/// - він працює тільки з action-станом через HeroStateHub;
/// - ходьба / падіння / драбина живуть у своїх моторах окремо.
///
/// Unity 2020.3.28f1+ / Unity 6:
/// - використовується звичайний Physics2D.OverlapPoint;
/// - ніякої залежності від старого HeroStateController більше немає.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(HeroStateHub))]
public sealed class MiningController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Joystick miningJoystick;

    [Header("Mining Distances")]
    [SerializeField] private float sideAndDownMiningDistance = 0.4f;
    [SerializeField] private float upMiningDistance = 0.7f;

    [Header("Mining Timing")]
    [SerializeField] private float miningDelay = 0.4f;
    [SerializeField] private float inputThreshold = 0.5f;

    [Header("Rules")]
    [SerializeField] private bool allowMiningOnLadder = false;

    private Animator animator;
    private HeroStateHub stateHub;

    private TileBehaviour currentTile;
    private float startTime;
    private bool isMiningStarted;

    /// <summary>
    /// Чи саме цей контролер зараз володіє ActionState = Mining.
    /// Це важливо, щоб не скинути чужий стан випадково.
    /// </summary>
    private bool miningOwnsAction;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        stateHub = GetComponent<HeroStateHub>();
    }

    private void Update()
    {
        // Якщо не призначений джойстик копання — просто гарантуємо,
        // що mining-стан і анімація вимкнені.
        if (miningJoystick == null)
        {
            StopMining();
            return;
        }

        // Якщо герой у сильному блокуючому стані — не копаємо.
        if (stateHub.ActionState == HeroActionState.Hurt)
        {
            StopMining();
            return;
        }

        // Якщо за правилами дизайну на драбині копати не можна — блокуємо це тут.
        // Це безпечніше, ніж намагатися міксувати climb + mining в одному animator layer.
        if (!allowMiningOnLadder && stateHub.IsOnLadder())
        {
            StopMining();
            return;
        }

        float horizontalInput = miningJoystick.Horizontal;
        float verticalInput = miningJoystick.Vertical;

        // Починаємо mining тільки якщо джойстик реально відхилений,
        // а не дрейфує біля нуля.
        bool hasMiningInput =
            Mathf.Abs(horizontalInput) >= inputThreshold ||
            Mathf.Abs(verticalInput) >= inputThreshold;

        if (!hasMiningInput)
        {
            StopMining();
            return;
        }

        // Беремо тільки домінуючу вісь:
        // або вліво/вправо, або вгору/вниз.
        // Це прибирає неоднозначність при діагональному відхиленні стика.
        Vector2 miningDirection = GetDominantAxisDirection(horizontalInput, verticalInput);

        // Вгору копаємо трохи далі, як і в старій логіці.
        float miningDistance = miningDirection.y > 0f
            ? upMiningDistance
            : sideAndDownMiningDistance;

        Vector2 miningPosition = (Vector2)transform.position + miningDirection * miningDistance;

        CheckTile(miningPosition);
    }

    /// <summary>
    /// Повертає напрямок копання лише по одній осі.
    /// Наприклад:
    /// (0.8, 0.7) -> (1, 0)
    /// (0.2, -0.9) -> (0, -1)
    /// </summary>
    private Vector2 GetDominantAxisDirection(float horizontalInput, float verticalInput)
    {
        if (Mathf.Abs(verticalInput) > Mathf.Abs(horizontalInput))
        {
            return new Vector2(0f, verticalInput > 0f ? 1f : -1f);
        }

        return new Vector2(horizontalInput > 0f ? 1f : -1f, 0f);
    }

    /// <summary>
    /// Запускає mining-анімацію і, якщо потрібно,
    /// переводить ActionState у Mining.
    /// </summary>
    private void StartMining()
    {
        if (!isMiningStarted)
        {
            isMiningStarted = true;
            startTime = Time.time;
        }

        animator.SetBool("IsMining", true);

        // Якщо вже хтось поставив Mining — просто не конфліктуємо.
        if (stateHub.ActionState == HeroActionState.Mining)
        {
            miningOwnsAction = true;
            return;
        }

        // Hurt не перетираємо.
        if (stateHub.ActionState == HeroActionState.Hurt)
        {
            miningOwnsAction = false;
            return;
        }

        stateHub.SetAction(HeroActionState.Mining);
        miningOwnsAction = true;
    }

    /// <summary>
    /// Повністю зупиняє копання:
    /// - скидає таймер,
    /// - вимикає анімацію,
    /// - повертає ActionState у None, якщо саме цей компонент його встановив.
    /// </summary>
    private void StopMining()
    {
        if (isMiningStarted)
        {
            isMiningStarted = false;
            startTime = 0f;
        }

        animator.SetBool("IsMining", false);

        if (miningOwnsAction && stateHub.ActionState == HeroActionState.Mining)
        {
            stateHub.SetAction(HeroActionState.None);
        }

        miningOwnsAction = false;
        currentTile = null;
    }

    /// <summary>
    /// Перевіряє тайл у точці miningPosition.
    /// Якщо там немає валідного блоку для копання — mining скидається.
    /// </summary>
    private void CheckTile(Vector2 targetPosition)
    {
        Collider2D hitCollider = Physics2D.OverlapPoint(targetPosition);

        if (hitCollider == null)
        {
            StopMining();
            return;
        }

        GameObject tile = hitCollider.gameObject;
        currentTile = tile.GetComponent<TileBehaviour>();

        // Ці типи не копаємо.
        if (tile.CompareTag("Edge") || tile.CompareTag("Stone") || tile.CompareTag("Cave"))
        {
            StopMining();
            return;
        }

        // Якщо на об'єкті немає TileBehaviour — це теж невалідна ціль.
        if (currentTile == null)
        {
            StopMining();
            return;
        }

        StartMining();

        // Чекаємо затримку перед ударом,
        // щоб зберегти старий ритм gameplay.
        if (Time.time - startTime < miningDelay)
        {
            return;
        }

        if (!currentTile.IsBroken)
        {
            // Залишаю твій поточний виклик, якщо саме так очікує TileBehaviour.
            currentTile.HitTile(currentTile);
        }

        if (currentTile.IsBroken)
        {
            StopMining();
        }
    }
}
