using UnityEngine;
public sealed class TrainingStage:MonoBehaviour {
 public const float Left=-6f,Right=6f;MatchDisplayProfile settings;GameObject markers,floorView;Material material;Sprite floorSprite;
 void Start(){
  if(GameObject.Find("Physics2D Ground")==null){var ground=new GameObject("Physics2D Ground");ground.transform.position=Vector3.down*.5f;ground.AddComponent<BoxCollider2D>().size=new Vector2(Right-Left+2,1);}
  floorView=new GameObject("2D floor display");floorView.transform.SetParent(transform,false);floorView.transform.position=new Vector3(0,-5,.5f);floorView.transform.localScale=new Vector3((Right-Left+2)/2,5,1);var renderer=floorView.AddComponent<SpriteRenderer>();floorSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),Vector2.one*.5f,1);renderer.sprite=floorSprite;renderer.color=new Color(.38f,.38f,.38f);renderer.sortingOrder=-100;
  markers=new GameObject("Training position markers");markers.transform.SetParent(transform,false);material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.color=new Color(.2f,.3f,.35f);
  for(int x=(int)Left;x<=Right;x++){var tick=new GameObject("Position "+x).AddComponent<LineRenderer>();tick.transform.SetParent(markers.transform,false);tick.sharedMaterial=material;tick.widthMultiplier=x==0?.035f:.015f;tick.positionCount=2;tick.SetPosition(0,new Vector3(x,0,.3f));tick.SetPosition(1,new Vector3(x,x%2==0?3:.25f,.3f));if(x%2==0){var label=new GameObject("Position label "+x).AddComponent<TextMesh>();label.transform.SetParent(markers.transform,false);label.transform.position=new Vector3(x,-.2f,.3f);label.text=x==0?"0 / CENTER":x.ToString("+0;-0;0");label.anchor=TextAnchor.MiddleCenter;label.fontSize=48;label.characterSize=.045f;label.color=x==0?Color.cyan:new Color(.65f,.7f,.75f);}}
 }
 void LateUpdate(){if(settings==null)settings=Resources.Load<MatchDisplayProfile>("MatchDisplayProfile");if(settings!=null&&markers!=null)foreach(var label in markers.GetComponentsInChildren<TextMesh>(true))label.gameObject.SetActive(settings.showStagePositionLabels);}
 void OnDestroy(){if(markers!=null)Destroy(markers);if(floorView!=null)Destroy(floorView);if(floorSprite!=null)Destroy(floorSprite);if(material!=null)Destroy(material);}
}
