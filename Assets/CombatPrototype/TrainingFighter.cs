using UnityEngine;

[RequireComponent(typeof(TrainingMotor2D))]
public sealed partial class TrainingFighter : MonoBehaviour
{
    public enum Phase { Idle, Move, Takeoff, Air, Landing, Attack, Hitstun, GuardStun, Crouch, Backstep, Knockdown, Down, GetUp, ThrowAttempt, ThrowHold, Thrown }
    public enum VisualFacingStyle { Current, MirrorLeft, ThreeQuarter }
    public VisualFacingStyle FacingStyle { get; private set; }
    public void SetVisualFacingStyle(VisualFacingStyle style) { FacingStyle = style; }
    [System.NonSerialized] public TrainingSession TrainingSession;
    public bool TrainingRecoveryBusy=>StunRemaining>0||HitstopRemaining>0||IsDown||IsThrowState||IsThrowLinked;
    public void SetTrainingHP(float hp){if(float.IsNaN(hp)||float.IsInfinity(hp))throw new System.ArgumentOutOfRangeException(nameof(hp));Health=Mathf.Clamp(hp,0,definition.maxHealth);TrainingSession?.HPAssigned(this);}
    public void ClearTrainingInputBoundary()=>ClearInputBoundary();
    public void RefreshTrainingFacing()=>RefreshFacing();
    public Animator visual;
    // Optional presentation override. Null preserves the existing Humanoid/R10 path.
    public TrainingFighterPresentation presentation;
    public TrainingFighter opponent;
    public TrainingCharacterDefinition definition;
    public MonoBehaviour inputSourceBehaviour;
    public float walkSpeed => definition.walkSpeed;
    public float dashSpeed => definition.dashSpeed;
    public float acceleration => definition.acceleration;
    public float jumpVelocity => definition.jumpVelocity;
    public float gravity => definition.gravity;
    public float takeoffSeconds => definition.takeoffSeconds;
    public float landingSeconds => definition.landingSeconds;
    public float attackStartup => currentMove == null ? definition.attackStartup : currentMove.startupFrames / 60f;
    public float attackActive => currentMove == null ? definition.attackActive : currentMove.activeFrames / 60f;
    public float attackRecovery => currentMove == null ? definition.attackRecovery : currentMove.recoveryFrames / 60f;
    public float attackLevel => currentMove == null ? definition.attackLevel : (CurrentPhase == Phase.Attack ? currentMove.AttackLevelAt(timer) : currentMove.attackLevel);
    public float damage => currentMove == null ? definition.damage : currentMove.damage;
    public string CharacterName=>presentation is ArmoredStaticPoseView approved?approved.CharacterName:"";
    public TrainingMoveSet moveSet;
    [System.NonSerialized] public TrainingFormulaProfile ComparisonProfile;
    public TrainingComparisonRule CurrentComparisonRule => (Application.isEditor||Debug.isDebugBuild)&&ComparisonProfile!=null&&currentMove!=null ? ComparisonProfile.Rule(currentMove.command) : TrainingComparisonRule.Legacy;
    public TrainingResolvedContact LastResolvedContact {get;private set;}
    public int ContactSequence {get;private set;}
    private TrainingMove currentMove;
    private bool airCombatActive;
    public TrainingMove CurrentMove => currentMove;
    public bool IsAirCombatAttack => airCombatActive;
    public readonly TrainingCommandBuffer InputBuffer = new TrainingCommandBuffer();
    public readonly TrainingTension Tension = new TrainingTension();
    public int ChainCount { get; private set; }
    public int CancelCount { get; private set; }
    public TrainingLockKind LockKind => CurrentPhase==Phase.ThrowAttempt ? (timer<ThrowSettings.startupFrames/60?TrainingLockKind.ThrowStartup:ThrowActive?TrainingLockKind.ThrowActive:TrainingLockKind.ThrowRecovery) : CurrentPhase==Phase.ThrowHold ? TrainingLockKind.ThrowHold : CurrentPhase==Phase.Thrown ? TrainingLockKind.Thrown : CurrentPhase == Phase.Knockdown ? TrainingLockKind.Knockdown : CurrentPhase == Phase.Down ? TrainingLockKind.Down : CurrentPhase == Phase.GetUp ? TrainingLockKind.GetUp : StunRemaining > 0f ? (V01BlockPaused?TrainingLockKind.Blockstun:LastResult == "Guard" ? TrainingLockKind.GuardStun : TrainingLockKind.Hitstun) : CurrentPhase == Phase.Backstep ? TrainingLockKind.Backstep : CurrentPhase == Phase.Takeoff ? TrainingLockKind.Takeoff : CurrentPhase == Phase.Landing ? TrainingLockKind.Landing : CurrentPhase == Phase.Attack ? (timer < attackStartup ? TrainingLockKind.Startup : timer < attackStartup + attackActive ? TrainingLockKind.Active : TrainingLockKind.Recovery) : airTrialClock.IsBusy ? TrainingLockKind.AirAction : TrainingLockKind.None;
    public float RemainingLockFrames => Mathf.Max(StunRemaining, ThrowLockSeconds, CurrentPhase == Phase.Backstep ? Mathf.Max(0f, definition.backstepSeconds-timer) : 0f, CurrentPhase == Phase.Attack ? Mathf.Max(0f, attackStartup + attackActive + attackRecovery - timer) : 0f, CurrentPhase == Phase.Takeoff ? Mathf.Max(0f, takeoffSeconds - timer) : 0f, CurrentPhase == Phase.Landing ? Mathf.Max(0f, landingSeconds - timer) : 0f, airTrialClock.IsBusy ? airTrialClock.Duration - airTrialClock.Elapsed : 0f) * 60f;
    public bool Guarding; // Existing persistent training dummy guard toggle.
    private bool guardHeld;
    private long guardWindowStart=-1, nextGuardWindowFrame;
    private bool GuardAvailable => !IsDown && (StunRemaining<=0 || LastResult=="Guard") && (CurrentPhase==Phase.Idle||CurrentPhase==Phase.Move||CurrentPhase==Phase.Crouch||CurrentPhase==Phase.Air||CurrentPhase==Phase.GuardStun);
    public bool IsGuarding => Guarding || (BackGuardIntent && GuardAvailable);
    public long GuardAgeFrames => guardWindowStart<0?-1:InputBuffer.Frame-guardWindowStart;
    public float GuardTimingMultiplier => guardHeld && GuardAvailable && IsGuarding && GuardAgeFrames>=0 && GuardAgeFrames<=Mathf.Max(1,definition.timedGuardWindowFrames) ? 1f+Mathf.Lerp(definition.timedGuardNearBonus,definition.timedGuardFarBonus,Mathf.Clamp01((GuardAgeFrames-1f)/Mathf.Max(1,definition.timedGuardWindowFrames-1))) : 1f;
    private void UpdateGuardInput(bool held)
    {
        if(CombatV01!=null){V01UpdateGuard();return;}
        if(held && !guardHeld){if(GuardAvailable && InputBuffer.Frame>=nextGuardWindowFrame){guardWindowStart=InputBuffer.Frame;nextGuardWindowFrame=InputBuffer.Frame+Mathf.Max(definition.timedGuardWindowFrames+1,definition.timedGuardCooldownFrames);}else guardWindowStart=-1;}
        if(!held)guardWindowStart=-1;
        guardHeld=held;
    }
    public float Health { get; private set; }
    public int HitCount { get; private set; }
    public int GuardCount { get; private set; }
    public float LastLevelDifference { get; private set; }
    public float StunRemaining { get; private set; }
    public string LastResult { get; private set; } = "None";
    public Phase CurrentPhase { get; private set; }
    public int Facing { get; private set; } = 1;
    public int CompletedJumps { get; private set; }
    public int CompletedAttacks { get; private set; }
    public float VerticalVelocity => verticalSpeed;
    public float HorizontalVelocity => horizontalSpeed;
    public float HitstopRemaining => hitstop;
    public TrainingCameraBounds CameraBounds { get; set; }
    public bool HasDashInertia => dashInertia;
    public int LastWallSide { get; private set; }
    public event System.Action<int> WallContact;
    public event System.Action<TrainingFighter> TdcPerformed;
    public bool IsGrounded => grounded;
    public bool IsCrouching { get; private set; }
    public bool HitboxActive => ThrowActive || (CurrentPhase == Phase.Attack && timer >= attackStartup && timer < attackStartup + attackActive) || (airCombatActive && CurrentPhase == Phase.Air && airTrialClock.CanApplyHit);
    public Vector3 HitboxCenter {get{var s=SpatialSample;if(s!=null)return TrainingCoordinates2D.World(transform.position,s.hitCenter,Facing);return CurrentPhase==Phase.ThrowAttempt&&ThrowSettings!=null?ThrowCenter:airCombatActive&&AirTrialCandidate!=null?AirTrialCandidate.ContactCenterFor(transform,Facing):TrainingCoordinates2D.World(transform.position,currentMove==null?new Vector3(0,.95f,.65f):currentMove.center,Facing);}}
    public Vector3 HitboxHalfSize {get{var s=SpatialSample;if(s!=null)return TrainingCoordinates2D.HalfSize(s.hitHalfSize);if(CurrentPhase==Phase.ThrowAttempt&&ThrowSettings!=null)return ThrowHalfSize;return TrainingCoordinates2D.HalfSize(airCombatActive&&AirTrialCandidate!=null?AirTrialCandidate.proposedHalfSize:currentMove==null?new Vector3(.32f,.3f,.4f):currentMove.halfSize);}}
    public bool IsDown => pendingKnockdown || CurrentPhase == Phase.Knockdown || CurrentPhase == Phase.Down || CurrentPhase == Phase.GetUp;
    public int DownFollowupCount { get; private set; }
    private bool pendingKnockdown;
    private float downElapsed;
    public TrainingAttackClass CurrentAttackClass => IsThrowState ? TrainingAttackClass.Throw : currentMove == null ? TrainingAttackClass.Mid : currentMove.attackClass;
    public TrainingHurtbox[] Hurtboxes { get; private set; }
    public TrainingHurtboxProfile hurtboxProfile;
    public event System.Action HurtboxesChanged;
    public int LastContactHurtboxId {get;private set;}=-1;
    private TrainingMotor2D body;
    public TrainingMotor2D Motor2D=>body;
    private float standingHeight;
    private float crouchPoseProgress;
    private Transform[] postureBones;
    private Vector3[] previousBonePositions;
    private Quaternion[] previousBoneRotations;
    private bool wasCrouchAnimation;
    private float postureBlendRemaining;
    private bool footReferenceReady;
    private float standingFootHeight = 0.08f;
    private float standingToeHeight;
    private Vector3 standingCenter;
    private Vector2 movement;
    private bool dash, jumpRequested, attackRequested, attackConnected;
    private bool grounded, airFromJump;
    private bool dashInertia;
    private bool previousDash;
    private float horizontalSpeed, verticalSpeed, timer, walkTime, idleTime, hitstop, locomotionDistance;
    private TrainingLocomotion locomotion;
    private Vector3 standingHipLocal;
    private Vector3 spawn;
    private ITrainingInputSource inputSource;
    private readonly RaycastHit[] floorHits = new RaycastHit[8];
    private Material[] materials;
    private Color[] baseColors;

