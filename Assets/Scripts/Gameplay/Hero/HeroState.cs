using UnityEngine;

public enum HeroLocomotionState
{
    Idle,
    Moving,
    Falling
}

public sealed class HeroState : MonoBehaviour
{
    [SerializeField] private HeroLocomotionState locomotion = HeroLocomotionState.Idle;

    public HeroLocomotionState Locomotion => locomotion;

    public bool IsGrounded => locomotion != HeroLocomotionState.Falling;
    public bool IsMoving => locomotion == HeroLocomotionState.Moving;
    public bool IsFalling => locomotion == HeroLocomotionState.Falling;

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
}
