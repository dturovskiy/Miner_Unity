using MinerUnity.Terrain;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(HeroMotor))]
[RequireComponent(typeof(HeroGroundSensor))]
[RequireComponent(typeof(HeroWallSensor))]
[RequireComponent(typeof(HeroCollision))]
[RequireComponent(typeof(HeroState))]
[RequireComponent(typeof(HeroLadder))]
public sealed class HeroGroundCore : MonoBehaviour
{
    private readonly struct GroundEvaluation
    {
        public GroundEvaluation(
            bool isGrounded,
            Collider2D supportCollider,
            bool hasSupportInfo,
            Vector2Int supportCell,
            TileID supportTileId,
            WorldCellType supportCellType,
            Vector2Int probeCell,
            TileID probeTileId,
            WorldCellType probeCellType)
        {
            IsGrounded = isGrounded;
            SupportCollider = supportCollider;
            HasSupportInfo = hasSupportInfo;
            SupportCell = supportCell;
            SupportTileId = supportTileId;
            SupportCellType = supportCellType;
            ProbeCell = probeCell;
            ProbeTileId = probeTileId;
            ProbeCellType = probeCellType;
        }

        public bool IsGrounded { get; }
        public Collider2D SupportCollider { get; }
        public bool HasSupportInfo { get; }
        public Vector2Int SupportCell { get; }
        public TileID SupportTileId { get; }
        public WorldCellType SupportCellType { get; }
        public Vector2Int ProbeCell { get; }
        public TileID ProbeTileId { get; }
        public WorldCellType ProbeCellType { get; }
    }

    private readonly struct WallEvaluation
    {
        public WallEvaluation(
            bool isBlocked,
            Collider2D blockerCollider,
            bool hasBlockerInfo,
            Vector2Int blockerCell,
            TileID blockerTileId,
            WorldCellType blockerCellType,
            float blockDistance,
            Vector2Int probeCell,
            TileID probeTileId,
            WorldCellType probeCellType)
        {
            IsBlocked = isBlocked;
            BlockerCollider = blockerCollider;
            HasBlockerInfo = hasBlockerInfo;
            BlockerCell = blockerCell;
            BlockerTileId = blockerTileId;
            BlockerCellType = blockerCellType;
            BlockDistance = blockDistance;
            ProbeCell = probeCell;
            ProbeTileId = probeTileId;
            ProbeCellType = probeCellType;
        }

        public bool IsBlocked { get; }
        public Collider2D BlockerCollider { get; }
        public bool HasBlockerInfo { get; }
        public Vector2Int BlockerCell { get; }
        public TileID BlockerTileId { get; }
        public WorldCellType BlockerCellType { get; }
        public float BlockDistance { get; }
        public Vector2Int ProbeCell { get; }
        public TileID ProbeTileId { get; }
        public WorldCellType ProbeCellType { get; }
    }

    [Header("References")]
    [SerializeField] private HeroMotor motor;
    [SerializeField] private HeroGroundSensor groundSensor;
    [SerializeField] private HeroWallSensor wallSensor;
    [SerializeField] private HeroCollision collision;
    [SerializeField] private HeroState state;
    [SerializeField] private HeroLadder ladder;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float inputDeadZone = 0.15f;
    [SerializeField] private bool flipSpriteByScale = true;

    [Header("Diagnostics")]
    [SerializeField] private bool logBlockedMovement = true;
    [SerializeField, Min(0)] private int fixedFramesToWaitAfterWorldReady = 1;

    private float desiredHorizontalInput;
    private bool wasGrounded;
    private bool wasBlocked;
    private bool locomotionBootstrapApplied;
    private int readyFixedFrames;

    public float DesiredHorizontalInput => desiredHorizontalInput;
    public float CurrentSpeedX => motor != null ? motor.CurrentSpeedX : 0f;
    public float CurrentSpeedY => motor != null ? motor.CurrentSpeedY : 0f;

    private void Reset()
    {
        motor = GetComponent<HeroMotor>();
        groundSensor = GetComponent<HeroGroundSensor>();
        wallSensor = GetComponent<HeroWallSensor>();
        collision = GetComponent<HeroCollision>();
        state = GetComponent<HeroState>();
        ladder = GetComponent<HeroLadder>();
    }

