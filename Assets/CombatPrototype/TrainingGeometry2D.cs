using System.Collections.Generic;using UnityEngine;
// Physics2D supplies candidate contacts; this only evaluates the existing authored
// linear strength field at the weakest point of the geometric overlap.
public static class TrainingGeometry2D {
 public static List<Vector2> OverlapVertices(Vector2 center,Vector2 half,BoxCollider2D hurt){
  var polygon=new List<Vector2>();var t=hurt.transform;var c=hurt.offset;var h=hurt.size*.5f;
  foreach(var corner in new[]{new Vector2(-h.x,-h.y),new Vector2(h.x,-h.y),new Vector2(h.x,h.y),new Vector2(-h.x,h.y)})polygon.Add(t.TransformPoint(c+corner));
  polygon=Clip(polygon,Vector2.right,center.x-half.x);polygon=Clip(polygon,Vector2.left,-center.x-half.x);polygon=Clip(polygon,Vector2.up,center.y-half.y);return Clip(polygon,Vector2.down,-center.y-half.y);
 }
 static List<Vector2> Clip(List<Vector2> input,Vector2 normal,float boundary){var output=new List<Vector2>();if(input.Count==0)return output;var a=input[input.Count-1];float da=Vector2.Dot(a,normal)-boundary;foreach(var b in input){float db=Vector2.Dot(b,normal)-boundary;bool ai=da>=-1e-6f,bi=db>=-1e-6f;if(ai!=bi){float denominator=da-db;if(Mathf.Abs(denominator)>1e-9f)output.Add(Vector2.LerpUnclamped(a,b,da/denominator));}if(bi)output.Add(b);a=b;da=db;}return output;}
 public static float MinimumLevel(Vector2 center,Vector2 half,BoxCollider2D hurt,Vector2 weak,Vector2 strong,float minimum,float maximum,float fallback){var vertices=OverlapVertices(center,half,hurt);var axis=strong-weak;if(vertices.Count==0||axis.sqrMagnitude<1e-8f)return fallback;float amount=1;foreach(var point in vertices)amount=Mathf.Min(amount,Mathf.Clamp01(Vector2.Dot(point-weak,axis)/axis.sqrMagnitude));return Mathf.Lerp(minimum,maximum,amount);}
}
