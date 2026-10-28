using UnityEngine;

using UnityEngine.UI;

using UnityEngine.InputSystem;

using UnityEngine.EventSystems;

using UnityEngine.InputSystem.UI;



public sealed partial class TrainingOverlay

{

    public bool OptionVisible{get;private set;}
    public bool ReturnToTitleOnOptionClose {get;set;}

    GameObject optionRoot,ownedEventSystem;

    RectTransform optionPanel;

    Text optionError;

    InputField counterField;

    float optionSavedScale;

    TrainingMenuInput menuInput;int menuOpenedFrame=-1;
    public TrainingMenuFrame MenuInputFrame=>menuInput==null?default:menuInput.Read();
    public bool MenuOpenedThisFrame=>menuOpenedFrame==Time.frameCount;
    void OnApplicationFocus(bool focus){if(!focus)menuInput?.Clear();}


    readonly System.Collections.Generic.List<Button> postureButtons=new System.Collections.Generic.List<Button>();

    readonly System.Collections.Generic.List<Button> guardButtons=new System.Collections.Generic.List<Button>();

    public InputField OptionCounterField=>counterField;

    public void SetOptionVisible(bool visible)

    {

        if(OptionVisible==visible)return;bool returnToTitle=!visible&&ReturnToTitleOnOptionClose;ReturnToTitleOnOptionClose=visible&&ReturnToTitleOnOptionClose;if(!visible){DiscardKeyDraftOnClose();CancelKeyCapture();if(keyDialog!=null)keyDialog.SetActive(false);}

        if(visible){menuOpenedFrame=Time.frameCount;Session?.MenuOpened();optionSavedScale=Time.timeScale;Time.timeScale=0;OptionVisible=true;SetHudHelpVisible(false);SetHudHistoryVisible(false);}

        else{OptionVisible=false;Time.timeScale=optionSavedScale;}

        foreach(var fighter in new[]{player,player==null?null:player.opponent})if(fighter!=null){fighter.SetOptionInputSuspended(visible);var first=fighter.GetComponent<TrainingControls>();if(first!=null)first.SetOptionBlocked(visible);var second=fighter.GetComponent<TrainingSecondPlayerControls>();if(second!=null)second.SetOptionBlocked(visible);}

        if(optionRoot!=null)optionRoot.SetActive(visible);

        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);

