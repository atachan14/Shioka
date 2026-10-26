using UnityEngine;

public sealed partial class TrainingFighter
{
    public TrainingThrowHurtboxProfile throwHurtboxProfile;
    public TrainingThrowHurtbox ThrowHurtbox{get;private set;}
    public TrainingThrowbox Throwbox{get;private set;}
    public bool ThrowInvulnerable{get;private set;}
    internal bool ThrowVolumeActive=>ThrowActive;
    public void SetThrowInvulnerable(bool value){ThrowInvulnerable=value;RefreshThrowVolumes();}
    private void CreateThrowVolumes(){
        if(throwHurtboxProfile==null)throwHurtboxProfile=Resources.Load<TrainingThrowHurtboxProfile>("TrainingThrowHurtboxProfile");
        if(throwHurtboxProfile==null)throwHurtboxProfile=ScriptableObject.CreateInstance<TrainingThrowHurtboxProfile>();
        var receive=new GameObject("Throw receiver");receive.transform.SetParent(transform,false);ThrowHurtbox=receive.AddComponent<TrainingThrowHurtbox>();ThrowHurtbox.Initialize(this);
        var attack=new GameObject("Throwbox");attack.transform.SetParent(transform,false);Throwbox=attack.AddComponent<TrainingThrowbox>();Throwbox.Initialize(this);
    }
    private void RefreshThrowVolumes(){if(ThrowHurtbox!=null)ThrowHurtbox.Refresh();if(Throwbox!=null)Throwbox.Refresh();}

