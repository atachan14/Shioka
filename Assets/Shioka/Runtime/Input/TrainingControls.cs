using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(TrainingFighter))]
public sealed partial class TrainingControls : MonoBehaviour, ITrainingInputSource, ITrainingResettableInputSource, ITrainingPendingChordSource
{
    bool optionBlocked,awaitNeutral;
    public void SetOptionBlocked(bool blocked){optionBlocked=blocked;Clear();if(blocked)map?.Disable();else{awaitNeutral=true;if(isActiveAndEnabled)map?.Enable();}}
    bool SuppressOptionInput(){if(optionBlocked){Clear();return true;}if(!awaitNeutral)return false;Clear();if(map!=null){foreach(var action in map.actions){if(action.name=="Move"){if(action.ReadValue<Vector2>().sqrMagnitude>.01f)return true;}else if(action.IsPressed())return true;}}awaitNeutral=false;return true;}

    private TrainingFighter fighter;
    private InputActionMap map;
    private TrainingInputFrame input;
    private readonly System.Collections.Generic.List<TrainingCommand> commands = new System.Collections.Generic.List<TrainingCommand>();
    private int doubleTapDirection, lastTapDirection;
    private double lastTapTime = -100;
    public float doubleTapSeconds = .25f;
    private void Awake()
    {
        fighter = GetComponent<TrainingFighter>();
        var config=TrainingInputConfig.Runtime(Resources.Load<TrainingInputConfig>("TrainingInputConfig"));
        if(config!=null&&config.layoutVersion>=2){ConfigureSix(config);return;}
        map = new InputActionMap("Training");
        var move = map.AddAction("Move", InputActionType.Value);
        move.AddCompositeBinding("2DVector(mode=1)").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector(mode=1)").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        move.AddBinding(config==null?"<Gamepad>/leftStick":config.firstMoveStickGamepadBinding); move.AddBinding(config==null?"<Gamepad>/dpad":config.firstMoveDpadGamepadBinding);
        move.performed += ctx => { input.Movement = ctx.ReadValue<Vector2>(); if(input.Movement.x * doubleTapDirection <= .1f) doubleTapDirection=0; };
        move.canceled += ctx => { input.Movement = Vector2.zero; doubleTapDirection=0; };
        var jump = map.AddAction("Jump", InputActionType.Button);
        jump.AddBinding("<Keyboard>/w"); jump.AddBinding("<Keyboard>/upArrow"); jump.AddBinding(config==null?"<Gamepad>/buttonSouth":config.firstJumpGamepadBinding);
        jump.performed += ctx => Enqueue(TrainingCommand.Jump);
        var attack = map.AddAction("Attack", InputActionType.Button);
        attack.AddBinding("<Keyboard>/j"); attack.AddBinding(config==null?"<Gamepad>/buttonWest":config.firstLightGamepadBinding);
        attack.performed += ctx => Enqueue(TrainingCommand.Light);
        BindCommand("Medium", "<Keyboard>/k", config==null?"<Gamepad>/buttonNorth":config.firstMediumGamepadBinding, TrainingCommand.Medium);
        BindCommand("Heavy", "<Keyboard>/l", config==null?"<Gamepad>/buttonEast":config.firstHeavyGamepadBinding, TrainingCommand.Heavy);
        BindCommand("Slash", "<Keyboard>/m", config==null?"<Gamepad>/leftShoulder":config.firstSlashGamepadBinding, TrainingCommand.Slash);
        BindCommand("HeavySlash", "<Keyboard>/comma", config==null?"<Gamepad>/leftTrigger":config.firstHeavySlashGamepadBinding, TrainingCommand.HeavySlash);
        BindCommand("Cancel", "<Keyboard>/period", config==null?"<Gamepad>/rightTrigger":config.firstCancelGamepadBinding, TrainingCommand.Cancel);
        var dash = map.AddAction("Dash", InputActionType.Button);
        dash.AddBinding("<Keyboard>/space"); dash.AddBinding(config==null?"<Gamepad>/rightShoulder":config.firstDashGamepadBinding);
        dash.performed += ctx => { input.Dash = true; input.DashPressed = true; };
        dash.canceled += ctx => input.Dash = false;
        BindCommand("Throw",config==null?"<Keyboard>/i":config.firstThrowKeyboardBinding,config==null?"<Gamepad>/leftStickPress":config.firstThrowGamepadBinding,TrainingCommand.Throw);
        var fd=map.AddAction("Mania",InputActionType.Button,"<Keyboard>/leftShift");
        fd.AddBinding("<Keyboard>/rightShift");
        fd.performed+=ctx=>input.FdHeld=true;fd.canceled+=ctx=>input.FdHeld=false;
        var reset = map.AddAction("Reset", InputActionType.Button, "<Keyboard>/leftCtrl");reset.AddBinding("<Keyboard>/rightCtrl");
        reset.performed += ctx => input.Reset = true;
        var guard = map.AddAction("Guard", InputActionType.Button, "<Keyboard>/g");
        guard.performed += ctx => input.ToggleOpponentGuard = true;
        BindDirectionTap("LeftTap","<Keyboard>/a","<Keyboard>/leftArrow",-1);
        BindDirectionTap("RightTap","<Keyboard>/d","<Keyboard>/rightArrow",1);
    }
    public int QueuedCommandCount => commands.Count;
    private void Enqueue(TrainingCommand command){if(optionBlocked||awaitNeutral||fighter==null||!fighter.isActiveAndEnabled)return;if(commands.Count>=32)commands.RemoveAt(0);commands.Add(command);}
    public void Clear(){input=default;commands.Clear();deviceInput?.Clear();ClearTap();sixRouter?.Clear();deviceInput?.Clear();}
    private void OnEnable() { Clear();if(!optionBlocked)map?.Enable(); }
    private void BindDirectionTap(string name,string keyboard,string arrow,int direction)
    {
        var action=map.AddAction(name,InputActionType.Button);action.AddBinding(keyboard);action.AddBinding(arrow);
        action.performed+=ctx=>{double now=Time.unscaledTimeAsDouble;if(lastTapDirection==direction&&now-lastTapTime<=doubleTapSeconds){doubleTapDirection=direction;input.DashPressed=true;}lastTapDirection=direction;lastTapTime=now;};
    }
    private void ClearTap(){doubleTapDirection=lastTapDirection=0;lastTapTime=-100;}
    private void BindCommand(string name, string keyboard, string gamepad, TrainingCommand command)
    {
        var action = map.AddAction(name, InputActionType.Button);
        action.AddBinding(keyboard); if(!string.IsNullOrEmpty(gamepad))action.AddBinding(gamepad);
        action.performed += ctx => Enqueue(command);
    }
    public TrainingInputFrame ReadInput()
    {
        if(fighter==null||!fighter.isActiveAndEnabled){Clear();return default;}
        if(SuppressOptionInput())return default;if(deviceInput!=null){var frame=deviceInput.Read(fighter.TrainingSession!=null);if(frame.Movement.x*doubleTapDirection<=.1f)doubleTapDirection=0;frame.Dash|=doubleTapDirection!=0&&frame.Movement.x*doubleTapDirection>.1f;frame.DashPressed|=input.DashPressed;input.DashPressed=false;return frame;}
        sixRouter?.Tick(UnityEngine.InputSystem.LowLevel.InputState.currentTime);
        TrainingInputFrame result = input;result.FdHeld|=sixRouter!=null&&sixRouter.FdHeld;
        result.Dash = input.Dash || (doubleTapDirection != 0 && input.Movement.x * doubleTapDirection > .1f);
        result.Commands = commands.ToArray(); commands.Clear();
        input.DashPressed = input.Jump = input.Attack = input.Reset = input.ToggleOpponentGuard = false;
        if(result.Reset)ClearTap();
        return result;
    }
    private void OnDisable() { map?.Disable(); Clear(); }
    private void OnApplicationFocus(bool focused)
    {
        if (!focused) { map?.Disable(); Clear(); }
        else if (isActiveAndEnabled&&!optionBlocked) map?.Enable();
    }
    private void OnDestroy() { deviceInput?.Detach();map?.Dispose(); }
}



