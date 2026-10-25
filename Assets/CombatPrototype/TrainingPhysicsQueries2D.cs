using System;using UnityEngine;
public static class TrainingPhysicsQueries2D {
 static Collider2D[] scratch=new Collider2D[32];
 // Explicit trigger policy and growth: never silently drop simultaneous hurt parts.
 public static Collider2D[] OverlapBox(Vector3 center,Vector3 half,float angle=0){
  Physics2D.SyncTransforms();var filter=new ContactFilter2D();filter.SetLayerMask(~0);filter.useTriggers=true;
  int count;while(true){count=Physics2D.OverlapBox(center,(Vector2)half*2,angle,filter,scratch);if(count<scratch.Length)break;Array.Resize(ref scratch,scratch.Length*2);}
  if(count==0)return Array.Empty<Collider2D>();var output=new Collider2D[count];Array.Copy(scratch,output,count);return output;
 }
}
