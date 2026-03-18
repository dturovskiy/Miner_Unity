using Miner.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class MinerHeroDiagSnapshotProvider : MonoBehaviour, IGameDiagSnapshotProvider
{
    [SerializeField] private HeroInputReader input;
    [SerializeField] private HeroGridMotor gridMotor;
    [SerializeField] private HeroLadderMotor ladderMotor;
    [SerializeField] private HeroSensors sensors;
    [SerializeField] private HeroStateHub stateHub;
    [SerializeField] private Rigidbody2D rb;

    private void Reset()
    {
        input = GetComponent<HeroInputReader>();
        gridMotor = GetComponent<HeroGridMotor>();
        ladderMotor = GetComponent<HeroLadderMotor>();
        sensors = GetComponent<HeroSensors>();
        stateHub = GetComponent<HeroStateHub>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (input == null) input = GetComponent<HeroInputReader>();
        if (gridMotor == null) gridMotor = GetComponent<HeroGridMotor>();
        if (ladderMotor == null) ladderMotor = GetComponent<HeroLadderMotor>();
        if (sensors == null) sensors = GetComponent<HeroSensors>();
        if (stateHub == null) stateHub = GetComponent<HeroStateHub>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    public HeroDiagSnapshot BuildSnapshot()
    {
        bool grounded = gridMotor != null && gridMotor.IsGrounded;
        bool climbing = stateHub != null && stateHub.IsOnLadder();
        bool touchingLadder = false;

        if (sensors != null)
        {
            touchingLadder = sensors.GetLadderAtBody() != null || sensors.GetLadderAtFeet() != null;
        }

        string stateLabel = "Unknown";
        if (stateHub != null)
        {
            stateLabel = stateHub.LocomotionState.ToString();
        }
        else if (climbing)
        {
            stateLabel = "Ladder";
        }
        else if (grounded)
        {
            stateLabel = "Grounded";
        }
        else
        {
            stateLabel = "Airborne";
        }

        return new HeroDiagSnapshot
        {
            state = stateLabel,
            scene = SceneManager.GetActiveScene().name,
            position = transform.position,
            velocity = rb != null ? rb.linearVelocity : Vector2.zero,
            input = new Vector2(input != null ? input.Horizontal : 0f, input != null ? input.Vertical : 0f),
            grounded = grounded,
            touchingLadder = touchingLadder,
            climbing = climbing,
            recentlyGrounded = false,
            recentlyTouchedLadder = false,
            topExitLocked = false,
            climbStartLocked = false,
            gravityScale = rb != null ? rb.gravityScale : 0f
        };
    }
}
