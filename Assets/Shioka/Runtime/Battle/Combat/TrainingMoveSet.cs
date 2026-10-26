using UnityEngine;using System;using System.Collections.Generic;using UnityEngine.Serialization;

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
public sealed class TrainingMoveSet : ScriptableObject,ISerializationCallbackReceiver
{
    public TrainingMoveReference[] catalog=new TrainingMoveReference[0];
    [SerializeField,HideInInspector,FormerlySerializedAs("moves")]TrainingMove[] legacyMoves=new TrainingMove[0];
    [NonSerialized]TrainingMove[] cachedMoves,overrideMoves;
    [NonSerialized]Dictionary<string,TrainingMove> resolved;
    public TrainingMove[] moves {get{if(overrideMoves!=null)return overrideMoves;bool any=false;foreach(var r in catalog)if(r!=null&&r.role.StartsWith("move:")){any=true;break;}if(!any)return legacyMoves??new TrainingMove[0];if(Application.isPlaying&&cachedMoves!=null)return cachedMoves;var list=new List<TrainingMove>();foreach(var r in catalog)if(r!=null&&r.role.StartsWith("move:")&&r.asset!=null)list.Add(GetRole(r.role,null));var value=list.ToArray();if(Application.isPlaying)cachedMoves=value;return value;}set{if(Application.isPlaying)overrideMoves=value;else if(catalog.Length==0)legacyMoves=value;else throw new InvalidOperationException("Edit individual skill assets, not catalog values");}}
    TrainingMove GetRole(string role,TrainingMove fallback){if(resolved!=null&&resolved.TryGetValue(role,out var cached))return cached;foreach(var r in catalog)if(r!=null&&r.role==role&&r.asset!=null){if(!Application.isPlaying)return r.asset.data;var copy=JsonUtility.FromJson<TrainingMove>(JsonUtility.ToJson(r.asset.data));if(resolved==null)resolved=new Dictionary<string,TrainingMove>();resolved[role]=copy;return copy;}return fallback;}
    void SetRole(string role,TrainingMove value){if(Application.isPlaying){if(resolved==null)resolved=new Dictionary<string,TrainingMove>();resolved[role]=value;return;}throw new InvalidOperationException("Set compatibility data only before catalog adoption");}
    public UnityEngine.Object PersistentOwner(TrainingMove move){foreach(var r in catalog)if(r?.asset!=null&&ReferenceEquals(r.asset.data,move))return r.asset;return this;}
    public void OnBeforeSerialize(){}public void OnAfterDeserialize(){cachedMoves=overrideMoves=null;resolved=null;}
    void OnValidate(){OnAfterDeserialize();}

    [SerializeField,HideInInspector,FormerlySerializedAs("sweep")]TrainingMove legacy_sweep;
    public TrainingMove sweep {get=>GetRole("sweep",legacy_sweep);set{if(!Application.isPlaying&&catalog.Length==0)legacy_sweep=value;else SetRole("sweep",value);}}
    [SerializeField,HideInInspector,FormerlySerializedAs("special")]TrainingMove legacy_special;
    public TrainingMove special {get=>GetRole("special",legacy_special);set{if(!Application.isPlaying&&catalog.Length==0)legacy_special=value;else SetRole("special",value);}}
    public TrainingThrowSettings throwSettings;
    public TrainingFdSettings fdSettings;
    public AnimationClip knockdownClip, getUpBackClip;
    public float knockdownSeconds = .35f, downHoldSeconds = .55f, getUpSeconds = .9f;
    public bool allowDownFollowup;
    public int maxDownFollowups = 1;
    public float downFollowupDamageScale = .25f;
    public TrainingAirAttackCandidate flyingKick, dropKick, airStrongKick, airDownStrongKick, airStrongPunch;

    [SerializeField,HideInInspector,FormerlySerializedAs("airStrongKickMove")]TrainingMove legacy_airStrongKickMove;
    public TrainingMove airStrongKickMove {get=>GetRole("airStrongKickMove",legacy_airStrongKickMove);set{if(!Application.isPlaying&&catalog.Length==0)legacy_airStrongKickMove=value;else SetRole("airStrongKickMove",value);}}
    [SerializeField,HideInInspector,FormerlySerializedAs("airDownStrongKickMove")]TrainingMove legacy_airDownStrongKickMove;
    public TrainingMove airDownStrongKickMove {get=>GetRole("airDownStrongKickMove",legacy_airDownStrongKickMove);set{if(!Application.isPlaying&&catalog.Length==0)legacy_airDownStrongKickMove=value;else SetRole("airDownStrongKickMove",value);}}
    [SerializeField,HideInInspector,FormerlySerializedAs("airStrongPunchMove")]TrainingMove legacy_airStrongPunchMove;
    public TrainingMove airStrongPunchMove {get=>GetRole("airStrongPunchMove",legacy_airStrongPunchMove);set{if(!Application.isPlaying&&catalog.Length==0)legacy_airStrongPunchMove=value;else SetRole("airStrongPunchMove",value);}}

    [SerializeField,HideInInspector,FormerlySerializedAs("special2")]TrainingMove legacy_special2;
    public TrainingMove special2 {get=>GetRole("special2",legacy_special2);set{if(!Application.isPlaying&&catalog.Length==0)legacy_special2=value;else SetRole("special2",value);}}
    [SerializeField,HideInInspector,FormerlySerializedAs("forwardSpecial1")]TrainingMove legacy_forwardSpecial1;
    public TrainingMove forwardSpecial1 {get=>GetRole("forwardSpecial1",legacy_forwardSpecial1);set{if(!Application.isPlaying&&catalog.Length==0)legacy_forwardSpecial1=value;else SetRole("forwardSpecial1",value);}}
    [SerializeField,HideInInspector,FormerlySerializedAs("airSpecial2Move")]TrainingMove legacy_airSpecial2Move;
    public TrainingMove airSpecial2Move {get=>GetRole("airSpecial2Move",legacy_airSpecial2Move);set{if(!Application.isPlaying&&catalog.Length==0)legacy_airSpecial2Move=value;else SetRole("airSpecial2Move",value);}}
    public TrainingAirAttackCandidate airSpecial2;
    public float receiverGain = 4f, dashGainPerSecond = 12f;
    public float tensionLevelGainRate = .1f;
    public void ClearAdoptedLegacy(){legacyMoves=new TrainingMove[0];legacy_sweep=null;legacy_special=null;legacy_airStrongKickMove=null;legacy_airDownStrongKickMove=null;legacy_airStrongPunchMove=null;legacy_special2=null;legacy_forwardSpecial1=null;legacy_airSpecial2Move=null;OnAfterDeserialize();}
    public TrainingMove Find(TrainingCommand command) { if (command == TrainingCommand.Special) return special; if(command==TrainingCommand.Special2)return special2; if(command==TrainingCommand.ForwardSpecial1)return forwardSpecial1; if (command == TrainingCommand.Sweep) return sweep; foreach (var move in moves) if (move.command == command) return move; return null; }
}
