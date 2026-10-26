using UnityEngine;
[CreateAssetMenu(menuName="Shioka/Approved Character Presentation")]
public sealed class TrainingApprovedCharacter:ScriptableObject {
 public string characterId="awari";
 public CharacterAttackSlot[] attackSlots=CharacterAttackSlot.AwariDefaults();
 public CharacterDirectionalMove[] contextualMoves=new CharacterDirectionalMove[0];
 public string characterName="\u3041\u308e\u308a";
 public TrainingProvisionalPoses provisionalPoses;
 [HideInInspector]public Sprite[] backstepSequence;
 [HideInInspector]public Sprite airDash;public Vector3 airDashVisualOffset=new Vector3(0,.85f,0);
 public TrainingBodyCalibration bodyCalibration;
 public TrainingMotionAction idleAction,forwardWalkAction,backwardWalkAction,crouchAction,groundDashAction,backstepAction,airDashAction,jumpAction;
 public bool OwnsMotion(TrainingMotionAction a)=>a!=null&&(a==idleAction||a==forwardWalkAction||a==backwardWalkAction||a==crouchAction||a==groundDashAction||a==backstepAction||a==airDashAction||a==jumpAction);
 static Sprite At(TrainingMotionAction a,int i,Sprite fallback)=>a!=null&&a.poses!=null&&a.poses.Length>i&&a.poses[i]!=null&&a.poses[i].sprite!=null?a.poses[i].sprite:fallback;
 public Sprite JumpSprite=>At(jumpAction,0,jump);
 public Sprite IdleSprite=>idleAction==null?idle:idleAction.Sample(0)?.sprite??idle;public Sprite ForwardSprite=>forwardWalkAction==null?forward:forwardWalkAction.Sample(0)?.sprite??forward;
 [HideInInspector]public Sprite idle,forward;
 [HideInInspector]public Sprite backward,jump,dash,crouch,jumpDescent,backstep1,backstep2,backstep3;public Sprite strongKick,weakPunch,mediumPunch,strongPunch,standingGuard;
 public bool Valid=>!string.IsNullOrEmpty(characterName)&&bodyCalibration!=null&&bodyCalibration.IsValid&&IdleSprite!=null&&ForwardSprite!=null&&At(backwardWalkAction,0,backward)!=null&&At(jumpAction,0,jump)!=null&&At(groundDashAction,0,dash)!=null&&At(crouchAction,0,crouch)!=null&&At(jumpAction,1,jumpDescent)!=null&&At(backstepAction,0,backstep1)!=null&&At(backstepAction,4,backstep2)!=null&&At(backstepAction,8,backstep3)!=null&&weakPunch!=null&&mediumPunch!=null&&strongPunch!=null&&standingGuard!=null;
 public void Capture(ArmoredStaticPoseView v){if(airDashAction==null)airDash=v.airDash;airDashVisualOffset=v.airDashVisualOffset;bodyCalibration=v.bodyCalibration;if(idleAction==null)idle=v.idle;if(forwardWalkAction==null)forward=v.forward;if(backwardWalkAction==null)backward=v.backward;if(jumpAction==null)jump=v.jump;strongKick=v.strongKick;if(groundDashAction==null)dash=v.dash;if(crouchAction==null)crouch=v.crouch;if(jumpAction==null)jumpDescent=v.jumpDescent;if(backstepAction==null)backstep1=v.backstep1;if(backstepAction==null)backstep2=v.backstep2;if(backstepAction==null)backstep3=v.backstep3;weakPunch=v.weakPunch;mediumPunch=v.mediumPunch;strongPunch=v.strongPunch;standingGuard=v.standingGuard;}
 public void Apply(ArmoredStaticPoseView v){v.airDash=At(airDashAction,0,airDash);v.airDashVisualOffset=airDashVisualOffset;v.bodyCalibration=bodyCalibration;v.idle=IdleSprite;v.forward=ForwardSprite;v.backward=At(backwardWalkAction,0,backward);v.jump=At(jumpAction,0,jump);v.strongKick=strongKick;v.dash=At(groundDashAction,0,dash);v.crouch=At(crouchAction,0,crouch);v.jumpDescent=At(jumpAction,1,jumpDescent);v.backstep1=At(backstepAction,0,backstep1);v.backstep2=At(backstepAction,4,backstep2);v.backstep3=At(backstepAction,8,backstep3);v.weakPunch=weakPunch;v.mediumPunch=mediumPunch;v.strongPunch=strongPunch;v.standingGuard=standingGuard;}
}
