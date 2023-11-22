using UnityEngine;

public class MiningController : MonoBehaviour
{
    // Аніматор для управління анімацією
    private Animator animator;
    private HeroController heroController;

    private float maxMiningDistance = 0.8f;

    public Joystick miningJoystick;

    private void Awake()
    {
        // Ініціалізація аніматора
        animator = GetComponent<Animator>();
        heroController = GetComponent<HeroController>();
    }

    private void FixedUpdate()
    {
        float horizontalInput = miningJoystick.Horizontal;
        float verticalInput = miningJoystick.Vertical;

        if (horizontalInput != 0 || verticalInput != 0)
        {
            Vector2 miningDirection = new Vector2(horizontalInput, verticalInput).normalized;
            Vector2 miningPosition = (Vector2)transform.position + miningDirection * maxMiningDistance;

            CheckTile(miningPosition);
        }
        else
        {
            // Зупинка анімації майнінгу, якщо вона вже почалася
            animator.SetBool("IsMining", false);
            heroController.SetCanMove(true);
        }
    }

    private void StartMiningAnimation()
    {
        animator.SetBool("IsMining", true);
        animator.SetBool("IsWalking", false);
        heroController.SetCanMove(false);
    }

    private void CheckTile(Vector2 targetPosition)
    {
        Collider2D hitCollider = Physics2D.OverlapPoint(targetPosition);

        // Перевірка наявності цілі
        if (hitCollider != null)
        {
            StartMiningAnimation();

            GameObject tile = hitCollider.gameObject;
            TileBehaviour tileBehaviour = tile.GetComponent<TileBehaviour>();

            // Перевірка тегів та стану плитки
            if (tile.CompareTag("Player") || tile.CompareTag("Stone") || tile.CompareTag("Cave")) return;

            // Перевірка чи плитка не розбита
            if (tileBehaviour != null && !tileBehaviour.IsBroken)
            {
                tileBehaviour.HitTile(tileBehaviour);
            }

            if (tileBehaviour.IsBroken)
            {
                heroController.SetCanMove(true);
                animator.SetBool("IsMining", false);
                animator.SetBool("IsWalking", true);
            }
        }
    }
}
