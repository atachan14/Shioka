using UnityEngine;
[CreateAssetMenu(menuName="Shioka/Approved Character Presentation")]
public sealed class TrainingApprovedCharacter:ScriptableObject {
 public string characterName="\u3041\u308e\u308a";
 public TrainingProvisionalPoses provisionalPoses;
 public Sprite[] backstepSequence;
 public Sprite airDash;public Vector3 airDashVisualOffset=new Vector3(0,.85f,0);
 public TrainingBodyCalibration bodyCalibration;
 public Sprite idle,forward,backward,jump,strongKick,dash,crouch,jumpDescent,backstep1,backstep2,backstep3,weakPunch,mediumPunch,strongPunch,standingGuard;
 public bool Valid=>!string.IsNullOrEmpty(characterName)&&bodyCalibration!=null&&bodyCalibration.IsValid&&idle!=null&&forward!=null&&backward!=null&&jump!=null&&dash!=null&&crouch!=null&&jumpDescent!=null&&backstep1!=null&&backstep2!=null&&backstep3!=null&&weakPunch!=null&&mediumPunch!=null&&strongPunch!=null&&standingGuard!=null;
 public void Capture(ArmoredStaticPoseView v){airDash=v.airDash;airDashVisualOffset=v.airDashVisualOffset;bodyCalibration=v.bodyCalibration;idle=v.idle;forward=v.forward;backward=v.backward;jump=v.jump;strongKick=v.strongKick;dash=v.dash;crouch=v.crouch;jumpDescent=v.jumpDescent;backstep1=v.backstep1;backstep2=v.backstep2;backstep3=v.backstep3;weakPunch=v.weakPunch;mediumPunch=v.mediumPunch;strongPunch=v.strongPunch;standingGuard=v.standingGuard;}
 public void Apply(ArmoredStaticPoseView v){v.airDash=airDash;v.airDashVisualOffset=airDashVisualOffset;v.bodyCalibration=bodyCalibration;v.idle=idle;v.forward=forward;v.backward=backward;v.jump=jump;v.strongKick=strongKick;v.dash=dash;v.crouch=crouch;v.jumpDescent=jumpDescent;v.backstep1=backstep1;v.backstep2=backstep2;v.backstep3=backstep3;v.weakPunch=weakPunch;v.mediumPunch=mediumPunch;v.strongPunch=strongPunch;v.standingGuard=standingGuard;}
}
