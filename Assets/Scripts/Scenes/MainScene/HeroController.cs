using UnityEngine;

public class HeroController : MonoBehaviour
{
    private Animator animator;

    [SerializeField] private float speed = 200f;
    [SerializeField] private Joystick joystick;

    private Rigidbody2D heroRigidbody;
    private float defaultGravityScale;

    private bool isWalking;
    private bool canMove = true;
    private bool isOnLadder;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        heroRigidbody = GetComponent<Rigidbody2D>();
        defaultGravityScale = heroRigidbody.gravityScale;
    }

    private void Update()
    {
        if (!canMove)
        {
            heroRigidbody.linearVelocity = new Vector2(0f, isOnLadder ? 0f : heroRigidbody.linearVelocity.y);
            return;
        }

        float moveHorizontal = joystick.Horizontal;
        float moveVertical = joystick.Vertical;

        if (isOnLadder)
        {
            heroRigidbody.linearVelocity = new Vector2(moveHorizontal * speed * Time.deltaTime, moveVertical * speed * Time.deltaTime);
        }
        else
        {
            heroRigidbody.linearVelocity = new Vector2(moveHorizontal * speed * Time.deltaTime, heroRigidbody.linearVelocity.y);
        }

        if (moveHorizontal > 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        if (moveHorizontal < 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

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

    public void SetCanMove(bool value)
    {
        canMove = value;
    }

    public void SetIsOnLadder(bool value)
    {
        isOnLadder = value;
        heroRigidbody.gravityScale = value ? 0f : defaultGravityScale;
    }
}
