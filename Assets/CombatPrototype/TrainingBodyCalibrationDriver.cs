using UnityEngine;
// Apply before TrainingFighter caches standing controller dimensions. Root scale and motion data are untouched.
[DefaultExecutionOrder(-100)] [RequireComponent(typeof(TrainingMotor2D))]
public sealed class TrainingBodyCalibrationDriver:MonoBehaviour {
 public TrainingApprovedCharacter characterProfile;
 public TrainingBodyCalibration profile;
 void Awake(){if(characterProfile!=null)profile=characterProfile.bodyCalibration;if(profile==null||!profile.IsValid){Debug.LogError("Missing or invalid character body calibration",this);return;}var body=GetComponent<TrainingMotor2D>();body.height=profile.standingHeight;var center=body.center;center.y=profile.standingHeight*.5f;body.center=center;}
}
