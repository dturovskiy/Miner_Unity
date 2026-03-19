using MinerUnity.Runtime;
using MinerUnity.Terrain;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(HeroController))]
[RequireComponent(typeof(HeroCollision))]
public sealed class HeroMining : MonoBehaviour
{
    private readonly struct MiningTarget
    {
        public MiningTarget(Vector2Int cell, Vector2Int direction, string directionName, TileID tileId)
        {
            Cell = cell;
            Direction = direction;
            DirectionName = directionName;
            TileId = tileId;
        }

        public Vector2Int Cell { get; }
        public Vector2Int Direction { get; }
        public string DirectionName { get; }
        public TileID TileId { get; }
    }

    [Header("References")]
    [SerializeField] private HeroController heroController;
    [SerializeField] private HeroCollision heroCollision;
    [SerializeField] private HeroState heroState;
    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private Animator animator;

    [Header("Input")]
    [SerializeField, Min(0f)] private float inputDeadZone = 0.35f;

    [Header("Hit Loop")]
    [SerializeField, Min(0.05f)] private float hitIntervalSeconds = 0.4f;
    [SerializeField, Min(1)] private int defaultHitsToBreak = 4;
    [SerializeField, Min(0)] private int pickaxePowerBonus;

    private bool hasCurrentTarget;
    private Vector2Int currentTargetCell;
    private Vector2Int currentTargetDirection;
    private float hitTimer;
    private bool isMining;

    private string lastRejectedReason = string.Empty;
    private Vector2Int lastRejectedTarget;
    private string lastRejectedDirection = string.Empty;
    private bool hasRejectedTarget;

    public bool IsMining => isMining;

    private void Reset()
    {
        heroController = GetComponent<HeroController>();
        heroCollision = GetComponent<HeroCollision>();
        heroState = GetComponent<HeroState>();
        animator = GetComponent<Animator>();
        chunkManager = FindFirstObjectByType<ChunkManager>();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        RunMiningLoop();
        SyncAnimator();
    }

    private void RunMiningLoop()
    {
        WorldRuntime runtime = chunkManager != null ? chunkManager.GetWorldRuntime() : null;
        if (heroController == null || heroCollision == null || heroState == null || chunkManager == null || runtime == null || !heroCollision.IsWorldReady())
        {
            StopMining();
            return;
        }

        if (heroState.IsFalling)
        {
            ClearRejectedLatch();
            StopMining();
            return;
        }

        Vector2 input = heroController.MovementInput;
        if (input.sqrMagnitude < inputDeadZone * inputDeadZone)
        {
            ClearRejectedLatch();
            StopMining();
            return;
        }

        Vector2Int anchorCell = heroCollision.TryGetOpenAnchorCell(out Vector2Int openCell)
            ? openCell
            : heroCollision.GetCurrentCell();
        if (!runtime.IsInsideMiningArea(anchorCell.x, anchorCell.y))
        {
            EmitDigBlockedOnce("outsideMiningArea", GetDirectionName(GetDominantAxisDirection(input)), anchorCell, runtime.GetTile(anchorCell.x, anchorCell.y));
            StopMining();
            return;
        }

        if (!TryGetActiveMiningTarget(runtime, input, anchorCell, out MiningTarget target, out string blockedReason, out Vector2Int blockedCell, out TileID blockedTile))
        {
            EmitDigBlockedOnce(blockedReason, GetDirectionName(GetDominantAxisDirection(input)), blockedCell, blockedTile);
            StopMining();
            return;
        }

        ClearRejectedLatch();

        if (!hasCurrentTarget || currentTargetCell != target.Cell)
        {
            currentTargetCell = target.Cell;
            currentTargetDirection = target.Direction;
            hasCurrentTarget = true;
            hitTimer = 0f;
            isMining = true;

            Diag.Event(
                "Hero",
                "DigStarted",
                "Hero started mining a nearby block.",
                this,
                ("direction", target.DirectionName),
                ("inputSource", heroController.UsesMovementJoystick ? "movementJoystick" : "keyboardFallback"),
                ("currentCell", heroCollision.GetCurrentCell()),
                ("targetCell", target.Cell),
                ("tile", target.TileId.ToString()),
                ("existingHits", runtime.GetMiningHits(target.Cell.x, target.Cell.y)),
                ("hitsRequired", GetHitsRequired(target.TileId)));
        }

        isMining = true;
        hitTimer += Time.deltaTime;
        if (hitTimer < hitIntervalSeconds)
        {
            return;
        }

        hitTimer -= hitIntervalSeconds;
        ApplyMiningHit(runtime, target);
    }

    private bool TryGetActiveMiningTarget(
        WorldRuntime runtime,
        Vector2 input,
        Vector2Int currentCell,
        out MiningTarget target,
        out string blockedReason,
        out Vector2Int blockedCell,
        out TileID blockedTile)
    {
        if (hasCurrentTarget && TryContinueLockedTarget(runtime, input, currentCell, out target, out blockedReason, out blockedCell, out blockedTile))
        {
            return true;
        }

        return TryResolveMiningTarget(runtime, input, currentCell, out target, out blockedReason, out blockedCell, out blockedTile);
    }

