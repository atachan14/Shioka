using UnityEngine;
public sealed class TrainingSampleMotionSet:ScriptableObject
{
 public TrainingCharacterDefinition character;
 public AnimationClip crouchStart,crouchIdle,crouchEnd,jump;
 public AnimationClip weakKick,mediumKick,strongKick;
 public AnimationClip crouchWeakKick,crouchMediumKick,crouchStrongKick;
 public AnimationClip airStrongKick,airDownStrongKick,airStrongPunch;
 public AnimationClip special1,special2,forwardSpecial1,airSpecial2;
 public bool mediumKickGroundFeet;
 public Vector2 mediumKickSupportFootXZ=new Vector2(.1025038f,.1577384f);
 public Vector2 mediumKickRestFootXZ=new Vector2(.2036605f,-.0467877f);
 [Min(0f)]public float mediumKickSoleClearance=.001f;
 public bool enabled=true;
}
