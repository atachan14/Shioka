using UnityEngine;

public readonly struct TrainingFrameSnapshot
{
    public readonly long Frame;
    public readonly TrainingFighter.Phase Phase;
    public readonly TrainingLockKind Lock;
    public readonly float Stop, Remaining, AttackReference, DefenseReference, LevelDifference;
    public readonly int TdcEvents, FdEvents;
    public TrainingFrameSnapshot(long frame,TrainingFighter.Phase phase,TrainingLockKind kind,float stop,float remaining,float attack,float defense,float difference,int tdc=0,int fd=0)
    {Frame=frame;Phase=phase;Lock=kind;Stop=stop;Remaining=remaining;AttackReference=attack;DefenseReference=defense;LevelDifference=difference;TdcEvents=tdc;FdEvents=fd;}
    public TrainingFrameSnapshot At(long frame,int tdc,int fd)=>new TrainingFrameSnapshot(frame,Phase,Lock,Stop,Remaining,AttackReference,DefenseReference,LevelDifference,tdc,fd);
}

// 60Hz display units from post-motor 50Hz samples; no sub-tick reconstruction.
public sealed class TrainingFrameHistory
{
    public const int Capacity=120;
    readonly TrainingFrameSnapshot[] items=new TrainingFrameSnapshot[Capacity];
    int next,lastReset=int.MinValue,lastTdc,lastFd;
    long lastFrame=-1;
    public int Count { get; private set; }
    public int Version { get; private set; }
    public TrainingFrameSnapshot Newest(int age=0){if(age<0||age>=Count)throw new System.ArgumentOutOfRangeException(nameof(age));return items[(next-1-age+Capacity)%Capacity];}
    public void Suspend(int resetVersion,int tdc,int fd){Count=next=0;lastFrame=-1;lastReset=resetVersion;lastTdc=tdc;lastFd=fd;Version++;}
    public void Capture(TrainingFrameSnapshot snapshot,int resetVersion,int tdcCount,int fdCount)
    {
        if(resetVersion!=lastReset||snapshot.Frame<lastFrame){Count=next=lastTdc=lastFd=0;lastFrame=-1;lastReset=resetVersion;Version++;}
        if(snapshot.Frame<=lastFrame)return;
        long first=lastFrame<0?snapshot.Frame:System.Math.Max(lastFrame+1,snapshot.Frame-Capacity+1);
        for(long frame=first;frame<=snapshot.Frame;frame++){
            items[next]=snapshot.At(frame,frame==snapshot.Frame?Mathf.Max(0,tdcCount-lastTdc):0,frame==snapshot.Frame?Mathf.Max(0,fdCount-lastFd):0);
            next=(next+1)%Capacity;Count=Mathf.Min(Capacity,Count+1);
        }
        lastFrame=snapshot.Frame;lastTdc=tdcCount;lastFd=fdCount;Version++;
    }
}
