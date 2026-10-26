using System;using System.Collections.Generic;using System.Text;using UnityEngine;
// Numpad directions are facing-relative: 6 forward, 4 back, 5 neutral.
// Both facing and airborne state belong to the input's issue tick, never render time.
public sealed class TrainingCommandHistory {
 readonly List<string> rows=new List<string>();readonly HashSet<TrainingCommandTrace> seen=new HashSet<TrainingCommandTrace>(),available=new HashSet<TrainingCommandTrace>();int reset=-1;long frame=-1;
 public int Count=>rows.Count;
 public void Capture(TrainingFighter fighter){
  if(fighter==null)return;var buffer=fighter.InputBuffer;
  if(reset!=buffer.ResetVersion){rows.Clear();seen.Clear();available.Clear();frame=-1;reset=buffer.ResetVersion;}
  if(buffer.Frame==frame)return;frame=buffer.Frame;
  var sample=buffer.Samples.Count==0?default:buffer.Samples[buffer.Samples.Count-1];
  bool actionEvent=false;var entries=new List<string>();available.Clear();foreach(var item in buffer.Trace)available.Add(item);seen.IntersectWith(available);
  foreach(var trace in buffer.Trace)if(trace.Outcome==TrainingCommandOutcome.Executed&&!seen.Contains(trace)){
   seen.Add(trace);string action=Name(trace.Command);if(action.Length==0)continue;
   var issued=sample;for(int i=buffer.Samples.Count-1;i>=0;i--)if(buffer.Samples[i].Frame==trace.IssuedFrame){issued=buffer.Samples[i];break;}
   actionEvent=true;if(entries.Count<8)entries.Add(Format(issued.Movement,issued.Facing,issued.Airborne,action));
  }
  if(entries.Count==0)entries.Add(Format(sample.Movement,sample.Facing,sample.Airborne,sample.FdHeld?"FD":sample.Dash?"D":""));
  string text=string.Join("/",entries);if(actionEvent||rows.Count==0||rows[rows.Count-1]!=text){rows.Add(text);if(rows.Count>64)rows.RemoveAt(0);}
 }
 public string Text(int visible=24){var b=new StringBuilder();for(int i=rows.Count-1;i>=Math.Max(0,rows.Count-visible);i--)b.Append(rows[i]).Append('\n');return b.ToString();}
 public static string Direction(Vector2 move,int facing=1){int x=move.x*(facing<0?-1:1)<-.1f?-1:move.x*(facing<0?-1:1)>.1f?1:0,y=move.y<-.1f?-1:move.y>.1f?1:0;return (5+x+3*y).ToString();}
 public static string Format(Vector2 move,int facing,bool airborne,string action)=>Direction(move,facing)+(string.IsNullOrEmpty(action)?"":(airborne?"J":"")+action);
 public static string Name(TrainingCommand c){switch(c){case TrainingCommand.Light:case TrainingCommand.CrouchWeakPunch:return "LP";case TrainingCommand.Medium:case TrainingCommand.CrouchMediumPunch:return "MP";case TrainingCommand.Heavy:case TrainingCommand.CrouchStrongPunch:case TrainingCommand.AirStrongPunch:return "HP";case TrainingCommand.Slash:case TrainingCommand.CrouchWeakKick:return "LK";case TrainingCommand.HeavySlash:case TrainingCommand.CrouchMediumKick:return "MK";case TrainingCommand.StrongKick:case TrainingCommand.CrouchStrongKick:case TrainingCommand.AirStrongKick:case TrainingCommand.AirDownStrongKick:return "HK";case TrainingCommand.Jump:return "";case TrainingCommand.Cancel:return "TRC";case TrainingCommand.Throw:return "Throw";default:return c.ToString();}}
}
