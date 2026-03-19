using UnityEngine;

[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(HeroGroundSensor))]
[RequireComponent(typeof(HeroWallSensor))]
public sealed class HeroCollision : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CapsuleCollider2D capsule;
    [SerializeField] private WorldGridService worldGrid;
    [SerializeField] private HeroGroundSensor groundSensor;
    [SerializeField] private HeroWallSensor wallSensor;

    public CapsuleCollider2D Capsule => capsule;
    public WorldGridService WorldGrid => worldGrid;
    public HeroGroundSensor GroundSensor => groundSensor;
    public HeroWallSensor WallSensor => wallSensor;

    private void Reset()
    {
        capsule = GetComponent<CapsuleCollider2D>();
        groundSensor = GetComponent<HeroGroundSensor>();
        wallSensor = GetComponent<HeroWallSensor>();
        worldGrid = FindFirstObjectByType<WorldGridService>();
    }

    private void Awake()
    {
        capsule ??= GetComponent<CapsuleCollider2D>();
        groundSensor ??= GetComponent<HeroGroundSensor>();
        wallSensor ??= GetComponent<HeroWallSensor>();
        worldGrid ??= WorldGridService.Instance;

        if (groundSensor == null)
        {
            groundSensor = gameObject.AddComponent<HeroGroundSensor>();
        }

        if (wallSensor == null)
        {
            wallSensor = gameObject.AddComponent<HeroWallSensor>();
        }
    }

    public bool IsGrounded()
    {
        groundSensor ??= GetComponent<HeroGroundSensor>();
        return groundSensor != null && groundSensor.IsGrounded();
    }

    public bool IsWorldReady()
    {
        return ResolveWorldGrid() != null && worldGrid.IsReady;
    }

    public bool IsBlockedHorizontally(float direction)
    {
        wallSensor ??= GetComponent<HeroWallSensor>();
        return wallSensor != null && wallSensor.IsBlockedHorizontally(direction);
    }

    public Vector2Int GetCurrentCell()
    {
        if (ResolveWorldGrid() == null || !worldGrid.IsReady)
        {
            return Vector2Int.zero;
        }

        return worldGrid.WorldToCell(transform.position);
    }

    public Vector2Int GetFootCell()
    {
        if (ResolveWorldGrid() == null || !worldGrid.IsReady)
        {
            return Vector2Int.zero;
        }

        Bounds bounds = capsule.bounds;
        Vector2 probe = new Vector2(bounds.center.x, bounds.min.y - 0.02f);
        return worldGrid.WorldToCell(probe);
    }

    public WorldCellType GetFootCellType()
    {
        if (ResolveWorldGrid() == null || !worldGrid.IsReady)
        {
            return WorldCellType.Empty;
        }

        return worldGrid.GetCellType(GetFootCell());
    }

    private WorldGridService ResolveWorldGrid()
    {
        worldGrid ??= WorldGridService.Instance;
        return worldGrid;
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

        groundSensor ??= GetComponent<HeroGroundSensor>();
        wallSensor ??= GetComponent<HeroWallSensor>();
        Bounds bounds = capsule.bounds;

        Gizmos.color = Color.green;
        float groundProbeDistance = groundSensor != null ? groundSensor.GroundProbeDistance : 0.08f;
        float probeWidthFactor = groundSensor != null ? groundSensor.ProbeWidthFactor : 0.9f;
        Vector3 groundSize = new Vector3(bounds.size.x * probeWidthFactor, groundProbeDistance, 0.01f);
        Vector3 groundCenter = new Vector3(bounds.center.x, bounds.min.y - groundProbeDistance * 0.5f, 0f);
        Gizmos.DrawWireCube(groundCenter, groundSize);

        Gizmos.color = Color.yellow;
        float wallProbeDistance = wallSensor != null ? wallSensor.WallProbeDistance : 0.05f;
        Vector3 wallSize = new Vector3(wallProbeDistance, bounds.size.y * 0.9f, 0.01f);
        Gizmos.DrawWireCube(new Vector3(bounds.min.x - wallProbeDistance * 0.5f, bounds.center.y, 0f), wallSize);
        Gizmos.DrawWireCube(new Vector3(bounds.max.x + wallProbeDistance * 0.5f, bounds.center.y, 0f), wallSize);
    }
#endif
}
