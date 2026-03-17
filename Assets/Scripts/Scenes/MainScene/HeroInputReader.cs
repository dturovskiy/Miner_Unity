using UnityEngine;

/// <summary>
/// Читає лише інпут руху.
/// Ніякої логіки станів або фізики тут бути не повинно.
/// </summary>
public sealed class HeroInputReader : MonoBehaviour
{
    [Header("Movement Input")]
    [SerializeField] private Joystick movementJoystick;

    /// <summary>
    /// Сирий напрямок від -1 до 1.
    /// </summary>
    public Vector2 Move { get; private set; }

    public float Horizontal => Move.x;
    public float Vertical => Move.y;

    private void Update()
    {
        if (movementJoystick != null)
        {
            Move = new Vector2(movementJoystick.Horizontal, movementJoystick.Vertical);
        }
        else
        {
            // Фолбек для редактора / клавіатури.
            Move = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );
        }

        // Щоб діагональ не ставала довшою за 1.
        if (Move.sqrMagnitude > 1f)
        {
            Move = Move.normalized;
        }
    }
}