    private bool TryContinueLockedTarget(
        WorldRuntime runtime,
        Vector2 input,
        Vector2Int currentCell,
        out MiningTarget target,
        out string blockedReason,
        out Vector2Int blockedCell,
        out TileID blockedTile)
    {
        target = default;
        blockedReason = string.Empty;
        blockedCell = currentTargetCell;
        blockedTile = runtime.IsInsideBounds(currentTargetCell.x, currentTargetCell.y)
            ? runtime.GetTile(currentTargetCell.x, currentTargetCell.y)
            : TileID.Edge;

        Vector2 inputNormalized = input.normalized;
        Vector2 expectedDirection = new Vector2(currentTargetDirection.x, currentTargetDirection.y);
        if (Vector2.Dot(inputNormalized, expectedDirection) < 0.55f)
        {
            blockedReason = "inputReleasedTarget";
            return false;
        }

        if (!runtime.IsInsideBounds(currentTargetCell.x, currentTargetCell.y) || !runtime.IsMineable(currentTargetCell.x, currentTargetCell.y))
        {
            blockedReason = "targetBecameInvalid";
            return false;
        }

        if (!IsTargetStillReachable(currentCell, currentTargetDirection, currentTargetCell))
        {
            blockedReason = "lostTargetContact";
            return false;
        }

        target = new MiningTarget(currentTargetCell, currentTargetDirection, GetDirectionName(currentTargetDirection), blockedTile);
        return true;
    }

    private bool TryResolveMiningTarget(
        WorldRuntime runtime,
        Vector2 input,
        Vector2Int anchorCell,
        out MiningTarget target,
        out string blockedReason,
        out Vector2Int blockedCell,
        out TileID blockedTile)
    {
        Vector2Int direction = GetDominantAxisDirection(input);
        string directionName = GetDirectionName(direction);
        Vector2Int targetCell = Vector2Int.zero;
        blockedCell = Vector2Int.zero;
        blockedTile = TileID.Empty;

        if (direction == Vector2Int.left || direction == Vector2Int.right)
        {
            if (!heroCollision.WallSensor.TryGetHorizontalBlockHit(direction.x, out _))
            {
                target = default;
                blockedReason = "noWallContact";
                blockedCell = anchorCell + direction;
                blockedTile = heroCollision.GetTileId(blockedCell);
                return false;
            }

            targetCell = anchorCell + direction;
        }
        else if (direction == Vector2Int.down)
        {
            if (!heroCollision.GroundSensor.TryGetGroundHit(out _))
            {
                target = default;
                blockedReason = "noGroundContact";
                blockedCell = anchorCell + direction;
                blockedTile = heroCollision.GetTileId(blockedCell);
                return false;
            }

            targetCell = anchorCell + direction;
        }
        else if (direction == Vector2Int.up)
        {
            targetCell = anchorCell + direction;
        }

        if (!runtime.IsInsideBounds(targetCell.x, targetCell.y))
        {
            target = default;
            blockedReason = "outOfBounds";
            blockedCell = targetCell;
            blockedTile = TileID.Edge;
            return false;
        }

        TileID targetTile = runtime.GetTile(targetCell.x, targetCell.y);
        if (!runtime.IsMineable(targetCell.x, targetCell.y))
        {
            target = default;
            blockedReason = "notMineable";
            blockedCell = targetCell;
            blockedTile = targetTile;
            return false;
        }

        target = new MiningTarget(targetCell, direction, directionName, targetTile);
        blockedReason = string.Empty;
        blockedCell = targetCell;
        blockedTile = targetTile;
        return true;
    }

    private void ApplyMiningHit(WorldRuntime runtime, MiningTarget target)
    {
        TileID currentTile = runtime.GetTile(target.Cell.x, target.Cell.y);
        if (!runtime.IsMineable(target.Cell.x, target.Cell.y))
        {
            EmitDigBlockedOnce("targetBecameInvalid", target.DirectionName, target.Cell, currentTile);
            StopMining();
            return;
        }

        int hitsRequired = GetHitsRequired(currentTile);
        int hitsApplied = Mathf.Min(runtime.GetMiningHits(target.Cell.x, target.Cell.y) + 1, hitsRequired);
        runtime.SetMiningHits(target.Cell.x, target.Cell.y, hitsApplied);
        int crackStage = hitsApplied >= hitsRequired ? 0 : WorldRuntime.GetCrackStage(hitsApplied, hitsRequired);
        UpdateTargetCrackStage(target.Cell, crackStage);

        Diag.Event(
            "Hero",
            "DigHit",
            "Hero applied a mining hit to the current target.",
            this,
            ("direction", target.DirectionName),
            ("inputSource", heroController.UsesMovementJoystick ? "movementJoystick" : "keyboardFallback"),
            ("currentCell", heroCollision.GetCurrentCell()),
            ("targetCell", target.Cell),
            ("tile", currentTile.ToString()),
            ("hitIndex", hitsApplied),
            ("hitsRequired", hitsRequired),
            ("crackStage", crackStage));

        if (hitsApplied < hitsRequired)
        {
            return;
        }

        if (!chunkManager.DestroyTileInWorld(target.Cell.x, target.Cell.y))
        {
            EmitDigBlockedOnce("destroyFailed", target.DirectionName, target.Cell, currentTile);
            StopMining();
            return;
        }

        Diag.Event(
            "Hero",
            "DigCompleted",
            "Hero completed dig and removed the target tile.",
            this,
            ("direction", target.DirectionName),
            ("inputSource", heroController.UsesMovementJoystick ? "movementJoystick" : "keyboardFallback"),
            ("currentCell", heroCollision.GetCurrentCell()),
            ("targetCell", target.Cell),
            ("tile", currentTile.ToString()),
            ("hitsRequired", hitsRequired));

        StopMining();
    }

