using UnityEngine;
public sealed class TrainingLocomotion:ScriptableObject {
 public AnimationClip forwardDash,forwardStep,backstep;
 public float backstepHopHeight=.045f; // Visual-only adjustable prototype; controller never jumps.
 public float runSpeedReference=4.4f,stepSpeedReference=.7f;
}
