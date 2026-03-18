using UnityEngine;

[RequireComponent(typeof(CapsuleCollider2D))]
public sealed class HeroCollision : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CapsuleCollider2D capsule;
    [SerializeField] private WorldGridService worldGrid;

    [Header("Ground Probe")]
    [SerializeField] private LayerMask solidMask = ~0;
    [SerializeField, Min(0.005f)] private float groundProbeDistance = 0.08f;
    [SerializeField, Range(0.1f, 1f)] private float probeWidthFactor = 0.9f;

    [Header("Wall Probe")]
    [SerializeField, Min(0.005f)] private float wallProbeDistance = 0.05f;

    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    public Rigidbody2D Rigidbody => rb;
    public CapsuleCollider2D Capsule => capsule;
    public WorldGridService WorldGrid => worldGrid;
    public LayerMask SolidMask => solidMask;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();
        worldGrid = FindFirstObjectByType<WorldGridService>();
    }

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (capsule == null)
        {
            capsule = GetComponent<CapsuleCollider2D>();
        }

        if (worldGrid == null)
        {
            worldGrid = WorldGridService.Instance;
        }
    }

    public bool IsGrounded()
    {
        Bounds bounds = capsule.bounds;
        Vector2 size = new Vector2(bounds.size.x * probeWidthFactor, groundProbeDistance);
        Vector2 center = new Vector2(bounds.center.x, bounds.min.y - groundProbeDistance * 0.5f);

        Collider2D hit = Physics2D.OverlapBox(center, size, 0f, solidMask);
        return hit != null && hit != capsule;
    }

    public bool IsBlockedHorizontally(float direction)
    {
        if (Mathf.Approximately(direction, 0f))
        {
            return false;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = solidMask;
        filter.useTriggers = false;

        int hitCount = capsule.Cast(new Vector2(Mathf.Sign(direction), 0f), filter, castHits, wallProbeDistance);
        for (int i = 0; i < hitCount; i++)
        {
            if (castHits[i].collider != null && castHits[i].collider != capsule)
            {
                return true;
            }
        }

        return false;
    }

    public Vector2Int GetCurrentCell()
    {
        if (worldGrid == null || !worldGrid.IsReady)
        {
            return Vector2Int.zero;
        }

        return worldGrid.WorldToCell(transform.position);
    }

    public Vector2Int GetFootCell()
    {
        if (worldGrid == null || !worldGrid.IsReady)
        {
            return Vector2Int.zero;
        }

        Bounds bounds = capsule.bounds;
        Vector2 probe = new Vector2(bounds.center.x, bounds.min.y - 0.02f);
        return worldGrid.WorldToCell(probe);
    }

    public WorldCellType GetFootCellType()
    {
        if (worldGrid == null || !worldGrid.IsReady)
        {
            return WorldCellType.Empty;
        }

        return worldGrid.GetCellType(GetFootCell());
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (capsule == null)
        {
            capsule = GetComponent<CapsuleCollider2D>();
            if (capsule == null)
            {
                return;
            }
        }

        Bounds bounds = capsule.bounds;

        Gizmos.color = Color.green;
        Vector3 groundSize = new Vector3(bounds.size.x * probeWidthFactor, groundProbeDistance, 0.01f);
        Vector3 groundCenter = new Vector3(bounds.center.x, bounds.min.y - groundProbeDistance * 0.5f, 0f);
        Gizmos.DrawWireCube(groundCenter, groundSize);

        Gizmos.color = Color.yellow;
        Vector3 wallSize = new Vector3(wallProbeDistance, bounds.size.y * 0.9f, 0.01f);
        Gizmos.DrawWireCube(new Vector3(bounds.min.x - wallProbeDistance * 0.5f, bounds.center.y, 0f), wallSize);
        Gizmos.DrawWireCube(new Vector3(bounds.max.x + wallProbeDistance * 0.5f, bounds.center.y, 0f), wallSize);
    }
#endif
}
