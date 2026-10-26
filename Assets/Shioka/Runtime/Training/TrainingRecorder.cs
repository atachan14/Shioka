using System;using System.Collections.Generic;using UnityEngine;
public sealed class TrainingRecorder {
 public enum Mode {Idle,Countdown,Recording,Playback}
 public const int FPS=60,CountdownFrames=180,MaximumFrames=600;
 public Mode State{get;private set;}public int Remaining{get;private set;}public int Position{get;private set;}public int SavedCount=>saved.Count;public int DraftCount=>draft.Count;
 readonly List<TrainingInputFrame> saved=new List<TrainingInputFrame>(),draft=new List<TrainingInputFrame>();
 public string Status{get;private set;}="R record | P play | Ctrl reset";
 public static TrainingInputFrame Logical(TrainingInputFrame f){f.Reset=f.ToggleOpponentGuard=f.RecordPressed=f.PlayPressed=false;if(f.Commands!=null){int count=Math.Min(32,f.Commands.Length);var list=new List<TrainingCommand>();for(int i=0;i<count;i++)if(Enum.IsDefined(typeof(TrainingCommand),f.Commands[i]))list.Add(f.Commands[i]);f.Commands=list.ToArray();}return f;}
 public void RecordKey(){if(State==Mode.Playback){Status="Playback busy; Ctrl cancels";return;}if(State==Mode.Idle){Remaining=CountdownFrames;Position=0;draft.Clear();State=Mode.Countdown;Status="Recording countdown";}else if(State==Mode.Countdown)Cancel(false);else Finish();}
 public bool PlayKey(){if(State!=Mode.Idle){Status="Recorder busy";return false;}if(saved.Count==0){Status="No saved recording";return false;}State=Mode.Playback;Position=0;Status="Playing dummy (one shot)";return true;}
 void Finish(){if(draft.Count>0){saved.Clear();saved.AddRange(draft);}draft.Clear();State=Mode.Idle;Position=0;Status="Recording saved: "+saved.Count+"F";}
 public void Cancel(bool savePartial){if(savePartial&&State==Mode.Recording){Finish();return;}draft.Clear();State=Mode.Idle;Remaining=Position=0;Status="Stopped; previous recording retained";}
 public TrainingInputFrame Tick(TrainingInputFrame live){switch(State){case Mode.Countdown:if(--Remaining<=0){State=Mode.Recording;Position=0;Status="RECORDING dummy | R stop";}return default;case Mode.Recording:var frame=Logical(live);draft.Add(frame);Position++;if(draft.Count>=MaximumFrames)Finish();return frame;case Mode.Playback:if(Position>=saved.Count){State=Mode.Idle;Position=0;Status="Playback finished";return default;}return Logical(saved[Position++]);default:return default;}}
 public int CountdownNumber=>Mathf.CeilToInt(Remaining/(float)FPS);
}
[Serializable]public sealed class TrainingPlayerSettings {public int hpPercent=100,tg=0;public bool recover=true,fixedTG=false;}
public sealed class TrainingRecoveryClock {
 float previous;double age;bool initialized;
 public void Rebase(float hp){previous=hp;age=0;initialized=true;}
 public bool Tick(float hp,float target,bool enabled,bool busy,double seconds){if(!initialized)Rebase(hp);if(hp<previous-.00001f)age=0;else age+=Math.Max(0,seconds);previous=hp;if(!enabled||busy||age+1e-7<3||hp>=target)return false;previous=target;return true;}
}
