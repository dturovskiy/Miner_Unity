using UnityEngine;

public class StoneBehaviour : MonoBehaviour
{
    private Rigidbody2D rb;
    private GameObject hero;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        rb.isKinematic = true;
        hero = GameObject.FindGameObjectWithTag("Player");
    }

    private void Start()
    {
        if(hero.transform.position.y <= transform.position.y)
        {
            StartFalling();
        }
    }

    public void StartFalling()
    {
        rb.isKinematic = false;
    }
}
