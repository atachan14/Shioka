using UnityEngine;
using UnityEngine.InputSystem;
public sealed partial class TrainingInputConfig:ScriptableObject
{

 // Old assets without this field retain the original mapping. This trial explicitly saves version2.
 public int layoutVersion=1;
 [HideInInspector] public float chordWindowSeconds=0; // retired compatibility field; never read by input resolution.
 [HideInInspector] public bool fdChordEnabled=true,throwChordEnabled=true,parryChordEnabled=false,special1ChordEnabled=true,special2ChordEnabled=true;
 [HideInInspector] public string firstWeakPKeyboardBinding="<Keyboard>/j",firstMediumPKeyboardBinding="<Keyboard>/k",firstStrongPKeyboardBinding="<Keyboard>/l";
 [HideInInspector] public string firstWeakKKeyboardBinding="<Keyboard>/m",firstMediumKKeyboardBinding="<Keyboard>/comma",firstStrongKKeyboardBinding="<Keyboard>/period";
 [HideInInspector] public string secondWeakPKeyboardBinding="<Keyboard>/numpad1",secondMediumPKeyboardBinding="<Keyboard>/numpad2",secondStrongPKeyboardBinding="<Keyboard>/numpad3";
 [HideInInspector] public string secondWeakKKeyboardBinding="<Keyboard>/numpad7",secondMediumKKeyboardBinding="<Keyboard>/numpad9",secondStrongKKeyboardBinding="<Keyboard>/numpadMinus";
 public string firstTrcKeyboardBinding="<Keyboard>/semicolon",secondTrcKeyboardBinding="<Keyboard>/numpadEnter";
 // Keep legacy pad TRC on rightTrigger; sixth attack pad slot stays unassigned until customized.
 [HideInInspector] public string firstStrongKickGamepadBinding="";
 [HideInInspector] public string firstSpecial1DedicatedBinding="",secondSpecial1DedicatedBinding="",firstSpecial2DedicatedBinding="",secondSpecial2DedicatedBinding="";
 [HideInInspector] public string firstParryKeyboardBinding="<Keyboard>/o",secondParryKeyboardBinding="<Keyboard>/numpadPlus",firstAssistKeyboardBinding="<Keyboard>/slash";
 static TrainingInputConfig runtime;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetRuntime(){runtime=null;}
 public static TrainingInputConfig Runtime(TrainingInputConfig source){if(!Application.isPlaying||source==null||source.layoutVersion<2)return source;if(runtime==null){runtime=Instantiate(source);runtime.hideFlags=HideFlags.HideAndDontSave;}return runtime;}
 [HideInInspector] public InputActionAsset inputActions;
 [HideInInspector] public string secondPlayerMap="Training2P";
 public float doubleTapSeconds=.25f;
 [HideInInspector] public string firstSpecialKeyboardBinding="<Keyboard>/o";
 [HideInInspector] public string firstSpecialGamepadBinding="";
 [HideInInspector] public string secondSpecialKeyboardBinding="<Keyboard>/numpadPlus";
 [HideInInspector] public string firstFdGamepadBinding="<Gamepad>/select";
 [HideInInspector] public string firstLightGamepadBinding="<Gamepad>/buttonWest";
 [HideInInspector] public string firstSlashGamepadBinding="<Gamepad>/leftShoulder";
 [HideInInspector] public string firstCancelGamepadBinding="<Gamepad>/rightTrigger";
 [HideInInspector] public string firstHeavySlashGamepadBinding="<Gamepad>/leftTrigger";
 public string firstMoveDpadGamepadBinding="<Gamepad>/dpad";
 [HideInInspector] public string firstDashGamepadBinding="<Gamepad>/rightShoulder";
 [HideInInspector] public string firstMediumGamepadBinding="<Gamepad>/buttonNorth";
 public string firstMoveStickGamepadBinding="<Gamepad>/leftStick";
 [HideInInspector] public string firstJumpGamepadBinding="<Gamepad>/buttonSouth";
 [HideInInspector] public string firstHeavyGamepadBinding="<Gamepad>/buttonEast";
 [InspectorName("Hold keyboard")] public string firstThrowKeyboardBinding="<Keyboard>/h";
 [HideInInspector] public string firstThrowGamepadBinding="<Gamepad>/leftStickPress";
 public string secondThrowKeyboardBinding="<Keyboard>/numpadDivide";
 [HideInInspector] public string firstFdKeyboardBinding="<Keyboard>/u";
 [HideInInspector] public string secondFdKeyboardBinding="<Keyboard>/numpadMultiply";
 [HideInInspector] public string firstGuardKeyboardBinding="<Keyboard>/f";
 [HideInInspector] public string firstGuardGamepadBinding="<Gamepad>/rightStickPress";
 [HideInInspector] public string secondGuardKeyboardBinding="<Keyboard>/numpadPeriod";
}