public sealed partial class TrainingControls
{
 TrainingDeviceInput deviceInput;
 TrainingSixButtonRouter sixRouter;public void ClearPendingChordInput(){sixRouter?.Clear();deviceInput?.Clear();}
 void ConfigureSix(TrainingInputConfig c){if(c.layoutVersion>=3){deviceInput=new TrainingDeviceInput(c,false);map=deviceInput.Map;doubleTapSeconds=c.doubleTapSeconds;BindDirectionTap("LeftTap","<Keyboard>/a","<Keyboard>/leftArrow",-1);BindDirectionTap("RightTap","<Keyboard>/d","<Keyboard>/rightArrow",1);return;}doubleTapSeconds=c.doubleTapSeconds;sixRouter=new TrainingSixButtonRouter(c,Enqueue,()=>(fighter.TrainingSession==null?fighter:fighter.TrainingSession.InputTargetFor(fighter)).CombatV01==null||(fighter.TrainingSession==null?fighter:fighter.TrainingSession.InputTargetFor(fighter)).V01CanFD(input.Movement));map=TrainingSixButtonMapping.Create(c,false,sixRouter,Enqueue,value=>{input.Movement=value;if(value.x*doubleTapDirection<=.1f)doubleTapDirection=0;},value=>input.GuardHeld=value,value=>input.FdHeld=value,value=>{input.Dash=value;if(value)input.DashPressed=true;},()=>input.Reset=true,()=>input.ToggleOpponentGuard=true);BindDirectionTap("LeftTap","<Keyboard>/a","<Keyboard>/leftArrow",-1);BindDirectionTap("RightTap","<Keyboard>/d","<Keyboard>/rightArrow",1);}
 public void ReloadBindings(){var config=TrainingInputConfig.Runtime(Resources.Load<TrainingInputConfig>("TrainingInputConfig"));deviceInput?.Detach();map?.Disable();map?.Dispose();map=null;deviceInput=null;ConfigureSix(config);Clear();awaitNeutral=true;if(isActiveAndEnabled&&!optionBlocked)map?.Enable();}
}
