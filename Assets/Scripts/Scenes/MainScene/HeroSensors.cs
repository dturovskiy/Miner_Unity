using UnityEngine;

/// <summary>
/// Сенсори героя.
/// Тут немає руху, тільки читання фізичного оточення:
/// - стоїмо на землі чи ні
/// - чи є драбина біля тулуба
/// - чи є драбина під ногами
/// </summary>
public sealed class HeroSensors : MonoBehaviour
{
    [Header("Ground")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.08f;
    [SerializeField] private LayerMask groundMask;

    [Header("Ladder")]
    [SerializeField] private Transform bodyLadderCheck;
    [SerializeField] private Vector2 bodyLadderCheckSize = new Vector2(0.35f, 0.7f);

    [SerializeField] private Transform feetLadderCheck;
    [SerializeField] private Vector2 feetLadderCheckSize = new Vector2(0.35f, 0.12f);

    [SerializeField] private LayerMask ladderMask;

    public LayerMask LadderMask => ladderMask;

    public bool IsGrounded()
    {
        if (groundCheck == null)
        {
            return false;
        }

        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask) != null;
    }

    public LadderBehaviour GetLadderAtBody()
    {
        return GetBestLadder(bodyLadderCheck, bodyLadderCheckSize);
    }

    public LadderBehaviour GetLadderAtFeet()
    {
        return GetBestLadder(feetLadderCheck, feetLadderCheckSize);
    }

    private LadderBehaviour GetBestLadder(Transform probe, Vector2 size)
    {
        if (probe == null)
        {
            return null;
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(probe.position, size, 0f, ladderMask);

        LadderBehaviour bestLadder = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            LadderBehaviour ladder = hits[i].GetComponent<LadderBehaviour>();
            if (ladder == null)
            {
                continue;
            }

            float distance = Mathf.Abs(ladder.CenterX - probe.position.x);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestLadder = ladder;
            }
        }

        return bestLadder;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (bodyLadderCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(bodyLadderCheck.position, bodyLadderCheckSize);
        }

        if (feetLadderCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(feetLadderCheck.position, feetLadderCheckSize);
        }
    }
}
