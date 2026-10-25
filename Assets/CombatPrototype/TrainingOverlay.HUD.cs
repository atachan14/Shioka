using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public sealed partial class TrainingOverlay
{
    sealed class HudItem { public RectTransform root; public Text text; public Image image; public Rect rect; }
    readonly Dictionary<string,HudItem> hudItems=new Dictionary<string,HudItem>();
    public MatchHud MatchDisplay {get;private set;}
    public MatchDisplayProfile DisplaySettings {get;private set;}
    bool ownsDisplaySettings;
    void RebindHud(){if(hudItems.Count>0||hudRoot==null)return;foreach(Transform child in hudRoot.transform){var image=child.GetComponent<Image>();var label=child.Find("Label");if(image==null||label==null)continue;var text=label.GetComponent<Text>();if(text!=null)hudItems[child.name]=new HudItem{root=(RectTransform)child,text=text,image=image};}}
    void EnsureMatchDisplay(){RebindHud();if(MatchDisplay!=null){if(DisplaySettings!=null&&!DisplaySettings.showWorldDefenseLabels&&hudRoot!=null)foreach(Transform child in hudRoot.transform)if(child.name.StartsWith("Defense value "))child.gameObject.SetActive(false);return;}DisplaySettings=Resources.Load<MatchDisplayProfile>("MatchDisplayProfile");if(DisplaySettings==null){DisplaySettings=ScriptableObject.CreateInstance<MatchDisplayProfile>();ownsDisplaySettings=true;}HudDamageVisible=DisplaySettings.trainingPanels;MatchDisplay=gameObject.AddComponent<MatchHud>();MatchDisplay.Configure(hudRoot.transform,player,dummy.Fighter,DisplaySettings,defenseFont==null?hudFont:defenseFont);hudDirty=true;}
    GameObject hudRoot;Canvas hudCanvas;Font hudFont;RawImage worldImage;RenderTexture worldTexture;Camera worldCamera;int worldMask;float hudAge;int hudWidth,hudHeight;bool hudDirty=true;
    public TrainingFrameBar FrameBar { get; private set; }
    RawImage frameImage;
    [SerializeField] bool hudTdcTimingCueVisible=true;
    public bool HudTdcTimingCueVisible => hudTdcTimingCueVisible;
    public void SetHudTdcTimingCueVisible(bool visible){hudTdcTimingCueVisible=visible;UpdateTdcTimingCue();}
    public float TdcTimingCueStrength(bool first){var f=first?player:dummy==null?null:dummy.Fighter;return hudTdcTimingCueVisible&&ActorsAvailable&&f!=null&&f.HitstopRemaining<=0f?Mathf.Clamp01((1f-f.TdcRecoveryCostScale)/.5f):0f;}
    void UpdateTdcTimingCue(){UpdateTdcTimingCueItem(true);UpdateTdcTimingCueItem(false);}
    void UpdateTdcTimingCueItem(bool first){if(hudItems.TryGetValue(first?"number1":"number2",out var item))item.text.color=Color.Lerp(Color.white,new Color(.65f,.95f,.85f),TdcTimingCueStrength(first));}
    public bool HudHistoryVisible {get;private set;}
    public void SetHudHistoryVisible(bool visible){HudHistoryVisible=visible;if(visible)HudHelpVisible=false;hudDirty=true;}
    bool ActorsAvailable=>player!=null&&dummy!=null&&dummy.Fighter!=null&&player.isActiveAndEnabled&&dummy.Fighter.isActiveAndEnabled;bool lastActorsAvailable=true;
    public bool HudFramesVisible { get; private set; }=true;
    public void SetHudFramesVisible(bool visible){HudFramesVisible=visible;hudDirty=true;}
    public Camera HudCamera { get; private set; }
    public bool HudHelpVisible { get; private set; }
    public Rect HudWorldRect { get; private set; }
    public IReadOnlyDictionary<string,Rect> HudRects => hudRects;
    readonly Dictionary<string,Rect> hudRects=new Dictionary<string,Rect>();
    public void SetHudHelpVisible(bool visible){HudHelpVisible=visible;if(visible)HudHistoryVisible=false;hudDirty=true;}
    void CreateHud()
    {
        hudRoot=new GameObject("Training HUD",typeof(RectTransform),typeof(Canvas));hudRoot.layer=5;
        hudCanvas=hudRoot.GetComponent<Canvas>();hudCanvas.renderMode=RenderMode.ScreenSpaceCamera;hudCanvas.planeDistance=1;hudCanvas.sortingOrder=100;
        var cameraObject=new GameObject("Training HUD camera",typeof(Camera));cameraObject.transform.SetParent(transform,false);cameraObject.transform.position=new Vector3(0,0,-50);
        HudCamera=cameraObject.GetComponent<Camera>();HudCamera.orthographic=true;HudCamera.nearClipPlane=.1f;HudCamera.farClipPlane=5;HudCamera.cullingMask=1<<5;HudCamera.clearFlags=CameraClearFlags.SolidColor;HudCamera.backgroundColor=Color.black;HudCamera.depth=100;hudCanvas.worldCamera=HudCamera;
        hudFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach(string id in new[]{"world","topPanel","hp1","hp2","hpfill1","hpfill2","gauge1","gauge2","gaugefill1","gaugefill2","zero1","zero2","number1","number2","panel","status","status1","status2","legend","mode","view","help","helpPanel","helpTitle","helpText","helpClose","frameTitle","frames","frameInfo","frameToggle","history","recordStatus","commands1","commands2"})AddHud(id);

        if(defenseFont==null)defenseFont=Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic","Meiryo","Arial"},13);foreach(var name in new[]{"hp1","hp2","mode"})hudItems[name].text.font=defenseFont;
        worldCamera=Camera.main;worldMask=worldCamera.cullingMask;worldCamera.cullingMask&=~(1<<5);
        var raw=new GameObject("World image",typeof(RectTransform),typeof(RawImage));raw.layer=5;raw.transform.SetParent(hudItems["world"].root,false);var rr=(RectTransform)raw.transform;rr.anchorMin=Vector2.zero;rr.anchorMax=Vector2.one;rr.offsetMin=rr.offsetMax=Vector2.zero;worldImage=raw.GetComponent<RawImage>();worldImage.raycastTarget=false;
        hudItems["mode"].text.text="2P: dummy";hudItems["view"].text.text="View: current";hudItems["help"].text.text="Controls";hudItems["helpClose"].text.text="Close controls";
        hudItems["helpTitle"].text.text="Training controls — provisional bindings";
        var controlsConfig=TrainingInputConfig.Runtime(Resources.Load<TrainingInputConfig>("TrainingInputConfig"));
        if(controlsConfig!=null&&controlsConfig.layoutVersion>=2)hudItems["helpText"].text.text="1P: WASD/arrows move; W/Up jump; Space/double-tap dash\nJ/K/L weak/medium/strong P; M/comma/period weak/medium/strong K\nSpecial1 J+L; Special2 M+period (move pending)\nGuard: back during opponent attack; FD U or J+M; throw I or K+comma; TRC semicolon\nO parry / slash assist reserved; roll not implemented\n2P: num1/2/3 P; num7/9/minus K; numEnter TRC\nGuard: num4/6 away during attack; numMultiply FD; numDivide throw\nPad legacy5 attacks +RT TRC retained; strongK pad slot unassigned\nOption input settings: per-action chord OFF, dedicated keys retained.";else
        hudItems["helpText"].text.text="1P: WASD/arrows move; W/Up jump; Space/double-tap dash\n1P: J/K/L light/medium/heavy; M S; comma HS; O special\n1P: back during opponent attack guards; U FD; I throw; period TRC; G dummy guard; Ctrl reset; R record; P play\n2P: num4/6 move; num8 jump; num5 crouch; num0 dash\n2P: num1/2/3 attacks; num7 S; num9 HS; numPlus special\n2P: back during opponent attack guards; numMultiply FD; numDivide throw; numEnter TRC\nPad 1P: stick/Dpad move; South jump; West/North/East attacks\nPad: LB S; LT HS; RB dash; RT TRC; L3 throw; Select FD; back during opponent attack guards\nConfigure special/gamepad/throw/FD in TrainingInputConfig. No 2P pad assignment.";
        controlsHelp=hudItems["helpText"].text.text;
        hudItems["legend"].text.text="Level color: strike / defense   Orange: throwbox   Blue: throw receiver";
        FrameBar=gameObject.AddComponent<TrainingFrameBar>();FrameBar.Configure(player,dummy.Fighter);
        var image=new GameObject("Frame strips",typeof(RectTransform),typeof(RawImage));image.layer=5;image.transform.SetParent(hudItems["frames"].root,false);var ir=(RectTransform)image.transform;ir.anchorMin=Vector2.zero;ir.anchorMax=Vector2.one;ir.offsetMin=ir.offsetMax=Vector2.zero;frameImage=image.GetComponent<RawImage>();frameImage.raycastTarget=false;
        hudItems["frameInfo"].text.fontSize=11;hudItems["frameTitle"].text.fontSize=11;hudItems["frameTitle"].text.text="1P top / 2P bottom | latest 60F (120F retained) | start yellow, active red, end orange, guard cyan, hurt purple";
        EnsureMatchDisplay();LayoutHud(Screen.width,Screen.height);UpdateHudText();
    }
    void AddHud(string id)
    {
        var go=new GameObject(id,typeof(RectTransform),typeof(Image));go.layer=5;go.transform.SetParent(hudRoot.transform,false);
        var label=new GameObject("Label",typeof(RectTransform),typeof(Text));label.layer=5;label.transform.SetParent(go.transform,false);
        var r=(RectTransform)label.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(4,0);r.offsetMax=new Vector2(-4,0);
        var t=label.GetComponent<Text>();t.text="";t.font=hudFont;t.fontSize=13;t.color=Color.white;t.alignment=TextAnchor.MiddleLeft;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;
        var im=go.GetComponent<Image>();im.color=Color.clear;im.raycastTarget=false;
        hudItems[id]=new HudItem{root=(RectTransform)go.transform,text=t,image=im};
    }
    void Place(string id,Rect rect,Color background)
    {
        var item=hudItems[id];item.rect=rect;hudRects[id]=rect;item.root.anchorMin=item.root.anchorMax=new Vector2(0,1);item.root.pivot=new Vector2(0,1);item.root.anchoredPosition=new Vector2(rect.x,-rect.y);item.root.sizeDelta=rect.size;item.image.color=background;
    }
    public void LayoutHud(int width,int height)
    {
        if(hudRoot==null)return;EnsureMatchDisplay();hudWidth=width;hudHeight=height;hudDirty=false;hudRects.Clear();
        foreach(var item in hudItems)item.Value.root.gameObject.SetActive(false);
        HudWorldRect=MatchDisplayProfile.Viewport(width,height);var v=HudWorldRect;float scale=v.width/1600;Color panel=new Color(.025f,.045f,.065f,DisplaySettings.panelOpacity),button=new Color(.06f,.1f,.14f,.68f);
        if(player!=null&&player.CameraBounds!=null){player.CameraBounds.ReferenceViewportAspect=0;player.CameraBounds.displaySettings=DisplaySettings;}
        Place("world",v,Color.clear);hudItems["world"].root.gameObject.SetActive(true);hudItems["world"].root.SetAsFirstSibling();
        int vw=Mathf.Max(1,Mathf.RoundToInt(v.width)),vh=Mathf.Max(1,Mathf.RoundToInt(v.height));
        if(worldTexture==null||worldTexture.width!=vw||worldTexture.height!=vh){if(worldTexture!=null){worldTexture.Release();Destroy(worldTexture);}worldTexture=new RenderTexture(vw,vh,24);worldTexture.name="16x9 match viewport";worldTexture.Create();worldImage.texture=worldTexture;}if(worldCamera!=null)worldCamera.targetTexture=worldTexture;
        System.Action<string,Rect,Color> placeTraining=(id,rect,color)=>{Place(id,new Rect(v.x+rect.x*scale,v.y+rect.y*scale,rect.width*scale,rect.height*scale),color);hudItems[id].root.gameObject.SetActive(ActorsAvailable);hudItems[id].text.fontSize=Mathf.Max(10,Mathf.RoundToInt(15*scale));};
        if(HudDamageVisible){placeTraining("status",new Rect(480,104,640,182),panel);placeTraining("status1",new Rect(488,108,304,174),Color.clear);placeTraining("status2",new Rect(808,108,304,174),Color.clear);}
        placeTraining("recordStatus",new Rect(680,4,240,54),new Color(.025f,.045f,.065f,.7f));
        if(HudCommandsVisible){placeTraining("commands1",new Rect(14,116,138,616),new Color(.025f,.045f,.065f,.42f));placeTraining("commands2",new Rect(1448,116,138,616),new Color(.025f,.045f,.065f,.42f));}
        // Legacy five-button strip removed; settings remain under Esc.
        if(HudFramesVisible){placeTraining("frameTitle",new Rect(4,744,1592,32),Color.clear);placeTraining("frames",new Rect(4,780,1592,36),new Color(0,0,0,.4f));placeTraining("frameInfo",new Rect(4,818,1592,28),Color.clear);}
        float helpH=Mathf.Min(430*scale,v.height-100*scale),helpY=v.y+(v.height-helpH)/2;float helpX=v.x+v.width*.12f,helpW=v.width*.76f;
        Place("helpPanel",new Rect(helpX,helpY,helpW,helpH),new Color(.025f,.045f,.065f,.96f));Place("helpTitle",new Rect(helpX+12,helpY+6,helpW-24,24),Color.clear);Place("helpText",new Rect(helpX+12,helpY+34,helpW-24,helpH-74),Color.clear);Place("helpClose",new Rect(helpX+12,helpY+helpH-32,helpW-24,26),button);
        foreach(string id in new[]{"helpPanel","helpTitle","helpText","helpClose"}){hudItems[id].root.gameObject.SetActive((HudHelpVisible||HudHistoryVisible)&&ActorsAvailable);hudItems[id].root.SetAsLastSibling();}
        MatchDisplay.Refresh(v);
        UpdateHudText();UpdateHudGauges();UpdateTdcTimingCue();if(OptionVisible&&optionRoot!=null)optionRoot.transform.SetAsLastSibling();Canvas.ForceUpdateCanvases();
    }
    void TickHud()
    {
        if(hudRoot==null)return;EnsureMatchDisplay();UpdateTdcTimingCue();if(lastActorsAvailable!=ActorsAvailable){lastActorsAvailable=ActorsAvailable;hudDirty=true;}int w=HudCamera.targetTexture==null?Screen.width:HudCamera.targetTexture.width,h=HudCamera.targetTexture==null?Screen.height:HudCamera.targetTexture.height;
        if(hudDirty||w!=hudWidth||h!=hudHeight)LayoutHud(w,h);
        if(worldCamera!=null&&worldCamera.targetTexture==null)worldCamera.targetTexture=worldTexture;
        MatchDisplay.Refresh(HudWorldRect);UpdateHudText();UpdateHudGauges();
        if(OptionVisible){if(optionRoot!=null)optionRoot.transform.SetAsLastSibling();return;}var mouse=Mouse.current;if(mouse!=null&&mouse.leftButton.wasPressedThisFrame){Vector2 point=mouse.position.ReadValue();foreach(string id in new[]{"mode","view","help","helpClose","frameToggle","history"})if(hudItems[id].root.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(hudItems[id].root,point,HudCamera)){if(id=="mode"&&dummy!=null)SetSecondPlayerControl(!SecondPlayerControlled);else if(id=="view"&&player!=null)CycleFacingView();else if(id=="history")SetHudHistoryVisible(!HudHistoryVisible);else if(id=="frameToggle")SetHudFramesVisible(!HudFramesVisible);else if(id=="help")SetHudHelpVisible(true);else if(id=="helpClose"){SetHudHelpVisible(false);SetHudHistoryVisible(false);}hudDirty=true;break;}}
    }
    string controlsHelp;
    static string InputHistoryText(TrainingFighter f,string name){var b=f.InputBuffer;var raw=b.Samples.Count==0?default:b.Samples[b.Samples.Count-1];string text=$"{name} Raw @{raw.Frame}: move {raw.Movement.x:0.#}/{raw.Movement.y:0.#} dash {raw.Dash} guard {raw.GuardHeld} FD {raw.FdHeld}";string edges="none";for(int i=b.Samples.Count-1;i>=0;i--)if(!string.IsNullOrEmpty(b.Samples[i].Presses)){edges=$"@{b.Samples[i].Frame} {b.Samples[i].Presses}";break;}text+="\nLast raw press: "+edges;for(int i=Mathf.Max(0,b.Trace.Count-2);i<b.Trace.Count;i++){var t=b.Trace[i];text+=$"\nCmd {t.Command} @{t.IssuedFrame} until {t.ExpiresFrame}: {t.Outcome}";}return text;}
    static string Number(double value)=>System.Math.Abs(value)<100000?value.ToString("+0.0;-0.0;0.0")+"%":value.ToString("+0.00E+0;-0.00E+0;0")+"%";
    void UpdateHudText()
    {
        if(!ActorsAvailable){foreach(var pair in hudItems)if(pair.Key!="world")pair.Value.root.gameObject.SetActive(false);return;}
        hudItems["history"].text.text=HudHistoryVisible?"History: on":"History";hudItems["helpTitle"].text.text=HudHistoryVisible?"Input history: raw samples / command outcomes":"Training controls — provisional bindings";if(HudHistoryVisible){hudItems["helpText"].text.fontSize=12;hudItems["helpText"].text.text=InputHistoryText(player,"1P")+"\n"+InputHistoryText(dummy.Fighter,"2P");}else{hudItems["helpText"].text.fontSize=13;hudItems["helpText"].text.text=controlsHelp;}
        if(Session!=null&&hudItems.ContainsKey("recordStatus")){var recorder=Session.Recorder;hudItems["recordStatus"].text.alignment=TextAnchor.MiddleCenter;hudItems["recordStatus"].text.text=recorder.State==TrainingRecorder.Mode.Countdown?"REC IN "+recorder.CountdownNumber:recorder.State==TrainingRecorder.Mode.Recording?"REC 2P "+(recorder.DraftCount/60f).ToString("0.0")+" / 10s | R stop":recorder.State==TrainingRecorder.Mode.Playback?"PLAY 2P "+recorder.Position+" / "+recorder.SavedCount+"F":"R Record | P Play\nCtrl Reset | saved "+recorder.SavedCount+"F";hudItems["recordStatus"].text.fontSize=Mathf.Max(11,Mathf.RoundToInt((recorder.State==TrainingRecorder.Mode.Countdown?28:14)*HudWorldRect.width/1600));}
        foreach(bool first in new[]{true,false}){var item=hudItems[first?"commands1":"commands2"];item.text.font=defenseFont==null?hudFont:defenseFont;item.text.alignment=TextAnchor.UpperLeft;item.text.text=(first?"1P":"2P")+" INPUT / newest ↑\n"+(first?commandsFirst:commandsSecond).Text(24);}
        for(int i=0;i<2;i++){var state=i==0?MatchDisplay.FirstAttack:MatchDisplay.SecondAttack;string n=(i+1).ToString();hudItems["status"+n].text.font=defenseFont==null?hudFont:defenseFont;hudItems["status"+n].text.text=$"{n}P → {(i==0?2:1)}P  LAST CONTACT\n"+(state.hasContact?$"A {state.last.rawAttack:0.##}  D {state.last.defense:0.##}\nH {state.last.finalResult:0.##}  {(state.last.hit?"HIT":(i==0?dummy.Fighter:player).LastResult)}\nDamage {state.lastDamage:0.##}\nCombo {state.count} / total {state.totalDamage:0.##}\nScaling: 未実装\nStop {state.last.hitstopFrames:0.#}F / stun {state.last.stunFrames:0.#}F":"A —  D —\nDamage —\nCombo 0 / total 0\nScaling: 未実装");}

        if(FrameBar!=null){frameImage.texture=FrameBar.GetTexture();hudItems["frameToggle"].text.text=HudFramesVisible?"Frames: on":"Frames: off";var a=TrainingFrameBar.Snapshot(player);var b=TrainingFrameBar.Snapshot(dummy.Fighter);hudItems["frameInfo"].text.text=$"F {a.Frame}/{b.Frame}  Lock {a.Remaining:0.#}/{b.Remaining:0.#}F  Stop {a.Stop:0.#}/{b.Stop:0.#}F  Est adv {FrameBar.EstimatedAdvantage:0.#}F  Atk {a.AttackReference:0.#}/{b.AttackReference:0.#}  Def {a.DefenseReference:0.#}/{b.DefenseReference:0.#}";}
        hudItems["mode"].text.text=(SecondPlayerControlled?"2P: keyboard":"2P: dummy")+" | "+dummy.Fighter.CharacterName;hudItems["view"].text.text="View: "+player.FacingStyle;
    }
    void UpdateHudGauges(){if(MatchDisplay!=null)MatchDisplay.Refresh(HudWorldRect);}
    public bool HudTextFits(string id)=>hudItems.ContainsKey(id)&&(string.IsNullOrEmpty(hudItems[id].text.text)||hudItems[id].text.preferredHeight<=hudItems[id].rect.height+.1f);
    public bool HudVisible(string id)=>hudItems.ContainsKey(id)&&hudItems[id].root.gameObject.activeInHierarchy;
    public string HudText(string id)=>hudItems.ContainsKey(id)?hudItems[id].text.text:"";
    void OnDisable(){if(hudRoot!=null)hudRoot.SetActive(false);if(HudCamera!=null)HudCamera.enabled=false;if(worldCamera!=null&&worldCamera.targetTexture==worldTexture)worldCamera.targetTexture=null;}
    void OnEnable(){if(hudRoot!=null)hudRoot.SetActive(true);if(HudCamera!=null)HudCamera.enabled=true;if(worldCamera!=null&&worldTexture!=null)worldCamera.targetTexture=worldTexture;hudDirty=true;}
    void DestroyHud(){if(MatchDisplay!=null)Destroy(MatchDisplay);if(ownsDisplaySettings&&DisplaySettings!=null)Destroy(DisplaySettings);if(worldCamera!=null){if(worldCamera.targetTexture==worldTexture)worldCamera.targetTexture=null;worldCamera.cullingMask=worldMask;}if(worldTexture!=null){worldTexture.Release();Destroy(worldTexture);}if(HudCamera!=null)Destroy(HudCamera.gameObject);if(hudRoot!=null)Destroy(hudRoot);}
}
