using MinerUnity.Terrain;
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

    public Vector2Int WorldToCell(Vector2 worldPosition)
    {
        if (ResolveWorldGrid() == null || !worldGrid.IsReady)
        {
            return Vector2Int.zero;
        }

        return worldGrid.WorldToCell(worldPosition);
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

    public Vector2Int GetGroundProbeCell()
    {
        if (ResolveWorldGrid() == null || !worldGrid.IsReady || groundSensor == null)
        {
            return Vector2Int.zero;
        }

        groundSensor.GetGroundProbe(out Vector2 center, out _);
        return worldGrid.WorldToCell(center);
    }

    public Vector2Int GetWallProbeCell(float direction)
    {
        if (ResolveWorldGrid() == null || !worldGrid.IsReady || wallSensor == null || Mathf.Approximately(direction, 0f))
        {
            return Vector2Int.zero;
        }

        return worldGrid.WorldToCell(wallSensor.GetHorizontalProbePoint(direction));
    }

    public WorldCellType GetFootCellType()
    {
        if (ResolveWorldGrid() == null || !worldGrid.IsReady)
        {
            return WorldCellType.Empty;
        }

        return worldGrid.GetCellType(GetFootCell());
    }

    public TileID GetFootTileId()
    {
        if (ResolveWorldGrid() == null || !worldGrid.IsReady)
        {
            return TileID.Empty;
        }

        return worldGrid.GetTileId(GetFootCell());
    }

    public WorldCellType GetGroundProbeCellType()
    {
        if (ResolveWorldGrid() == null || !worldGrid.IsReady)
        {
            return WorldCellType.Empty;
        }

        return worldGrid.GetCellType(GetGroundProbeCell());
    }

    public TileID GetGroundProbeTileId()
    {
        if (ResolveWorldGrid() == null || !worldGrid.IsReady)
        {
            return TileID.Empty;
        }

        return worldGrid.GetTileId(GetGroundProbeCell());
    }

    public WorldCellType GetWallProbeCellType(float direction)
    {
        if (ResolveWorldGrid() == null || !worldGrid.IsReady)
        {
            return WorldCellType.Empty;
        }

        return worldGrid.GetCellType(GetWallProbeCell(direction));
    }

    public TileID GetWallProbeTileId(float direction)
    {
        if (ResolveWorldGrid() == null || !worldGrid.IsReady)
        {
            return TileID.Empty;
        }

        return worldGrid.GetTileId(GetWallProbeCell(direction));
    }

    public WorldCellType GetCellType(Vector2Int cell)
    {
        if (ResolveWorldGrid() == null || !worldGrid.IsReady)
        {
            return WorldCellType.Empty;
        }

        return worldGrid.GetCellType(cell);
    }

    public TileID GetTileId(Vector2Int cell)
    {
        if (ResolveWorldGrid() == null || !worldGrid.IsReady)
        {
            return TileID.Empty;
        }

        return worldGrid.GetTileId(cell);
    }

    public bool TryGetColliderCell(Collider2D collider, out Vector2Int cell)
    {
        cell = Vector2Int.zero;
        if (ResolveWorldGrid() == null || !worldGrid.IsReady || collider == null)
        {
            return false;
        }

        if (collider.isTrigger)
        {
            return false;
        }

        TileBehaviour tileBehaviour = collider.GetComponent<TileBehaviour>();
        if (tileBehaviour != null)
        {
            cell = new Vector2Int(tileBehaviour.gridX, tileBehaviour.gridY);
            return true;
        }

        cell = worldGrid.WorldToCell(collider.bounds.center);
        return true;
    }

    public bool TryDescribeCollider(Collider2D collider, out Vector2Int cell, out TileID tileId, out WorldCellType cellType)
    {
        cell = Vector2Int.zero;
        tileId = TileID.Empty;
        cellType = WorldCellType.Empty;

        if (!TryGetColliderCell(collider, out cell))
        {
            return false;
        }

        tileId = GetTileId(cell);
        cellType = GetCellType(cell);
        return true;
    }

    public bool TryGetCurrentSupportInfo(out Collider2D supportCollider, out Vector2Int supportCell, out TileID supportTileId, out WorldCellType supportCellType)
    {
        supportCollider = null;
        supportCell = Vector2Int.zero;
        supportTileId = TileID.Empty;
        supportCellType = WorldCellType.Empty;

        if (groundSensor == null || !groundSensor.TryGetGroundHit(out supportCollider))
        {
            return false;
        }

        return TryDescribeCollider(supportCollider, out supportCell, out supportTileId, out supportCellType);
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
