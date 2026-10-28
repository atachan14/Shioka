using System;using System.Collections.Generic;using System.Linq;
using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;using UnityEngine.InputSystem;using UnityEngine.InputSystem.UI;using UnityEngine.SceneManagement;
public enum FighterSelectionMode { Player, CPU }
public sealed class TrainingFrontMenu:MonoBehaviour {
 public FighterCatalog catalog;public string supportedTrainingFighterId;
 public bool FighterSelectionVisible{get;private set;}public FighterSelectionMode SelectionMode{get;private set;}
 public string SelectedId{get;private set;}public string PlayerId{get;private set;}public string CpuId{get;private set;}
 public string Status{get;private set;}public bool Starting{get;private set;}public Camera MenuCamera{get;private set;}
 public event Action TrainingRequested;public bool SuppressSceneLoadForTests{get;set;}
 public string FocusedButton=>focus>=0&&focus<cells.Count?cells[focus].button.name:"";
 sealed class Cell {public Button button;public int row,column;public string fighterId;public bool random;}
 readonly List<Cell> cells=new List<Cell>();PlayerPreferencesStore store;PlayerPreferences preferences;
 TrainingMenuInput input;RectTransform panel;Font font;Text preview;int focus;bool ready;
 public void UseStoreForTests(PlayerPreferencesStore isolated){store=isolated;if(ready){Load();input?.Dispose();input=new TrainingMenuInput();Build();}}
 void Load(){preferences=store.Load(catalog.FirstId,out var status);Status=status;TrainingFlow.TryResolve(catalog,preferences.selectedFighterId,out var p,out _);TrainingFlow.TryResolve(catalog,preferences.cpuFighterId,out var c,out _);PlayerId=p.fighterId;CpuId=c.fighterId;SelectedId=SelectionMode==FighterSelectionMode.CPU?CpuId:PlayerId;}
 void Start(){
  store=store??new PlayerPreferencesStore();input=new TrainingMenuInput();font=Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic","Meiryo","Arial"},24);
  var canvasGo=new GameObject("Title and fighter selection",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasGo.transform.SetParent(transform,false);
  var cameraGo=new GameObject("Menu camera",typeof(Camera));cameraGo.transform.SetParent(transform,false);cameraGo.transform.position=new Vector3(0,0,-10);MenuCamera=cameraGo.GetComponent<Camera>();MenuCamera.orthographic=true;MenuCamera.nearClipPlane=.1f;MenuCamera.farClipPlane=100;MenuCamera.clearFlags=CameraClearFlags.SolidColor;MenuCamera.backgroundColor=new Color(.035f,.055f,.065f);
  var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=MenuCamera;canvas.planeDistance=1;canvas.sortingOrder=200;
  var scaler=canvasGo.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
  var body=new GameObject("Menu",typeof(RectTransform));body.transform.SetParent(canvasGo.transform,false);panel=(RectTransform)body.transform;panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.sizeDelta=new Vector2(1240,690);
  // Fit the complete fixed layout even in a narrow Game view.
  if(EventSystem.current==null){var events=new GameObject("Menu EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(transform,false);events.GetComponent<EventSystem>().sendNavigationEvents=false;}
  ready=true;if(catalog==null||!catalog.Validate(out var error)){Status="Fighter catalog missing or invalid";Build();return;}Load();Build();
 }
 void LateUpdate(){if(MenuCamera!=null)RefreshLayout(MenuCamera.pixelWidth,MenuCamera.pixelHeight);} public void RefreshLayout(int width,int height){if(panel!=null)panel.localScale=Vector3.one*Mathf.Min(1,(float)width/Mathf.Max(1,height)*720/1280);}
 RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
 Text Label(string name,string text,int size,Vector2 pos,Vector2 dimensions,Transform parent=null){var r=Rect(name,parent??panel,pos,dimensions);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=text;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
 void Box(string name,Vector2 pos,Vector2 size){var r=Rect(name,panel,pos,size);r.gameObject.AddComponent<Image>().color=new Color(.07f,.11f,.14f,.85f);}
 string NameOf(string id)=>FighterCatalog.Character(catalog?.Find(id))?.characterName??"FighterName";
 bool Available(string id)=>id==supportedTrainingFighterId&&catalog?.Find(id)!=null;
 void Build(){
  foreach(Transform child in panel){child.gameObject.SetActive(false);Destroy(child.gameObject);}cells.Clear();focus=0;preview=null;
  if(catalog==null||!catalog.Validate(out _)){Label("Error",Status,24,Vector2.zero,new Vector2(1100,200));return;}
  if(!FighterSelectionVisible)BuildTitle();else BuildSelection();
  Label("Help","WASD / Dpad: 選択　決定: "+MenuBinding("menu.Confirm")+"　戻る: "+MenuBinding("menu.Cancel")+" / Esc　Mouse: click",16,new Vector2(0,-321),new Vector2(1200,38));
  if(!string.IsNullOrEmpty(Status)&&Status.StartsWith("Save failed"))Label("Error","保存できませんでした。選択は未確定です。",18,new Vector2(0,303),new Vector2(1000,35));
  if(FighterSelectionVisible){int saved=cells.FindIndex(c=>c.fighterId==SelectedId&&c.button.interactable);if(saved>=0)focus=saved;}
  if(cells.Count>0)SetFocus(focus);input?.Clear();
 }
 void BuildTitle(){
  Label("Logo","Shioka",36,new Vector2(0,282),new Vector2(700,65));Box("Future player art",new Vector2(0,135),new Vector2(500,210));
  Label("Future art","ArtGraphic（準備中）",19,new Vector2(0,135),new Vector2(480,180));
  Label("FighterName",NameOf(PlayerId),30,new Vector2(-420,30),new Vector2(350,50));
  Add("FighterSelect","FighterSelect",()=>OpenFighterSelect(),true,0,0,new Vector2(-420,-35),new Vector2(350,54));
  Add("Training","Training",OpenCpuSelect,true,1,0,new Vector2(-420,-105),new Vector2(350,54));
  Add("CommandList","CommandList（未実装）",null,false,2,0,new Vector2(-420,-175),new Vector2(350,54));
  Add("Option","Option",OpenOptions,true,3,0,new Vector2(-420,-245),new Vector2(350,54));
  Label("Online","Online",27,new Vector2(0,-35),new Vector2(340,54));Label("Offline","Offline",27,new Vector2(420,-35),new Vector2(340,54));
  Add("Rank","Rank（未実装）",null,false,1,1,new Vector2(0,-120),new Vector2(350,54));Add("Exhibition","Exhibition（未実装）",null,false,2,1,new Vector2(0,-200),new Vector2(350,54));
  Add("Arcade","Arcade（未実装）",null,false,1,2,new Vector2(420,-120),new Vector2(350,54));Add("VS CPU","VS CPU（未実装）",null,false,2,2,new Vector2(420,-200),new Vector2(350,54));
 }
 void BuildSelection(){
  bool cpu=SelectionMode==FighterSelectionMode.CPU;
  if(cpu){Box("Player art",new Vector2(-310,180),new Vector2(500,220));Box("CPU art",new Vector2(310,180),new Vector2(500,220));Label("Confirmed Player","Player: "+NameOf(PlayerId),28,new Vector2(-310,50),new Vector2(580,50));preview=Label("CPU preview","CPU: "+NameOf(SelectedId),28,new Vector2(310,50),new Vector2(580,50));}
  else{Box("Player art",new Vector2(0,180),new Vector2(600,220));preview=Label("Player preview",NameOf(SelectedId),30,new Vector2(0,50),new Vector2(1000,50));}
  Box("Selection panel",new Vector2(0,-150),new Vector2(1220,300));Label("Selection mode",cpu?"Dummy（CPU）":"Player",24,new Vector2(-440,-28),new Vector2(300,42));
  var implemented=catalog.fighters.Select(FighterCatalog.Character).Where(c=>Available(c.characterId)).ToArray();
  // Awari occupies the third top-left cell, as in the supplied wireframe. Empty cells remain visibly disabled.
  int[] order={2,0,1,6,7,8,3,4,5,9,10,11};var ids=new Dictionary<int,string>();for(int i=0;i<Mathf.Min(12,implemented.Length);i++)ids[order[i]]=implemented[i].characterId;
  for(int i=0;i<12;i++){int row=i/6,col=i%6;float x=col<3?-530+col*156:218+(col-3)*156;string id=ids.TryGetValue(i,out var value)?value:null;Add("FighterSlot"+i,id==null?"（未実装）":NameOf(id),()=>ConfirmFighter(id),id!=null,row,col<3?col:col+1,new Vector2(x,-101-row*108),new Vector2(142,94),id);}
  Add("Random","Random",ConfirmRandom,implemented.Length>0,0,3,new Vector2(0,-155),new Vector2(136,104),null,true);
  Add("Back","戻る",Back,true,2,0,new Vector2(-510,-278),new Vector2(170,34));
 }
 void Add(string name,string text,Action action,bool enabled,int row,int column,Vector2 pos,Vector2 size,string fighterId=null,bool random=false){
  var r=Rect(name,panel,pos,size);var image=r.gameObject.AddComponent<Image>();var button=r.gameObject.AddComponent<Button>();button.interactable=enabled;button.navigation=new Navigation{mode=Navigation.Mode.None};
  var colors=button.colors;colors.disabledColor=new Color(.23f,.26f,.28f);colors.highlightedColor=new Color(.65f,.95f,1);button.colors=colors;
  var label=Label("Label",text,enabled?23:19,Vector2.zero,size-new Vector2(12,4),r);label.color=enabled?Color.white:new Color(.55f,.58f,.6f);
  var cell=new Cell{button=button,row=row,column=column,fighterId=fighterId,random=random};cells.Add(cell);
  button.onClick.AddListener(()=>{if(!button.interactable||Starting)return;SetFocus(cells.IndexOf(cell));action?.Invoke();});
  var events=r.gameObject.AddComponent<EventTrigger>();var enter=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};enter.callback.AddListener(_=>{if(button.interactable)SetFocus(cells.IndexOf(cell));});events.triggers.Add(enter);
 }
 void SetFocus(int index){if(index<0||index>=cells.Count||!cells[index].button.interactable)return;focus=index;var selected=cells[index];if(FighterSelectionVisible){if(selected.fighterId!=null)SelectedId=selected.fighterId;if(preview!=null)preview.text=(SelectionMode==FighterSelectionMode.CPU?"CPU: ":"")+(selected.random?"Random":NameOf(SelectedId));}
  foreach(var cell in cells)cell.button.GetComponent<Image>().color=cell==selected?new Color(.15f,.48f,.48f):new Color(.1f,.19f,.2f);if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(selected.button.gameObject);
 }
 void Navigate(Vector2 direction){if(cells.Count==0)return;var current=cells[focus];IEnumerable<Cell> candidates=cells.Where(c=>c.button.interactable&&c!=current);Cell next=null;
  if(direction.y!=0){int sign=direction.y>0?-1:1;next=candidates.Where(c=>(c.row-current.row)*sign>0).OrderBy(c=>Mathf.Abs(c.row-current.row)).ThenBy(c=>Mathf.Abs(c.column-current.column)).FirstOrDefault();}
  else if(direction.x!=0){int sign=direction.x>0?1:-1;next=candidates.Where(c=>c.row==current.row&&(c.column-current.column)*sign>0).OrderBy(c=>Mathf.Abs(c.column-current.column)).FirstOrDefault();}
  if(next!=null)SetFocus(cells.IndexOf(next));
 }
 void Update(){if(!ready||Starting||input==null)return;var frame=input.Read();if(frame.cancel||frame.option){if(FighterSelectionVisible)Back();else if(frame.option)OpenOptions();else input.Clear();return;}Navigate(frame.direction);if(frame.confirm&&cells.Count>0&&cells[focus].button.interactable)cells[focus].button.onClick.Invoke();}
 public void OpenFighterSelect(){Open(FighterSelectionMode.Player);}public void OpenCpuSelect(){Open(FighterSelectionMode.CPU);}
 void Open(FighterSelectionMode mode){if(Starting)return;SelectionMode=mode;SelectedId=mode==FighterSelectionMode.Player?PlayerId:CpuId;FighterSelectionVisible=true;Build();}
 // Cursor changes are drafts. Only confirmation persists.
 public void SelectFighter(string id){if(!FighterSelectionVisible||!Available(id))return;SelectedId=id;int index=cells.FindIndex(c=>c.fighterId==id);SetFocus(index);}
 public void ConfirmFighter(string id){if(!FighterSelectionVisible||!Available(id)||Starting)return;var draft=preferences.Copy(catalog.FirstId);if(SelectionMode==FighterSelectionMode.Player)draft.selectedFighterId=id;else draft.cpuFighterId=id;
  if(!store.Save(draft,out var error)){Status="Save failed: "+error;Build();return;}preferences=draft;PlayerId=Resolve(draft.selectedFighterId);CpuId=Resolve(draft.cpuFighterId);SelectedId=id;Status="";
  if(SelectionMode==FighterSelectionMode.Player){FighterSelectionVisible=false;Build();}else StartTraining();
 }
 string Resolve(string id){TrainingFlow.TryResolve(catalog,id,out var request,out _);return request.fighterId;}
 public void ConfirmRandom(){var ids=catalog.fighters.Select(FighterCatalog.Character).Where(c=>Available(c.characterId)).Select(c=>c.characterId).ToArray();if(ids.Length>0)ConfirmFighter(ids[UnityEngine.Random.Range(0,ids.Length)]);}
 public void Back(){if(Starting)return;FighterSelectionVisible=false;SelectedId=PlayerId;Build();}
 public void StartTraining(){if(Starting||!Available(PlayerId)||!Available(CpuId))return;Starting=true;TrainingFlowSession.PlayerFighterId=PlayerId;TrainingFlowSession.CpuFighterId=CpuId;TrainingFlowSession.AwaitingTrainingEntry=true;input.Clear();TrainingRequested?.Invoke();if(!SuppressSceneLoadForTests){Time.timeScale=1;SceneManager.LoadScene(TrainingFlowSession.TrainingScene);}}
 public void OpenOptions(){if(Starting)return;Starting=true;TrainingFlowSession.OpenOptionsFromTitle=true;input.Clear();Time.timeScale=1;if(!SuppressSceneLoadForTests)SceneManager.LoadScene(TrainingFlowSession.TrainingScene);}
 string MenuBinding(string id){var row=KeyConfigService.Ensure().Row(id);return Binding(row.keyboard)+" / "+Binding(row.pad);}
 static string Binding(string value){if(string.IsNullOrEmpty(value))return "未割当";int modifier=KeyConfigProfile.Modifier(value);return(modifier==0?"":"T"+modifier+"+")+InputControlPath.ToHumanReadableString(KeyConfigProfile.PathPart(value),InputControlPath.HumanReadableStringOptions.OmitDevice);}
 void OnApplicationFocus(bool focused){if(!focused)input?.Clear();}void OnDestroy(){input?.Dispose();if(font!=null)Destroy(font);}
}
