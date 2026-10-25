using System;using UnityEngine;using UnityEngine.InputSystem;
public static class TrainingSixButtonMapping
{
 public static InputActionMap Create(TrainingInputConfig c,bool second,TrainingSixButtonRouter router,Action<TrainingCommand> emit,Action<Vector2> movement,Action<bool> guard,Action<bool> fd,Action<bool> dash,Action reset,Action toggle)
 {
  var map=new InputActionMap(second?"Training2P-Six":"Training-Six");var move=map.AddAction("Move",InputActionType.Value);
  var composite=move.AddCompositeBinding("2DVector(mode=1)");if(second)composite.With("Up","<Keyboard>/numpad8").With("Down","<Keyboard>/numpad5").With("Left","<Keyboard>/numpad4").With("Right","<Keyboard>/numpad6");else{composite.With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");move.AddCompositeBinding("2DVector(mode=1)").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");Add(move,c.firstMoveStickGamepadBinding);Add(move,c.firstMoveDpadGamepadBinding);}move.performed+=x=>movement(x.ReadValue<Vector2>());move.canceled+=x=>movement(Vector2.zero);
  string[] keyboard=second?new[]{c.secondWeakPKeyboardBinding,c.secondMediumPKeyboardBinding,c.secondStrongPKeyboardBinding,c.secondWeakKKeyboardBinding,c.secondMediumKKeyboardBinding,c.secondStrongKKeyboardBinding}:new[]{c.firstWeakPKeyboardBinding,c.firstMediumPKeyboardBinding,c.firstStrongPKeyboardBinding,c.firstWeakKKeyboardBinding,c.firstMediumKKeyboardBinding,c.firstStrongKKeyboardBinding};
  string[] pad={c.firstLightGamepadBinding,c.firstMediumGamepadBinding,c.firstHeavyGamepadBinding,c.firstSlashGamepadBinding,c.firstHeavySlashGamepadBinding,c.firstStrongKickGamepadBinding};string[] names={"Light","Medium","Heavy","Slash","HeavySlash","StrongKick"};
  for(int i=0;i<6;i++){int button=i;var a=map.AddAction(names[i],InputActionType.Button);Add(a,keyboard[i]);if(!second)Add(a,pad[i]);a.performed+=x=>router.Down(x.control.device.deviceId,button,x.time);a.canceled+=x=>router.Up(x.control.device.deviceId,button);}
  Pulse(map,"Jump",second?"<Keyboard>/numpad8":"<Keyboard>/w",second?"":c.firstJumpGamepadBinding,()=>emit(TrainingCommand.Jump));if(!second)Add(map["Jump"],"<Keyboard>/upArrow");
  Pulse(map,"Cancel",second?c.secondTrcKeyboardBinding:c.firstTrcKeyboardBinding,second?"":c.firstCancelGamepadBinding,()=>{router.Clear();emit(TrainingCommand.Cancel);});
  Pulse(map,"Throw",second?c.secondThrowKeyboardBinding:c.firstThrowKeyboardBinding,second?"":c.firstThrowGamepadBinding,()=>emit(TrainingCommand.Throw));
  Pulse(map,"Special",second?c.secondSpecial1DedicatedBinding:c.firstSpecial1DedicatedBinding,second?"":c.firstSpecialGamepadBinding,()=>emit(TrainingCommand.Special));
  Pulse(map,"Special2",second?c.secondSpecial2DedicatedBinding:c.firstSpecial2DedicatedBinding,"",()=>emit(TrainingCommand.Special2));
  Pulse(map,"ParryFuture",second?c.secondParryKeyboardBinding:c.firstParryKeyboardBinding,"",()=>{});if(!second)Pulse(map,"AssistFuture",c.firstAssistKeyboardBinding,"",()=>{});
  // Player guard is derived from movement plus opponent attack state in TrainingFighter.
  Held(map,"FD",second?c.secondFdKeyboardBinding:c.firstFdKeyboardBinding,second?"":c.firstFdGamepadBinding,fd);Held(map,"Dash",second?"<Keyboard>/numpad0":"<Keyboard>/space",second?"":c.firstDashGamepadBinding,dash);
  if(!second){Pulse(map,"Reset","<Keyboard>/leftCtrl","",reset);Add(map["Reset"],"<Keyboard>/rightCtrl");Pulse(map,"Guard","<Keyboard>/g","",toggle);}return map;
 }
 static void Add(InputAction a,string binding){if(!string.IsNullOrWhiteSpace(binding))a.AddBinding(binding);}
 static void Pulse(InputActionMap map,string name,string key,string pad,Action action){var a=map.AddAction(name,InputActionType.Button);Add(a,key);Add(a,pad);a.performed+=x=>action();}
 static void Held(InputActionMap map,string name,string key,string pad,Action<bool> action){var a=map.AddAction(name,InputActionType.Button);Add(a,key);Add(a,pad);a.performed+=x=>action(true);a.canceled+=x=>action(false);}
}
