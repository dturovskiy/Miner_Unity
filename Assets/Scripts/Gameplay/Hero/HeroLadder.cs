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
    private enum LadderVerticalActionType
    {
        None,
        Climb,
        Mine
    }

    private readonly struct LadderVerticalAction
    {
        public LadderVerticalAction(LadderVerticalActionType type, Vector2Int ladderCell, Vector2Int targetCell)
        {
            Type = type;
            LadderCell = ladderCell;
            TargetCell = targetCell;
        }

        public LadderVerticalActionType Type { get; }
        public Vector2Int LadderCell { get; }
        public Vector2Int TargetCell { get; }
    }

    [Header("References")]
    [SerializeField] private HeroController heroController;
    [SerializeField] private HeroState heroState;
    [SerializeField] private HeroCollision heroCollision;
    [SerializeField] private HeroMotor heroMotor;
    [SerializeField] private CapsuleCollider2D heroCollider;
    [SerializeField] private HeroMining heroMining;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float climbSpeed = 3f;
    [SerializeField, Min(0f)] private float inputDeadZone = 0.35f;
    [SerializeField, Min(0f)] private float verticalIntentBias = 0.15f;
    [SerializeField, Min(0f)] private float verticalMiningReachMargin = 0.04f;

    private bool isClimbing;
    private bool hasLadderSupport;
    private Vector2Int supportLadderCell;
    private bool hasResolvedVerticalAction;
    private LadderVerticalAction resolvedVerticalAction;

    public bool IsOnLadder => isClimbing;
    public bool HasPassiveSupport => hasLadderSupport;
    public bool ShouldSuppressOtherMovement
    {
        get
        {
            if (heroController == null || heroCollision == null || !heroCollision.IsWorldReady())
            {
                return false;
            }

            return IsVerticalIntent(heroController.MovementInput)
                && hasResolvedVerticalAction
                && resolvedVerticalAction.Type == LadderVerticalActionType.Climb;
        }
    }

    private void Reset()
    {
        heroController = GetComponent<HeroController>();
        heroState = GetComponent<HeroState>();
        heroCollision = GetComponent<HeroCollision>();
        heroMotor = GetComponent<HeroMotor>();
        heroCollider = GetComponent<CapsuleCollider2D>();
        heroMining = GetComponent<HeroMining>();
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
        if (heroCollision == null || !heroCollision.IsWorldReady())
        {
            return false;
        }

        return IsVerticalIntent(input)
            && hasResolvedVerticalAction
            && resolvedVerticalAction.Type == LadderVerticalActionType.Climb;
    }

    public bool HasVerticalContext(Vector2 input)
    {
        if (heroCollision == null || !heroCollision.IsWorldReady() || !IsVerticalIntent(input))
        {
            return false;
        }

        return hasLadderSupport;
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

    public bool TryGetVerticalMiningTarget(Vector2 input, out Vector2Int targetCell)
    {
        targetCell = Vector2Int.zero;
        if (!IsVerticalIntent(input) || !hasResolvedVerticalAction || resolvedVerticalAction.Type != LadderVerticalActionType.Mine)
        {
            return false;
        }

        targetCell = resolvedVerticalAction.TargetCell;
        return true;
    }

    private void RunLadderLoop()
    {
        if (heroController == null || heroState == null || heroCollision == null || heroMotor == null)
        {
            return;
        }

        if (!heroCollision.IsWorldReady())
        {
            ReleaseAllLadderState();
            return;
        }

        Vector2 input = heroController.MovementInput;
        hasLadderSupport = TryResolveSupportLadderCell(out supportLadderCell);

        if (!hasLadderSupport)
        {
            hasResolvedVerticalAction = false;
            resolvedVerticalAction = default;
            ReleaseAllLadderState();
            return;
        }

        hasResolvedVerticalAction = TryResolveVerticalActionInternal(input, supportLadderCell, out resolvedVerticalAction);
        LadderVerticalAction action = resolvedVerticalAction;

        if (hasResolvedVerticalAction && action.Type == LadderVerticalActionType.Climb)
        {
            if (!isClimbing)
            {
                EnterClimb(action.LadderCell, input.y >= 0f ? "Up" : "Down");
            }

            heroMotor.SetVerticalAttachment(true, false);
            SnapToLadderColumn(action.LadderCell);
            heroMotor.ApplyLadderVelocity(input.y, climbSpeed);
            return;
        }

        if (isClimbing)
        {
            string reason = action.Type == LadderVerticalActionType.Mine
                ? "MiningPriority"
                : (IsHorizontalIntent(input) ? "HorizontalReleased" : "Released");
            ExitClimb(reason);
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

    private void ExitClimb(string reason)
    {
        Vector2Int ladderCell = supportLadderCell;
        isClimbing = false;
        heroState.SetTraversalModeSilently(HeroTraversalMode.Ground);
        heroState.SetLocomotionSilently(HeroLocomotionState.Idle);
        heroMotor.SetVerticalAttachment(false, false);

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
        hasResolvedVerticalAction = false;
        resolvedVerticalAction = default;
        if (isClimbing)
        {
            ExitClimb("LostContact");
        }
        else
        {
            heroMotor.SetVerticalAttachment(false, false);
            heroState.SetTraversalModeSilently(HeroTraversalMode.Ground);
        }

        hasLadderSupport = false;
        supportLadderCell = Vector2Int.zero;
    }

    private void EnterPassiveSupport()
    {
        isClimbing = false;
        heroState.SetTraversalModeSilently(HeroTraversalMode.Ground);
        heroMotor.SetVerticalAttachment(true, true);
    }

    private bool TryResolveSupportLadderCell(out Vector2Int ladderCell)
    {
        ladderCell = Vector2Int.zero;
        if (heroCollision == null || !heroCollision.IsWorldReady())
        {
            return false;
        }

        Vector2Int currentCell = heroCollision.GetCurrentCell();
        if (heroCollision.IsClimbableCell(currentCell))
        {
            ladderCell = currentCell;
            return true;
        }

        Vector2Int footCell = heroCollision.GetFootCell();
        if (heroCollision.IsClimbableCell(footCell))
        {
            ladderCell = footCell;
            return true;
        }

        return false;
    }

    private bool TryResolveVerticalActionInternal(Vector2 input, Vector2Int ladderCell, out LadderVerticalAction action)
    {
        action = new LadderVerticalAction(LadderVerticalActionType.None, Vector2Int.zero, Vector2Int.zero);
        if (heroCollision == null || !heroCollision.IsWorldReady() || !IsVerticalIntent(input))
        {
            return false;
        }

        if (heroMining != null && heroMining.TryGetLockedVerticalTarget(input, out Vector2Int lockedTargetCell))
        {
            action = new LadderVerticalAction(LadderVerticalActionType.Mine, ladderCell, lockedTargetCell);
            return true;
        }

        Vector2Int direction = input.y > 0f ? Vector2Int.up : Vector2Int.down;
        Vector2Int targetCell = ladderCell + direction;
        TileID targetTile = heroCollision.GetTileId(targetCell);
        bool nearMiningReach = input.y > 0f
            ? IsNearTopMiningReach(ladderCell)
            : IsNearBottomMiningReach(ladderCell);

        if (WorldCellRules.IsMineable(targetTile))
        {
            action = new LadderVerticalAction(
                nearMiningReach ? LadderVerticalActionType.Mine : LadderVerticalActionType.Climb,
                ladderCell,
                targetCell);
            return true;
        }

        if (heroCollision.IsClimbableCell(targetCell) || WorldCellRules.IsPassable(targetTile))
        {
            action = new LadderVerticalAction(LadderVerticalActionType.Climb, ladderCell, targetCell);
            return true;
        }

        return false;
    }

    private void SnapToLadderColumn(Vector2Int ladderCell)
    {
        Vector2 currentPosition = heroMotor.Rigidbody.position;
        float targetX = heroCollision.CellToWorldCenter(new Vector2Int(ladderCell.x, 0)).x;
        heroMotor.SnapToPosition(new Vector2(targetX, currentPosition.y));
    }

    private void ResolveReferences()
    {
        heroController ??= GetComponent<HeroController>();
        heroState ??= GetComponent<HeroState>();
        heroCollision ??= GetComponent<HeroCollision>();
        heroMotor ??= GetComponent<HeroMotor>();
        heroCollider ??= GetComponent<CapsuleCollider2D>();
        heroMining ??= GetComponent<HeroMining>();
    }

    private bool IsVerticalIntent(Vector2 input)
    {
        return Mathf.Abs(input.y) > inputDeadZone
            && Mathf.Abs(input.y) > Mathf.Abs(input.x) + verticalIntentBias;
    }

    private bool IsHorizontalIntent(Vector2 input)
    {
        return Mathf.Abs(input.x) > inputDeadZone
            && Mathf.Abs(input.x) >= Mathf.Abs(input.y) + verticalIntentBias;
    }

    private bool IsNearTopMiningReach(Vector2Int ladderCell)
    {
        if (heroCollider == null)
        {
            return false;
        }

        float heroBottomY = heroCollider.bounds.min.y;
        float ladderTopY = heroCollision.GetCellTopY(ladderCell.y);
        return heroBottomY >= ladderTopY - verticalMiningReachMargin;
    }

    private bool IsNearBottomMiningReach(Vector2Int ladderCell)
    {
        if (heroCollider == null)
        {
            return false;
        }

        float heroBottomY = heroCollider.bounds.min.y;
        float ladderBottomY = heroCollision.GetCellBottomY(ladderCell.y);
        return heroBottomY <= ladderBottomY + verticalMiningReachMargin;
    }
}
