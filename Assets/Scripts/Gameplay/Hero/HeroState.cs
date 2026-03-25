using UnityEngine;

public enum HeroLocomotionState
{
    Idle,
    Moving,
    Falling
}

public enum HeroTraversalMode
{
    Ground,
    Ladder
}

public sealed class HeroState : MonoBehaviour
{
    [SerializeField] private HeroLocomotionState locomotion = HeroLocomotionState.Idle;
    [SerializeField] private HeroTraversalMode traversalMode = HeroTraversalMode.Ground;

    public HeroLocomotionState Locomotion => locomotion;
    public HeroTraversalMode TraversalMode => traversalMode;

    public bool IsGrounded => locomotion != HeroLocomotionState.Falling;
    public bool IsMoving => locomotion == HeroLocomotionState.Moving;
    public bool IsFalling => locomotion == HeroLocomotionState.Falling;
    public bool IsOnLadder => traversalMode == HeroTraversalMode.Ladder;

    public bool SetLocomotion(HeroLocomotionState next)
    {
        if (locomotion == next)
        {
            return false;
        }

        HeroLocomotionState previous = locomotion;
        locomotion = next;

        Diag.Event(
            "Hero",
            "StateChanged",
            $"{previous} -> {next}",
            this,
            ("previous", previous.ToString()),
            ("next", next.ToString()));

        return true;
    }

    public void SetLocomotionSilently(HeroLocomotionState next)
    {
        locomotion = next;
    }

    public bool SetTraversalMode(HeroTraversalMode next)
    {
        if (traversalMode == next)
        {
            return false;
        }

        traversalMode = next;
        return true;
    }

    public void SetTraversalModeSilently(HeroTraversalMode next)
    {
        traversalMode = next;
    }
}
