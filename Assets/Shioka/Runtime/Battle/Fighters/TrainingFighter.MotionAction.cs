using UnityEngine;
public sealed partial class TrainingFighter {
 TrainingMotionAction sampledMotion;float motionStart;
 public TrainingMotionAction MotionAction {get{var c=InputCharacter;if(c==null||IsCrouching||IsGuarding||!IsGrounded)return null;if(CurrentPhase==Phase.Idle)return c.idleAction;if(CurrentPhase==Phase.Move&&HorizontalVelocity*Facing>=-.01f&&!HasDashInertia&&Mathf.Abs(HorizontalVelocity)<=walkSpeed+.1f)return c.forwardWalkAction;return null;}}
 public TrainingMotionPose MotionPose {get{var action=MotionAction;if(action!=sampledMotion||timer<motionStart){sampledMotion=action;motionStart=timer;}return action==null?null:action.Sample(Mathf.Max(0,Mathf.FloorToInt((timer-motionStart)*60+.0001f)));}}
 public bool MotionHurtGeometry(int id,out Vector2 center,out Vector2 size){center=size=Vector2.zero;var pose=MotionPose;if(pose==null)return false;if(pose.hurt==null)return false;foreach(var hurt in pose.hurt)if(hurt!=null&&hurt.partId==id){center=new Vector2(hurt.center.x*Facing,hurt.center.y);size=hurt.size;return true;}return false;}
}
