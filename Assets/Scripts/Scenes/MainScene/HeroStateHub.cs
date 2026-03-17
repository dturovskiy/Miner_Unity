using System;
using UnityEngine;

/// <summary>
/// Єдиний контейнер станів героя.
/// Важливо: локомоція і дія розділені.
/// Це прибирає конфлікт типу "герой не може одночасно бути на драбині і майнити".
/// Насправді може: Locomotion = Ladder, Action = None.
/// Або Locomotion = Grounded, Action = Mining.
/// </summary>
public sealed class HeroStateHub : MonoBehaviour
{
    public HeroLocomotionState LocomotionState { get; private set; } = HeroLocomotionState.Grounded;
    public HeroActionState ActionState { get; private set; } = HeroActionState.None;

    public event Action<HeroLocomotionState, HeroLocomotionState> OnLocomotionChanged;
    public event Action<HeroActionState, HeroActionState> OnActionChanged;

    public void SetLocomotion(HeroLocomotionState newState)
    {
        if (LocomotionState == newState)
        {
            return;
        }

        HeroLocomotionState oldState = LocomotionState;
        LocomotionState = newState;
        OnLocomotionChanged?.Invoke(oldState, newState);
    }

    public void SetAction(HeroActionState newState)
    {
        if (ActionState == newState)
        {
            return;
        }

        HeroActionState oldState = ActionState;
        ActionState = newState;
        OnActionChanged?.Invoke(oldState, newState);
    }

    public bool IsBusy()
    {
        return ActionState == HeroActionState.Hurt;
    }

    public bool IsMining()
    {
        return ActionState == HeroActionState.Mining;
    }

    public bool IsOnLadder()
    {
        return LocomotionState == HeroLocomotionState.Ladder;
    }
}
