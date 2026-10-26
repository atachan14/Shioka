using UnityEngine;

[CreateAssetMenu(menuName = "Shioka/Training Character Definition")]
public sealed class TrainingCharacterDefinition : ScriptableObject
{
    public float walkSpeed = 3.5f, dashSpeed = 6f, acceleration = 30f;
    public float dashDeceleration = 10f;
    public float backstepSpeed = 6f, backstepSeconds = .18f;
    public float jumpVelocity = 9.5f, gravity = -25f;
    public float takeoffSeconds = 0.10f, landingSeconds = 0.14f;
    public float crouchHeight = 1.1f;
    public float crouchTransitionSeconds = 0.15f;
    public float attackStartup = 0.12f, attackActive = 0.08f, attackRecovery = 0.22f;
    public float attackLevel = 60f, damage = 10f;
    public float maxHealth = 100f, defenseLevel = 50f, guardDefenseLevel = 100f;
    // Multipliers preserve the continuous attack-minus-defense model. Provisional tuning.
    public float standingLowGuardMultiplier = .65f, crouchingLowGuardMultiplier = 1.5f;
    public float standingHighGuardMultiplier = 1.3f, crouchingHighGuardMultiplier = .7f;
    public int timedGuardWindowFrames=10, timedGuardCooldownFrames=20;
    public float timedGuardNearBonus=.5f, timedGuardFarBonus=.05f;
    public AnimationClip idleClip, jabClip;
    public AnimationClip crouchStartClip, crouchIdleClip, crouchEndClip;
    [Range(0f, 1f)] public float jabContactNormalized = 0.35f;
}