    private void Awake()
    {
        body = GetComponent<TrainingMotor2D>();
        locomotion = Resources.Load<TrainingLocomotion>("TrainingLocomotion");
        if(GetComponent<TrainingCancelEffect>()==null)gameObject.AddComponent<TrainingCancelEffect>();
        standingHeight = body.height;
        standingCenter = body.center;


        if (definition == null) definition = ScriptableObject.CreateInstance<TrainingCharacterDefinition>();
        if (moveSet == null) moveSet = Resources.Load<TrainingMoveSet>("TrainingMoveSet");
        if(hurtboxProfile==null)hurtboxProfile=Resources.Load<TrainingHurtboxProfile>("TrainingHurtboxProfile");
        if(hurtboxProfile==null)hurtboxProfile=ScriptableObject.CreateInstance<TrainingHurtboxProfile>();
        RebuildHurtboxes();CreateThrowVolumes();
        inputSource = inputSourceBehaviour as ITrainingInputSource;
        if (inputSource == null) foreach (var component in GetComponents<MonoBehaviour>()) if (component is ITrainingInputSource source) { inputSource = source; break; }
        spawn = transform.position;
        Health = definition.maxHealth;
        if (presentation == null)
        {
        visual.applyRootMotion = false;
        visual.speed = 0f;
        visual.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var list = new System.Collections.Generic.List<Material>();
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>()) list.AddRange(renderer.materials);
        materials = list.ToArray();
        baseColors = System.Array.ConvertAll(materials, material => material.color);
        }
        else { materials = new Material[0]; baseColors = new Color[0]; }
        grounded = HasFloorSupport();RefreshThrowVolumes();
    }
    private void Start() { IgnoreCharacterSupport(); }
    private void IgnoreCharacterSupport() { if(opponent!=null&&opponent.GetComponent<TrainingMotor2D>()!=null)Physics2D.IgnoreCollision(body.Shape,opponent.GetComponent<TrainingMotor2D>().Shape,true); }
    public void SetMovement(Vector2 value) { movement = value; }
    private void ClearInputBoundary(){if(inputSource is ITrainingResettableInputSource resettable)resettable.Clear();InputBuffer.ClearPending();movement=Vector2.zero;dash=previousDash=jumpRequested=attackRequested=false;guardHeld=fdHeld=false;guardWindowStart=-1;}
    private void OnEnable(){ClearInputBoundary();RefreshThrowVolumes();}
    public void SetInputSource(MonoBehaviour source)
    {
        if(inputSource is ITrainingResettableInputSource previous)previous.Clear();
        ReleaseThrow();inputSourceBehaviour = source; inputSource = source as ITrainingInputSource;guardHeld=false;fdHeld=false;guardWindowStart=-1;
        if(inputSource is ITrainingResettableInputSource next)next.Clear();
        movement = Vector2.zero; dash = jumpRequested = attackRequested = false;
        InputBuffer.ClearPending();
    }
    public void SetDash(bool value) { dash = value; }
    public void RequestJump() { jumpRequested = true; }
    public void RequestAttack() { attackRequested = true; }
    private readonly TrainingAirActionClock airTrialClock = new TrainingAirActionClock();
    public TrainingAirActionClock AirTrialClock => airTrialClock;
    public TrainingAirAttackCandidate AirTrialCandidate { get; private set; }
    // Test API only: no input action, animation override or damage is bound here.
    public bool TryBeginAirAttackTrial(TrainingAirAttackCandidate candidate)
    {
        if (CurrentPhase != Phase.Air || hitstop > 0f || StunRemaining > 0f || candidate == null) return false;
        var move=AirMoveFor(candidate);
        float duration=move==null?candidate.DurationSeconds:move.Duration;
        if (!(duration>0f) || float.IsNaN(duration) || float.IsInfinity(duration)) return false;
        if (!airTrialClock.TryBegin(duration, move==null?candidate.ProposedStartupSeconds:move.startupFrames/60f, move==null?candidate.ProposedActiveSeconds:move.activeFrames/60f)) return false;
        StopAirDash();ResetSpatialMotion();AirTrialCandidate = candidate;
        return true;
    }
    public void EndAirAttackTrial() { airTrialClock.End(); }
    public bool ResolveAirTrialLanding(TrainingAirActionClock.LandingChoice choice) => airTrialClock.ResolveLanding(choice);
    public void ResetTraining()
    {
        ResetAirDash();ResetSpatialMotion();
        ResetLocal();
        if (opponent != null) opponent.ResetLocal();
        RefreshFacing();
        if (opponent != null) opponent.RefreshFacing();
    }
    private void RefreshFacing() { Facing = opponent == null || opponent.transform.position.x > transform.position.x ? 1 : -1; }
    private void ResetLocal()
    {
        ResetFocusedMediumKickPlant();
        LastTdcCostScale=1f;LastTdcBaseCost=0f;ResetFd();ReleaseThrow();ThrowCount=0;LastThrowResult="None";throwResolved=false;
        body.enabled = false;
        SetCrouching(false);
        transform.position = spawn;
        body.enabled = true;
        IgnoreCharacterSupport();
        CurrentPhase = Phase.Idle;
        airTrialClock.Reset(); AirTrialCandidate = null;
        currentMove = null; airCombatActive = false;
        if(inputSource is ITrainingResettableInputSource resettable)resettable.Clear();
        ThrowInvulnerable=false;RefreshThrowVolumes();LastContactHurtboxId=-1; InputBuffer.Reset(); Tension.Reset(); ChainCount = CancelCount = 0;guardHeld=false;fdHeld=false;guardWindowStart=-1;nextGuardWindowFrame=0;
        timer = verticalSpeed = horizontalSpeed = hitstop = walkTime = idleTime = locomotionDistance = 0f;
        pendingKnockdown=false; downElapsed=0; DownFollowupCount=0; UpdateHurtboxes();
        crouchPoseProgress = 0f;
        wasCrouchAnimation = false; postureBlendRemaining = 0f;
        jumpRequested = attackRequested = attackConnected = false;
        movement = Vector2.zero; dash = previousDash = false;
        CompletedJumps = CompletedAttacks = 0;
        Facing = opponent == null || opponent.transform.position.x > transform.position.x ? 1 : -1;
        grounded = HasFloorSupport();RefreshThrowVolumes();
        airFromJump = false;
        dashInertia = false; previousDash = false; LastWallSide = 0;
        Health = definition.maxHealth;
        HitCount = GuardCount = 0; LastLevelDifference = 0f;LastResolvedContact=null;
        StunRemaining = 0f;
        LastResult = "None";
        if(CombatV01!=null)V01Reset();
    }
    private void FixedUpdate()
    {
        if(OptionInputSuspended)return;
        float dt = Time.fixedDeltaTime;
        bool dashPressed = false;
        v01FrozenThisTick=false;
        InputBuffer.Advance(dt);
        if (inputSource != null || TrainingSession!=null)
        {
            TrainingInputFrame input = TrainingSession==null?inputSource.ReadInput():TrainingSession.ReadFor(this);
            movement = input.Movement; dash = input.Dash; fdHeld=input.FdHeld;
            dashPressed = input.DashPressed || (dash && !previousDash);
            if (input.Reset) { if(TrainingSession!=null){TrainingSession.ResetConfigured();return;}if(CombatV01!=null)CombatV01.ResetTest();else ResetTraining(); return; }
            CaptureCommands(input);
            if (input.ToggleOpponentGuard && opponent != null) opponent.Guarding = !opponent.Guarding;
        }
        UpdateGuardInput(RelativeBackHeld);
        if (jumpRequested) InputBuffer.Push(TrainingCommand.Jump, LockKind);
        if (attackRequested) InputBuffer.Push(TrainingCommand.Light, LockKind);
        jumpRequested = attackRequested = false;
        ExecuteBuffered();
        previousDash = dash;if(dashPressed)TryStartAirDash();
        if (dashPressed && grounded && hitstop<=0 && StunRemaining<=0 && movement.y>=-.5f && (CurrentPhase==Phase.Idle||CurrentPhase==Phase.Move||CurrentPhase==Phase.Crouch))
        {
            RefreshFacing();
            if(movement.x*Facing<-.1f){SetCrouching(false);CurrentPhase=Phase.Backstep;timer=0;horizontalSpeed=-Facing*definition.backstepSpeed;dashInertia=false;}
        }
        if (hitstop > 0f) { airTrialClock.Hold(TrainingAirActionClock.Pause.Hitstop); if(CombatV01!=null)V01TickStop();else hitstop = Mathf.Max(0f, hitstop - dt); if (hitstop == 0f) ExecuteBuffered(); return; }
        if(CombatV01!=null&&V01TickBlock())return;
        if(TickThrow(dt))return;
        bool wasStunned = StunRemaining > 0f;
        if (StunRemaining > 0f && !IsDown)
        {
            if(CombatV01!=null)V01TickStun();else StunRemaining = Mathf.Max(0f, StunRemaining - dt);
            if (StunRemaining == 0f && (CurrentPhase == Phase.Hitstun || CurrentPhase == Phase.GuardStun)) CurrentPhase = Phase.Idle;
            if (StunRemaining == 0f) ExecuteBuffered(true);
        }
        bool floorBeforeMove = HasFloorSupport();
        if (CurrentPhase != Phase.Air && !floorBeforeMove) EnterAir(false);
        if (!wasStunned && grounded && (CurrentPhase == Phase.Idle || CurrentPhase == Phase.Move || CurrentPhase == Phase.Crouch))
        {
            if (opponent != null && Mathf.Abs(opponent.transform.position.x - transform.position.x) > 0.08f)
                Facing = opponent.transform.position.x > transform.position.x ? 1 : -1;
            SetCrouching(movement.y < -0.5f);
            if (IsCrouching) CurrentPhase = Phase.Crouch;
            else CurrentPhase = Mathf.Abs(movement.x) > 0.1f ? Phase.Move : Phase.Idle;
        }
        if (IsDown && grounded) AdvanceDown(dt);
        if(CombatV01==null||!wasStunned)timer += dt;
        // Acceleration belongs to the floor. Air input never changes stored inertia.
        if (CurrentPhase != Phase.Air && CurrentPhase != Phase.Backstep)
        {
            bool canMove = (TrainingSession==null||!TrainingSession.HoldDummyPosition(this)) && (CombatV01==null||Health>0) && !wasStunned && (CurrentPhase == Phase.Idle || CurrentPhase == Phase.Move || CurrentPhase == Phase.Takeoff);
            float targetSpeed = canMove ? movement.x * (dash && movement.x*Facing>0f ? dashSpeed : walkSpeed) : 0f;
            if (canMove && dash && movement.x*Facing > .1f) dashInertia = true;
            bool reversing = canMove && Mathf.Abs(movement.x) > .1f && horizontalSpeed * movement.x < 0f;
            bool slowingDash = dashInertia && !dash && !reversing && CurrentPhase != Phase.Crouch && Mathf.Abs(targetSpeed) < Mathf.Abs(horizontalSpeed);
            horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, targetSpeed, (slowingDash ? Mathf.Max(.1f, definition.dashDeceleration) : acceleration) * dt);
            if (reversing || Mathf.Abs(horizontalSpeed) < .001f || (!dash && canMove && Mathf.Abs(movement.x) > .1f && Mathf.Abs(horizontalSpeed) <= walkSpeed)) dashInertia = false;
        }
        if (CurrentPhase == Phase.Takeoff && timer >= takeoffSeconds) EnterAir(true);
        // All unsupported motion uses the same gravity, including walking off an edge.
        verticalSpeed = CurrentPhase == Phase.Air ? verticalSpeed + gravity * dt : -2f;
        PrepareAirDash();float motionStartX=transform.position.x;
        var motorCollision=body.Move(new Vector2(horizontalSpeed,verticalSpeed)*dt+Vector2.right*ConsumeSpatialMotion(!wasStunned));if((IsAirDashing||(CurrentPhase==Phase.Air&&AirDashUsed))&&(motorCollision&TrainingMotorContact2D.Sides)!=0){StopAirDash();airDashResidual=false;horizontalSpeed=0;}
        if(CombatV01!=null)V01TickPush();
        FinishAirDashStep();ResolveOpponentOverlap();
        // Keep the simulation in the side-view plane, inside the training floor.
        var p = transform.position;
        p.z = 0f;
        float left = CameraBounds == null ? TrainingStage.Left : CameraBounds.Left;
        float right = CameraBounds == null ? TrainingStage.Right : CameraBounds.Right;
        LastWallSide = p.x < left ? -1 : p.x > right ? 1 : 0;
        p.x = CameraBounds==null?Mathf.Clamp(p.x,left,right):CameraBounds.LimitPosition(motionStartX,p.x);
        if (LastWallSide != 0 && horizontalSpeed * LastWallSide > 0f)
        {
            StopAirDash();airDashResidual=false;horizontalSpeed = 0f; dashInertia = false;
            WallContact?.Invoke(LastWallSide);
        }
        body.Teleport(p);
        if(CurrentPhase==Phase.Move)locomotionDistance+=Mathf.Abs(p.x-motionStartX);
        bool floorAfterMove = HasFloorSupport() && verticalSpeed <= 0f;
        if (CurrentPhase == Phase.Air && floorAfterMove)
        {
            LandAirDash();
            // Resolve the landing side before recovery can consume a buffered jump.
            if (!pendingKnockdown) RefreshFacing();
            if (pendingKnockdown) BeginDown(); else CurrentPhase = Phase.Landing;
            verticalSpeed = 0f;
            timer = 0f;
            grounded = true;
            if (airFromJump) CompletedJumps++;
            if (airCombatActive) { airTrialClock.End(TrainingAirActionClock.Finish.Landing); airCombatActive = false; currentMove=null; InputBuffer.ClearPending(); }
        }
        else if (CurrentPhase != Phase.Air && !floorAfterMove) EnterAir(false);
        else if (CurrentPhase != Phase.Air) grounded = true;
        if (CurrentPhase == Phase.Landing && timer >= landingSeconds) { CurrentPhase = Phase.Idle; timer = 0f; ExecuteBuffered(true); }
        if (CurrentPhase == Phase.Backstep && timer >= definition.backstepSeconds) { CurrentPhase = Phase.Idle; timer=0; horizontalSpeed=0; ExecuteBuffered(true); }
        if (airTrialClock.IsBusy)
        {
            if (CurrentPhase != Phase.Air && !airTrialClock.GroundRecoveryAllowed) airTrialClock.Hold(TrainingAirActionClock.Pause.Landing);
            else if (wasStunned) airTrialClock.Hold(TrainingAirActionClock.Pause.Stun);
            else airTrialClock.Advance(dt);
        }
        if (airCombatActive && !airTrialClock.IsBusy) { airCombatActive = false; currentMove=null; CompletedAttacks++; ExecuteBuffered(true); }
        UpdateHurtboxes();RefreshThrowVolumes();ResolveThrowAttempt();RefreshThrowVolumes();
        if (CombatV01==null && HitboxActive && CurrentPhase!=Phase.ThrowAttempt && !attackConnected)
        {
            foreach (var selection in SelectHurtboxContacts(TrainingPhysicsQueries2D.OverlapBox(HitboxCenter,HitboxHalfSize),CurrentAttackClass))
            {
                var target = selection.Fighter;
                if (target == null || target == this) continue;
                float selectedLevel = AttackLevelFor(selection);
                if (!target.TryReceiveHurtboxAttack(selectedLevel * Tension.Bonus, damage, selectedLevel, CurrentAttackClass, currentMove != null && currentMove.causesKnockdown,selection,CurrentComparisonRule)) continue;
                if(currentMove!=null && target.LastResult=="Hit")target.ApplyGroundHitImpulse(Facing,currentMove.groundHitKnockbackSpeed);
                hitstop = TrainingHitFormula.HitstopFrames(selectedLevel) / 60f;
                attackConnected = true;
                Tension.Gain(currentMove == null ? 8f : currentMove.hitGain, moveSet == null ? .1f : moveSet.tensionLevelGainRate);
                break;
            }
        }
        if (CurrentPhase == Phase.Attack && timer >= attackStartup + attackActive + attackRecovery)
        { bool low=IsCrouchAttack;CurrentPhase = Phase.Idle; timer = 0f; CompletedAttacks++; currentMove = null;if(low)crouchPoseProgress=1f; ExecuteBuffered(true); }
        if (CombatV01==null && moveSet != null && CurrentPhase == Phase.Move && dash && horizontalSpeed * Facing > 0f) Tension.Gain(moveSet.dashGainPerSecond * dt, moveSet.tensionLevelGainRate);
        if (CurrentPhase == Phase.Move) walkTime += dt * (movement.x * Facing < 0f ? -1f : 1f);
        if (CurrentPhase == Phase.Backstep) walkTime -= dt;
        if (CurrentPhase == Phase.Idle || CurrentPhase == Phase.Crouch) idleTime += dt;
        UpdateHurtboxes();
        bool interruptsCrouchPose = IsThrowState || CurrentPhase == Phase.Takeoff || CurrentPhase == Phase.Air || CurrentPhase == Phase.Landing || (CurrentPhase == Phase.Attack&&!IsCrouchAttack);
        crouchPoseProgress = interruptsCrouchPose ? 0f : Mathf.MoveTowards(crouchPoseProgress, IsCrouching ? 1f : 0f, dt / Mathf.Max(0.02f, definition.crouchTransitionSeconds));
    }
    private void CaptureCommands(TrainingInputFrame input)
    {
        InputBuffer.RecordSample(input, LockKind, Facing, !grounded);
        if(CombatV01!=null&&input.FdHeld&&V01CanFD(input.Movement)){InputBuffer.RemovePending(c=>c!=TrainingCommand.Cancel&&c!=TrainingCommand.Jump);if(input.Commands!=null)input.Commands=System.Array.FindAll(input.Commands,c=>c==TrainingCommand.Cancel||c==TrainingCommand.Jump);input.Attack=input.Medium=input.Heavy=input.Slash=input.HeavySlash=input.Throw=input.Special=false;}
        if (input.Commands != null && input.Commands.Length > 0) { foreach (var command in input.Commands) InputBuffer.Push(DirectionalCommand(command), LockKind); return; }
        if (input.Jump) InputBuffer.Push(TrainingCommand.Jump, LockKind);
        if (input.Attack) InputBuffer.Push(TrainingCommand.Light, LockKind);
        if (input.Medium) InputBuffer.Push(TrainingCommand.Medium, LockKind);
        if (input.Heavy) InputBuffer.Push(DirectionalCommand(TrainingCommand.Heavy), LockKind);
        if (input.Slash) InputBuffer.Push(TrainingCommand.Slash, LockKind);
        if (input.HeavySlash) InputBuffer.Push(TrainingCommand.HeavySlash, LockKind);
        if(input.Throw)InputBuffer.Push(TrainingCommand.Throw,LockKind);
        if(input.Special)InputBuffer.Push(TrainingCommand.Special,LockKind);
        if (input.Cancel) InputBuffer.Push(TrainingCommand.Cancel, LockKind);
    }
    private TrainingCommand DirectionalCommand(TrainingCommand command)
    {
        int facing=grounded && (CurrentPhase==Phase.Idle||CurrentPhase==Phase.Move||CurrentPhase==Phase.Crouch) && opponent!=null ? (opponent.transform.position.x>transform.position.x?1:-1) : Facing;
        if(UsesSixLayout&&(CurrentPhase==Phase.Air||CurrentPhase==Phase.Takeoff)){if(command==TrainingCommand.StrongKick)return movement.y<-.5f?TrainingCommand.AirDownStrongKick:TrainingCommand.AirStrongKick;if(command==TrainingCommand.Heavy)return TrainingCommand.AirStrongPunch;if(command==TrainingCommand.Special2)return TrainingCommand.AirSpecial2;}
        if(UsesSixLayout&&grounded&&movement.y>-.5f&&movement.x*facing>.1f&&command==TrainingCommand.Special)return TrainingCommand.ForwardSpecial1;
        if((command==TrainingCommand.StrongKick||!UsesSixLayout&&command==TrainingCommand.Heavy)&&grounded&&movement.y<-.5f&&movement.x*facing>.1f)return TrainingCommand.Sweep;
        if(UsesSixLayout&&grounded&&movement.y<-.5f){if(command==TrainingCommand.Light)return TrainingCommand.CrouchWeakPunch;if(command==TrainingCommand.Medium)return TrainingCommand.CrouchMediumPunch;if(command==TrainingCommand.Heavy)return TrainingCommand.CrouchStrongPunch;if(command==TrainingCommand.Slash)return TrainingCommand.CrouchWeakKick;if(command==TrainingCommand.HeavySlash)return TrainingCommand.CrouchMediumKick;if(command==TrainingCommand.StrongKick)return TrainingCommand.CrouchStrongKick;}return command;
    }
    private bool CanAccept(TrainingCommand command)
    {
        if(CombatV01!=null&&Health<=0)return false;
        if (command == TrainingCommand.Cancel) return true;
        if (StunRemaining > 0f || IsDown) return false;
        if (hitstop > 0f) return false;
        if(command==TrainingCommand.Throw)return CanStartThrow;
        if (CurrentPhase == Phase.Attack) return command != TrainingCommand.Jump && currentMove != null && (attackConnected || (currentMove.command == TrainingCommand.Light && timer >= attackStartup + attackActive)) && currentMove.CanChain(command==TrainingCommand.ForwardSpecial1?TrainingCommand.Special:command == TrainingCommand.Sweep ? (UsesSixLayout?TrainingCommand.StrongKick:TrainingCommand.Heavy) : command) && moveSet.Find(command) != null;
        if (hitstop > 0f) return false;
        if (CurrentPhase == Phase.Air) return !airTrialClock.IsBusy && AirCandidateFor(command)!=null;

        if (!grounded || (movement.y < -.5f && command != TrainingCommand.Sweep && !IsCrouchMove(command)) || (CurrentPhase != Phase.Idle && CurrentPhase != Phase.Move && CurrentPhase != Phase.Crouch)) return false;
        return command == TrainingCommand.Jump || command == TrainingCommand.Light || (moveSet != null && moveSet.Find(command) != null);
    }
    private void ExecuteBuffered(bool endsLock = false)
    {
        TryQueueConfiguredReaction();
        if (!InputBuffer.TryTake(CanAccept, out var command))
        {
            if (endsLock) InputBuffer.ClearPending();
            return;
        }
        // A selected command ends this hold; newer invalid commands cannot leak
        // into the next move/stance after the current lock has been released.
        InputBuffer.ClearPending();
        if (command == TrainingCommand.Cancel) { PerformTDC(); InputBuffer.ConfirmSelected(true); return; }
        if(command==TrainingCommand.Throw){BeginThrow();InputBuffer.ConfirmSelected(true);return;}
        if (CurrentPhase == Phase.Air)
        {
            var candidate = AirCandidateFor(command);
            bool began = TryBeginAirAttackTrial(candidate); if (began) { airCombatActive = true; currentMove = AirMoveFor(candidate); attackConnected = false; } InputBuffer.ConfirmSelected(began);
            return;
        }
        if (command == TrainingCommand.Jump) { SetCrouching(false); CurrentPhase = Phase.Takeoff; timer = 0f; InputBuffer.ConfirmSelected(true); return; }
        if (CurrentPhase == Phase.Attack) ChainCount++;
        RefreshFacing(); SetCrouching(IsCrouchMove(command)); currentMove = moveSet == null ? null : moveSet.Find(command);
        ResetSpatialMotion();CurrentPhase = Phase.Attack; timer = 0f; attackConnected = false; InputBuffer.ConfirmSelected(true);
    }
    public float TdcRecoveryCostScale => grounded && CurrentPhase==Phase.Attack && StunRemaining<=0f && currentMove!=null ? currentMove.TimedTdcRecoveryScaleAt(timer) : 1f;
    public float LastTdcCostScale { get; private set; } = 1f;
    public float LastTdcBaseCost { get; private set; }
    public void PerformTRC()=>PerformTDC();
    TrainingInputConfig mappingConfig;bool UsesSixLayout{get{if(mappingConfig==null)mappingConfig=TrainingInputConfig.Runtime(Resources.Load<TrainingInputConfig>("TrainingInputConfig"));return mappingConfig!=null&&mappingConfig.layoutVersion>=2;}}
    public void PerformTDC()
    {
        StopAirDash();bool lowCancel=IsCrouchAttack;
        if(inputSource is ITrainingPendingChordSource chordSource)chordSource.ClearPendingChordInput();
        float lockFrames=RemainingLockFrames;LastTdcCostScale=TdcRecoveryCostScale;
        float cost = lockFrames*LastTdcCostScale + hitstop*60f;
        LastTdcBaseCost=lockFrames+hitstop*60f;
        if (StunRemaining > 0f || IsDown) { cost *= 2f;LastTdcBaseCost*=2f; }
        Tension.Spend(cost, moveSet == null ? .1f : moveSet.tensionLevelGainRate);LastTdcCost=(float)Tension.LastCost;
        ReleaseThrow();
        airTrialClock.End(); airCombatActive = false; currentMove = null;
        hitstop = StunRemaining = timer = 0f; attackConnected = false;
        if(CombatV01!=null){v01Stop=v01Stun=0;v01Block=false;Tension.SetTestGauge(Tension.Gauge);}
        pendingKnockdown=false; downElapsed=0; DownFollowupCount=0;
        CurrentPhase = grounded ? Phase.Idle : Phase.Air;
        SetCrouching(lowCancel&&grounded&&movement.y<-.5f); crouchPoseProgress = lowCancel?1f:0f;
        if (Mathf.Abs(horizontalSpeed) > .001f) dashInertia = true;
        InputBuffer.ClearPending(); CancelCount++;
        if(grounded && movement.y>.5f){CurrentPhase=Phase.Takeoff;timer=0;}
        UpdateHurtboxes();
        TdcPerformed?.Invoke(this);
    }
    // Compatibility for older isolated tests; current name is TrueDokodemoCansel.
    public void PerformSDC() { PerformTDC(); }
    private void SetCrouching(bool value)
    {
        IsCrouching = value;
        body.height = value ? Mathf.Clamp(definition.crouchHeight, body.radius * 2f, standingHeight) : standingHeight;
        body.center = standingCenter - Vector3.up * (standingHeight - body.height) * 0.5f;
    }
    private void ApplyGroundHitImpulse(int direction,float speed)
    {
        if(float.IsNaN(speed) || float.IsInfinity(speed) || speed<=0f || !isActiveAndEnabled || !grounded || IsDown)return;
        direction=direction<0?-1:1;
        horizontalSpeed=direction*Mathf.Max(speed,horizontalSpeed*direction);
    }
    public float DefenseFor(TrainingAttackClass attackClass)
    {
        if(CombatV01!=null)return Mathf.Max(0,definition.defenseLevel);
        float defense=IsGuarding?definition.guardDefenseLevel:definition.defenseLevel;
        if(IsGuarding && attackClass==TrainingAttackClass.Low) defense*=IsCrouching?definition.crouchingLowGuardMultiplier:definition.standingLowGuardMultiplier;
        if(IsGuarding && attackClass==TrainingAttackClass.High) defense*=IsCrouching?definition.crouchingHighGuardMultiplier:definition.standingHighGuardMultiplier;
        if(CurrentPhase==Phase.Attack && currentMove!=null && !IsGuarding) defense*=currentMove.DefenseScaleAt(timer);
        return defense*Tension.Bonus*GuardTimingMultiplier;
    }
    public void RebuildHurtboxes(){
        if(hurtboxProfile==null||!hurtboxProfile.HasValidIds())throw new System.ArgumentException("Hurtbox regions require finite defense levels and non-null unique stable IDs");
        if(Hurtboxes!=null)foreach(var old in Hurtboxes)if(old!=null){old.Collider.enabled=false;Destroy(old.gameObject);}
        Hurtboxes=new TrainingHurtbox[hurtboxProfile.regions.Length];
        for(int i=0;i<Hurtboxes.Length;i++){var r=hurtboxProfile.regions[i];var part=new GameObject("Hurtbox "+r.stableId);part.transform.SetParent(transform,false);Hurtboxes[i]=part.AddComponent<TrainingHurtbox>();Hurtboxes[i].Initialize(this,r);}
        HurtboxesChanged?.Invoke();
    }
    public float DefenseFor(TrainingAttackClass kind,TrainingHurtbox region)=>HurtboxDefense(kind,region,false);
    public float ContactDefenseFor(TrainingAttackClass kind,TrainingHurtbox region)=>HurtboxDefense(kind,region,true);
    private float HurtboxDefense(TrainingAttackClass kind,TrainingHurtbox region,bool contact){
        if(region==null||kind==TrainingAttackClass.Throw)return DefenseFor(kind);
        if(CombatV01!=null)return V01Defense(region,kind);
        bool released=contact&&(IsThrowLinked||IsThrowState);
        bool guarding=released?(Guarding||BackGuardIntent):IsGuarding;
        float timing=released?(guardHeld&&GuardAgeFrames>=0&&GuardAgeFrames<=Mathf.Max(1,definition.timedGuardWindowFrames)?1f+Mathf.Lerp(definition.timedGuardNearBonus,definition.timedGuardFarBonus,Mathf.Clamp01((GuardAgeFrames-1f)/Mathf.Max(1,definition.timedGuardWindowFrames-1))):1f):GuardTimingMultiplier;
        float defense=region.Region.BaseDefense(kind);
        if(guarding){float scale=definition.defenseLevel==0f?1f:definition.guardDefenseLevel/definition.defenseLevel;defense*=scale;
            if(kind==TrainingAttackClass.Low)defense*=IsCrouching?definition.crouchingLowGuardMultiplier:definition.standingLowGuardMultiplier;
            if(kind==TrainingAttackClass.High)defense*=IsCrouching?definition.crouchingHighGuardMultiplier:definition.standingHighGuardMultiplier;}
        else if(CurrentPhase==Phase.Attack&&currentMove!=null)defense*=currentMove.DefenseScaleAt(timer);
        return defense*Tension.Bonus*GuardTimingMultiplier;
    }
    public float AttackLevelFor(TrainingHurtbox region) {
        if(region==null||currentMove==null||currentMove.strength==null||!currentMove.strength.enabled)return attackLevel;
        float scale=CurrentPhase==Phase.Attack?currentMove.LevelScale(currentMove.attackLevelCurve,timer):1f;
        return currentMove.strength.MinimumOverBoxOverlap(HitboxCenter,HitboxHalfSize,region.Collider,transform.position,Facing,attackLevel,scale);
    }
    // Pure selection: never changes HP, clocks, gauge, guard or throw links.
    public System.Collections.Generic.List<TrainingHurtbox> SelectHurtboxContacts(Collider2D[] contacts,TrainingAttackClass kind){
        var rule=CurrentComparisonRule;
        var selected=new System.Collections.Generic.Dictionary<TrainingFighter,TrainingHurtbox>();
        foreach(var c in contacts){if(c==null)continue;var h=c.GetComponent<TrainingHurtbox>();if(h==null||h.Fighter==this||h.Fighter==null||!h.Fighter.isActiveAndEnabled||!h.Collider.enabled||!h.gameObject.activeInHierarchy)continue;
            if(!selected.TryGetValue(h.Fighter,out var prev)){selected.Add(h.Fighter,h);continue;}
            float value=rule.Final(AttackLevelFor(h)*Tension.Bonus,rule.Defense(h.Fighter.HurtboxDefense(kind,h,true))),before=rule.Final(AttackLevelFor(prev)*Tension.Bonus,rule.Defense(prev.Fighter.HurtboxDefense(kind,prev,true)));
            if(CombatV01!=null){value=(float)CombatV01Formula.HitLevel(AttackLevelFor(h),h.Fighter.V01Defense(h,kind),0,0);before=(float)CombatV01Formula.HitLevel(AttackLevelFor(prev),prev.Fighter.V01Defense(prev,kind),0,0);}
            if(TrainingContactOrder.Prefer(h.Region.selectionPriority,value,h.StableId,prev.Region.selectionPriority,before,prev.StableId))selected[h.Fighter]=h;
        }
        var result=new System.Collections.Generic.List<TrainingHurtbox>(selected.Values);
        result.Sort((a,b)=>a.Fighter.GetInstanceID().CompareTo(b.Fighter.GetInstanceID()));return result;
    }
    public bool TryReceiveHurtboxAttack(float level,float incomingDamage,float sourceLevel,TrainingAttackClass kind,bool knocksDown,TrainingHurtbox selected,TrainingComparisonRule? comparisonRule=null){
        if(selected==null||selected.Fighter!=this)return false;
        float defense=HurtboxDefense(kind,selected,true);bool accepted=ResolveAttack(level,incomingDamage,sourceLevel,kind,knocksDown,defense,comparisonRule);
        if(accepted){LastContactHurtboxId=selected.StableId;if(LastResolvedContact!=null)LastResolvedContact.partId=selected.StableId;}return accepted;
    }
    private void UpdateHurtboxes() { if(Hurtboxes!=null)foreach(var box in Hurtboxes)if(box!=null)box.Refresh(); }
    private float DownDuration => moveSet.knockdownSeconds+moveSet.downHoldSeconds+moveSet.getUpSeconds;
    private void BeginDown()
    {
        pendingKnockdown=true; downElapsed=0; DownFollowupCount=0;
        CurrentPhase=Phase.Knockdown; timer=0; StunRemaining=DownDuration;
        horizontalSpeed=0; dashInertia=false; SetCrouching(false); currentMove=null; airCombatActive=false; airTrialClock.End(); InputBuffer.ClearPending(); UpdateHurtboxes();
    }
    private void AdvanceDown(float dt)
    {
        downElapsed+=dt; StunRemaining=Mathf.Max(0,DownDuration-downElapsed);
        CurrentPhase=downElapsed<moveSet.knockdownSeconds?Phase.Knockdown:downElapsed<moveSet.knockdownSeconds+moveSet.downHoldSeconds?Phase.Down:Phase.GetUp;
        if(StunRemaining<=0){pendingKnockdown=false;CurrentPhase=Phase.Idle;timer=0;UpdateHurtboxes();ExecuteBuffered(true);}
    }
    private bool HasFloorSupport()
    {
        return body!=null&&body.HasFloorSupport();
    }
    private void EnterAir(bool jump)
    {
        SetCrouching(false);
        CurrentPhase = Phase.Air;
        timer = 0f;
        grounded = false;
        airFromJump = jump;
        crouchPoseProgress = 0f;
        verticalSpeed = jump ? jumpVelocity : 0f;
    }
    private void ResolveOpponentOverlap()
    {
        if (opponent == null) return;
        var otherBody = opponent.GetComponent<TrainingMotor2D>();
        if (otherBody == null || !otherBody.enabled) return;
        Bounds other = otherBody.bounds;
        Bounds own = body.bounds;
        // Character colliders do not support each other; this shared side pushbox
        // separates a descending character horizontally, never upwards.
        if (own.min.y >= other.max.y - 0.01f || own.max.y <= other.min.y + 0.01f) return;
        if (grounded && !opponent.grounded) return;
        float gap = body.radius + other.extents.x + 0.015f;
        float difference = transform.position.x - opponent.transform.position.x;
        if (Mathf.Abs(difference) >= gap) return;
        float side = Mathf.Abs(difference) > 0.005f ? Mathf.Sign(difference) : (Mathf.Abs(horizontalSpeed) > 0.01f ? Mathf.Sign(horizontalSpeed) : -Facing);
        if(horizontalSpeed * -side > .01f && opponent.hitstop<=0f)
        {
            float push = Mathf.Max(0f,gap-Mathf.Abs(difference));
            float low=CameraBounds==null?TrainingStage.Left:CameraBounds.Left, high=CameraBounds==null?TrainingStage.Right:CameraBounds.Right;
            if(CameraBounds!=null)CameraBounds.MovementLimits(opponent,out low,out high);
            float destinationOther=Mathf.Clamp(opponent.transform.position.x-side*push,low,high);
            otherBody.Move(Vector3.right*(destinationOther-opponent.transform.position.x));
        }
        float destination = opponent.transform.position.x + side * gap;
        float minimum=CameraBounds==null?TrainingStage.Left:CameraBounds.Left,maximum=CameraBounds==null?TrainingStage.Right:CameraBounds.Right;
        if(CameraBounds!=null)CameraBounds.MovementLimits(this,out minimum,out maximum);
        if (destination < minimum || destination > maximum)
        {
            destination = Mathf.Clamp(destination, minimum, maximum);
            float otherLow=minimum,otherHigh=maximum;if(CameraBounds!=null)CameraBounds.MovementLimits(opponent,out otherLow,out otherHigh);
            float correctedOther = Mathf.Clamp(destination - side * gap, otherLow, otherHigh);
            otherBody.Move(Vector3.right * (correctedOther - opponent.transform.position.x));
        }
        body.Move(Vector3.right * (destination - transform.position.x));
    }
    public void ReceiveAttack(float level, float incomingDamage, float sourceLevel = -1f) => TryReceiveAttack(level,incomingDamage,sourceLevel,TrainingAttackClass.Mid,false);
    public bool TryReceiveAttack(float level, float incomingDamage, float sourceLevel, TrainingAttackClass attackClass, bool knocksDown) => ResolveAttack(level,incomingDamage,sourceLevel,attackClass,knocksDown,null);
    private bool ResolveAttack(float level, float incomingDamage, float sourceLevel, TrainingAttackClass attackClass, bool knocksDown, float? defenseOverride, TrainingComparisonRule? comparisonRule=null)
    {
        if(!isActiveAndEnabled)return false;
        StopAirDash();bool fdEligible=FdActive;
        if(IsThrowLinked||IsThrowState)ReleaseThrow();
        if(IsDown)
        {
            // Optional second stage: bounded damage only; never restart/extend this down episode.
            if(moveSet==null||!moveSet.allowDownFollowup||CurrentPhase!=Phase.Down||DownFollowupCount>=Mathf.Max(0,moveSet.maxDownFollowups))return false;
            Health=Mathf.Max(0,Health-incomingDamage*Mathf.Clamp01(moveSet.downFollowupDamageScale));HitCount++;DownFollowupCount++;LastResult="Hit";
            hitstop=(sourceLevel<0?level:sourceLevel)*.2f/60f;UpdateHurtboxes();return true;
        }
        airTrialClock.Hold(TrainingAirActionClock.Pause.Stun);
        var rule=comparisonRule??TrainingComparisonRule.Legacy;
        float defense = rule.Defense(defenseOverride ?? DefenseFor(attackClass));
        float finalResult=rule.Final(level,defense);
        bool continuedCombo=LastResult=="Hit"&&StunRemaining>0;float healthBefore=Health;
        float baseLevel = sourceLevel < 0f ? level : sourceLevel;
        LastLevelDifference = TrainingHitFormula.Result(level, defense);
        if (rule.IsHit(finalResult))
        {
            Health = Mathf.Max(0f, Health - incomingDamage); HitCount++; LastResult = "Hit";
            StunRemaining = Mathf.Max(0f,TrainingHitFormula.HitstunFrames(baseLevel, LastLevelDifference)) / 60f;
            if (grounded) CurrentPhase = Phase.Hitstun;
            // A successful sweep knocks down; a level-based guard never does.
            if(knocksDown && moveSet!=null){pendingKnockdown=true;StunRemaining=DownDuration;airCombatActive=false;airTrialClock.End();currentMove=null;InputBuffer.ClearPending();if(grounded)BeginDown();}
        }
        else
        {
            if(fdEligible)ApplyFdGuard(baseLevel,attackClass);
            Health=Mathf.Max(0,Health-incomingDamage*rule.guardDamageScale);
            GuardCount++; LastResult = "Guard";
            StunRemaining = TrainingHitFormula.GuardstunFrames(baseLevel, defense) / 60f;
            if (grounded) CurrentPhase = Phase.GuardStun;
        }
        hitstop = TrainingHitFormula.HitstopFrames(baseLevel) / 60f; timer = 0f;
        LastResolvedContact=new TrainingResolvedContact{sequence=++ContactSequence,rawAttack=baseLevel,effectiveAttack=level,defense=defense,finalResult=finalResult,threshold=rule.threshold,formula=rule.formula,hit=LastResult=="Hit",continuedCombo=continuedCombo,damage=healthBefore-Health,stunFrames=StunRemaining*60,hitstopFrames=hitstop*60};
        if (moveSet != null) Tension.Gain(moveSet.receiverGain, moveSet.tensionLevelGainRate);
        UpdateHurtboxes();return true;
    }
    private void GetPresentationSample(out string state, out float sample)
    {
        sample = 1f;
        state = "Jump";
        bool hasCrouchClips = definition.crouchStartClip != null && definition.crouchIdleClip != null && definition.crouchEndClip != null;
        if(IsThrowState)
        {
            state=CurrentPhase==Phase.Thrown?"Idle":"Jab";
            sample=CurrentPhase==Phase.Thrown?0:CurrentPhase==Phase.ThrowHold?.55f:Mathf.Lerp(0,.55f,Mathf.Clamp01(timer/Mathf.Max(.01f,ThrowSettings.startupFrames/60)));
        }
        else if (IsDown && grounded)
        {
            if(CurrentPhase==Phase.GetUp){state="GetUpBackGameplay";sample=Mathf.Clamp01((downElapsed-moveSet.knockdownSeconds-moveSet.downHoldSeconds)/Mathf.Max(.02f,moveSet.getUpSeconds));}
            else {state="KnockdownGameplay";sample=CurrentPhase==Phase.Down?1f:Mathf.Clamp01(downElapsed/Mathf.Max(.02f,moveSet.knockdownSeconds));}
        }
        else if (hasCrouchClips && !IsCrouchAttack && (IsCrouching || crouchPoseProgress > 0f))
        {
            if (!IsCrouching) { state = "CrouchEnd"; sample = 1f - crouchPoseProgress; }
            else if (crouchPoseProgress < 1f) { state = "CrouchStart"; sample = crouchPoseProgress; }
            else { state = "CrouchIdle"; sample = Mathf.Repeat(idleTime / definition.crouchIdleClip.length, 1f); }
        }
        else if ((CurrentPhase == Phase.Idle || CurrentPhase == Phase.Crouch) && definition.idleClip != null) { state = "Idle"; sample = Mathf.Repeat(idleTime / definition.idleClip.length, 1f); }
        else if (CurrentPhase == Phase.Attack && (currentMove == null ? definition.jabClip : currentMove.clip) != null)
        {
            state = currentMove == null ? "Jab" : currentMove.state;
            float contact = currentMove == null ? definition.jabContactNormalized : currentMove.contactNormalized;
            float activeEnd = Mathf.Min(1f, contact + 0.08f);
            if (timer < attackStartup) sample = Mathf.Lerp(0f, contact, timer / attackStartup);
            else if (timer < attackStartup + attackActive) sample = Mathf.Lerp(contact, activeEnd, (timer - attackStartup) / attackActive);
            else sample = Mathf.Lerp(activeEnd, 1f, Mathf.Clamp01((timer - attackStartup - attackActive) / attackRecovery));
        }
        else if (CurrentPhase == Phase.Backstep && locomotion != null && locomotion.backstep != null)
        { state = "BackstepGameplay"; sample = Mathf.Clamp01(timer / Mathf.Max(.02f, definition.backstepSeconds)); }
        else if (CurrentPhase == Phase.Move && horizontalSpeed * Facing > .01f && locomotion != null)
        {
            bool running=dash || Mathf.Abs(horizontalSpeed)>walkSpeed+.1f;
            var clip=running?locomotion.forwardDash:locomotion.forwardStep;
            if(clip!=null){state=running?"ForwardDash":"ForwardStepGameplay";float speed=running?locomotion.runSpeedReference:locomotion.stepSpeedReference;sample=Mathf.Repeat(locomotionDistance/Mathf.Max(.01f,speed*clip.length),1f);}
            else {state="Walk";sample=Mathf.Repeat(walkTime,1f);}
        }
        else if (CurrentPhase == Phase.Move || CurrentPhase == Phase.Backstep) { state = "Walk"; sample = Mathf.Repeat(walkTime, 1f); }
        else if (CurrentPhase == Phase.Takeoff) sample = Mathf.Lerp(0f, 0.12f, Mathf.Clamp01(timer / takeoffSeconds));
        else if (CurrentPhase == Phase.Air)
        {
            if (airCombatActive && airTrialClock.IsBusy)
            { state = currentMove!=null?currentMove.state:AirTrialCandidate == moveSet.flyingKick ? "FlyingKickAttackPreview" : "DropKickAttackPreview"; sample = airTrialClock.Normalized; }
            else sample = verticalSpeed > 0f && airFromJump
                ? Mathf.Lerp(0.12f, 0.48f, Mathf.Clamp01(1f - verticalSpeed / jumpVelocity))
                : Mathf.Lerp(0.48f, 0.78f, Mathf.Clamp01(-verticalSpeed / jumpVelocity));
        }
        else if (CurrentPhase == Phase.Landing) sample = Mathf.Lerp(0.78f, 1f, Mathf.Clamp01(timer / landingSeconds));
    }
    private void LateUpdate()
    {
        GetPresentationSample(out string state, out float sample);
        if (presentation != null) { presentation.Present(this, state, sample); return; }
        bool hasCrouchClips = definition.crouchStartClip != null && definition.crouchIdleClip != null && definition.crouchEndClip != null;
        if (visual == null || !visual.isInitialized) return;
        if (postureBones == null)
        {
            int count = (int)HumanBodyBones.LastBone;
            postureBones = new Transform[count]; previousBonePositions = new Vector3[count]; previousBoneRotations = new Quaternion[count];
            for (int i = 0; i < count; i++)
            {
                postureBones[i] = visual.GetBoneTransform((HumanBodyBones)i);
                if (postureBones[i] == null) continue;
                previousBonePositions[i] = postureBones[i].localPosition; previousBoneRotations[i] = postureBones[i].localRotation;
            }
        }
        float visualYaw = FacingStyle == VisualFacingStyle.ThreeQuarter ? (Facing > 0 ? 10f : 170f) : (Facing > 0 ? 0f : 180f);
        visual.transform.localRotation = Quaternion.Euler(0f, visualYaw, 0f);
        string displayedState = FacingStyle != VisualFacingStyle.Current && Facing < 0 ? state + "Left" : state;
        if (displayedState != state && !visual.HasState(0, Animator.StringToHash(displayedState))) displayedState = state;
        if(!visual.HasState(0,Animator.StringToHash(displayedState))&&HasSampleMotion(state))displayedState="Idle";
        visual.Play(displayedState, 0, sample);
        visual.Update(0f);
        ApplySampleMotion(state,sample);
        visual.transform.localPosition = airCombatActive && AirTrialCandidate != null ? AirTrialCandidate.PoseOffsetFor(visual.transform.localRotation, displayedState.EndsWith("Left")) : Vector3.zero;
        bool locomotionPose=state=="ForwardDash"||state=="ForwardStepGameplay"||state=="BackstepGameplay";
        if((locomotionPose || (IsDown && grounded))&&footReferenceReady){var hip=visual.transform.InverseTransformPoint(visual.GetBoneTransform(HumanBodyBones.Hips).position);var correction=new Vector3(standingHipLocal.x-hip.x,0,standingHipLocal.z-hip.z);visual.transform.localPosition=visual.transform.localRotation*correction;}
        if((locomotionPose||state=="Walk")&&grounded&&footReferenceReady)
        {
            float foot=Mathf.Min(visual.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,visual.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
            float toe=Mathf.Min(visual.GetBoneTransform(HumanBodyBones.LeftToes).position.y,visual.GetBoneTransform(HumanBodyBones.RightToes).position.y);
            float lift=Mathf.Max(0f,transform.position.y+standingFootHeight-foot,transform.position.y+standingToeHeight-toe);
            visual.transform.localPosition+=Vector3.up*lift;
        }
        if(state=="BackstepGameplay"&&locomotion!=null)visual.transform.localPosition+=Vector3.up*Mathf.Sin(Mathf.PI*sample)*locomotion.backstepHopHeight;

        bool crouchAnimation = hasCrouchClips && !IsCrouchAttack && (IsCrouching || crouchPoseProgress > 0f);
        if (!footReferenceReady && CurrentPhase == Phase.Idle && !crouchAnimation)
        {
            standingFootHeight = Mathf.Min(visual.GetBoneTransform(HumanBodyBones.LeftFoot).position.y, visual.GetBoneTransform(HumanBodyBones.RightFoot).position.y) - transform.position.y;
            standingToeHeight=Mathf.Min(visual.GetBoneTransform(HumanBodyBones.LeftToes).position.y,visual.GetBoneTransform(HumanBodyBones.RightToes).position.y)-transform.position.y;
            standingHipLocal=visual.transform.InverseTransformPoint(visual.GetBoneTransform(HumanBodyBones.Hips).position);
            footReferenceReady = true;
        }
        if(IsCrouchAttack){wasCrouchAnimation=false;postureBlendRemaining=0;}
        if (IsThrowState || (CurrentPhase == Phase.Attack&&!IsCrouchAttack) || CurrentPhase == Phase.Takeoff || CurrentPhase == Phase.Air || CurrentPhase == Phase.Landing)
        { wasCrouchAnimation = false; postureBlendRemaining = 0f; }
        if (wasCrouchAnimation && !crouchAnimation) postureBlendRemaining = 0.1f;
        bool blendPosture = crouchAnimation || postureBlendRemaining > 0f || (IsCrouchAttack&&timer<.08f);
        float postureWeight = hitstop > 0f ? 0f : 1f - Mathf.Exp(-Time.deltaTime / 0.035f);
        for (int i = 0; i < postureBones.Length; i++)
        {
            var bone = postureBones[i]; if (bone == null) continue;
            if (blendPosture)
            {
                bone.localPosition = Vector3.Lerp(previousBonePositions[i], bone.localPosition, postureWeight);
                bone.localRotation = Quaternion.Slerp(previousBoneRotations[i], bone.localRotation, postureWeight);
            }
            previousBonePositions[i] = bone.localPosition; previousBoneRotations[i] = bone.localRotation;
        }
        if (hitstop <= 0f) postureBlendRemaining = Mathf.Max(0f, postureBlendRemaining - Time.deltaTime);
        wasCrouchAnimation = crouchAnimation;
        if (hasCrouchClips && (blendPosture||IsCrouchAttack) && grounded)
        {
            // Retargeted crouch feet can sit below the standing foot reference.
            // Anchor only the pose; never move or scale the physics/visual root.
            var hips = visual.GetBoneTransform(HumanBodyBones.Hips);
            float footHeight = Mathf.Min(visual.GetBoneTransform(HumanBodyBones.LeftFoot).position.y, visual.GetBoneTransform(HumanBodyBones.RightFoot).position.y) - transform.position.y;
            hips.position += Vector3.up * (standingFootHeight - footHeight);
        }
        if (IsCrouching && !hasCrouchClips) ApplyTemporaryCrouchPose();
        ApplyThrowPose();
        if (CurrentPhase==Phase.Attack && currentMove!=null && currentMove.command==TrainingCommand.Sweep) ApplySweepPose();
        for (int i = 0; i < materials.Length; i++)
            materials[i].color = CurrentPhase==Phase.Thrown?Color.Lerp(baseColors[i],new Color(.7f,.25f,1),.4f):StunRemaining > 0f ? Color.Lerp(baseColors[i], LastResult == "Hit" ? Color.red : Color.cyan, 0.6f) : (IsGuarding ? Color.Lerp(baseColors[i], Color.blue, 0.3f) : baseColors[i]);
        if (CurrentPhase == Phase.Attack && definition.jabClip == null)
        {
            // Temporary jab pose using existing humanoid bones, not a final attack animation.
            float weight = timer < attackStartup ? timer / attackStartup : 1f - Mathf.Clamp01((timer - attackStartup - attackActive) / attackRecovery);
            Transform arm = visual.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform forearm = visual.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Transform hand = visual.GetBoneTransform(HumanBodyBones.RightHand);
            if (arm != null && forearm != null && hand != null)
            {
                Vector3 aim = Vector3.forward * Facing + Vector3.up * 0.1f;
                arm.rotation = Quaternion.Slerp(arm.rotation, Quaternion.FromToRotation(forearm.position - arm.position, aim) * arm.rotation, weight);
                forearm.rotation = Quaternion.Slerp(forearm.rotation, Quaternion.FromToRotation(hand.position - forearm.position, aim) * forearm.rotation, weight);
            }
        }
        ApplyFocusedMediumKickGrounding(state);
    }
    private void ApplySweepPose()
    {
        float weight=timer<attackStartup?Mathf.Clamp01(timer/attackStartup):1f-Mathf.Clamp01((timer-attackStartup-attackActive)/attackRecovery);
        var foot=visual.GetBoneTransform(HumanBodyBones.RightFoot);
        if(foot==null)return;
        var target=transform.position+new Vector3(0,.14f,.78f*Facing);
        BendLeg(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,foot,Vector3.Lerp(foot.position,target,weight));
    }
    private void ApplyTemporaryCrouchPose()
    {
        // Placeholder articulated pose, not a dedicated crouch animation or scale change.
        var hips = visual.GetBoneTransform(HumanBodyBones.Hips);
        var leftFoot = visual.GetBoneTransform(HumanBodyBones.LeftFoot);
        var rightFoot = visual.GetBoneTransform(HumanBodyBones.RightFoot);
        if (hips == null || leftFoot == null || rightFoot == null) return;
        Vector3 leftTarget = leftFoot.position, rightTarget = rightFoot.position;
        Quaternion leftRotation = leftFoot.rotation, rightRotation = rightFoot.rotation;
        hips.position -= Vector3.up * ((standingHeight - body.height) * 0.95f);
        BendLeg(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, leftFoot, leftTarget);
        BendLeg(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, rightFoot, rightTarget);
        leftFoot.rotation = leftRotation; rightFoot.rotation = rightRotation;
    }
    private void BendLeg(HumanBodyBones upperBone, HumanBodyBones lowerBone, Transform foot, Vector3 target)
    {
        Transform upper = visual.GetBoneTransform(upperBone), lower = visual.GetBoneTransform(lowerBone);
        if (upper == null || lower == null) return;
        float upperLength = Vector3.Distance(upper.position, lower.position), lowerLength = Vector3.Distance(lower.position, foot.position);
        Vector3 delta = target - upper.position;
        float distance = Mathf.Clamp(delta.magnitude, 0.001f, upperLength + lowerLength - 0.001f);
        Vector3 direction = delta.normalized;
        float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2f * distance);
        float bend = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
        Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward * Facing, direction).normalized;
        Vector3 knee = upper.position + direction * along + forward * bend;
        upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, knee - upper.position) * upper.rotation;
        lower.rotation = Quaternion.FromToRotation(foot.position - lower.position, target - lower.position) * lower.rotation;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = HitboxActive ? Color.red : Color.yellow;
        Gizmos.DrawWireCube(HitboxCenter, HitboxHalfSize * 2f);
    }
    private void OnDestroy() { ReleaseThrow();if (materials != null) foreach (var material in materials) Destroy(material); }
}





