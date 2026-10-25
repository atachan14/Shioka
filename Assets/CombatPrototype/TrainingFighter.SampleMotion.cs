using UnityEngine;
public sealed partial class TrainingFighter
{
 TrainingSampleMotionSet sampleMotions;bool sampleMotionsLoaded;
 AnimationClip SampleMotion(string state){if(!sampleMotionsLoaded){sampleMotions=Resources.Load<TrainingSampleMotionSet>("TrainingSampleMotionSet1");sampleMotionsLoaded=true;}if(sampleMotions==null||!sampleMotions.enabled||sampleMotions.character!=definition)return null;return state=="CrouchStart"?sampleMotions.crouchStart:state=="CrouchIdle"?sampleMotions.crouchIdle:state=="CrouchEnd"?sampleMotions.crouchEnd:state=="Jump"?sampleMotions.jump:state=="WeakKickGameplay"?sampleMotions.weakKick:state=="MediumKickGameplay"?sampleMotions.mediumKick:state=="StrongKickGameplay"?sampleMotions.strongKick:state=="CrouchWeakKickGameplay"?sampleMotions.crouchWeakKick:state=="CrouchMediumKickGameplay"?sampleMotions.crouchMediumKick:state=="CrouchStrongKickGameplay"?sampleMotions.crouchStrongKick:state=="AirStrongKickGameplay"?sampleMotions.airStrongKick:state=="AirDownStrongKickGameplay"?sampleMotions.airDownStrongKick:state=="AirStrongPunchGameplay"?sampleMotions.airStrongPunch:(state=="Special1Gameplay"||state=="SideKick")?sampleMotions.special1:state=="Special2Gameplay"?sampleMotions.special2:state=="ForwardSpecial1Gameplay"?sampleMotions.forwardSpecial1:state=="AirSpecial2Gameplay"?sampleMotions.airSpecial2:null;}
 bool HasSampleMotion(string state)=>SampleMotion(state)!=null;
 void ApplySampleMotion(string state,float normalized){var clip=SampleMotion(state);if(clip!=null)clip.SampleAnimation(visual.gameObject,Mathf.Clamp01(normalized)*clip.length);}
}