    private void Awake()
    {
        motor ??= GetComponent<HeroMotor>();
        groundSensor ??= GetComponent<HeroGroundSensor>();
        wallSensor ??= GetComponent<HeroWallSensor>();
        collision ??= GetComponent<HeroCollision>();
        state ??= GetComponent<HeroState>();
        ladder ??= GetComponent<HeroLadder>();

        if (motor == null)
        {
            motor = gameObject.AddComponent<HeroMotor>();
        }

        if (groundSensor == null)
        {
            groundSensor = gameObject.AddComponent<HeroGroundSensor>();
        }

        if (wallSensor == null)
        {
            wallSensor = gameObject.AddComponent<HeroWallSensor>();
        }

        if (collision == null)
        {
            collision = gameObject.AddComponent<HeroCollision>();
        }

        if (state == null)
        {
            state = gameObject.AddComponent<HeroState>();
        }

        if (ladder == null)
        {
            ladder = gameObject.AddComponent<HeroLadder>();
        }

        motor.ConfigureBody();
    }

    private void FixedUpdate()
    {
        if (ladder != null && ladder.ShouldSuppressOtherMovement)
        {
            state.SetLocomotionSilently(HeroLocomotionState.Idle);
            ResetBootstrapState();
            return;
        }

        if (!IsGameplayLoopReady())
        {
            ResetBootstrapState();
            return;
        }

        if (!EnsureBootstrappedState())
        {
            return;
        }

        GroundEvaluation ground = EvaluateGround();
        if (ground.IsGrounded != wasGrounded)
        {
            EmitGroundedDiagnostics(ground);
            wasGrounded = ground.IsGrounded;
        }

        WallEvaluation wall = EvaluateWall();
        if (wall.IsBlocked && logBlockedMovement && Mathf.Abs(desiredHorizontalInput) > inputDeadZone && !wasBlocked)
        {
            EmitBlockedDiagnostics(wall);
        }

        wasBlocked = wall.IsBlocked;

        float velocityX = motor.ApplyHorizontalMovement(desiredHorizontalInput, wall.IsBlocked, inputDeadZone, moveSpeed);
        motor.UpdateFacing(desiredHorizontalInput, inputDeadZone, flipSpriteByScale);
        UpdateState(ground.IsGrounded, Mathf.Abs(velocityX) > 0.01f);
    }

    public void SetDesiredHorizontalInput(float input)
    {
        desiredHorizontalInput = Mathf.Clamp(input, -1f, 1f);
    }

    public void ConfigureMovement(float speed, float deadZone, bool flipByScale)
    {
        moveSpeed = Mathf.Max(0f, speed);
        inputDeadZone = Mathf.Max(0f, deadZone);
        flipSpriteByScale = flipByScale;
    }

    public void ConfigureDiagnostics(bool shouldLogBlockedMovement, int bootstrapFramesToWait)
    {
        logBlockedMovement = shouldLogBlockedMovement;
        fixedFramesToWaitAfterWorldReady = Mathf.Max(0, bootstrapFramesToWait);
    }

    private bool IsGameplayLoopReady()
    {
        if (motor == null || groundSensor == null || wallSensor == null || collision == null || state == null)
        {
            return false;
        }

        return collision.IsWorldReady();
    }

    private bool EnsureBootstrappedState()
    {
        if (locomotionBootstrapApplied)
        {
            return true;
        }

        readyFixedFrames++;
        if (readyFixedFrames <= fixedFramesToWaitAfterWorldReady)
        {
            return false;
        }

        GroundEvaluation ground = EvaluateGround();
        WallEvaluation wall = EvaluateWall();
        wasGrounded = ground.IsGrounded;
        wasBlocked = wall.IsBlocked;
        state.SetLocomotionSilently(ResolveLocomotionState(
            ground.IsGrounded,
            Mathf.Abs(desiredHorizontalInput) > inputDeadZone && !wall.IsBlocked));
        locomotionBootstrapApplied = true;
        return true;
    }

    private void ResetBootstrapState()
    {
        locomotionBootstrapApplied = false;
        readyFixedFrames = 0;
        wasGrounded = false;
        wasBlocked = false;
    }

    private void UpdateState(bool grounded, bool moving)
    {
        state.SetLocomotion(ResolveLocomotionState(grounded, moving));
    }

    private static HeroLocomotionState ResolveLocomotionState(bool grounded, bool moving)
    {
        if (!grounded)
        {
            return HeroLocomotionState.Falling;
        }

        if (moving)
        {
            return HeroLocomotionState.Moving;
        }

        return HeroLocomotionState.Idle;
    }

