using System;using UnityEngine;
[Serializable]public sealed class TrainingMotionHurt {public int partId;public Vector2 center,size;}
[Serializable]public sealed class TrainingMotionPose {public Sprite sprite;[Min(1)]public int holdFrames=1;public Vector2 visualOffset;public TrainingMotionHurt[] hurt=new TrainingMotionHurt[0];}
[CreateAssetMenu(menuName="Shioka/Motion action")]
public sealed class TrainingMotionAction:ScriptableObject {
 public bool loop=true;public TrainingMotionPose[] poses=new TrainingMotionPose[0];
 public bool Validate(out string error){error="";if(poses==null||poses.Length==0){error="At least one pose required";return false;}long total=0;foreach(var p in poses){if(p==null||p.sprite==null||p.holdFrames<1||!V(p.visualOffset)||p.hurt==null){error="Sprite, positive integer hold and finite offset required";return false;}total+=p.holdFrames;var ids=new System.Collections.Generic.HashSet<int>();foreach(var h in p.hurt)if(h==null||!ids.Add(h.partId)||!V(h.center)||!V(h.size)||h.size.x<=0||h.size.y<=0){error="Unique part IDs and positive finite hurt sizes required";return false;}}if(total>36000){error="Maximum total 36000 frames";return false;}return true;}
 static bool V(Vector2 v)=>!float.IsNaN(v.x)&&!float.IsInfinity(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.y);
 public TrainingMotionPose Sample(int frame){if(poses==null||poses.Length==0)return null;int total=0;foreach(var p in poses){if(p==null||p.sprite==null||p.holdFrames<1||p.holdFrames>36000||!V(p.visualOffset))return null;total+=p.holdFrames;if(total>36000)return null;}int f=loop?Mathf.Max(0,frame)%total:Mathf.Min(Mathf.Max(0,frame),total-1);foreach(var p in poses){if(f<p.holdFrames)return p;f-=p.holdFrames;}return poses[poses.Length-1];}
}
