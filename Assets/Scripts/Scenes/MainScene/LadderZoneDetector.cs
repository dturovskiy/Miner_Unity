using UnityEngine;

[RequireComponent(typeof(HeroStateController), typeof(HeroInputReader))]
public class LadderZoneDetector : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.08f;
    [SerializeField] private LayerMask groundMask;

    [Header("Ladder Input")]
    [SerializeField] private float climbEnterThreshold = 0.25f;
    [SerializeField] private float climbExitThreshold = -0.15f;

    private HeroStateController heroState;
    private HeroInputReader inputReader;
    private int ladderContacts = 0;

    private void Awake()
    {
        heroState = GetComponent<HeroStateController>();
        inputReader = GetComponent<HeroInputReader>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Рахуємо всі входи в драбинні тригери.
        if (other.CompareTag("Ladder") || other.GetComponent<LadderBehaviour>() != null)
        {
            ladderContacts++;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Зменшуємо лічильник контактів із драбинами.
        if (other.CompareTag("Ladder") || other.GetComponent<LadderBehaviour>() != null)
        {
            ladderContacts--;
            if (ladderContacts < 0)
                ladderContacts = 0;

            // Якщо ми повністю покинули драбину —
            // завершуємо climb-стан.
            if (ladderContacts == 0 && heroState.CurrentState == HeroState.Climbing)
            {
                heroState.ChangeState(HeroState.Normal);
            }
        }
    }

    private void Update()
    {
        bool isInsideLadderZone = ladderContacts > 0;
        
        bool isGrounded = false;
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
        }
        
        float vertical = inputReader.Vertical;

        // Вхід у climb:
        // - або тиснемо вниз, стоячи зверху на драбині
        // - або тиснемо вгору (тепер завжди, бо інакше не залізеш знизу)
        if (isInsideLadderZone && heroState.CurrentState == HeroState.Normal)
        {
            bool wantsClimbDown = vertical < -climbEnterThreshold;
            bool wantsClimbUp = vertical > climbEnterThreshold;

            if (wantsClimbDown || wantsClimbUp)
            {
                heroState.ChangeState(HeroState.Climbing);
                return;
            }
        }

        // Вихід із climb на верхній платформі:
        // якщо герой уже стоїть ногами на землі,
        // і при цьому не тисне явно вниз — завершуємо climb.
        if (heroState.CurrentState == HeroState.Climbing && isGrounded && vertical >= climbExitThreshold)
        {
            heroState.ChangeState(HeroState.Normal);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