    private int GetHitsRequired(TileID tileId)
    {
        if (!WorldCellRules.IsMineable(tileId))
        {
            return int.MaxValue;
        }

        return Mathf.Max(1, defaultHitsToBreak - pickaxePowerBonus);
    }

    private void UpdateTargetCrackStage(Vector2Int targetCell, int crackStage)
    {
        if (chunkManager != null && chunkManager.TryGetSpawnedTileBehaviour(targetCell.x, targetCell.y, out TileBehaviour tileBehaviour))
        {
            tileBehaviour.SetCrackStage(crackStage);
        }
    }

    private void StopMining()
    {
        isMining = false;
        hitTimer = 0f;
        hasCurrentTarget = false;
        currentTargetCell = Vector2Int.zero;
        currentTargetDirection = Vector2Int.zero;
    }

    private void EmitDigBlockedOnce(string reason, string directionName, Vector2Int targetCell, TileID tile)
    {
        if (hasRejectedTarget && lastRejectedReason == reason && lastRejectedTarget == targetCell && lastRejectedDirection == directionName)
        {
            return;
        }

        hasRejectedTarget = true;
        lastRejectedReason = reason;
        lastRejectedTarget = targetCell;
        lastRejectedDirection = directionName;

        Diag.Warning(
            "Hero",
            "DigBlocked",
            "Hero dig was blocked.",
            this,
            ("reason", reason),
            ("direction", directionName),
            ("inputSource", heroController != null && heroController.UsesMovementJoystick ? "movementJoystick" : "keyboardFallback"),
            ("currentCell", heroCollision != null ? heroCollision.GetCurrentCell() : Vector2Int.zero),
            ("targetCell", targetCell),
            ("tile", tile.ToString()),
            ("worldReady", heroCollision != null && heroCollision.IsWorldReady()),
            ("hasChunkManager", chunkManager != null));
    }

    private void ClearRejectedLatch()
    {
        hasRejectedTarget = false;
        lastRejectedReason = string.Empty;
        lastRejectedTarget = default;
        lastRejectedDirection = string.Empty;
    }

    private void ResolveReferences()
    {
        heroController ??= GetComponent<HeroController>();
        heroCollision ??= GetComponent<HeroCollision>();
        heroState ??= GetComponent<HeroState>();
        animator ??= GetComponent<Animator>();
        chunkManager ??= FindFirstObjectByType<ChunkManager>();
    }

    private void SyncAnimator()
    {
        if (animator != null)
        {
            animator.SetBool("IsMining", isMining);
        }
    }

    private static Vector2Int GetDominantAxisDirection(Vector2 rawInput)
    {
        if (Mathf.Abs(rawInput.x) >= Mathf.Abs(rawInput.y))
        {
            return rawInput.x >= 0f ? Vector2Int.right : Vector2Int.left;
        }

        return rawInput.y >= 0f ? Vector2Int.up : Vector2Int.down;
    }

    private static string GetDirectionName(Vector2Int direction)
    {
        if (direction == Vector2Int.up)
        {
            return "Up";
        }

        if (direction == Vector2Int.down)
        {
            return "Down";
        }

        if (direction == Vector2Int.left)
        {
            return "Left";
        }

        if (direction == Vector2Int.right)
        {
            return "Right";
        }

        return "None";
    }

    private bool IsTargetStillReachable(Vector2Int currentCell, Vector2Int direction, Vector2Int targetCell)
    {
        Vector2Int anchorCell = heroCollision.TryGetOpenAnchorCell(out Vector2Int openCell)
            ? openCell
            : currentCell;

        if (direction == Vector2Int.left || direction == Vector2Int.right)
        {
            return heroCollision.WallSensor.TryGetHorizontalBlockHit(direction.x, out _)
                && anchorCell + direction == targetCell;
        }

        if (direction == Vector2Int.down)
        {
            return heroCollision.GroundSensor.TryGetGroundHit(out _)
                && anchorCell + direction == targetCell;
        }

        if (direction == Vector2Int.up)
        {
            return anchorCell + direction == targetCell;
        }

        return false;
    }
}