public sealed partial class TrainingFighter
{
 static bool IsCrouchMove(TrainingCommand c)=>c==TrainingCommand.CrouchWeakKick||c==TrainingCommand.CrouchMediumKick||c==TrainingCommand.CrouchStrongKick||c==TrainingCommand.CrouchWeakPunch||c==TrainingCommand.CrouchMediumPunch||c==TrainingCommand.CrouchStrongPunch;
 bool IsCrouchKickAttack=>IsCrouchAttack&&(currentMove.command==TrainingCommand.CrouchWeakKick||currentMove.command==TrainingCommand.CrouchMediumKick||currentMove.command==TrainingCommand.CrouchStrongKick);
 bool IsCrouchAttack=>CurrentPhase==Phase.Attack&&currentMove!=null&&IsCrouchMove(currentMove.command);
}

public sealed partial class TrainingFighter{
 TrainingAirAttackCandidate AirCandidateFor(TrainingCommand c){if(moveSet==null)return null;if(c==(UsesSixLayout?TrainingCommand.Slash:TrainingCommand.Light))return moveSet.flyingKick;if(c==(UsesSixLayout?TrainingCommand.HeavySlash:TrainingCommand.Medium))return moveSet.dropKick;if(!UsesSixLayout)return null;return c==TrainingCommand.AirStrongKick?moveSet.airStrongKick:c==TrainingCommand.AirDownStrongKick?moveSet.airDownStrongKick:c==TrainingCommand.AirStrongPunch?moveSet.airStrongPunch:c==TrainingCommand.AirSpecial2?moveSet.airSpecial2:null;}
 TrainingMove AirMoveFor(TrainingAirAttackCandidate c){if(moveSet==null||c==null)return null;return c==moveSet.airStrongKick?moveSet.airStrongKickMove:c==moveSet.airDownStrongKick?moveSet.airDownStrongKickMove:c==moveSet.airStrongPunch?moveSet.airStrongPunchMove:c==moveSet.airSpecial2?moveSet.airSpecial2Move:null;}
}
