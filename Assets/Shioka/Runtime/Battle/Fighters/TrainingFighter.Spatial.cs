using UnityEngine;
public sealed partial class TrainingFighter {
 TrainingMoveSpatialProfile SpatialProfile=>CurrentPhase==Phase.Attack?currentMove==null?null:currentMove.spatialProfile:CurrentPhase==Phase.Air&&airCombatActive?(currentMove!=null&&currentMove.spatialProfile!=null?currentMove.spatialProfile:AirTrialCandidate==null?null:AirTrialCandidate.spatialProfile):CurrentPhase==Phase.ThrowAttempt&&ThrowSettings!=null?ThrowSettings.spatialProfile:null;
 public float ActionVisualFrame=>SpatialFrame;
 public Sprite SkillSequenceSprite=>(CurrentPhase==Phase.Attack||IsAirCombatAttack)&&CurrentMove!=null?CurrentMove.SequenceSpriteAt(ActionVisualFrame):null;
 float SpatialFrame=>CurrentPhase==Phase.Air?airTrialClock.Elapsed*60:timer*60;
 public TrainingSpatialKey SpatialSample=>SpatialProfile==null?null:SpatialProfile.Sample(SpatialFrame);
 public Vector3 SpatialVisualOffset {get{var s=SpatialSample;return s==null?Vector3.zero:TrainingCoordinates2D.World(Vector3.zero,s.visualOffset,Facing);}}
 float spatialLastFrame;bool spatialMotionStarted;
 void ResetSpatialMotion(){spatialLastFrame=0;spatialMotionStarted=false;}
 float ConsumeSpatialMotion(bool clockAdvances=true){var s=SpatialSample;if(s==null){ResetSpatialMotion();return 0;}float frame=SpatialFrame;if(CurrentPhase==Phase.Air&&clockAdvances&&StunRemaining<=0&&!airTrialClock.NeedsLandingDecision)frame=Mathf.Min(airTrialClock.Duration,airTrialClock.Elapsed+Time.fixedDeltaTime)*60;float previous=spatialMotionStarted?spatialLastFrame:0;float displacement=SpatialProfile.Sample(frame).rootForward-SpatialProfile.Sample(previous).rootForward;spatialLastFrame=frame;spatialMotionStarted=true;return displacement*Facing;}
 public bool SpatialHurtGeometry(int id,out Vector3 center,out Vector3 size){center=size=Vector3.zero;var s=SpatialSample;if(s==null)return false;var h=System.Array.Find(s.hurt,x=>x.partId==id);if(h==null)return false;center=TrainingCoordinates2D.World(Vector3.zero,h.center,Facing);size=TrainingCoordinates2D.HalfSize(h.size);return true;}
}
