using MinerUnity.Terrain;
using UnityEngine;

[DefaultExecutionOrder(-60)]
[DisallowMultipleComponent]
[RequireComponent(typeof(HeroController))]
[RequireComponent(typeof(HeroState))]
[RequireComponent(typeof(HeroCollision))]
[RequireComponent(typeof(HeroMotor))]
[RequireComponent(typeof(CapsuleCollider2D))]
public sealed class HeroLadder : MonoBehaviour
{
    private readonly struct LadderSupportContext
    {
        public LadderSupportContext(Vector2Int currentCell, Vector2Int footCell, bool currentIsLadder, bool footIsLadder, Vector2Int supportCell)
        {
            CurrentCell = currentCell;
            FootCell = footCell;
            CurrentIsLadder = currentIsLadder;
            FootIsLadder = footIsLadder;
            SupportCell = supportCell;
        }

        public Vector2Int CurrentCell { get; }
        public Vector2Int FootCell { get; }
        public bool CurrentIsLadder { get; }
        public bool FootIsLadder { get; }
        public Vector2Int SupportCell { get; }
        public bool IsTopStanding => !CurrentIsLadder && FootIsLadder;
    }

    [Header("References")]
    [SerializeField] private HeroController heroController;
    [SerializeField] private HeroState heroState;
    [SerializeField] private HeroCollision heroCollision;
    [SerializeField] private HeroMotor heroMotor;
    [SerializeField] private CapsuleCollider2D heroCollider;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float climbSpeed = 3f;
    [SerializeField, Min(0f)] private float inputDeadZone = 0.35f;
    [SerializeField, Min(0f)] private float verticalIntentBias = 0.15f;
    [SerializeField, Min(0f)] private float topExitReachMargin = 0.01f;
    [SerializeField, Min(0f)] private float bottomReachMargin = 0.03f;
    [SerializeField, Min(0f)] private float topStandOffset = 0.01f;
    [SerializeField, Min(0f)] private float upperMiningReachMargin = 0.03f;

    private bool isClimbing;
    private bool hasLadderSupport;
    private bool hasClimbAction;
    private Vector2Int supportLadderCell;
    private LadderSupportContext cachedSupportContext;
    private bool hasBlockedLatch;
    private string lastBlockedDirection = string.Empty;
    private string lastBlockedReason = string.Empty;
    private Vector2Int lastBlockedCell;

    public bool IsOnLadder => isClimbing;
    public bool HasPassiveSupport => hasLadderSupport;
    public bool ShouldSuppressOtherMovement => hasClimbAction;

    private void Reset()
    {
        heroController = GetComponent<HeroController>();
        heroState = GetComponent<HeroState>();
        heroCollision = GetComponent<HeroCollision>();
        heroMotor = GetComponent<HeroMotor>();
        heroCollider = GetComponent<CapsuleCollider2D>();
    }

    private void Awake()
    {
        ResolveReferences();
        heroState?.SetTraversalModeSilently(HeroTraversalMode.Ground);
    }

    private void FixedUpdate()
    {
        ResolveReferences();
        RunLadderLoop();
    }

    public bool ShouldSuppressMiningInput(Vector2 input)
    {
        if (!IsVerticalIntent(input) || heroCollision == null || !heroCollision.IsWorldReady())
        {
            return false;
        }

        return hasClimbAction;
    }

    public bool HasVerticalContext(Vector2 input)
    {
        return false;
    }

    public bool TryGetVerticalMiningTarget(Vector2 input, out Vector2Int targetCell)
    {
        targetCell = Vector2Int.zero;
        return false;
    }

    public bool TryGetPassiveSupportInfo(out Vector2Int supportCell, out TileID supportTileId, out WorldCellType supportCellType)
    {
        supportCell = Vector2Int.zero;
        supportTileId = TileID.Empty;
        supportCellType = WorldCellType.Empty;

        if (!hasLadderSupport)
        {
            return false;
        }

        supportCell = supportLadderCell;
        supportTileId = heroCollision.GetTileId(supportCell);
        supportCellType = heroCollision.GetCellType(supportCell);
        return WorldCellRules.IsClimbable(supportTileId);
    }

