using UnityEngine;

// Orthogonal action time only. This class cannot move or change a fighter's motor.
public sealed class TrainingAirActionClock
{
    public enum State { Idle, Running, Held, Finished }
    public enum Pause { None, Hitstop, Stun, Landing }
    public enum Finish { None, Completed, Cancelled, Landing, Reset }
    public enum LandingChoice { Undecided, End, RecoveryOnly }
    public State CurrentState { get; private set; }
    public Pause PauseReason { get; private set; }
    public Finish FinishReason { get; private set; }
    public float Elapsed { get; private set; }
    public float Duration { get; private set; }
    public bool NeedsLandingDecision { get; private set; }
    public bool GroundRecoveryAllowed { get; private set; }
    public bool IsBusy => CurrentState == State.Running || CurrentState == State.Held;
    public float Normalized => Duration > 0f ? Mathf.Clamp01(Elapsed / Duration) : 0f;
    public bool InActiveWindow => IsBusy && Elapsed >= startup && Elapsed < startup + active;
    public bool CanApplyHit => CurrentState == State.Running && InActiveWindow;
    private float startup, active;
    public bool TryBegin(float duration, float startupSeconds, float activeSeconds)
    {
        if (IsBusy || !Finite(duration) || !Finite(startupSeconds) || !Finite(activeSeconds) || duration <= 0f || startupSeconds < 0f || activeSeconds < 0f || startupSeconds + activeSeconds > duration) return false;
        Duration = duration; startup = startupSeconds; active = activeSeconds; Elapsed = 0f;
        NeedsLandingDecision = GroundRecoveryAllowed = false;
        CurrentState = State.Running; PauseReason = Pause.None; FinishReason = Finish.None;
        return true;
    }
    public void Hold(Pause reason)
    {
        if (!IsBusy) return;
        if (reason == Pause.Landing) NeedsLandingDecision = true;
        CurrentState = State.Held;
        PauseReason = NeedsLandingDecision ? Pause.Landing : reason;
    }
    public void Advance(float seconds)
    {
        if (!IsBusy || NeedsLandingDecision || !Finite(seconds) || seconds <= 0f) return;
        CurrentState = State.Running; PauseReason = Pause.None;
        Elapsed = Mathf.Min(Duration, Elapsed + seconds);
        if (Elapsed >= Duration - 0.000001f) { Elapsed = Duration; End(Finish.Completed); }
    }
    public bool ResolveLanding(LandingChoice choice)
    {
        if (!NeedsLandingDecision || choice == LandingChoice.Undecided) return false;
        if (choice == LandingChoice.End) { End(Finish.Landing); return true; }
        // Probe recovery without allowing any remaining damaging window on the floor.
        NeedsLandingDecision = false; GroundRecoveryAllowed = true;
        Elapsed = Mathf.Max(Elapsed, startup + active);
        CurrentState = State.Running; PauseReason = Pause.None;
        if (Elapsed >= Duration) End(Finish.Completed);
        return true;
    }
    public void End(Finish reason = Finish.Cancelled)
    {
        CurrentState = State.Finished; PauseReason = Pause.None;
        NeedsLandingDecision = GroundRecoveryAllowed = false; FinishReason = reason;
    }
    public void Reset()
    {
        CurrentState = State.Idle; PauseReason = Pause.None; FinishReason = Finish.Reset;
        Elapsed = Duration = startup = active = 0f;
        NeedsLandingDecision = GroundRecoveryAllowed = false;
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
