using UnityEngine;
using System;

public class HeroStateController : MonoBehaviour
{
    public HeroState CurrentState { get; private set; } = HeroState.Normal;
    
    public event Action<HeroState, HeroState> OnStateChanged;

    public void ChangeState(HeroState newState)
    {
        if (CurrentState == newState) return;
        
        HeroState oldState = CurrentState;
        CurrentState = newState;
        
        OnStateChanged?.Invoke(oldState, newState);
    }
}
