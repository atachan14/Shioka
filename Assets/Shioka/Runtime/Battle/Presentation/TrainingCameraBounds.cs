using UnityEngine;
// Authoritative wall state advances only on the combat fixed tick. Render smoothing
// consumes that state and never writes Left/Right or fighter transforms.
[DefaultExecutionOrder(-1000)]public sealed class TrainingCameraBounds:MonoBehaviour {
 public MatchDisplayProfile displaySettings;public Camera view;public TrainingFighter first,second;
 public float followRate=24,characterMargin=1.1f,fitPadding=1.5f,minimumZoom=1.45f,maximumZoom=3.4f,groundAnchor=.8f;
 public float ReferenceViewportAspect;
 float width,center,zoomVelocity,panVelocity,renderWidth,renderCenter,renderZoomVelocity,renderPanVelocity;bool ready,renderReady;
 public float Left{get;private set;}=TrainingStage.Left;public float Right{get;private set;}=TrainingStage.Right;
 public float EffectiveMinimum{get;private set;}public float EffectiveMaximum{get;private set;}
 public float SimulationWidth=>width;public float SimulationCenter=>center;
 public float HorizontalViewWidth=>view==null?0:2*view.orthographicSize*view.aspect;
 public float BodyMargin=>Mathf.Max(Margin(first),Margin(second));
 static float Margin(TrainingFighter actor){var body=actor==null?null:actor.GetComponent<TrainingMotor2D>();return body==null?.35f:Mathf.Abs(body.center.x*actor.transform.lossyScale.x)+body.radius*Mathf.Abs(actor.transform.lossyScale.x)+body.skinWidth+.02f;}
 void Start(){SnapFraming();}
 void FixedUpdate(){if(first!=null&&second!=null&&!first.OptionInputSuspended&&!second.OptionInputSuspended)AdvanceSimulation(Time.fixedDeltaTime);}
 void LateUpdate(){RenderFrame(Time.deltaTime);}
 public void Refresh(){AdvanceSimulation(0);}
 public void SnapFraming(){ready=renderReady=false;AdvanceSimulation(0,true);RenderFrame(0,true);}
 // Compatibility entry for explicit development checks, not called from LateUpdate.
 public void AdvanceFraming(float dt,bool snap=false){AdvanceSimulation(dt,snap);RenderFrame(0,true);}
 public void AdvanceSimulation(float dt,bool snap=false){
  if(first==null||second==null)return;const float aspect=16f/9;
  float minimum=displaySettings==null?6:Mathf.Max(.1f,displaySettings.minimumWidth),maximum=displaySettings==null?10:Mathf.Max(minimum,displaySettings.maximumWidth);
  float a=first.transform.position.x,b=second.transform.position.x,gap=Mathf.Abs(b-a),separating=Mathf.Max(0,Mathf.Sign(b-a)*(second.HorizontalVelocity-first.HorizontalVelocity));
  float look=displaySettings==null?.2f:Mathf.Max(0,displaySettings.lookAheadSeconds),padding=displaySettings==null?1.1f:Mathf.Max(.1f,displaySettings.fitPadding),anchor=displaySettings==null?.82f:Mathf.Clamp01(displaySettings.groundAnchor);
  var firstBody=first.GetComponent<TrainingMotor2D>();var secondBody=second.GetComponent<TrainingMotor2D>();
  float top=Mathf.Max(first.transform.position.y+(firstBody==null?1.55f:firstBody.height)+Mathf.Max(0,first.VerticalVelocity)*look,second.transform.position.y+(secondBody==null?1.55f:secondBody.height)+Mathf.Max(0,second.VerticalVelocity)*look)+(displaySettings==null?.35f:Mathf.Max(0,displaySettings.airPadding));
  float target=Mathf.Clamp(Mathf.Max(gap+padding*2+separating*look,top/Mathf.Max(.5f,1+anchor-.12f)*2*aspect),minimum,maximum);dt=Mathf.Clamp(dt,0,.1f);
  if(!ready||snap){width=target;center=(a+b)*.5f;zoomVelocity=panVelocity=0;ready=true;}
  else if(dt>0){width=Mathf.SmoothDamp(width,target,ref zoomVelocity,displaySettings==null?.25f:Mathf.Max(.01f,displaySettings.zoomSmoothSeconds),Mathf.Infinity,dt);center=Mathf.SmoothDamp(center,(a+b)*.5f,ref panVelocity,displaySettings==null?.18f:Mathf.Max(.01f,displaySettings.panSmoothSeconds),Mathf.Infinity,dt);}
  width=Mathf.Clamp(Mathf.Max(width,gap+BodyMargin*2),minimum,maximum);float half=width*.5f;
  float stageLow=TrainingStage.Left+half,stageHigh=TrainingStage.Right-half;if(stageLow<=stageHigh)center=Mathf.Clamp(center,stageLow,stageHigh);
  float low=Mathf.Max(a,b)+BodyMargin-half,high=Mathf.Min(a,b)-BodyMargin+half;if(low<=high)center=Mathf.Clamp(center,low,high);
  Left=Mathf.Max(TrainingStage.Left,center-half+BodyMargin);Right=Mathf.Min(TrainingStage.Right,center+half-BodyMargin);
  EffectiveMinimum=minimum/(2*aspect);EffectiveMaximum=maximum/(2*aspect);
 }
 public void RenderFrame(float dt,bool snap=false){
  if(view==null||!view.orthographic||!ready)return;dt=Mathf.Clamp(dt,0,.1f);
  if(!renderReady||snap){renderWidth=width;renderCenter=center;renderZoomVelocity=renderPanVelocity=0;renderReady=true;}
  else if(dt>0){renderWidth=Mathf.SmoothDamp(renderWidth,width,ref renderZoomVelocity,.08f,Mathf.Infinity,dt);renderCenter=Mathf.SmoothDamp(renderCenter,center,ref renderPanVelocity,.08f,Mathf.Infinity,dt);}
  float minimum=displaySettings==null?6:displaySettings.minimumWidth,maximum=displaySettings==null?10:displaySettings.maximumWidth;
  float a=first.transform.position.x,b=second.transform.position.x;renderWidth=Mathf.Clamp(Mathf.Max(renderWidth,Mathf.Abs(a-b)+2*BodyMargin),minimum,maximum);
  float low=Mathf.Max(a,b)+BodyMargin-renderWidth*.5f,high=Mathf.Min(a,b)-BodyMargin+renderWidth*.5f;if(low<=high)renderCenter=Mathf.Clamp(renderCenter,low,high);
  view.orthographicSize=renderWidth/(2*Mathf.Max(.1f,view.aspect));var p=view.transform.position;p.x=renderCenter;p.y=view.orthographicSize*(displaySettings==null?.82f:displaySettings.groundAnchor);view.transform.position=p;
 }
 public void MovementLimits(TrainingFighter actor,out float low,out float high){float x=actor.transform.position.x;low=Mathf.Min(Left,x);high=Mathf.Max(Right,x);}
 public float LimitPosition(float start,float desired)=>Mathf.Clamp(desired,Mathf.Min(Left,start),Mathf.Max(Right,start));
}
