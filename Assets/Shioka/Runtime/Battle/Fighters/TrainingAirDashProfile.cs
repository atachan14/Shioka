using UnityEngine;
[CreateAssetMenu(menuName="Shioka/Air dash profile")]
public sealed class TrainingAirDashProfile:ScriptableObject {
 [Range(3,60)]public int durationFrames=12;[Min(.1f)]public float distance=1.8f;
 [Tooltip("If enabled, set vertical velocity to zero once at start. Gravity always continues. Default preserves current vertical velocity.")]public bool resetVerticalVelocityOnStart;
 [Range(0,1)]public float postDashSpeedRatio=.5f;[Min(0)]public float postDashDeceleration=4f;
 public float Speed=>distance*60/durationFrames;
 public bool Valid=>durationFrames>=3&&durationFrames<=60&&!float.IsNaN(distance)&&!float.IsInfinity(distance)&&distance>=.1f&&distance<=6&&!float.IsNaN(postDashSpeedRatio)&&!float.IsInfinity(postDashSpeedRatio)&&postDashSpeedRatio>=0&&postDashSpeedRatio<=1&&!float.IsNaN(postDashDeceleration)&&!float.IsInfinity(postDashDeceleration)&&postDashDeceleration>=0&&postDashDeceleration<=100;
}
