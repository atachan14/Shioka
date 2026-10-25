using System;using UnityEngine;
[Serializable]public sealed class TrainingPoseEntry {public string id;public Sprite sprite;public float offsetY;public bool previewOnly;}
[CreateAssetMenu(menuName="Shioka/Provisional pose set")]
public sealed class TrainingProvisionalPoses:ScriptableObject {
 public TrainingPoseEntry[] entries;public TrainingPoseEntry Find(string id){if(entries!=null)foreach(var e in entries)if(e!=null&&e.id==id)return e;return null;}
 public string StateKey(TrainingFighter f){
  if(f.CurrentPhase==TrainingFighter.Phase.Down||f.CurrentPhase==TrainingFighter.Phase.Knockdown)return "Down";
  if(f.StunRemaining>0&&f.LastResult=="Hit")return !f.IsGrounded?"jHit":f.IsCrouching?"2Hit":"5Hit";
  bool guard=f.CurrentPhase==TrainingFighter.Phase.GuardStun||f.IsGuarding;
  if(guard&&((f.StunRemaining>0&&f.LastResult=="Guard")||(!f.IsAirCombatAttack&&f.CurrentPhase!=TrainingFighter.Phase.Attack))&&(!f.IsGrounded||f.IsCrouching))return f.IsGrounded?"2Guard":"jGuard";
  if(f.CurrentPhase==TrainingFighter.Phase.ThrowAttempt||f.CurrentPhase==TrainingFighter.Phase.ThrowHold)return "Throw";
  if(f.IsAirCombatAttack&&f.AirTrialClock.InActiveWindow){if(f.CurrentMove!=null){switch(f.CurrentMove.command){case TrainingCommand.AirStrongPunch:return "jHP";case TrainingCommand.AirStrongKick:case TrainingCommand.AirDownStrongKick:return "jHK";}}if(f.AirTrialCandidate==f.moveSet.flyingKick)return "jLK";if(f.AirTrialCandidate==f.moveSet.dropKick)return "jMK";}
  if(f.CurrentPhase==TrainingFighter.Phase.Attack&&f.HitboxActive&&f.CurrentMove!=null){switch(f.CurrentMove.command){case TrainingCommand.CrouchWeakPunch:return "2LP";case TrainingCommand.CrouchMediumPunch:return "2MP";case TrainingCommand.CrouchStrongPunch:return "2HP";case TrainingCommand.CrouchWeakKick:return "2LK";case TrainingCommand.CrouchMediumKick:return "2MK";case TrainingCommand.CrouchStrongKick:case TrainingCommand.Sweep:return "2HK";case TrainingCommand.Slash:return "5LK";case TrainingCommand.HeavySlash:return "5MK";case TrainingCommand.StrongKick:return "5HK";}}
  return null;
 }
 public TrainingPoseEntry Select(TrainingFighter f)=>Find(StateKey(f));
}
