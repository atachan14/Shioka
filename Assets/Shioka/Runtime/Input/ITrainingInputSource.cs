using UnityEngine;

public struct TrainingInputFrame
{
    public Vector2 Movement;
    public int AttackPressedMask,AttackHeldMask; public bool ForceHeld,RecordPressed,PlayPressed;
    public bool Dash, DashPressed, Jump, Attack, Reset, ToggleOpponentGuard;
    public bool Medium, Heavy, Slash, HeavySlash, Cancel, GuardHeld, Throw, FdHeld, Special;
    public TrainingCommand[] Commands;
}
public interface ITrainingInputSource { TrainingInputFrame ReadInput(); }