    private void RunLadderLoop()
    {
        if (heroController == null || heroState == null || heroCollision == null || heroMotor == null)
        {
            return;
        }

        if (!heroCollision.IsWorldReady())
        {
            ClearBlockedLatch();
            ReleaseAllLadderState();
            return;
        }

        hasLadderSupport = TryResolveSupportContext(out LadderSupportContext supportContext);
        cachedSupportContext = supportContext;
        supportLadderCell = supportContext.SupportCell;

        if (!hasLadderSupport)
        {
            ClearBlockedLatch();
            hasClimbAction = false;
            ReleaseAllLadderState();
            return;
        }

        Vector2 input = heroController.MovementInput;
        if (!IsVerticalIntent(input))
        {
            ClearBlockedLatch();
        }

        hasClimbAction = TryResolveClimbAction(input, supportContext, out Vector2Int climbCell, out string directionName);
        if (hasClimbAction)
        {
            ClearBlockedLatch();
            if (!isClimbing)
            {
                EnterClimb(climbCell, directionName);
            }

            supportLadderCell = climbCell;
            heroState.SetTraversalModeSilently(HeroTraversalMode.Ladder);
            heroState.SetLocomotionSilently(HeroLocomotionState.Idle);
            heroMotor.SetVerticalAttachment(true, true);
            SnapToLadderColumn(climbCell);
            heroMotor.ApplyLadderVelocity(input.y, climbSpeed);
            return;
        }

        if (TryResolveBlockedAction(input, supportContext, out string blockedDirection, out string blockedReason, out Vector2Int blockedCell, out TileID blockedTile))
        {
            EmitLadderBlockedOnce(blockedDirection, blockedReason, blockedCell, blockedTile);
        }
        else
        {
            ClearBlockedLatch();
        }

        if (isClimbing)
        {
            ExitClimbIntoSupport("ClimbStopped");
        }

        EnterPassiveSupport();
    }

    private void EnterClimb(Vector2Int ladderCell, string directionName)
    {
        isClimbing = true;
        supportLadderCell = ladderCell;
        heroState.SetTraversalModeSilently(HeroTraversalMode.Ladder);
        heroState.SetLocomotionSilently(HeroLocomotionState.Idle);

        Diag.Event(
            "Hero",
            "LadderEntered",
            "Hero entered ladder climb mode.",
            this,
            ("direction", directionName),
            ("source", heroController.UsesMovementJoystick ? "movementJoystick" : "keyboardFallback"),
            ("ladderCell", ladderCell),
            ("currentCell", heroCollision.GetCurrentCell()));
    }

    private void ExitClimbIntoSupport(string reason)
    {
        if (!isClimbing)
        {
            return;
        }

        Vector2Int ladderCell = supportLadderCell;
        isClimbing = false;
        heroState.SetTraversalModeSilently(HeroTraversalMode.Ground);
        heroState.SetLocomotionSilently(HeroLocomotionState.Idle);
        heroMotor.SetVerticalAttachment(true, true);

        Diag.Event(
            "Hero",
            "LadderExited",
            "Hero exited ladder climb mode.",
            this,
            ("reason", reason),
            ("ladderCell", ladderCell),
            ("currentCell", heroCollision.GetCurrentCell()));
    }

    private void ReleaseAllLadderState()
    {
        hasClimbAction = false;
        if (isClimbing)
        {
            Vector2Int ladderCell = supportLadderCell;
            isClimbing = false;
            heroState.SetTraversalModeSilently(HeroTraversalMode.Ground);
            heroState.SetLocomotionSilently(HeroLocomotionState.Idle);

            Diag.Event(
                "Hero",
                "LadderExited",
                "Hero exited ladder climb mode.",
                this,
                ("reason", "LostContact"),
                ("ladderCell", ladderCell),
                ("currentCell", heroCollision.GetCurrentCell()));
        }

        heroMotor.SetVerticalAttachment(false, false);
        hasLadderSupport = false;
        cachedSupportContext = default;
        supportLadderCell = Vector2Int.zero;
        ClearBlockedLatch();
    }

    private void EnterPassiveSupport()
    {
        isClimbing = false;
        heroState.SetTraversalModeSilently(HeroTraversalMode.Ground);
        heroMotor.SetVerticalAttachment(true, true);

        if (cachedSupportContext.IsTopStanding)
        {
            SnapToTopStand(cachedSupportContext.SupportCell);
        }
    }

    private bool TryResolveSupportContext(out LadderSupportContext context)
    {
        context = default;
        if (heroCollision == null || !heroCollision.IsWorldReady())
        {
            return false;
        }

        Vector2Int currentCell = heroCollision.GetCurrentCell();
        Vector2Int footCell = heroCollision.GetFootCell();
        bool currentIsLadder = heroCollision.IsClimbableCell(currentCell);
        bool footIsLadder = heroCollision.IsClimbableCell(footCell);
        if (!currentIsLadder && !footIsLadder)
        {
            Vector2Int belowCurrentCell = currentCell + Vector2Int.down;
            if (!heroCollision.IsClimbableCell(belowCurrentCell) || !IsNearTopSupport(belowCurrentCell))
            {
                return false;
            }

            context = new LadderSupportContext(currentCell, belowCurrentCell, false, true, belowCurrentCell);
            return true;
        }

        Vector2Int supportCell = currentIsLadder ? currentCell : footCell;
        context = new LadderSupportContext(currentCell, footCell, currentIsLadder, footIsLadder, supportCell);
        return true;
    }

