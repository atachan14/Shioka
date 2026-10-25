using UnityEngine;
public sealed partial class TrainingFighter {
 // Existing strike state spans startup, active and recovery. Air attacks have their own complete action clock.
 public bool IsGuardThreatMotion=>CurrentPhase==Phase.Attack||(CurrentPhase==Phase.Air&&airCombatActive&&airTrialClock.IsBusy);
 public int OpponentDirectionForGuard{get{if(opponent==null)return Facing;float delta=opponent.transform.position.x-transform.position.x;return Mathf.Abs(delta)>.0001f?(delta>0?1:-1):Facing;}}
 public bool RelativeBackHeld=>opponent!=null&&opponent.isActiveAndEnabled&&movement.x*OpponentDirectionForGuard<-.1f;
 public bool BackGuardIntent=>RelativeBackHeld&&opponent.IsGuardThreatMotion;
}
