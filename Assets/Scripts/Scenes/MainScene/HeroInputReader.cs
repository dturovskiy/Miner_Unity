using UnityEngine;

public class HeroInputReader : MonoBehaviour
{
    // If not using the inspector to assign, it will attempt to find one in the scene.
    [SerializeField] private Joystick joystick;
    
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
        if (joystick != null)
        {
            Direction = new Vector2(joystick.Horizontal, joystick.Vertical);
        }
        else
        {
            Direction = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }
    }
}
