using MinerUnity.Terrain;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(HeroMotor))]
[RequireComponent(typeof(HeroGroundSensor))]
[RequireComponent(typeof(HeroWallSensor))]
[RequireComponent(typeof(HeroCollision))]
[RequireComponent(typeof(HeroState))]
public sealed class HeroGroundCore : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HeroMotor motor;
    [SerializeField] private HeroGroundSensor groundSensor;
    [SerializeField] private HeroWallSensor wallSensor;
    [SerializeField] private HeroCollision collision;
    [SerializeField] private HeroState state;

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
    }

    private void Awake()
    {
        motor ??= GetComponent<HeroMotor>();
        groundSensor ??= GetComponent<HeroGroundSensor>();
        wallSensor ??= GetComponent<HeroWallSensor>();
        collision ??= GetComponent<HeroCollision>();
        state ??= GetComponent<HeroState>();

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

        motor.ConfigureBody();
    }

    private void FixedUpdate()
    {
        if (!IsGameplayLoopReady())
        {
            ResetBootstrapState();
            return;
        }

        if (!EnsureBootstrappedState())
        {
            return;
        }

        bool grounded = groundSensor.TryGetGroundHit(out Collider2D groundHit);
        if (grounded != wasGrounded)
        {
            Vector2Int supportCell = Vector2Int.zero;
            TileID supportTileId = TileID.Empty;
            WorldCellType supportCellType = WorldCellType.Empty;
            bool hasSupportInfo = grounded
                && collision.TryDescribeCollider(groundHit, out supportCell, out supportTileId, out supportCellType);

            Diag.Event(
                "Hero",
                "GroundedChanged",
                grounded ? "Hero is grounded." : "Hero left ground.",
                this,
                ("grounded", grounded),
                ("velocityY", CurrentSpeedY),
                ("groundSupportObject", grounded ? groundHit.name : "None"),
                ("groundSupportLayer", grounded ? LayerMask.LayerToName(groundHit.gameObject.layer) : "None"),
                ("groundSupportCell", hasSupportInfo ? supportCell.ToString() : "None"),
                ("groundSupportTile", hasSupportInfo ? supportTileId.ToString() : "None"),
                ("groundSupportType", hasSupportInfo ? supportCellType.ToString() : "None"),
                ("groundProbeCell", collision.GetFootCell().ToString()),
                ("groundProbeTile", collision.GetFootTileId().ToString()),
                ("groundProbeType", collision.GetFootCellType().ToString()));

            if (grounded)
            {
                Diag.Event("Hero", "Landed", null, this, ("velocityY", CurrentSpeedY));
            }
            else
            {
                Diag.Event("Hero", "FallStarted", null, this, ("velocityY", CurrentSpeedY));
            }

            wasGrounded = grounded;
        }

        bool blocked = wallSensor.TryGetHorizontalBlockHit(desiredHorizontalInput, out RaycastHit2D blockHit);
        if (blocked && logBlockedMovement && Mathf.Abs(desiredHorizontalInput) > inputDeadZone && !wasBlocked)
        {
            Vector2Int blockerCell = Vector2Int.zero;
            TileID blockerTileId = TileID.Empty;
            WorldCellType blockerCellType = WorldCellType.Empty;
            bool hasBlockerInfo = collision.TryDescribeCollider(
                blockHit.collider,
                out blockerCell,
                out blockerTileId,
                out blockerCellType);

            Diag.Warning(
                "Hero",
                "MoveBlocked",
                "Horizontal movement was blocked.",
                this,
                ("x", desiredHorizontalInput),
                ("blocker", blockHit.collider != null ? blockHit.collider.name : "None"),
                ("blockerLayer", blockHit.collider != null ? LayerMask.LayerToName(blockHit.collider.gameObject.layer) : "None"),
                ("blockerCell", hasBlockerInfo ? blockerCell.ToString() : "None"),
                ("blockerTile", hasBlockerInfo ? blockerTileId.ToString() : "None"),
                ("blockerType", hasBlockerInfo ? blockerCellType.ToString() : "None"),
                ("blockDistance", blockHit.distance),
                ("wallProbeCell", collision.GetFootCell().ToString()),
                ("wallProbeTile", collision.GetFootTileId().ToString()),
                ("wallProbeType", collision.GetFootCellType().ToString()));
        }

        wasBlocked = blocked;

        float velocityX = motor.ApplyHorizontalMovement(desiredHorizontalInput, blocked, inputDeadZone, moveSpeed);
        motor.UpdateFacing(desiredHorizontalInput, inputDeadZone, flipSpriteByScale);
        UpdateState(grounded, Mathf.Abs(velocityX) > 0.01f);
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

        bool grounded = groundSensor.TryGetGroundHit(out _);
        bool blocked = wallSensor.TryGetHorizontalBlockHit(desiredHorizontalInput, out _);
        wasGrounded = grounded;
        wasBlocked = blocked;
        state.SetLocomotionSilently(ResolveLocomotionState(grounded, Mathf.Abs(desiredHorizontalInput) > inputDeadZone && !blocked));
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
}
