using System;
using System.Collections.Generic;
using UnityEngine;
public enum TrainingCommand { Jump, Light, Medium, Heavy, Slash, HeavySlash, Cancel, Sweep, Throw, Special, StrongKick, Special2, CrouchWeakKick, CrouchMediumKick, CrouchStrongKick, AirStrongKick, AirDownStrongKick, AirStrongPunch, ForwardSpecial1, AirSpecial2, CrouchWeakPunch, CrouchMediumPunch, CrouchStrongPunch }
public enum TrainingLockKind { None, Takeoff, Landing, Startup, Active, Recovery, Hitstun, GuardStun, AirAction, Backstep, Knockdown, Down, GetUp, ThrowStartup, ThrowActive, ThrowRecovery, ThrowHold, Thrown, Blockstun }
public enum TrainingCommandOutcome { Queued, Selected, Executed, Rejected, Expired, Replaced, Cleared }
public sealed class TrainingCommandTrace {
 public readonly TrainingCommand Command; public readonly long IssuedFrame,ExpiresFrame;public readonly TrainingLockKind LockAtIssue;
 public TrainingCommandOutcome Outcome {get;internal set;} public long ResolvedFrame {get;internal set;}
 internal TrainingCommandTrace(TrainingBufferedCommand v){Command=v.Command;IssuedFrame=v.IssuedFrame;ExpiresFrame=v.ExpiresFrame;LockAtIssue=v.LockAtIssue;Outcome=TrainingCommandOutcome.Queued;ResolvedFrame=-1;}
}
public readonly struct TrainingBufferedCommand {
 public readonly TrainingCommand Command;public readonly long IssuedFrame,ExpiresFrame;public readonly TrainingLockKind LockAtIssue;
 public TrainingBufferedCommand(TrainingCommand c,long issued,int lifetime,TrainingLockKind kind){Command=c;IssuedFrame=issued;ExpiresFrame=issued+lifetime;LockAtIssue=kind;}
}
public readonly struct TrainingInputSample {
 public readonly int Facing;public readonly bool Airborne;
 public readonly long Frame;public readonly Vector2 Movement;public readonly bool Dash,GuardHeld,FdHeld;public readonly TrainingLockKind LockKind;public readonly string Presses;
 public TrainingInputSample(long frame,TrainingInputFrame input,TrainingLockKind kind,int facing=1,bool airborne=false){Facing=facing<0?-1:1;Airborne=airborne;Frame=frame;Movement=input.Movement;Dash=input.Dash;GuardHeld=input.GuardHeld;FdHeld=input.FdHeld;LockKind=kind;
 if(input.Commands!=null&&input.Commands.Length>0)Presses=string.Join(",",input.Commands);
 else if(!input.Jump&&!input.Attack&&!input.Medium&&!input.Heavy&&!input.Slash&&!input.HeavySlash&&!input.Throw&&!input.Cancel&&!input.Special)Presses=string.Empty;else {var s=new System.Text.StringBuilder();if(input.Jump)s.Append("Jump,");if(input.Attack)s.Append("Light,");if(input.Medium)s.Append("Medium,");if(input.Heavy)s.Append("Heavy,");if(input.Slash)s.Append("Slash,");if(input.HeavySlash)s.Append("HeavySlash,");if(input.Throw)s.Append("Throw,");if(input.Cancel)s.Append("Cancel,");if(input.Special)s.Append("Special,");Presses=s.ToString().TrimEnd(',');}}
}
// Input time runs through hitstop; attack/motor time may pause independently.
public sealed class TrainingCommandBuffer {
 public const int FramesPerSecond=60;public int WindowFrames=10;public long Frame{get;private set;}public int ResetVersion{get;private set;}public int Count=>pending.Count;
 double fraction;readonly List<TrainingBufferedCommand> pending=new List<TrainingBufferedCommand>(),history=new List<TrainingBufferedCommand>();
 readonly List<TrainingCommandTrace> pendingTrace=new List<TrainingCommandTrace>(),trace=new List<TrainingCommandTrace>();TrainingCommandTrace selected;
 readonly List<TrainingInputSample> samples=new List<TrainingInputSample>();
 public IReadOnlyList<TrainingBufferedCommand> History=>history;public IReadOnlyList<TrainingInputSample> Samples=>samples;public IReadOnlyList<TrainingCommandTrace> Trace=>trace;
 public void RecordSample(TrainingInputFrame input,TrainingLockKind kind,int facing=1,bool airborne=false){samples.Add(new TrainingInputSample(Frame,input,kind,facing,airborne));if(samples.Count>180)samples.RemoveAt(0);}
 void Resolve(TrainingCommandTrace t,TrainingCommandOutcome status){t.Outcome=status;t.ResolvedFrame=Frame;}
 public void Advance(float seconds){fraction+=Math.Round((double)seconds*FramesPerSecond,6);int elapsed=(int)Math.Floor(fraction+.00001);Frame+=elapsed;fraction-=elapsed;
 for(int i=pending.Count-1;i>=0;i--)if(Frame>=pending[i].ExpiresFrame){Resolve(pendingTrace[i],TrainingCommandOutcome.Expired);pending.RemoveAt(i);pendingTrace.RemoveAt(i);}}
 public void Push(TrainingCommand c,TrainingLockKind kind=TrainingLockKind.None){var v=new TrainingBufferedCommand(c,Frame,Mathf.Max(1,WindowFrames),kind);var t=new TrainingCommandTrace(v);pending.Add(v);pendingTrace.Add(t);history.Add(v);trace.Add(t);if(pending.Count>32){Resolve(pendingTrace[0],TrainingCommandOutcome.Replaced);pending.RemoveAt(0);pendingTrace.RemoveAt(0);}if(history.Count>180)history.RemoveAt(0);if(trace.Count>180)trace.RemoveAt(0);}
 public bool TryTake(Func<TrainingCommand,bool> allowed,out TrainingCommand command){for(int i=pending.Count-1;i>=0;i--){if(!allowed(pending[i].Command))continue;command=pending[i].Command;selected=pendingTrace[i];Resolve(selected,TrainingCommandOutcome.Selected);for(int j=0;j<i;j++)Resolve(pendingTrace[j],TrainingCommandOutcome.Replaced);pending.RemoveRange(0,i+1);pendingTrace.RemoveRange(0,i+1);return true;}command=default;return false;}
 public void ConfirmSelected(bool executed){if(selected!=null){Resolve(selected,executed?TrainingCommandOutcome.Executed:TrainingCommandOutcome.Rejected);selected=null;}}
 public void RemovePending(Func<TrainingCommand,bool> match){for(int i=pending.Count-1;i>=0;i--)if(match(pending[i].Command)){Resolve(pendingTrace[i],TrainingCommandOutcome.Cleared);pending.RemoveAt(i);pendingTrace.RemoveAt(i);}}
 public void ClearPending(){foreach(var t in pendingTrace)Resolve(t,TrainingCommandOutcome.Cleared);pending.Clear();pendingTrace.Clear();}
 public void Reset(){ResetVersion++;pending.Clear();pendingTrace.Clear();history.Clear();trace.Clear();samples.Clear();selected=null;Frame=0;fraction=0;}
}
