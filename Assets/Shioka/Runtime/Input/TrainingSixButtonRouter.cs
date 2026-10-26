using System;using System.Collections.Generic;
public interface ITrainingPendingChordSource { void ClearPendingChordInput(); }
// Legacy six-slot adapter: immediate down events only. Old system/Special chords are retired.
public sealed class TrainingSixButtonRouter {
 readonly Dictionary<int,int> held=new Dictionary<int,int>();readonly Action<TrainingCommand> emit;
 static readonly TrainingCommand[] normal={TrainingCommand.Light,TrainingCommand.Medium,TrainingCommand.Heavy,TrainingCommand.Slash,TrainingCommand.HeavySlash,TrainingCommand.StrongKick};
 public bool FdHeld=>false;public string LastAction=>"";
 public TrainingSixButtonRouter(TrainingInputConfig config,Action<TrainingCommand> output,Func<bool> unused=null){emit=output;}
 public void Down(int device,int button,double unused){held.TryGetValue(device,out int mask);int bit=1<<button;if((mask&bit)!=0)return;held[device]=mask|bit;emit(normal[button]);}
 public void Up(int device,int button){held.TryGetValue(device,out int mask);held[device]=mask&~(1<<button);}
 public void Tick(double unused){}public void Clear(){held.Clear();}
}
