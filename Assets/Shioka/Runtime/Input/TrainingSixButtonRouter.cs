using System;using System.Collections.Generic;using UnityEngine;
public interface ITrainingPendingChordSource{void ClearPendingChordInput();}
// One resolver per actor, separate physical-device histories. Logical input buffer remains unchanged.
public sealed class TrainingSixButtonRouter
{
 sealed class Device{public readonly bool[] held=new bool[6],pending=new bool[6],consumed=new bool[6];public readonly double[] at=new double[6];public readonly List<int> locks=new List<int>();public int candidates;public bool fd;}
 readonly Dictionary<int,Device> devices=new Dictionary<int,Device>();readonly Action<TrainingCommand> emit;readonly TrainingInputConfig config;readonly Func<bool> fdEligible;
 static readonly TrainingCommand[] normal={TrainingCommand.Light,TrainingCommand.Medium,TrainingCommand.Heavy,TrainingCommand.Slash,TrainingCommand.HeavySlash,TrainingCommand.StrongKick};
 // Trial multi-button priority: FD, throw, reserved parry, Special1, Special2. Unique two-key pairs otherwise do not conflict.
 static readonly int[] masks={(1<<0)|(1<<3),(1<<1)|(1<<4),(1<<2)|(1<<5),(1<<0)|(1<<2),(1<<3)|(1<<5)};
 public string LastAction{get;private set;}="";public bool FdHeld{get;private set;}
 public TrainingSixButtonRouter(TrainingInputConfig value,Action<TrainingCommand> output,Func<bool> canFd=null){config=value;emit=output;fdEligible=canFd;}
 bool Enabled(int pair)=>pair==0?config.fdChordEnabled:pair==1?config.throwChordEnabled:pair==2?config.parryChordEnabled:pair==3?config.special1ChordEnabled:config.special2ChordEnabled;
 bool Participant(int button){for(int p=0;p<masks.Length;p++)if(Enabled(p)&&(masks[p]&(1<<button))!=0)return true;return false;}
 Device Get(int id){if(!devices.TryGetValue(id,out var d)){d=new Device();devices[id]=d;}return d;}
 public void Down(int id,int button,double now){var d=Get(id);if(d.held[button])return;d.held[button]=true;d.at[button]=now;if(d.consumed[button])return;if(!Participant(button)){emit(normal[button]);return;}d.pending[button]=true;for(int p=0;p<masks.Length;p++){if(!Enabled(p)||(masks[p]&(1<<button))==0)continue;bool valid=true;double first=now,last=now;for(int i=0;i<6;i++)if((masks[p]&(1<<i))!=0){valid&=d.held[i]&&d.pending[i]&&!d.consumed[i];first=Math.Min(first,d.at[i]);last=Math.Max(last,d.at[i]);}if(valid&&last-first<=Mathf.Clamp(config.chordWindowSeconds,0,.06f)+.000001)d.candidates|=1<<p;}}
 public void Up(int id,int button){var d=Get(id);d.held[button]=false;Unlock(d);}
 void Unlock(Device d){for(int n=d.locks.Count-1;n>=0;n--){int mask=d.locks[n];bool any=false;for(int i=0;i<6;i++)if((mask&(1<<i))!=0)any|=d.held[i];if(!any){for(int i=0;i<6;i++)if((mask&(1<<i))!=0)d.consumed[i]=false;d.locks.RemoveAt(n);}}if(!d.held[0]||!d.held[3])d.fd=false;}
 public void Tick(double now){FdHeld=false;foreach(var d in devices.Values){if(config.fdChordEnabled&&d.pending[0]&&d.pending[3]&&d.held[0]&&d.held[3]&&!d.consumed[0]&&!d.consumed[3]&&Math.Abs(d.at[0]-d.at[3])<=Mathf.Clamp(config.chordWindowSeconds,0,.06f)+.000001)d.candidates|=1;for(int p=0;p<masks.Length;p++)if((d.candidates&(1<<p))!=0&&Enabled(p)&&(p!=0||fdEligible==null||fdEligible())){int mask=masks[p];bool available=true;for(int i=0;i<6;i++)if((mask&(1<<i))!=0)available&=d.pending[i]&&!d.consumed[i];if(!available)continue;for(int i=0;i<6;i++)if((mask&(1<<i))!=0){d.pending[i]=false;d.consumed[i]=true;}d.locks.Add(mask);if(p==0){d.fd=d.held[0]&&d.held[3];LastAction="FD";}else if(p==1){emit(TrainingCommand.Throw);LastAction="Throw";}else if(p==2)LastAction="Parry (future)";else{emit(p==3?TrainingCommand.Special:TrainingCommand.Special2);LastAction=p==3?"Special1":"Special2 (pending move)";}}d.candidates=0;for(int i=0;i<6;i++)if(d.pending[i]&&(now-d.at[i]>=Mathf.Clamp(config.chordWindowSeconds,0,.06f)-.000001||!Participant(i))){d.pending[i]=false;emit(normal[i]);}Unlock(d);FdHeld|=d.fd&&config.fdChordEnabled;}}
 public void Clear(){devices.Clear();FdHeld=false;LastAction="";}
}
