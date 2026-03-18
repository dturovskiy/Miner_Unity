using UnityEngine;

public sealed class MiningController : MonoBehaviour
{
    [SerializeField] private WorldGridService worldGrid;

    // Для діагностики руху краще тимчасово не читати той самий joystick,
    // який використовується для ходьби.
    [SerializeField] private Joystick miningJoystick;

    [SerializeField] private Animator animator;
    [SerializeField] private float miningDelay = 0.4f;

    private float timer;
    private Vector2Int currentTarget;
    private string lastRejectedReason;
    private Vector2Int lastRejectedTarget;
    private bool hasRejectedTarget;

    public bool IsMining { get; private set; }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        if (worldGrid == null)
        {
            worldGrid = WorldGridService.Instance;
        }

        // Поки лагодимо рух, можна просто вийти,
        // якщо окремий joystick для копання не призначений.
        if (worldGrid == null)
        {
            LogRejectedOnce("world_grid_missing", Vector2Int.zero);
            StopMining("world_grid_missing");
            return;
        }

        if (miningJoystick == null)
        {
            LogRejectedOnce("mining_joystick_missing", Vector2Int.zero);
            StopMining("mining_joystick_missing");
            return;
        }

        Vector2 input = new Vector2(miningJoystick.Horizontal, miningJoystick.Vertical);

        if (input.magnitude < 0.5f)
        {
            ClearRejectedLatch();
            StopMining("input_below_threshold");
            return;
        }

        Vector2Int dir = Vector2Int.zero;

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            dir.x = input.x > 0 ? 1 : -1;
        }
        else
        {
            dir.y = input.y > 0 ? 1 : -1;
        }

        Vector2Int target = worldGrid.WorldToCell(transform.position) + dir;

        if (!worldGrid.IsMineable(target))
        {
            LogRejectedOnce("target_not_mineable", target);
            StopMining("target_not_mineable");
            return;
        }

        ClearRejectedLatch();

        if (!IsMining)
        {
            IsMining = true;
            currentTarget = target;
            timer = 0f;

            Diag.Event(
                "Mining",
                "Started",
                "Mining started.",
                this,
                ("targetCell", currentTarget),
                ("delay", miningDelay));
        }
        else if (currentTarget != target)
        {
            Vector2Int previousTarget = currentTarget;
            currentTarget = target;
            timer = 0f;

            Diag.Event(
                "Mining",
                "TargetChanged",
                "Mining target changed.",
                this,
                ("from", previousTarget),
                ("to", currentTarget));
        }

        timer += Time.deltaTime;

        if (timer >= miningDelay)
        {
            BreakCell(target);
            timer = 0f;
        }

        if (animator != null)
        {
            animator.SetBool("IsMining", IsMining);
        }
    }

    private void BreakCell(Vector2Int cell)
    {
        WorldCellType previousType = worldGrid.GetCellType(cell);
        worldGrid.SetCellType(cell, WorldCellType.Empty);

        Diag.Event(
            "Mining",
            "BreakApplied",
            "Mining break applied.",
            this,
            ("targetCell", cell),
            ("previousType", previousType));

        var chunkManager = Object.FindFirstObjectByType<MinerUnity.Terrain.ChunkManager>();
        if (chunkManager != null)
        {
            chunkManager.DestroyTileInWorld(cell.x, cell.y);
        }
        else
        {
            Diag.Warning(
                "Mining",
                "ChunkManagerMissing",
                "ChunkManager was not found while applying mining result.",
                this,
                ("targetCell", cell));
        }
    }

    private void StopMining(string reason)
    {
        if (IsMining)
        {
            Diag.Event(
                "Mining",
                "Stopped",
                "Mining stopped.",
                this,
                ("reason", reason),
                ("targetCell", currentTarget));
        }

        IsMining = false;
        timer = 0f;

        if (animator != null)
        {
            animator.SetBool("IsMining", false);
        }
    }

    private void LogRejectedOnce(string reason, Vector2Int target)
    {
        if (hasRejectedTarget && lastRejectedReason == reason && lastRejectedTarget == target)
        {
            return;
        }

        hasRejectedTarget = true;
        lastRejectedReason = reason;
        lastRejectedTarget = target;

        Diag.Event(
            "Mining",
            "Rejected",
            "Mining rejected.",
            this,
            ("reason", reason),
            ("targetCell", target));
    }

    private void ClearRejectedLatch()
    {
        hasRejectedTarget = false;
        lastRejectedReason = null;
        lastRejectedTarget = default;
    }
}
