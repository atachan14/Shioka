using UnityEngine;
using UnityEngine.InputSystem;
public interface ITrainingResettableInputSource { void Clear(); }
[RequireComponent(typeof(TrainingFighter))]
public sealed partial class TrainingSecondPlayerControls:MonoBehaviour,ITrainingInputSource,ITrainingResettableInputSource,ITrainingPendingChordSource
{
    bool optionBlocked,awaitNeutral;
    public void SetOptionBlocked(bool blocked){optionBlocked=blocked;Clear();if(blocked)map?.Disable();else{awaitNeutral=true;if(isActiveAndEnabled)map?.Enable();}}
    bool SuppressOptionInput(){if(optionBlocked){Clear();return true;}if(!awaitNeutral)return false;Clear();if(map!=null){foreach(var action in map.actions){if(action.name=="Move"){if(action.ReadValue<Vector2>().sqrMagnitude>.01f)return true;}else if(action.IsPressed())return true;}}awaitNeutral=false;return true;}

 InputActionMap map;TrainingInputFrame input;readonly System.Collections.Generic.List<TrainingCommand> commands=new System.Collections.Generic.List<TrainingCommand>();
 int lastDirection,dashDirection;double lastTime=-100;float tapWindow;
 public void Configure(TrainingInputConfig config)
 {
  config=TrainingInputConfig.Runtime(config);if(config.layoutVersion>=2){ConfigureSix(config);return;}
  map?.Dispose();map=config.inputActions.FindActionMap(config.secondPlayerMap,true).Clone();tapWindow=config.doubleTapSeconds;
  map["Move"].performed+=c=>{input.Movement=c.ReadValue<Vector2>();if(input.Movement.x*dashDirection<=.1f)dashDirection=0;};
  map["Move"].canceled+=c=>{input.Movement=Vector2.zero;dashDirection=0;};
  Bind("Jump",TrainingCommand.Jump);Bind("Light",TrainingCommand.Light);Bind("Medium",TrainingCommand.Medium);Bind("Heavy",TrainingCommand.Heavy);Bind("Slash",TrainingCommand.Slash);Bind("HeavySlash",TrainingCommand.HeavySlash);Bind("Cancel",TrainingCommand.Cancel);
  map["Dash"].performed+=c=>{input.Dash=input.DashPressed=true;};map["Dash"].canceled+=c=>input.Dash=false;
  map.AddAction("Throw",InputActionType.Button,config.secondThrowKeyboardBinding);Bind("Throw",TrainingCommand.Throw);
  map.AddAction("Special",InputActionType.Button,config.secondSpecialKeyboardBinding);Bind("Special",TrainingCommand.Special);
var fd=map.AddAction("FD",InputActionType.Button,config.secondFdKeyboardBinding);
  fd.performed+=c=>input.FdHeld=true;fd.canceled+=c=>input.FdHeld=false;
  Tap("LeftTap",-1);Tap("RightTap",1);Clear();if(isActiveAndEnabled&&!optionBlocked)map.Enable();
 }
 public int QueuedCommandCount=>commands.Count;
 void Enqueue(TrainingCommand command){var f=GetComponent<TrainingFighter>();if(optionBlocked||awaitNeutral||f==null||!f.isActiveAndEnabled)return;if(commands.Count>=32)commands.RemoveAt(0);commands.Add(command);}
 void Bind(string name,TrainingCommand command){map[name].performed+=c=>Enqueue(command);}
 void Tap(string name,int direction){map[name].performed+=c=>{double now=Time.unscaledTimeAsDouble;if(lastDirection==direction&&now-lastTime<=tapWindow){dashDirection=direction;input.DashPressed=true;}lastDirection=direction;lastTime=now;};}
 public void Clear(){input=default;commands.Clear();lastDirection=dashDirection=0;lastTime=-100;sixRouter?.Clear();}
 public TrainingInputFrame ReadInput(){var f=GetComponent<TrainingFighter>();if(f==null||!f.isActiveAndEnabled){Clear();return default;}if(SuppressOptionInput())return default;sixRouter?.Tick(UnityEngine.InputSystem.LowLevel.InputState.currentTime);var result=input;result.FdHeld|=sixRouter!=null&&sixRouter.FdHeld;result.Dash=input.Dash||(dashDirection!=0&&input.Movement.x*dashDirection>.1f);result.Commands=commands.ToArray();commands.Clear();input.DashPressed=false;return result;}
 void OnEnable(){Clear();if(!optionBlocked)map?.Enable();}
 void OnDisable(){map?.Disable();Clear();}
 void OnApplicationFocus(bool focused){if(!focused){map?.Disable();Clear();}else if(isActiveAndEnabled&&!optionBlocked)map?.Enable();}
 void OnDestroy(){map?.Dispose();}
}



public sealed partial class TrainingSecondPlayerControls
{
 TrainingSixButtonRouter sixRouter;public void ClearPendingChordInput()=>sixRouter?.Clear();
 void ConfigureSix(TrainingInputConfig c){map?.Dispose();tapWindow=c.doubleTapSeconds;sixRouter=new TrainingSixButtonRouter(c,Enqueue,()=>GetComponent<TrainingFighter>().CombatV01==null||GetComponent<TrainingFighter>().V01CanFD(input.Movement));map=TrainingSixButtonMapping.Create(c,true,sixRouter,Enqueue,value=>{input.Movement=value;if(value.x*dashDirection<=.1f)dashDirection=0;},value=>input.GuardHeld=value,value=>input.FdHeld=value,value=>{input.Dash=value;if(value)input.DashPressed=true;},()=>{},()=>{});map.AddAction("LeftTap",UnityEngine.InputSystem.InputActionType.Button,"<Keyboard>/numpad4");map.AddAction("RightTap",UnityEngine.InputSystem.InputActionType.Button,"<Keyboard>/numpad6");Tap("LeftTap",-1);Tap("RightTap",1);Clear();if(isActiveAndEnabled&&!optionBlocked)map.Enable();}
}
