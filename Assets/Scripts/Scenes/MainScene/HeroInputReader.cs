using UnityEngine;

public class HeroInputReader : MonoBehaviour
{
    // If not using the inspector to assign, it will attempt to find one in the scene.
    [SerializeField] private Joystick joystick;
    [SerializeField] private float deadZone = 0.2f;
    
    public Vector2 Direction { get; private set; }
    
    public float Horizontal => Direction.x;
    public float Vertical => Direction.y;

    private void Start()
    {
        if (joystick == null)
        {
            joystick = FindFirstObjectByType<Joystick>();
        }
    }

    private void Update()
    {
        Vector2 rawInput;

        if (joystick != null)
        {
            rawInput = new Vector2(joystick.Horizontal, joystick.Vertical);
        }
        else
        {
            rawInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        if (Mathf.Abs(rawInput.x) < deadZone) rawInput.x = 0f;
        if (Mathf.Abs(rawInput.y) < deadZone) rawInput.y = 0f;

        Direction = rawInput;
    }
}