        if(visible){counterField.SetTextWithoutNotify(dummy.CounterCommand);RefreshOptionSettings();LayoutOption(hudWidth,hudHeight);optionRoot.transform.SetAsLastSibling();}
        if(returnToTitle){Time.timeScale=1;UnityEngine.SceneManagement.SceneManager.LoadScene(TrainingFlowSession.TitleScene);}

    }

    void Update()

    {

        if(menuInput==null)menuInput=new TrainingMenuInput();UpdateKeyConfig();if(MenuInputFrame.option&&!OptionVisible)SetOptionVisible(true);

        if(OptionVisible&&optionRoot!=null){optionRoot.transform.SetAsLastSibling();LayoutOption(hudWidth,hudHeight);RefreshOptionSettings();}

    }

    GameObject OptionObject(string name,Transform parent)

    {

        var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);return go;

    }

    Text OptionText(string name,string text,Transform parent,int size=14)

    {

        var go=OptionObject(name,parent);var t=go.AddComponent<Text>();t.font=defenseFont==null?hudFont:defenseFont;t.fontSize=size;t.text=text;t.color=new Color(.9f,.94f,.92f);t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Overflow;return t;

    }

    void OptionPlace(RectTransform rect,float x,float y,float width,float height)

    {rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}

    Button OptionButton(string name,string label,Transform parent,UnityEngine.Events.UnityAction action,bool enabled=true)

    {

        var go=OptionObject(name,parent);var image=go.AddComponent<Image>();image.color=new Color(.14f,.22f,.20f);var button=go.AddComponent<Button>();button.interactable=enabled;button.targetGraphic=image;

        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.6f,.9f,.75f);colors.pressedColor=new Color(.4f,.75f,.6f);colors.disabledColor=new Color(.5f,.5f,.5f);button.colors=colors;

        if(action!=null)button.onClick.AddListener(action);var text=OptionText("Label",label,go.transform,13);text.alignment=TextAnchor.MiddleCenter;var r=text.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return button;

    }

    void CreateOption()

    {

        if(hudCanvas.GetComponent<GraphicRaycaster>()==null)hudCanvas.gameObject.AddComponent<GraphicRaycaster>();

        if(EventSystem.current==null){ownedEventSystem=new GameObject("Training Option EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule),typeof(TrainingOptionBaseInput));var module=ownedEventSystem.GetComponent<InputSystemUIInputModule>();module.inputOverride=ownedEventSystem.GetComponent<TrainingOptionBaseInput>();module.AssignDefaultActions();}

        optionRoot=OptionObject("Training Option",hudRoot.transform);var mask=optionRoot.AddComponent<Image>();mask.color=new Color(0,0,0,.78f);var root=(RectTransform)optionRoot.transform;root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;

        var panel=OptionObject("Panel",root);panel.AddComponent<Image>().color=new Color(.045f,.065f,.06f,1);optionPanel=(RectTransform)panel.transform;

        OptionText("Title","Option",panel.transform,21);OptionText("Navigation","ダミー設定",panel.transform,15);

        OptionButton("Close","Esc / 閉じる",panel.transform,()=>SetOptionVisible(false));

        OptionText("PostureLabel","姿勢",panel.transform);OptionText("GuardLabel","ガード",panel.transform);OptionText("CounterLabel","被弾後の反撃コマンド",panel.transform);

        for(int i=0;i<3;i++){var mode=(TrainingDummy.PostureMode)i;postureButtons.Add(OptionButton("Posture"+i,new[]{"立ち","しゃがみ","ジャンプ"}[i],panel.transform,()=>dummy.SetPosture(mode)));}

        guardButtons.Add(OptionButton("Guard0","なし",panel.transform,()=>dummy.Guarding=false));guardButtons.Add(OptionButton("Guard1","ガード",panel.transform,()=>dummy.Guarding=true));

        guardButtons.Add(OptionButton("Guard2","FD（将来）",panel.transform,null,false));guardButtons.Add(OptionButton("Guard3","直ガ（将来）",panel.transform,null,false));

        var input=OptionObject("CounterInput",panel.transform);input.AddComponent<Image>().color=new Color(.12f,.16f,.14f);counterField=input.AddComponent<TrainingOptionInputField>();counterField.lineType=InputField.LineType.SingleLine;counterField.characterLimit=40;

        var text=OptionText("InputText","",input.transform);var tr=text.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(8,4);tr.offsetMax=new Vector2(-8,-4);counterField.textComponent=text;

        var placeholder=OptionText("Placeholder","空欄: 反撃なし / 例: 3強",input.transform,13);var pr=placeholder.rectTransform;pr.anchorMin=Vector2.zero;pr.anchorMax=Vector2.one;pr.offsetMin=new Vector2(8,4);pr.offsetMax=new Vector2(-8,-4);placeholder.color=new Color(.5f,.62f,.55f);counterField.placeholder=placeholder;

        counterField.onValueChanged.AddListener(value=>{dummy.SetCounterCommand(value);RefreshOptionSettings();});

        optionError=OptionText("Validation","",panel.transform,12);

        OptionText("Note","方向は相手向き相対。行動可能になる最速タイミングで反撃。\n硬直はキャンセルしません。FD・直ガはダミー自動選択の将来項目。",panel.transform,12);

        OptionText("Pause","設定中は戦闘停止。閉じた後は操作キーを一度離してください。",panel.transform,12);

        OptionButton("NavTitle","Titleへ戻る",optionPanel,()=>{bool returning=ReturnToTitleOnOptionClose;SetOptionVisible(false);if(!returning)UnityEngine.SceneManagement.SceneManager.LoadScene(TrainingFlowSession.TitleScene);});CreateInputOptions();CreateDeveloperOptions();CreateTrainingOptions();optionRoot.AddComponent<TrainingOptionNavigation>().Configure(this,optionRoot);optionPanel.Find("Pause").GetComponent<Text>().text="WASD: select | J: confirm / edit | K: back | Enter: finish text | Esc: close | Mouse: click";optionRoot.SetActive(false);LayoutOption(hudWidth,hudHeight);

    }

    void LayoutOption(int width,int height)

    {

        if(optionPanel==null)return;RefreshOptionSettings();float w=1080,h=700,scale=Mathf.Min(1,Mathf.Max(1,height-24)/h,Mathf.Max(1,width-24)/w),left=Mathf.Clamp(w*.25f,130,190),x=left+20,right=w-x-18;

        optionPanel.anchorMin=optionPanel.anchorMax=new Vector2(.5f,.5f);optionPanel.pivot=new Vector2(.5f,.5f);optionPanel.anchoredPosition=Vector2.zero;optionPanel.sizeDelta=new Vector2(w,h);optionPanel.localScale=new Vector3(scale,scale,1);

        foreach(Transform child in optionPanel){var r=(RectTransform)child;switch(child.name){case "Title":OptionPlace(r,18,12,200,30);break;case "Navigation":OptionPlace(r,18,62,left-24,35);break;case "Close":OptionPlace(r,w-145,12,126,30);break;case "PostureLabel":OptionPlace(r,x,65,right,25);break;case "GuardLabel":OptionPlace(r,x,140,right,25);break;case "CounterLabel":OptionPlace(r,x,245,right,25);break;case "CounterInput":OptionPlace(r,x,274,right,34);break;case "Validation":OptionPlace(r,x,314,right,44);break;case "Note":OptionPlace(r,x,365,right,54);break;case "Pause":OptionPlace(r,18,h-30,w-36,25);break;}}

        for(int i=0;i<3;i++)OptionPlace((RectTransform)postureButtons[i].transform,x+i*(right/3),96,right/3-5,32);

        for(int i=0;i<4;i++)OptionPlace((RectTransform)guardButtons[i].transform,x+(i%2)*(right/2),174+(i/2)*35,right/2-5,30);LayoutInputOptions(w,h,left,x,right);LayoutDeveloperOptions(w,h,left,x,right);LayoutTrainingOptions(w,h,left,x,right);var back=optionPanel.Find("NavTitle");if(back!=null)OptionPlace((RectTransform)back,18,h-82,left-24,32);

    }

    void RefreshOptionSettings()

    {

        if(dummy==null)return;int posture=dummy.Crouch?1:(int)dummy.Posture;

        for(int i=0;i<3;i++)postureButtons[i].GetComponent<Image>().color=i==posture?new Color(.25f,.48f,.36f):new Color(.14f,.22f,.20f);

        for(int i=0;i<2;i++)guardButtons[i].GetComponent<Image>().color=i==(dummy.Guarding?1:0)?new Color(.25f,.48f,.36f):new Color(.14f,.22f,.20f);

        optionError.text=string.IsNullOrEmpty(dummy.CounterError)?(string.IsNullOrWhiteSpace(dummy.CounterCommand)?"反撃なし":"設定済み: "+dummy.CounterCommand):dummy.CounterError;optionError.color=string.IsNullOrEmpty(dummy.CounterError)?new Color(.6f,.8f,.7f):new Color(1,.55f,.4f);RefreshTrainingOptions();

    }

    void DestroyOption(){if(OptionVisible)SetOptionVisible(false);if(optionRoot!=null)Destroy(optionRoot);if(ownedEventSystem!=null)Destroy(ownedEventSystem);}

}