    private bool TryResolveClimbAction(Vector2 input, LadderSupportContext context, out Vector2Int climbCell, out string directionName)
    {
        climbCell = context.SupportCell;
        directionName = string.Empty;
        if (!IsVerticalIntent(input))
        {
            return false;
        }

        if (input.y > 0f)
        {
            directionName = "Up";
            return TryResolveUpClimb(context, out climbCell);
        }

        directionName = "Down";
        return TryResolveDownClimb(context, out climbCell);
    }

    private bool TryResolveBlockedAction(Vector2 input, LadderSupportContext context, out string directionName, out string blockedReason, out Vector2Int blockedCell, out TileID blockedTile)
    {
        directionName = string.Empty;
        blockedReason = string.Empty;
        blockedCell = Vector2Int.zero;
        blockedTile = TileID.Empty;

        if (!IsVerticalIntent(input))
        {
            return false;
        }

        if (input.y > 0f)
        {
            directionName = "Up";
            return TryResolveUpBlocked(context, out blockedReason, out blockedCell, out blockedTile);
        }

        directionName = "Down";
        return TryResolveDownBlocked(context, out blockedReason, out blockedCell, out blockedTile);
    }

    private bool TryResolveUpClimb(LadderSupportContext context, out Vector2Int climbCell)
    {
        climbCell = context.SupportCell;

        if (context.IsTopStanding)
        {
            if (!HasClearedTopEdge(context.SupportCell))
            {
                return true;
            }

            return false;
        }

        if (HasMineableCeiling())
        {
            return !IsNearUpperMiningReach(context.SupportCell);
        }

        Vector2Int nextCell = context.CurrentIsLadder ? context.CurrentCell + Vector2Int.up : context.SupportCell + Vector2Int.up;
        TileID nextTile = heroCollision.GetTileId(nextCell);
        if (heroCollision.IsClimbableCell(nextCell) || WorldCellRules.IsPassable(nextTile))
        {
            return true;
        }

        return !HasReachedCeilingStop(nextCell.y);
    }

    private bool TryResolveUpBlocked(LadderSupportContext context, out string blockedReason, out Vector2Int blockedCell, out TileID blockedTile)
    {
        blockedReason = string.Empty;
        blockedCell = context.CurrentIsLadder ? context.CurrentCell + Vector2Int.up : context.SupportCell + Vector2Int.up;
        blockedTile = heroCollision.GetTileId(blockedCell);

        if (context.IsTopStanding && HasClearedTopEdge(context.SupportCell))
        {
            return false;
        }

        if (HasMineableCeiling())
        {
            return false;
        }

        if (heroCollision.IsClimbableCell(blockedCell) || WorldCellRules.IsPassable(blockedTile))
        {
            return false;
        }

        if (!HasReachedCeilingStop(blockedCell.y))
        {
            return false;
        }

        blockedReason = "topBlocked";
        return true;
    }

    private bool TryResolveDownClimb(LadderSupportContext context, out Vector2Int climbCell)
    {
        climbCell = context.SupportCell;

        if (context.IsTopStanding)
        {
            climbCell = context.FootCell;
            return true;
        }

        if (HasMineableBelow())
        {
            return false;
        }

        Vector2Int nextCell = context.SupportCell + Vector2Int.down;
        TileID nextTile = heroCollision.GetTileId(nextCell);
        if (heroCollision.IsClimbableCell(nextCell) || WorldCellRules.IsPassable(nextTile))
        {
            return true;
        }

        return !IsAtBottomReach(context.SupportCell);
    }

    private bool TryResolveDownBlocked(LadderSupportContext context, out string blockedReason, out Vector2Int blockedCell, out TileID blockedTile)
    {
        blockedReason = string.Empty;
        blockedCell = context.SupportCell + Vector2Int.down;
        blockedTile = heroCollision.GetTileId(blockedCell);

        if (context.IsTopStanding)
        {
            return false;
        }

        if (HasMineableBelow())
        {
            return false;
        }

        if (heroCollision.IsClimbableCell(blockedCell) || WorldCellRules.IsPassable(blockedTile))
        {
            return false;
        }

        if (!IsAtBottomReach(context.SupportCell))
        {
            return false;
        }

        blockedReason = "bottomBlocked";
        return true;
    }

    private void SnapToLadderColumn(Vector2Int ladderCell)
    {
        Vector2 currentPosition = heroMotor.Rigidbody.position;
        float targetX = heroCollision.CellToWorldCenter(new Vector2Int(ladderCell.x, 0)).x;
        heroMotor.SnapToPosition(new Vector2(targetX, currentPosition.y));
    }