    private TrainingFighter throwPartner;
    private bool throwResolved;
    private float caughtLevel,caughtDefense;
    public string LastThrowResult { get; private set; }="None";
    public int ThrowCount { get; private set; }
    public bool IsThrowLinked=>throwPartner!=null;
    private bool IsThrowState=>CurrentPhase==Phase.ThrowAttempt||CurrentPhase==Phase.ThrowHold||CurrentPhase==Phase.Thrown;
    private TrainingThrowSettings ThrowSettings=>moveSet==null?null:moveSet.throwSettings;
    private float ThrowLockSeconds=>CurrentPhase==Phase.ThrowAttempt?Mathf.Max(0,ThrowSettings.AttemptSeconds-timer):CurrentPhase==Phase.ThrowHold?Mathf.Max(0,ThrowSettings.HoldSeconds-timer):0;
    private bool ThrowActive=>CurrentPhase==Phase.ThrowAttempt&&ThrowSettings!=null&&timer>=ThrowSettings.startupFrames/60f&&timer<(ThrowSettings.startupFrames+ThrowSettings.activeFrames)/60f;
    private Vector3 ThrowCenter=>Throwbox==null?transform.position+new Vector3(ThrowSettings.range*.5f*Facing,.8f,0):Throwbox.WorldCenter;
    private Vector3 ThrowHalfSize=>Throwbox==null?new Vector3(ThrowSettings.range*.5f,.85f,0):Throwbox.WorldHalfSize;
    private bool CanStartThrow=>ThrowSettings!=null&&grounded&&!IsDown&&!IsThrowState&&hitstop<=0&&StunRemaining<=0&&(CurrentPhase==Phase.Idle||CurrentPhase==Phase.Move||CurrentPhase==Phase.Crouch);
    private void BeginThrow()
    {
        ResetSpatialMotion();RefreshFacing();SetCrouching(false);crouchPoseProgress=0;currentMove=null;airCombatActive=false;
        CurrentPhase=Phase.ThrowAttempt;timer=0;throwResolved=false;LastThrowResult="Attempt";
    }
    // The owning actor advances one shared hold clock. Victim motor is held but TDC input is read first.
    private bool TickThrow(float dt)
    {
        if(CurrentPhase==Phase.Thrown)return true;
        if(CurrentPhase!=Phase.ThrowHold)return false;
        if(throwPartner==null||!throwPartner.isActiveAndEnabled){ReleaseThrow();return true;}
        timer+=dt;throwPartner.StunRemaining=Mathf.Max(0,ThrowSettings.HoldSeconds-timer);
        if(timer>=ThrowSettings.HoldSeconds)
        {
            var victim=throwPartner;float level=caughtLevel,defense=caughtDefense;var settings=ThrowSettings;
            ReleaseThrow();victim.ResolveAttack(level,settings.damage,settings.attackLevel,TrainingAttackClass.Throw,true,defense);
            hitstop=settings.attackLevel*.2f/60f;Tension.Gain(settings.hitGain,moveSet.tensionLevelGainRate);ThrowCount++;LastThrowResult="Hit";victim.LastThrowResult="Received";
        }
        return true;
    }
    private void ResolveThrowAttempt()
    {
        if(CurrentPhase!=Phase.ThrowAttempt)return;
        if(ThrowActive&&!throwResolved)
        {
            var target=opponent;var settings=ThrowSettings;
            if(target!=null&&target.isActiveAndEnabled&&!target.IsDown&&!target.IsThrowLinked&&target.StunRemaining<=0&&target.hitstop<=0)
            {
                RefreshThrowVolumes();target.RefreshThrowVolumes();Physics2D.SyncTransforms();
                if(Throwbox!=null&&Throwbox.OverlapsAndMatches(target.ThrowHurtbox))
                {
                    if(target.CurrentPhase==Phase.ThrowAttempt&&!target.throwResolved&&target.timer<=(target.ThrowSettings.startupFrames+target.ThrowSettings.activeFrames)/60f)
                    {
                        throwResolved=target.throwResolved=true;LastThrowResult=target.LastThrowResult="Clash";
                        timer=(settings.startupFrames+settings.activeFrames)/60f;
                        target.timer=(target.ThrowSettings.startupFrames+target.ThrowSettings.activeFrames)/60f;target.RefreshThrowVolumes();
                    }
                    else
                    {
                        throwResolved=true;caughtLevel=settings.attackLevel*Tension.Bonus;caughtDefense=target.DefenseFor(TrainingAttackClass.Throw);
                        if(caughtLevel<=caughtDefense)
                        {
                            target.TryReceiveAttack(caughtLevel,0,settings.attackLevel,TrainingAttackClass.Throw,false);
                            hitstop=settings.attackLevel*.2f/60f;Tension.Gain(settings.hitGain,moveSet.tensionLevelGainRate);LastThrowResult="Resisted";
                        }
                        else
                        {
                            throwPartner=target;target.throwPartner=this;CurrentPhase=Phase.ThrowHold;target.CurrentPhase=Phase.Thrown;
                            timer=target.timer=0;target.StunRemaining=settings.HoldSeconds;target.LastResult="ThrowHold";
                            target.SetCrouching(false);target.crouchPoseProgress=0;target.currentMove=null;target.airCombatActive=false;target.airTrialClock.End();target.InputBuffer.ClearPending();target.UpdateHurtboxes();
                            LastThrowResult="Holding";target.LastThrowResult="Caught";
                        }
                    }
                }
            }
        }
        if(CurrentPhase==Phase.ThrowAttempt&&timer>=ThrowSettings.AttemptSeconds)
        {
            if(!throwResolved)LastThrowResult="Whiff";
            CurrentPhase=Phase.Idle;timer=0;CompletedAttacks++;ExecuteBuffered(true);
        }
    }
    private void ReleaseThrow()
    {
        var partner=throwPartner;throwPartner=null;
        if(CurrentPhase==Phase.ThrowAttempt){CurrentPhase=grounded?Phase.Idle:Phase.Air;timer=0;throwResolved=true;LastThrowResult="Canceled";InputBuffer.ClearPending();}
        if(CurrentPhase==Phase.Thrown||CurrentPhase==Phase.ThrowHold){CurrentPhase=grounded?Phase.Idle:Phase.Air;timer=0;StunRemaining=0;currentMove=null;InputBuffer.ClearPending();LastThrowResult="Released";}
        UpdateHurtboxes();RefreshThrowVolumes();
        if(partner!=null&&partner.throwPartner==this)
        {
            partner.throwPartner=null;
            if(partner.CurrentPhase==Phase.Thrown||partner.CurrentPhase==Phase.ThrowHold){partner.CurrentPhase=partner.grounded?Phase.Idle:Phase.Air;partner.timer=0;partner.StunRemaining=0;partner.currentMove=null;partner.InputBuffer.ClearPending();partner.LastThrowResult="Released";partner.UpdateHurtboxes();partner.RefreshThrowVolumes();}
        }
    }
    private void ApplyThrowPose()
    {
        if(!IsThrowState)return;
        if(CurrentPhase==Phase.Thrown){var hips=visual.GetBoneTransform(HumanBodyBones.Hips);if(hips!=null)hips.rotation=Quaternion.AngleAxis(-4*Facing,Vector3.right)*hips.rotation;
            if(footReferenceReady){float foot=Mathf.Min(visual.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,visual.GetBoneTransform(HumanBodyBones.RightFoot).position.y);float toe=Mathf.Min(visual.GetBoneTransform(HumanBodyBones.LeftToes).position.y,visual.GetBoneTransform(HumanBodyBones.RightToes).position.y);visual.transform.localPosition+=Vector3.up*Mathf.Max(transform.position.y+standingFootHeight-foot,transform.position.y+standingToeHeight-toe);}return;}
        var target=throwPartner!=null?throwPartner:opponent;if(target==null)return;
        float weight=CurrentPhase==Phase.ThrowHold?1:ThrowActive?1:Mathf.Clamp01(timer/Mathf.Max(.01f,ThrowSettings.startupFrames/60));
        foreach(bool right in new[]{false,true}){
            var arm=visual.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);
            var elbow=visual.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);
            var hand=visual.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
            if(arm==null||elbow==null||hand==null)continue;
            var aim=target.transform.position+new Vector3(right?.08f:-.08f,1.1f,0)-arm.position;
            arm.rotation=Quaternion.Slerp(arm.rotation,Quaternion.FromToRotation(elbow.position-arm.position,aim)*arm.rotation,weight);
            elbow.rotation=Quaternion.Slerp(elbow.rotation,Quaternion.FromToRotation(hand.position-elbow.position,target.transform.position+Vector3.up*1.1f-elbow.position)*elbow.rotation,weight);
        }
    }
    private void OnDisable(){ReleaseThrow();RefreshThrowVolumes();ClearInputBoundary();}
}
