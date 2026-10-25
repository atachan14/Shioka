using UnityEngine;
[CreateAssetMenu(menuName="Shioka/Match display settings")]
public sealed class MatchDisplayProfile:ScriptableObject {
 [Header("Horizontal world view width (meters)")]public bool horizontalWidthMode=true;public float minimumWidth=6f,maximumWidth=10f,zoomSmoothSeconds=.25f,panSmoothSeconds=.18f,lookAheadSeconds=.2f;
 [Header("Legacy zoom fields (ignored in horizontal width mode)")] public float minimumZoom=1.25f,maximumZoom=3.4f,characterMargin=1.05f,fitPadding=1.1f,groundAnchor=.82f,airPadding=.35f;
 [Header("Training overlays only")] public bool trainingPanels=true;public bool showWorldDefenseLabels=false,showStagePositionLabels=false;[Range(0,1)]public float panelOpacity=.68f;public float completedComboSeconds=2f;
 [Header("Signed TG display only; does not enable negative gameplay TG")]
 public Color negativeHigh=new Color(.31f,.40f,.93f),negativeLow=new Color(.13f,.74f,.86f),zero=new Color(.65f,.70f,.75f),positiveLow=new Color(.94f,.17f,.23f),positiveHigh=new Color(1f,.74f,.17f);
 public Color TGColor(double value)=>value < -100?negativeHigh:value<0?negativeLow:value==0?zero:value<=100?positiveLow:positiveHigh;
 public static string TGBand(double value)=>double.IsNaN(value)||double.IsInfinity(value)?"invalid":value < -100?"-200..-101":value<0?"-100..-1":value==0?"0":value<=100?"1..100":"101..200";
 public static float TGFill(double value)=>double.IsNaN(value)||double.IsInfinity(value)?0:Mathf.Clamp01((float)(System.Math.Abs(value)/200));
 public static Rect Viewport(float width,float height){float w=Mathf.Max(1,width),h=Mathf.Max(1,height);if(w/h>16f/9)w=h*16f/9;else h=w*9f/16;return new Rect((width-w)/2,(height-h)/2,w,h);}
}