    private void SnapToTopStand(Vector2Int ladderCell)
    {
        if (heroCollider == null)
        {
            return;
        }

        Vector2 currentPosition = heroMotor.Rigidbody.position;
        float targetY = heroCollision.GetCellTopY(ladderCell.y) + heroCollider.bounds.extents.y - topStandOffset;
        heroMotor.SnapToPosition(new Vector2(currentPosition.x, targetY));
    }

    private void ResolveReferences()
    {
        heroController ??= GetComponent<HeroController>();
        heroState ??= GetComponent<HeroState>();
        heroCollision ??= GetComponent<HeroCollision>();
        heroMotor ??= GetComponent<HeroMotor>();
        heroCollider ??= GetComponent<CapsuleCollider2D>();
    }

    private bool IsVerticalIntent(Vector2 input)
    {
        return Mathf.Abs(input.y) > inputDeadZone
            && Mathf.Abs(input.y) > Mathf.Abs(input.x) + verticalIntentBias;
    }

    private bool HasMineableCeiling()
    {
        Vector2Int ceilingCell = heroCollision.GetCeilingProbeCell();
        return WorldCellRules.IsMineable(heroCollision.GetTileId(ceilingCell));
    }

    private bool HasMineableBelow()
    {
        Vector2Int footCell = heroCollision.GetFootCell();
        if (WorldCellRules.IsMineable(heroCollision.GetTileId(footCell)))
        {
            return true;
        }

        Vector2Int probeCell = heroCollision.GetGroundProbeCell();
        return probeCell != footCell && WorldCellRules.IsMineable(heroCollision.GetTileId(probeCell));
    }

    private bool HasClearedTopEdge(Vector2Int ladderCell)
    {
        if (heroCollider == null)
        {
            return false;
        }

        float heroBottomY = heroCollider.bounds.min.y;
        float ladderTopY = heroCollision.GetCellTopY(ladderCell.y);
        return heroBottomY >= ladderTopY - topExitReachMargin;
    }

    private bool IsNearUpperMiningReach(Vector2Int ladderCell)
    {
        if (heroCollider == null)
        {
            return false;
        }

        float heroTopY = heroCollider.bounds.max.y;
        float ladderTopY = heroCollision.GetCellTopY(ladderCell.y);
        return heroTopY >= ladderTopY - upperMiningReachMargin;
    }

    private bool IsNearTopSupport(Vector2Int ladderCell)
    {
        if (heroCollider == null)
        {
            return false;
        }

        float heroBottomY = heroCollider.bounds.min.y;
        float ladderTopY = heroCollision.GetCellTopY(ladderCell.y);
        return heroBottomY >= ladderTopY - 0.08f
            && heroBottomY <= ladderTopY + 0.08f;
    }

    private bool HasReachedCeilingStop(int blockedCellY)
    {
        if (heroCollider == null)
        {
            return false;
        }

        float heroTopY = heroCollider.bounds.max.y;
        float blockedCellBottomY = heroCollision.GetCellBottomY(blockedCellY);
        return heroTopY >= blockedCellBottomY - topExitReachMargin;
    }

    private bool IsAtBottomReach(Vector2Int ladderCell)
    {
        if (heroCollider == null)
        {
            return false;
        }

        float heroBottomY = heroCollider.bounds.min.y;
        float ladderBottomY = heroCollision.GetCellBottomY(ladderCell.y);
        return heroBottomY <= ladderBottomY + bottomReachMargin;
    }

    private void EmitLadderBlockedOnce(string directionName, string blockedReason, Vector2Int blockedCell, TileID blockedTile)
    {
        if (hasBlockedLatch
            && lastBlockedDirection == directionName
            && lastBlockedReason == blockedReason
            && lastBlockedCell == blockedCell)
        {
            return;
        }

        hasBlockedLatch = true;
        lastBlockedDirection = directionName;
        lastBlockedReason = blockedReason;
        lastBlockedCell = blockedCell;

        Diag.Warning(
            "Hero",
            "LadderBlocked",
            "Hero ladder movement was blocked.",
            this,
            ("direction", directionName),
            ("reason", blockedReason),
            ("ladderCell", supportLadderCell),
            ("currentCell", heroCollision.GetCurrentCell()),
            ("targetCell", blockedCell),
            ("tile", blockedTile.ToString()),
            ("source", heroController.UsesMovementJoystick ? "movementJoystick" : "keyboardFallback"));
    }

    private void ClearBlockedLatch()
    {
        hasBlockedLatch = false;
        lastBlockedDirection = string.Empty;
        lastBlockedReason = string.Empty;
        lastBlockedCell = Vector2Int.zero;
    }
}
