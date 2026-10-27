using System;using UnityEngine;
[Serializable]public sealed class TrainingSpriteFrame {public Sprite sprite;[HideInInspector]public string sourceIdentity,sourceName;[Min(1)]public int holdFrames=1;}
[Serializable]public sealed class TrainingSpriteHold {public string identity;public int holdFrames;}
[Serializable]public sealed class TrainingSpriteSequence {
 [HideInInspector]public TrainingSpriteHold[] rememberedHolds=new TrainingSpriteHold[0];
 public TrainingSpriteFrame[] frames=new TrainingSpriteFrame[0];
 public bool Validate(out string error){error=null;if(frames==null){error="Use an empty array for legacy fallback";return false;}if(frames.Length>256){error="At most256 sprite frames";return false;}int sum=0;foreach(var f in frames){if(f==null||f.sprite==null||f.holdFrames<1||f.holdFrames>36000){error="Each sprite requires holdFrames1..36000";return false;}sum+=f.holdFrames;if(sum>36000){error="Sequence maximum36000F";return false;}}return true;}
 public int TotalFrames {get{int total=0;if(frames!=null)foreach(var f in frames)if(f!=null)total+=Mathf.Clamp(f.holdFrames,0,36000);return total;}}
 public Sprite Sample(float frame){if(float.IsNaN(frame)||float.IsInfinity(frame)||frames==null||frames.Length==0||!Validate(out _))return null;float f=Mathf.Max(0,frame);float nearest=Mathf.Round(f);if(Mathf.Abs(f-nearest)<.0001f)f=nearest;int end=0;foreach(var pose in frames){end+=pose.holdFrames;if(f<end)return pose.sprite;}return frames[frames.Length-1].sprite;}
}
