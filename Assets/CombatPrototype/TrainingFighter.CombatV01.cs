using System.Collections.Generic;using UnityEngine;
public sealed partial class TrainingFighter {
 [System.NonSerialized]public CombatTestV01 CombatV01;
 public CombatV01Result? V01LastContact{get;private set;}public long V01JGAge=>v01JGStart<0?-1:InputBuffer.Frame-v01JGStart;public int V01JGReduction=>TrainingSession?.DummyJGFor(this)??CombatV01Formula.JGReduction(V01JGAge,v01JGContinuous);
 public bool V01BlockPaused=>v01Block&&v01Stun>0;public float V01ActionFrame=>timer*60;public bool V01FdDirectionAvailable=>V01CanFD(movement);
 bool v01FrozenThisTick;public bool V01CanContact=>Health>0&&!OptionInputSuspended&&HitboxActive&&!attackConnected&&!v01FrozenThisTick&&StunRemaining<=0&&hitstop<=0&&!IsThrowState;public void V01Connect(){attackConnected=true;}
 long v01JGStart=-1;int v01GuardDirection,v01Stop,v01Stun;bool v01JGContinuous,v01Block;
 sealed class Push{public double distance;public int frame,total;public float direction;}
 readonly List<Push> v01Push=new List<Push>();
 public bool V01CanFD(Vector2 direction)=>opponent!=null&&opponent.IsGuardThreatMotion&&direction.x*OpponentDirectionForGuard<-.1f&&GuardAvailable;
 void V01UpdateGuard(){int direction=RelativeBackHeld?(movement.y<-.5f?1:movement.y>.5f?7:4):0;int raw=(movement.x<-.1f?-1:movement.x>.1f?1:0)*10+(movement.y<-.5f?-1:movement.y>.5f?1:0);if(direction==0){v01JGStart=-1;}else if(raw!=v01GuardDirection){v01JGStart=InputBuffer.Frame;v01JGContinuous=LastResult=="Guard"&&StunRemaining>0;}v01GuardDirection=raw;guardHeld=direction!=0;}
 public bool V01MatchingGuard(TrainingAttackClass kind)=>IsGuarding&&(kind!=TrainingAttackClass.Low||IsCrouching)&&(kind!=TrainingAttackClass.High||!IsCrouching);
 public float V01Defense(TrainingHurtbox part,TrainingAttackClass kind)=>Mathf.Max(0,part.Region.BaseDefense(kind))*Mathf.Max(0,part.Region.combatPositionCorrection);
 void V01Reset(){v01Stop=v01Stun=0;v01Block=false;v01JGStart=-1;v01GuardDirection=0;v01JGContinuous=false;v01Push.Clear();V01LastContact=null;if(CombatV01!=null){Health=CombatV01.profile.hp;Tension.SetTestGauge(CombatV01.TGFor(this));}}
 void V01TickStop(){v01FrozenThisTick=true;v01Stop=Mathf.Max(0,v01Stop-1);hitstop=v01Stop/60f;}
 void V01TickStun(){v01FrozenThisTick=true;v01Stun=Mathf.Max(0,v01Stun-1);StunRemaining=v01Stun/60f;if(v01Stun==0)v01Block=false;}
 bool V01TickBlock(){if(!V01BlockPaused)return false;V01TickStun();V01TickPush();UpdateHurtboxes();return true;}
 void V01TickPush(){for(int i=v01Push.Count-1;i>=0;i--){var push=v01Push[i];float delta=(float)CombatV01Formula.PushStep(push.distance,push.frame++,push.total)*push.direction;MoveFdActor(this,delta);if(push.frame>=push.total)v01Push.RemoveAt(i);}}
 public void V01SetTestHP(float hp){if(CombatV01!=null&&CombatV01Formula.Finite(hp))SetTrainingHP(hp);}
 public void V01AddPush(double distance,float direction){if(distance>0)v01Push.Add(new Push{distance=distance,direction=direction,total=CombatV01.profile.pushFrames});}
 public void V01StopFor(int frames){v01Stop=Mathf.Max(v01Stop,frames);hitstop=v01Stop/60f;}
 public void V01Receive(CombatV01Result result,TrainingFighter attacker,TrainingHurtbox part){if(!isActiveAndEnabled||Health<=0)return;V01LastContact=result;LastContactHurtboxId=part.StableId;LastLevelDifference=(float)(result.H-result.B);Health=Mathf.Max(0,Health-(float)result.damage);LastResult=result.outcome.ToString();v01Stun=result.stunFrames;StunRemaining=v01Stun/60f;v01Block=result.outcome==CombatV01Outcome.Block;
 if(result.outcome==CombatV01Outcome.Hit){StopAirDash();ReleaseThrow();airTrialClock.End();airCombatActive=false;currentMove=null;InputBuffer.ClearPending();CurrentPhase=grounded?Phase.Hitstun:Phase.Air;timer=0;ResetSpatialMotion();HitCount++;}else if(result.outcome==CombatV01Outcome.Guard){GuardCount++;if(grounded)CurrentPhase=Phase.GuardStun;timer=0;if(result.fd){FdGuardCount++;LastFdCost=0;LastFdPushDistance=(float)result.attackerPush;}}
 if(v01Stun==0&&(CurrentPhase==Phase.GuardStun||CurrentPhase==Phase.Hitstun))CurrentPhase=grounded?Phase.Idle:Phase.Air;
 LastResolvedContact=new TrainingResolvedContact{sequence=++ContactSequence,partId=part.StableId,rawAttack=(float)result.A,effectiveAttack=(float)result.A,defense=(float)result.D,finalResult=(float)result.H,threshold=(float)result.B,hit=result.outcome==CombatV01Outcome.Hit,damage=(float)result.damage,stunFrames=result.stunFrames,hitstopFrames=result.hitstopFrames};float away=transform.position.x==attacker.transform.position.x?-attacker.Facing:Mathf.Sign(transform.position.x-attacker.transform.position.x);V01AddPush(result.selfPush,away);attacker.V01AddPush(result.attackerPush,-away);V01StopFor(result.hitstopFrames);attacker.V01StopFor(result.hitstopFrames);UpdateHurtboxes();}
}
