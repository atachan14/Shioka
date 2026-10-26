using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

// Adapt uGUI's text/IME interfaces to this project's existing Input-System-only backend.
public sealed class TrainingOptionBaseInput:BaseInput
{
    Keyboard keyboard;string composition="";IMECompositionMode mode;Vector2 cursor;
    void Update(){if(keyboard==Keyboard.current)return;if(keyboard!=null)keyboard.onIMECompositionChange-=Composition;keyboard=Keyboard.current;if(keyboard!=null)keyboard.onIMECompositionChange+=Composition;}
    void Composition(IMECompositionString text){composition=text.ToString();}
    protected override void OnDisable(){if(keyboard!=null)keyboard.onIMECompositionChange-=Composition;keyboard=null;composition="";base.OnDisable();}
    public override string compositionString=>composition;
    public override IMECompositionMode imeCompositionMode{get=>mode;set{mode=value;if(Keyboard.current!=null)Keyboard.current.SetIMEEnabled(value==IMECompositionMode.On);}}
    public override Vector2 compositionCursorPos{get=>cursor;set{cursor=value;if(Keyboard.current!=null)Keyboard.current.SetIMECursorPosition(value);}}
    public override bool mousePresent=>Mouse.current!=null;
    public override Vector2 mousePosition=>Mouse.current==null?Vector2.zero:Mouse.current.position.ReadValue();
    public override Vector2 mouseScrollDelta=>Mouse.current==null?Vector2.zero:Mouse.current.scroll.ReadValue();
    ButtonControl MouseButton(int button)=>Mouse.current==null?null:button==0?Mouse.current.leftButton:button==1?Mouse.current.rightButton:Mouse.current.middleButton;
    public override bool GetMouseButtonDown(int button)=>MouseButton(button)?.wasPressedThisFrame??false;
    public override bool GetMouseButtonUp(int button)=>MouseButton(button)?.wasReleasedThisFrame??false;
    public override bool GetMouseButton(int button)=>MouseButton(button)?.isPressed??false;
    public override bool touchSupported=>false;
    public override int touchCount=>0;
}
public sealed class TrainingOptionInputField:InputField
{
    Keyboard keyboard;
    protected override void OnEnable(){base.OnEnable();keyboard=Keyboard.current;if(keyboard!=null)keyboard.onTextInput+=TextInput;}
    protected override void OnDisable(){if(keyboard!=null)keyboard.onTextInput-=TextInput;keyboard=null;base.OnDisable();}
    void TextInput(char c){if(isFocused&&c!='\n'&&c!='\r'&&c!='\b'){Append(c);UpdateLabel();}}
    public override void OnUpdateSelected(BaseEventData eventData)
    {
        if(!isFocused||Keyboard.current==null)return;var k=Keyboard.current;
        EventModifiers mods=0;if(k.shiftKey.isPressed)mods|=EventModifiers.Shift;if(k.ctrlKey.isPressed)mods|=EventModifiers.Control;if(k.altKey.isPressed)mods|=EventModifiers.Alt;
        var pairs=new[]{KeyCode.Backspace,KeyCode.Delete,KeyCode.LeftArrow,KeyCode.RightArrow,KeyCode.Home,KeyCode.End,KeyCode.A,KeyCode.C,KeyCode.V,KeyCode.X};
        foreach(var code in pairs){Key key=code==KeyCode.Backspace?Key.Backspace:code==KeyCode.Delete?Key.Delete:code==KeyCode.LeftArrow?Key.LeftArrow:code==KeyCode.RightArrow?Key.RightArrow:code==KeyCode.Home?Key.Home:code==KeyCode.End?Key.End:code==KeyCode.A?Key.A:code==KeyCode.C?Key.C:code==KeyCode.V?Key.V:Key.X;if(k[key].wasPressedThisFrame)KeyPressed(new Event{type=EventType.KeyDown,keyCode=code,modifiers=mods});}
        if(k.enterKey.wasPressedThisFrame||k.numpadEnterKey.wasPressedThisFrame)DeactivateInputField();
        UpdateLabel();eventData.Use();
    }
}
