using UnityEngine;

/// <summary>
/// Читає тільки інпут руху.
/// Не керує станами, фізикою або анімаціями.
/// </summary>
public sealed class HeroInputReader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Joystick movementJoystick;

    [Header("Input Settings")]
    [SerializeField] private float deadZone = 0.15f;

    /// <summary>
    /// Підсумковий вектор руху героя.
    /// </summary>
    public Vector2 Move { get; private set; }

    public float Horizontal => Move.x;
    public float Vertical => Move.y;

    private void Awake()
    {
        // Даємо явний сигнал у консоль, якщо джойстик не підключений.
        if (movementJoystick == null)
        {
            Debug.LogWarning(
                "[HeroInputReader] Movement Joystick is not assigned. " +
                "Touch input will not work until you assign it in Inspector. " +
                "Fallback to Keyboard (WASD/Arrows) enabled for Editor/Desktop.",
                this
            );
        }
    }

    private void Update()
    {
        Vector2 rawInput = Vector2.zero;

        // Основний шлях: мобільний / UI-джойстик.
        if (movementJoystick != null)
        {
            rawInput = new Vector2(
                movementJoystick.Horizontal,
                movementJoystick.Vertical
            );
        }
        else
        {
            // Зручний fallback для тесту в редакторі.
            rawInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );
        }

        // Dead zone прибирає дрібний шум стика.
        if (Mathf.Abs(rawInput.x) < deadZone)
        {
            rawInput.x = 0f;
        }

        if (Mathf.Abs(rawInput.y) < deadZone)
        {
            rawInput.y = 0f;
        }

        // Нормалізація, щоб діагональ не була сильнішою за прямий рух.
        if (rawInput.sqrMagnitude > 1f)
        {
            rawInput = rawInput.normalized;
        }

        Move = rawInput;
    }
}
