using System;
using System.Collections.Generic;
using UnityEngine;

// Optional root-local, right-facing axial field. Existing assets remain uniform.
[Serializable] public sealed class TrainingAttackStrength {
 public bool enabled;
 public Vector3 weakPoint=new Vector3(0,.95f,.25f),strongPoint=new Vector3(0,.95f,1.05f);
 [Min(0)] public float minimumLevel=30,maximumLevel=60;
 public bool IsValid => Finite(weakPoint)&&Finite(strongPoint)&&Finite(minimumLevel)&&Finite(maximumLevel)&&minimumLevel>=0&&maximumLevel>=minimumLevel&&(strongPoint-weakPoint).sqrMagnitude>1e-8f;
 static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
 static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
 public float MinimumOverBoxOverlap(Vector3 center,Vector3 half,BoxCollider2D hurtbox,Vector3 root,int facing,float uniformLevel,float temporalScale){if(!enabled||!IsValid)return uniformLevel;Vector2 weak=(Vector2)root+TrainingCoordinates2D.FromLegacy(weakPoint,facing),strong=(Vector2)root+TrainingCoordinates2D.FromLegacy(strongPoint,facing);return TrainingGeometry2D.MinimumLevel(center,half,hurtbox,weak,strong,minimumLevel,maximumLevel,uniformLevel/Mathf.Max(.000001f,temporalScale))*temporalScale;}
 // Retained only for historical 3D geometry tests; active fighters use Collider2D above.
 public float MinimumOverBoxOverlap(Vector3 center,Vector3 half,BoxCollider hurtbox,Vector3 root,int facing,float uniformLevel,float temporalScale) {
  if(!enabled||!IsValid)return uniformLevel;
  var vertices=TrainingBoxIntersection.Vertices(center,half,hurtbox);
  if(vertices.Count==0)return uniformLevel;
  var w=root+new Vector3(weakPoint.x,weakPoint.y,weakPoint.z*facing);
  var axis=new Vector3(strongPoint.x-weakPoint.x,strongPoint.y-weakPoint.y,(strongPoint.z-weakPoint.z)*facing);
  float t=1;foreach(var p in vertices)t=Mathf.Min(t,Mathf.Clamp01(Vector3.Dot(p-w,axis)/axis.sqrMagnitude));
  return Mathf.Lerp(minimumLevel,maximumLevel,t)*temporalScale;
 }
}

// Exact convex box intersection vertices, including rotated/scaled hurtboxes.
// Linear axial strength reaches its minimum at an intersection vertex.
public static class TrainingBoxIntersection {
 struct Plane {public Vector3 n;public float d;public Plane(Vector3 n,float d){this.n=n;this.d=d;}}
 public static List<Vector3> Vertices(Vector3 center,Vector3 half,BoxCollider box){
  var planes=new List<Plane>(12);Add(planes,center,Vector3.right,half.x);Add(planes,center,Vector3.up,half.y);Add(planes,center,Vector3.forward,half.z);
  var tr=box.transform;var c=tr.TransformPoint(box.center);var s=box.size*.5f;
  AddTransformed(planes,tr,c,Vector3.right,s.x);AddTransformed(planes,tr,c,Vector3.up,s.y);AddTransformed(planes,tr,c,Vector3.forward,s.z);
  var result=new List<Vector3>();
  for(int i=0;i<10;i++)for(int j=i+1;j<11;j++)for(int k=j+1;k<12;k++){
   var a=planes[i];var b=planes[j];var q=planes[k];var cross=Vector3.Cross(b.n,q.n);float det=Vector3.Dot(a.n,cross);if(Mathf.Abs(det)<1e-7f)continue;
   var p=(a.d*cross+b.d*Vector3.Cross(q.n,a.n)+q.d*Vector3.Cross(a.n,b.n))/det;
   bool inside=true;foreach(var plane in planes)if(Vector3.Dot(plane.n,p)>plane.d+1e-5f){inside=false;break;}if(inside)result.Add(p);
  }return result;
 }
 static void Add(List<Plane> p,Vector3 c,Vector3 n,float h){p.Add(new Plane(n,Vector3.Dot(n,c)+h));p.Add(new Plane(-n,Vector3.Dot(-n,c)+h));}
 static void AddTransformed(List<Plane> p,Transform tr,Vector3 c,Vector3 axis,float half){var n=tr.localToWorldMatrix.inverse.transpose.MultiplyVector(axis).normalized;float h=Mathf.Abs(Vector3.Dot(n,tr.TransformVector(axis*half)));Add(p,c,n,h);}
}

// Pure replaceable result formula; values are in simulation frames (60 Hz).
public static class TrainingHitFormula {
 public static float Result(float attack,float defense)=>attack-defense;
 public static bool IsHit(float result)=>result>0;
 public static float HitstunFrames(float source,float result)=>source*.1f+result*.4f;
 public static float GuardstunFrames(float source,float defense)=>source*.4f*Mathf.Pow(2,-defense/100);
 public static float HitstopFrames(float source)=>source*.2f;
}
public static class TrainingContactOrder {
 public static bool Prefer(int priority,float result,int id,int previousPriority,float previousResult,int previousId)=>priority>previousPriority||(priority==previousPriority&&(result>previousResult||(result==previousResult&&id<previousId)));
}
