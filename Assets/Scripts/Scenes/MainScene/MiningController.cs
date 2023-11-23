using UnityEngine;

public class MiningController : MonoBehaviour
{
    // Аніматор для управління анімацією
    private Animator animator;
    private HeroController heroController;
    private TileBehaviour tileBehaviour;

    private float maxMiningDistance = 0.5f;

    public Joystick miningJoystick;

    private float miningDelay = 1.0f;

    private bool canHit;

    private void Awake()
    {
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
            canHit = false;
        }
    }

    private void StartMiningAnimation()
    {
        animator.SetBool("IsMining", true);
        animator.SetBool("IsWalking", false);
        heroController.SetCanMove(false);
        Invoke(nameof(CanHit), 1f);
    }

    private void StopMiningAnimation()
    {
        animator.SetBool("IsMining", false);
        animator.SetBool("IsWalking", true);
        heroController.SetCanMove(true);
        canHit = false;
    }

    public void CanHit()
    {
        canHit = true;
    }

    private void CheckTile(Vector2 targetPosition)
    {
        Collider2D hitCollider = Physics2D.OverlapPoint(targetPosition);

        // Перевірка наявності цілі
        if (hitCollider != null)
        {
            GameObject tile = hitCollider.gameObject;
            tileBehaviour = tile.GetComponent<TileBehaviour>();

            // Перевірка тегів та стану плитки
            if (tile.CompareTag("Player") || tile.CompareTag("Stone") || tile.CompareTag("Cave")) return;

            StartMiningAnimation();

            if (canHit)
            {
                if (tileBehaviour != null && !tileBehaviour.IsBroken)
                {
                    tileBehaviour.HitTile(tileBehaviour);
                }
                if (tileBehaviour != null && tileBehaviour.IsBroken)
                {
                    StopMiningAnimation();
                }
            }
            if (tileBehaviour == null)
            {
                StopMiningAnimation();
            }
        }
    }
}
