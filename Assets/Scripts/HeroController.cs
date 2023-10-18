using UnityEngine;

public class HeroController : MonoBehaviour
{
    private Animator animator;
    public float speed = 5f;
    private bool isWalking = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");

        // Рух героя
        Vector3 movement = new Vector3(moveHorizontal, 0, 0) * speed * Time.deltaTime;
        transform.Translate(movement);

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