    private GroundEvaluation EvaluateGround()
    {
        bool grounded = groundSensor.TryGetGroundHit(out Collider2D groundHit);
        Vector2Int probeCell = collision.GetGroundProbeCell();
        TileID probeTileId = collision.GetGroundProbeTileId();
        WorldCellType probeCellType = collision.GetGroundProbeCellType();

        Vector2Int supportCell = Vector2Int.zero;
        TileID supportTileId = TileID.Empty;
        WorldCellType supportCellType = WorldCellType.Empty;
        bool hasSupportInfo = grounded
            && collision.TryDescribeCollider(groundHit, out supportCell, out supportTileId, out supportCellType);

        if (!grounded
            && ladder != null
            && ladder.TryGetPassiveSupportInfo(out supportCell, out supportTileId, out supportCellType))
        {
            grounded = true;
            hasSupportInfo = true;
        }

        return new GroundEvaluation(
            grounded,
            groundHit,
            hasSupportInfo,
            supportCell,
            supportTileId,
            supportCellType,
            probeCell,
            probeTileId,
            probeCellType);
    }

    private WallEvaluation EvaluateWall()
    {
        bool blocked = wallSensor.TryGetHorizontalBlockHit(desiredHorizontalInput, out RaycastHit2D blockHit);
        Vector2Int probeCell = collision.GetWallProbeCell(desiredHorizontalInput);
        TileID probeTileId = collision.GetWallProbeTileId(desiredHorizontalInput);
        WorldCellType probeCellType = collision.GetWallProbeCellType(desiredHorizontalInput);

        Vector2Int blockerCell = Vector2Int.zero;
        TileID blockerTileId = TileID.Empty;
        WorldCellType blockerCellType = WorldCellType.Empty;
        bool hasBlockerInfo = blocked
            && collision.TryDescribeCollider(blockHit.collider, out blockerCell, out blockerTileId, out blockerCellType);

        return new WallEvaluation(
            blocked,
            blockHit.collider,
            hasBlockerInfo,
            blockerCell,
            blockerTileId,
            blockerCellType,
            blockHit.distance,
            probeCell,
            probeTileId,
            probeCellType);
    }

    private void EmitGroundedDiagnostics(GroundEvaluation ground)
    {
        Diag.Event(
            "Hero",
            "GroundedChanged",
            ground.IsGrounded ? "Hero is grounded." : "Hero left ground.",
            this,
            ("grounded", ground.IsGrounded),
            ("velocityY", CurrentSpeedY),
            ("groundSupportObject", ground.IsGrounded && ground.SupportCollider != null ? ground.SupportCollider.name : "None"),
            ("groundSupportLayer", ground.IsGrounded && ground.SupportCollider != null ? LayerMask.LayerToName(ground.SupportCollider.gameObject.layer) : "None"),
            ("groundSupportCell", ground.HasSupportInfo ? ground.SupportCell.ToString() : "None"),
            ("groundSupportTile", ground.HasSupportInfo ? ground.SupportTileId.ToString() : "None"),
            ("groundSupportType", ground.HasSupportInfo ? ground.SupportCellType.ToString() : "None"),
            ("groundProbeCell", ground.ProbeCell.ToString()),
            ("groundProbeTile", ground.ProbeTileId.ToString()),
            ("groundProbeType", ground.ProbeCellType.ToString()));

        if (ground.IsGrounded)
        {
            Diag.Event("Hero", "Landed", null, this, ("velocityY", CurrentSpeedY));
        }
        else
        {
            Diag.Event("Hero", "FallStarted", null, this, ("velocityY", CurrentSpeedY));
        }
    }

    private void EmitBlockedDiagnostics(WallEvaluation wall)
    {
        Diag.Warning(
            "Hero",
            "MoveBlocked",
            "Horizontal movement was blocked.",
            this,
            ("x", desiredHorizontalInput),
            ("blocker", wall.BlockerCollider != null ? wall.BlockerCollider.name : "None"),
            ("blockerLayer", wall.BlockerCollider != null ? LayerMask.LayerToName(wall.BlockerCollider.gameObject.layer) : "None"),
            ("blockerCell", wall.HasBlockerInfo ? wall.BlockerCell.ToString() : "None"),
            ("blockerTile", wall.HasBlockerInfo ? wall.BlockerTileId.ToString() : "None"),
            ("blockerType", wall.HasBlockerInfo ? wall.BlockerCellType.ToString() : "None"),
            ("blockDistance", wall.BlockDistance),
            ("wallProbeCell", wall.ProbeCell.ToString()),
            ("wallProbeTile", wall.ProbeTileId.ToString()),
            ("wallProbeType", wall.ProbeCellType.ToString()));
    }
}
