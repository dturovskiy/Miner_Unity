using UnityEngine;

public class HeroController : MonoBehaviour
{
    private Animator animator;
    public float speed = 5f;
    private bool isWalking = false;
    public bool inCave;
    
    Rigidbody2D heroRigidbody;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        heroRigidbody = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.CompareTag("Cave"))
            inCave = false;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.CompareTag("Cave"))
            inCave = true;
    }

    private void Update()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");

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
