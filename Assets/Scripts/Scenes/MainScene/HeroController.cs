using UnityEngine;

public class HeroController : MonoBehaviour
{
    public Animator animator;
    public float speed = 5f;
    private bool isWalking = false;
    [SerializeField] Joystick joystick;

    Rigidbody2D heroRigidbody;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        heroRigidbody = GetComponent<Rigidbody2D>();
    }    

    private void Update()
    {
        float moveHorizontal = joystick.Horizontal;

        // Рух героя
        heroRigidbody.velocity = new Vector2(moveHorizontal * speed, heroRigidbody.velocity.y);

        // Зміна напрямку героя
        if (moveHorizontal > 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (moveHorizontal < 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        // Встановлення анімації ходьби
        if (moveHorizontal != 0 && !isWalking)
        {
            isWalking = true;
            animator.SetBool("IsWalking", true);
        }
        else if (moveHorizontal == 0 && isWalking)
        {
            isWalking = false;
            animator.SetBool("IsWalking", false);
        }
    }
}
