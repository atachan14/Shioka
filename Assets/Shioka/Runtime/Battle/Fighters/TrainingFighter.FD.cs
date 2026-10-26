using UnityEngine;

public sealed partial class TrainingFighter
{
    private bool fdHeld;
    public bool FdActive => fdHeld && (CombatV01==null||RelativeBackHeld) && IsGuarding && GuardAvailable && !IsThrowState && !IsThrowLinked;
    public int FdGuardCount { get; private set; }
    public float LastFdCost { get; private set; }
    public float LastFdPushDistance { get; private set; }
    public float LastTdcCost { get; private set; }

    // Trial: held FD modifies a numerically guarded strike, never forces a guard.
    private void ApplyFdGuard(float sourceLevel, TrainingAttackClass attackClass)
    {
        var settings = moveSet == null ? null : moveSet.fdSettings;
        var attacker = opponent;
        if (!FdActive || attackClass == TrainingAttackClass.Throw || settings == null ||
            attacker == null || !attacker.isActiveAndEnabled || attacker.IsThrowLinked) return;
        LastFdCost = Mathf.Max(0f, sourceLevel) * Mathf.Max(0f, settings.costPerAttackLevel);
        Tension.Spend(LastFdCost, moveSet.tensionLevelGainRate);LastFdCost=(float)Tension.LastCost;
        FdGuardCount++;
        float oldGap = Mathf.Abs(transform.position.x - attacker.transform.position.x);
        float away = oldGap > .001f ? Mathf.Sign(attacker.transform.position.x - transform.position.x) : Facing;
        float requested = Mathf.Max(0f, settings.pushDistance);
        float moved = MoveFdActor(attacker, away * requested);
        float remaining = Mathf.Max(0f, requested - Mathf.Abs(moved));
        if (remaining > .001f) MoveFdActor(this, -away * remaining);
        LastFdPushDistance = Mathf.Max(0f, Mathf.Abs(transform.position.x - attacker.transform.position.x) - oldGap);
    }

    // World-Z displacement only; stored movement/gravity and the shared wall snapshot are preserved.
    private static float MoveFdActor(TrainingFighter actor, float displacement)
    {
        if (actor.body == null || !actor.body.enabled) return 0f;
        float before = actor.transform.position.x;
        float low = actor.CameraBounds == null ? TrainingStage.Left : actor.CameraBounds.Left;
        float high = actor.CameraBounds == null ? TrainingStage.Right : actor.CameraBounds.Right;
        float after = actor.CameraBounds==null?Mathf.Clamp(before+displacement,low,high):actor.CameraBounds.LimitPosition(before,before+displacement);
        actor.body.Move(Vector3.right * (after - before));
        return actor.transform.position.x - before;
    }

    private void ResetFd()
    {
        fdHeld = false;
        FdGuardCount = 0;
        LastFdCost = LastFdPushDistance = LastTdcCost = 0f;
    }
}
