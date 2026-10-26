using UnityEngine;
// Serialized legacy geometry is forward=Z, height=Y, thickness=X. This explicit
// compatibility boundary preserves existing asset GUIDs and values during migration.
// Live bodies, Collider2D shapes, queries and Scene handles use native XY.
public static class TrainingCoordinates2D {
 public static Vector2 FromLegacy(Vector3 value,int facing=1)=>new Vector2(value.z*facing,value.y);
 public static Vector3 ToLegacy(Vector2 value,float thickness=0,int facing=1)=>new Vector3(thickness,value.y,value.x*facing);
 public static Vector2 HalfSize(Vector3 value)=>new Vector2(Mathf.Abs(value.z),Mathf.Abs(value.y));
 public static Vector3 World(Vector3 origin,Vector3 local,int facing){var offset=FromLegacy(local,facing);return origin+new Vector3(offset.x,offset.y,0);}
}
