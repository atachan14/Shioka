using UnityEngine;
public sealed class ArmoredStaticPoseView:TrainingFighterPresentation{
 public TrainingApprovedCharacter characterProfile;public bool showTrialControls=true;public bool frameSharedCamera=true;
 public string CharacterName=>characterProfile==null?"":characterProfile.characterName;
 void Awake(){if(characterProfile!=null)characterProfile.Apply(this);}
 public TrainingBodyCalibration bodyCalibration;
 public float CalibratedVisualScale=>bodyCalibration==null?1f:bodyCalibration.VisualScale;
 public Sprite idle,forward,backward,jump,strongKick;
 public static int BackstepSequenceIndex(float elapsedSeconds,float key2Frame,float key3Frame){float f=Mathf.Max(0,elapsedSeconds*60+.0001f);if(f<key2Frame)return Mathf.Clamp(Mathf.FloorToInt(f*4/Mathf.Max(.0001f,key2Frame)),0,3);if(f<key3Frame)return 4+Mathf.Clamp(Mathf.FloorToInt((f-key2Frame)*4/Mathf.Max(.0001f,key3Frame-key2Frame)),0,3);return 8;}
 public Sprite dash,crouch,jumpDescent,backstep1,backstep2,backstep3;
 public Sprite airDash;public Vector3 airDashVisualOffset=new Vector3(0,.85f,0);
 public Sprite weakPunch,mediumPunch,strongPunch,standingGuard;
 public Sprite ActivePunch(TrainingFighter fighter){if(fighter.CurrentPhase!=TrainingFighter.Phase.Attack||!fighter.HitboxActive||fighter.CurrentMove==null)return null;switch(fighter.CurrentMove.command){case TrainingCommand.Light:return weakPunch;case TrainingCommand.Medium:return mediumPunch;case TrainingCommand.Heavy:return strongPunch;default:return null;}}
 public bool StandingGuardVisible(TrainingFighter fighter)=>fighter.IsGrounded&&!fighter.IsCrouching&&(fighter.CurrentPhase==TrainingFighter.Phase.GuardStun||((fighter.CurrentPhase==TrainingFighter.Phase.Idle||fighter.CurrentPhase==TrainingFighter.Phase.Move)&&fighter.IsGuarding));

 public SpriteRenderer image;
 public string DisplayedState{get;private set;}public int SpriteChanges{get;private set;}
 public override void Present(TrainingFighter fighter,string state,float sample){
  // This trial's tall approved art needs room above the unchanged physical jump arc.
  var motionTuning=fighter.GetComponent<ApprovedLocomotionTuning>();float framing=motionTuning==null?2.3f:Mathf.Max(2.3f,(motionTuning.Applied.jumpHeight+2.45f)/1.8f);
  if(frameSharedCamera&&fighter.GetComponent<TrainingMotor2D>()==null&&fighter.CameraBounds!=null&&Mathf.Abs(fighter.CameraBounds.minimumZoom-framing)>.001f){fighter.CameraBounds.minimumZoom=framing;fighter.CameraBounds.Refresh();}
  bool kick=fighter.CurrentMove!=null&&(fighter.CurrentMove.command==TrainingCommand.StrongKick||fighter.CurrentMove.command==TrainingCommand.AirStrongKick)&&(fighter.CurrentPhase==TrainingFighter.Phase.Attack||fighter.IsAirCombatAttack);
  Sprite selected=idle;string name="Idle";
  Sprite punch=ActivePunch(fighter);
  if(punch!=null){selected=punch;name="Active "+fighter.CurrentMove.command+" P";}
  else if(kick){selected=strongKick;name="StrongKick";}
  else if(fighter.CurrentPhase==TrainingFighter.Phase.Backstep){
   float elapsed=fighter.definition.backstepSeconds-fighter.RemainingLockFrames/60f;
   var tuning=fighter.GetComponent<ApprovedLocomotionTuning>();int phase=tuning!=null?tuning.PoseIndex(elapsed):Mathf.Clamp(Mathf.FloorToInt(3f*elapsed/Mathf.Max(.02f,fighter.definition.backstepSeconds)),0,2);
   selected=phase==0?backstep1:phase==1?backstep2:backstep3;name="Backstep"+(phase+1);
   var sequence=characterProfile==null?null:characterProfile.backstepSequence;
   if(sequence!=null&&sequence.Length==9&&System.Array.TrueForAll(sequence,s=>s!=null)){float end1=tuning!=null?tuning.Applied.pose1EndFrame:fighter.definition.backstepSeconds*60/3;float end2=tuning!=null?tuning.Applied.pose2EndFrame:2*fighter.definition.backstepSeconds*60/3;int index=BackstepSequenceIndex(elapsed,end1,end2);selected=sequence[index];name="Backstep pose "+(index+1);}
  }
  else if(fighter.IsAirDashing){selected=airDash;name="AirDash";}
  else if(fighter.CurrentPhase==TrainingFighter.Phase.Takeoff||fighter.CurrentPhase==TrainingFighter.Phase.Air){bool rising=fighter.CurrentPhase==TrainingFighter.Phase.Takeoff||fighter.VerticalVelocity>0;selected=rising?jump:jumpDescent;name=rising?"JumpAscent":"JumpDescent";}
  else if(fighter.IsCrouching){selected=crouch;name="Crouch";}
  else if(StandingGuardVisible(fighter)){selected=standingGuard;name="StandingGuard";}
  else if(fighter.CurrentPhase==TrainingFighter.Phase.Move){bool running=state=="ForwardDash"||(fighter.HorizontalVelocity*fighter.Facing>.01f&&(fighter.HasDashInertia||Mathf.Abs(fighter.HorizontalVelocity)>fighter.walkSpeed+.1f));bool retreating=fighter.HorizontalVelocity*fighter.Facing<-.01f;selected=retreating?backward:running?dash:forward;name=retreating?"BackwardWalk":running?"Dash":"Walk";}
  var extra=characterProfile==null||characterProfile.provisionalPoses==null?null:characterProfile.provisionalPoses.Select(fighter);if(extra!=null&&extra.sprite!=null){selected=extra.sprite;name="Provisional "+extra.id;}
  var motion=extra==null?fighter.MotionPose:null;if(motion!=null)selected=motion.sprite;
  if(selected==null)selected=characterProfile==null?idle:characterProfile.IdleSprite;
  if(image.sprite!=selected){SpriteChanges++;image.sprite=selected;}DisplayedState=name;
  image.transform.localRotation=Quaternion.identity;image.transform.localPosition=Vector3.up*(extra==null?0:extra.offsetY)+fighter.SpatialVisualOffset+(motion==null&&fighter.IsAirDashing&&selected==airDash?TrainingCoordinates2D.World(Vector3.zero,airDashVisualOffset,fighter.Facing):Vector3.zero);if(motion!=null)image.transform.localPosition+=new Vector3(motion.visualOffset.x*fighter.Facing,motion.visualOffset.y,0);image.transform.localScale=Vector3.one*CalibratedVisualScale;image.flipX=fighter.Facing<0;
 }
 public bool TrialControlsVisible=>showTrialControls&&GetComponentInParent<TrainingFighter>()?.TrainingSession==null;
 void OnGUI(){if(!TrialControlsVisible)return;GUI.Box(new Rect(12,88,870,44),"APPROVED KEY POSES | A/D: move | W: jump | S: crouch | double tap forward/back: dash/backstep | R: reset\n"+DisplayedState+" | J K L / M , .: combat | guard: back during attack | jump 1/2");}
}
