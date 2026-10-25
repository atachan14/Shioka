using UnityEngine;
public sealed partial class TrainingFighter {
 public TrainingAirDashProfile airDashProfile;
 public bool IsAirDashing{get;private set;}public bool AirDashUsed{get;private set;}public int AirDashCount{get;private set;}
 int airDashRemaining,airDashDirection;float airDashSpeed,airDashResidualRatio,airDashResidualDeceleration;bool airDashResidual;
 bool TryStartAirDash(){if(airDashProfile==null||!airDashProfile.Valid||CurrentPhase!=Phase.Air||grounded||AirDashUsed||IsAirDashing||airCombatActive||airTrialClock.IsBusy||StunRemaining>0||hitstop>0||IsDown||movement.x*Facing<-.1f)return false;AirDashUsed=true;IsAirDashing=true;AirDashCount++;airDashRemaining=airDashProfile.durationFrames;airDashSpeed=airDashProfile.Speed;airDashDirection=Facing;airDashResidualRatio=airDashProfile.postDashSpeedRatio;airDashResidualDeceleration=airDashProfile.postDashDeceleration;airDashResidual=false;horizontalSpeed=airDashDirection*airDashSpeed;if(airDashProfile.resetVerticalVelocityOnStart)verticalSpeed=0;return true;}
 void StopAirDash(){if(!IsAirDashing)return;IsAirDashing=false;airDashRemaining=0;airDashResidual=true;horizontalSpeed=airDashDirection*airDashSpeed*airDashResidualRatio;}
 void ResetAirDash(){StopAirDash();airDashResidual=false;AirDashUsed=false;AirDashCount=0;}
 void PrepareAirDash(){if(!IsAirDashing){if(airDashResidual&&CurrentPhase==Phase.Air){horizontalSpeed=Mathf.MoveTowards(horizontalSpeed,0,airDashResidualDeceleration*Time.fixedDeltaTime);if(Mathf.Abs(horizontalSpeed)<.00001f)airDashResidual=false;}return;}if(CurrentPhase!=Phase.Air||StunRemaining>0||airCombatActive||IsDown||airDashRemaining<=0){StopAirDash();return;}horizontalSpeed=airDashDirection*airDashSpeed;}
 void FinishAirDashStep(){if(IsAirDashing)airDashRemaining--;}
 void LandAirDash(){StopAirDash();airDashResidual=false;AirDashUsed=false;}
}
