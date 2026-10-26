using UnityEngine;

public enum TrainingAttackClass { Mid, High, Low, Throw }
[System.Serializable]
public sealed class TrainingMove
{
    public TechniqueCategory category; // Metadata only; never an input button or automatic Mania effect.
    public TrainingMoveSpatialProfile spatialProfile;
    [Min(0)]public float combatDamageMultiplier=1,combatChipMultiplier=.1f,combatPushbackScale=.001f;
    public TrainingCommand command;
    public TrainingAttackStrength strength = new TrainingAttackStrength();
    public TrainingAttackClass attackClass;
    public bool causesKnockdown;
    public string state = "Jab";
    public AnimationClip clip;
    public float startupFrames = 7.2f, activeFrames = 4.8f, recoveryFrames = 13.2f;
    [Min(0f)] public float groundHitKnockbackSpeed;
    public float attackLevel = 60f, damage = 10f, hitGain = 8f;
    public Vector3 center = new Vector3(0f, .95f, .65f);
    public Vector3 halfSize = new Vector3(.32f, .3f, .4f);
    [Range(0f, 1f)] public float contactNormalized = .5f;
    public TrainingCommand[] chains = new TrainingCommand[0];
    // Optional attack-motor-time curves. Constant legacy behavior is the default.
    public bool useLevelCurves;
    public AnimationCurve attackLevelCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    public AnimationCurve defenseLevelCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    public float LevelScale(AnimationCurve curve, float elapsed) => !useLevelCurves || curve == null || curve.length == 0 ? 1f : Mathf.Clamp(curve.Evaluate(Duration <= 0 ? 0f : Mathf.Clamp01(elapsed / Duration)), 0f, 4f);
    public float AttackLevelAt(float elapsed) => attackLevel * LevelScale(attackLevelCurve, elapsed);
    public float DefenseScaleAt(float elapsed) => LevelScale(defenseLevelCurve, elapsed);
    // Opt-in recovery-time discount; never grants cancellation or invulnerability.
    public bool useTimedTdcRecovery;
    public AnimationCurve timedTdcRecoveryCostCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    public float TimedTdcRecoveryScaleAt(float elapsed)
    {
        if(!useTimedTdcRecovery || timedTdcRecoveryCostCurve==null || timedTdcRecoveryCostCurve.length==0 || recoveryFrames<=0 || float.IsNaN(elapsed) || float.IsInfinity(elapsed))return 1f;
        float recovery=elapsed*60f-startupFrames-activeFrames;
        if(recovery<0f || recovery>=recoveryFrames)return 1f;
        float value=timedTdcRecoveryCostCurve.Evaluate(Mathf.Clamp01(recovery/recoveryFrames));
        return float.IsNaN(value)||float.IsInfinity(value)?1f:Mathf.Clamp(value,.25f,1f);
    }
    public float Duration => (startupFrames + activeFrames + recoveryFrames) / 60f;
    public bool CanChain(TrainingCommand next) => System.Array.IndexOf(chains, next) >= 0;
}
[CreateAssetMenu(menuName = "Shioka/Training Move Set")]
public sealed class TrainingMoveSet : ScriptableObject
{
    public TrainingMove[] moves = new TrainingMove[0];
    public TrainingMove sweep;
    public TrainingMove special;
    public TrainingThrowSettings throwSettings;
    public TrainingFdSettings fdSettings;
    public AnimationClip knockdownClip, getUpBackClip;
    public float knockdownSeconds = .35f, downHoldSeconds = .55f, getUpSeconds = .9f;
    public bool allowDownFollowup;
    public int maxDownFollowups = 1;
    public float downFollowupDamageScale = .25f;
    public TrainingAirAttackCandidate flyingKick, dropKick, airStrongKick, airDownStrongKick, airStrongPunch;
    public TrainingMove airStrongKickMove, airDownStrongKickMove, airStrongPunchMove;
    public TrainingMove special2, forwardSpecial1, airSpecial2Move;
    public TrainingAirAttackCandidate airSpecial2;
    public float receiverGain = 4f, dashGainPerSecond = 12f;
    public float tensionLevelGainRate = .1f;
    public TrainingMove Find(TrainingCommand command) { if (command == TrainingCommand.Special) return special; if(command==TrainingCommand.Special2)return special2; if(command==TrainingCommand.ForwardSpecial1)return forwardSpecial1; if (command == TrainingCommand.Sweep) return sweep; foreach (var move in moves) if (move.command == command) return move; return null; }
}
