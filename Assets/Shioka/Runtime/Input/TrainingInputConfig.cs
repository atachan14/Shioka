using UnityEngine;
using UnityEngine.InputSystem;
public sealed partial class TrainingInputConfig:ScriptableObject
{

 // Old assets without this field retain the original mapping. This trial explicitly saves version2.
 public int layoutVersion=1;
 [HideInInspector] public float chordWindowSeconds=0; // retired compatibility field; never read by input resolution.
 [HideInInspector] public bool fdChordEnabled=true,throwChordEnabled=true,parryChordEnabled=false,special1ChordEnabled=true,special2ChordEnabled=true;
 public string firstWeakPKeyboardBinding="<Keyboard>/j",firstMediumPKeyboardBinding="<Keyboard>/k",firstStrongPKeyboardBinding="<Keyboard>/l";
 public string firstWeakKKeyboardBinding="<Keyboard>/m",firstMediumKKeyboardBinding="<Keyboard>/comma",firstStrongKKeyboardBinding="<Keyboard>/period";
 public string secondWeakPKeyboardBinding="<Keyboard>/numpad1",secondMediumPKeyboardBinding="<Keyboard>/numpad2",secondStrongPKeyboardBinding="<Keyboard>/numpad3";
 public string secondWeakKKeyboardBinding="<Keyboard>/numpad7",secondMediumKKeyboardBinding="<Keyboard>/numpad9",secondStrongKKeyboardBinding="<Keyboard>/numpadMinus";
 public string firstTrcKeyboardBinding="<Keyboard>/semicolon",secondTrcKeyboardBinding="<Keyboard>/numpadEnter";
 // Keep legacy pad TRC on rightTrigger; sixth attack pad slot stays unassigned until customized.
 public string firstStrongKickGamepadBinding="";
 public string firstSpecial1DedicatedBinding="",secondSpecial1DedicatedBinding="",firstSpecial2DedicatedBinding="",secondSpecial2DedicatedBinding="";
 public string firstParryKeyboardBinding="<Keyboard>/o",secondParryKeyboardBinding="<Keyboard>/numpadPlus",firstAssistKeyboardBinding="<Keyboard>/slash";
 static TrainingInputConfig runtime;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetRuntime(){runtime=null;}
 public static TrainingInputConfig Runtime(TrainingInputConfig source){if(!Application.isPlaying||source==null||source.layoutVersion<2)return source;if(runtime==null){runtime=Instantiate(source);runtime.hideFlags=HideFlags.HideAndDontSave;}return runtime;}
 public InputActionAsset inputActions;
 public string secondPlayerMap="Training2P";
 public float doubleTapSeconds=.25f;
 public string firstSpecialKeyboardBinding="<Keyboard>/o";
 public string firstSpecialGamepadBinding="";
 public string secondSpecialKeyboardBinding="<Keyboard>/numpadPlus";
 public string firstFdGamepadBinding="<Gamepad>/select";
 public string firstLightGamepadBinding="<Gamepad>/buttonWest";
 public string firstSlashGamepadBinding="<Gamepad>/leftShoulder";
 public string firstCancelGamepadBinding="<Gamepad>/rightTrigger";
 public string firstHeavySlashGamepadBinding="<Gamepad>/leftTrigger";
 public string firstMoveDpadGamepadBinding="<Gamepad>/dpad";
 public string firstDashGamepadBinding="<Gamepad>/rightShoulder";
 public string firstMediumGamepadBinding="<Gamepad>/buttonNorth";
 public string firstMoveStickGamepadBinding="<Gamepad>/leftStick";
 public string firstJumpGamepadBinding="<Gamepad>/buttonSouth";
 public string firstHeavyGamepadBinding="<Gamepad>/buttonEast";
 public string firstThrowKeyboardBinding="<Keyboard>/i";
 public string firstThrowGamepadBinding="<Gamepad>/leftStickPress";
 public string secondThrowKeyboardBinding="<Keyboard>/numpadDivide";
 public string firstFdKeyboardBinding="<Keyboard>/u";
 public string secondFdKeyboardBinding="<Keyboard>/numpadMultiply";
 public string firstGuardKeyboardBinding="<Keyboard>/f";
 public string firstGuardGamepadBinding="<Gamepad>/rightStickPress";
 public string secondGuardKeyboardBinding="<Keyboard>/numpadPeriod";
}
