using UnityEngine;

public class HeroController : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private float speed = 200f;
    private bool isWalking = false;
    private bool canMove = true;

    [SerializeField] Joystick joystick;
    private Rigidbody2D heroRigidbody;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        heroRigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (canMove)
        {
            float moveHorizontal = joystick.Horizontal;

            // Рух героя
            heroRigidbody.velocity = new Vector2(moveHorizontal * speed * Time.deltaTime, heroRigidbody.velocity.y);

            // Зміна напрямку героя
            if (moveHorizontal > 0)
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            if (moveHorizontal < 0)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }

            // Встановлення анімації ходьби
            if (moveHorizontal != 0 && !isWalking)
            {
                isWalking = true;
                animator.SetBool("IsWalking", true);
            }
            if (moveHorizontal == 0 && isWalking)
            {
                isWalking = false;
                animator.SetBool("IsWalking", false);
            }
        }
        else
        {
            // Якщо не можна рухатися, обнулити швидкість
            heroRigidbody.velocity = new Vector2(0, heroRigidbody.velocity.y);
        }
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
    }
}
