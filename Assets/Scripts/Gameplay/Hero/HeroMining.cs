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
    [SerializeField] private HeroLadder heroLadder;
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

    public bool TryGetLockedVerticalTarget(Vector2 input, out Vector2Int targetCell)
    {
        targetCell = Vector2Int.zero;
        if (!isMining || !hasCurrentTarget)
        {
            return false;
        }

        if (currentTargetDirection != Vector2Int.up && currentTargetDirection != Vector2Int.down)
        {
            return false;
        }

        if (input.sqrMagnitude < inputDeadZone * inputDeadZone)
        {
            return false;
        }

        Vector2 expectedDirection = new Vector2(currentTargetDirection.x, currentTargetDirection.y);
        if (Vector2.Dot(input.normalized, expectedDirection) < 0.55f)
        {
            return false;
        }

        targetCell = currentTargetCell;
        return true;
    }

    private void Reset()
    {
        heroController = GetComponent<HeroController>();
        heroCollision = GetComponent<HeroCollision>();
        heroState = GetComponent<HeroState>();
        heroLadder = GetComponent<HeroLadder>();
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

        Vector2 input = heroController.MovementInput;
        if (heroLadder != null && heroLadder.ShouldSuppressMiningInput(input))
        {
            ClearRejectedLatch();
            StopMining();
            return;
        }

        if (heroState.IsFalling)
        {
            ClearRejectedLatch();
            StopMining();
            return;
        }

        if (input.sqrMagnitude < inputDeadZone * inputDeadZone)
        {
            ClearRejectedLatch();
            StopMining();
            return;
        }

        Vector2Int anchorCell = heroCollision.TryGetOpenAnchorCell(out Vector2Int openCell)
            ? openCell
            : heroCollision.GetCurrentCell();
        Vector2Int footCell = heroCollision.GetFootCell();
        bool hasMiningAreaAccess = runtime.IsInsideMiningArea(anchorCell.x, anchorCell.y)
            || runtime.IsInsideMiningArea(footCell.x, footCell.y);
        if (!hasMiningAreaAccess)
        {
            EmitDigBlockedOnce("outsideMiningArea", GetDirectionName(GetDominantAxisDirection(input)), anchorCell, runtime.GetTile(anchorCell.x, anchorCell.y));
            StopMining();
            return;
        }

        if (!TryGetActiveMiningTarget(runtime, input, anchorCell, out MiningTarget target, out string blockedReason, out Vector2Int blockedCell, out TileID blockedTile))
        {
            if (ShouldEmitDigBlocked(blockedReason, blockedTile))
            {
                EmitDigBlockedOnce(blockedReason, GetDirectionName(GetDominantAxisDirection(input)), blockedCell, blockedTile);
            }
            else
            {
                ClearRejectedLatch();
            }

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
            Vector2Int footCell = heroCollision.GetFootCell();
            TileID footTile = heroCollision.GetTileId(footCell);
            if (WorldCellRules.IsMineable(footTile))
            {
                targetCell = footCell;
            }
            else
            {
                Vector2Int groundProbeCell = heroCollision.GetGroundProbeCell();
                TileID groundProbeTile = heroCollision.GetTileId(groundProbeCell);
                if (WorldCellRules.IsMineable(groundProbeTile))
                {
                    targetCell = groundProbeCell;
                }
                else
                {
                    target = default;
                    blockedReason = "noDigTarget";
                    blockedCell = footCell;
                    blockedTile = footTile;
                    return false;
                }
            }
        }
        else if (direction == Vector2Int.up)
        {
            Vector2Int upCell = anchorCell + Vector2Int.up;
            TileID upTile = heroCollision.GetTileId(upCell);
            if (!WorldCellRules.IsMineable(upTile))
            {
                target = default;
                blockedReason = "noDigTarget";
                blockedCell = upCell;
                blockedTile = upTile;
                return false;
            }

            targetCell = upCell;
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
            blockedReason = IsPassiveNoTargetTile(targetTile) ? "noDigTarget" : "notMineable";
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
        if (!runtime.TryApplyMiningHit(target.Cell.x, target.Cell.y, hitsRequired, out WorldRuntime.MiningHitResult hitResult))
        {
            EmitDigBlockedOnce("applyHitFailed", target.DirectionName, target.Cell, currentTile);
            StopMining();
            return;
        }

        UpdateTargetCrackStage(target.Cell, hitResult.CrackStage);

        Diag.Event(
            "World",
            "MiningHitApplied",
            "Mining hit was applied through the world runtime.",
            chunkManager != null ? (Object)chunkManager : this,
            ("cell", hitResult.Cell),
            ("tile", hitResult.TileId.ToString()),
            ("hitIndex", hitResult.HitsApplied),
            ("hitsRequired", hitResult.HitsRequired),
            ("crackStage", hitResult.CrackStage),
            ("destroyed", hitResult.Destroyed));

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
            ("hitIndex", hitResult.HitsApplied),
            ("hitsRequired", hitResult.HitsRequired),
            ("crackStage", hitResult.CrackStage));

        if (!hitResult.Destroyed)
        {
            return;
        }

        Diag.Event(
            "World",
            "TileDestroyed",
            "World tile was destroyed through the mining runtime path.",
            chunkManager != null ? (Object)chunkManager : this,
            ("cell", hitResult.Cell),
            ("tile", hitResult.TileId.ToString()),
            ("hitsRequired", hitResult.HitsRequired));

        chunkManager.ApplyDestroyedTileView(target.Cell.x, target.Cell.y);

        Diag.Event(
            "Hero",
            "DigCompleted",
            "Hero completed dig and removed the target tile.",
            this,
            ("direction", target.DirectionName),
            ("inputSource", heroController.UsesMovementJoystick ? "movementJoystick" : "keyboardFallback"),
            ("currentCell", heroCollision.GetCurrentCell()),
            ("targetCell", target.Cell),
            ("tile", hitResult.TileId.ToString()),
            ("hitsRequired", hitResult.HitsRequired));

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

    private static bool ShouldEmitDigBlocked(string reason, TileID tile)
    {
        switch (reason)
        {
            case "outsideMiningArea":
            case "notMineable":
            case "applyHitFailed":
            case "targetBecameInvalid":
                return true;
            case "outOfBounds":
                return tile != TileID.Empty;
            default:
                return false;
        }
    }

    private static bool IsPassiveNoTargetTile(TileID tile)
    {
        return tile == TileID.Empty || tile == TileID.Tunnel || tile == TileID.Ladder;
    }

    private void ResolveReferences()
    {
        heroController ??= GetComponent<HeroController>();
        heroCollision ??= GetComponent<HeroCollision>();
        heroState ??= GetComponent<HeroState>();
        heroLadder ??= GetComponent<HeroLadder>();
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
            Vector2Int footCell = heroCollision.GetFootCell();
            if (WorldCellRules.IsMineable(heroCollision.GetTileId(footCell)))
            {
                return footCell == targetCell;
            }

            Vector2Int groundProbeCell = heroCollision.GetGroundProbeCell();
            return WorldCellRules.IsMineable(heroCollision.GetTileId(groundProbeCell))
                && groundProbeCell == targetCell;
        }

        if (direction == Vector2Int.up)
        {
            return anchorCell + direction == targetCell
                && WorldCellRules.IsMineable(heroCollision.GetTileId(targetCell));
        }

        return false;
    }
}
