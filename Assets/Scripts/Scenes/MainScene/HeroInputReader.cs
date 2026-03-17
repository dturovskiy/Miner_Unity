using UnityEngine;

public sealed class HeroInputReader : MonoBehaviour
{
    [SerializeField] private Joystick movementJoystick;

    public float Horizontal => movementJoystick != null ? movementJoystick.Horizontal : Input.GetAxisRaw("Horizontal");
    public float Vertical => movementJoystick != null ? movementJoystick.Vertical : Input.GetAxisRaw("Vertical");
}
