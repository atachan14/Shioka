using System;using UnityEngine;using UnityEngine.InputSystem;
public struct TrainingMenuFrame {public bool option,confirm,cancel,cancelFromPad;public Vector2 direction;}
public sealed class TrainingMenuInput:IDisposable {
 InputActionMap map;TrainingMenuFrame pending,current;int last=-1;Vector2 previous;
 public TrainingMenuInput(){map=new InputActionMap("Menu");Pulse("Option","<Keyboard>/escape","<Gamepad>/start",()=>pending.option=true);Pulse("Confirm","<Keyboard>/j","<Gamepad>/buttonEast",()=>pending.confirm=true);Pulse("Cancel","<Keyboard>/k","<Gamepad>/buttonSouth",()=>pending.cancel=true);var move=map.AddAction("Navigate",InputActionType.Value);move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");move.AddBinding("<Gamepad>/dpad");move.AddBinding("<Gamepad>/leftStick");move.performed+=c=>{var v=c.ReadValue<Vector2>();v=new Vector2(v.x<-.5f?-1:v.x>.5f?1:0,v.y<-.5f?-1:v.y>.5f?1:0);if(v!=previous&&v!=Vector2.zero)pending.direction=v;previous=v;};move.canceled+=c=>previous=Vector2.zero;map.Enable();InputSystem.onDeviceChange+=Changed;}
 void Pulse(string name,string key,string pad,Action action){var a=map.AddAction(name,InputActionType.PassThrough);a.AddBinding(key);a.AddBinding(pad);a.performed+=c=>{if(c.ReadValue<float>()>.5f){if(name=="Cancel"&&c.control.device is Gamepad)pending.cancelFromPad=true;action();}};}
 public TrainingMenuFrame Read()=>Read(Time.frameCount);
 public TrainingMenuFrame Read(int renderFrame){if(last!=renderFrame){current=pending;pending=default;last=renderFrame;}return current;}
 public void Clear(){pending=current=default;previous=Vector2.zero;last=-1;}
 void Changed(InputDevice d,InputDeviceChange c){if(c==InputDeviceChange.Removed||c==InputDeviceChange.Disconnected||c==InputDeviceChange.Disabled)Clear();}
 public void Dispose(){InputSystem.onDeviceChange-=Changed;map.Dispose();}
}
