using System;
using System.Collections.Generic;
// Pure per-device resolver. Called at the logical input frame boundary after collecting device events.
// No grace timer. Modifier snapshot at this boundary wins over callback arrival order.
public enum ModifiedInputAction { None, A, B, C, X, Y, Z, Throw, TRC, Force, Dash, Record, Play, Reset }
public struct ModifierFrameResult { public int attackMask;public bool forceHeld;public ModifiedInputAction[] pulses; }
public sealed class ModifierPressOwnership {
 readonly ModifiedInputAction[] owners=new ModifiedInputAction[6];readonly int[] ownerModifier=new int[6];int previousHeld;
 public readonly ModifiedInputAction[] t1={ModifiedInputAction.Record,ModifiedInputAction.Throw,ModifiedInputAction.Force,ModifiedInputAction.Reset,ModifiedInputAction.Play,ModifiedInputAction.Dash};
 public readonly ModifiedInputAction[] t2=new ModifiedInputAction[6];
 public ModifierFrameResult Sample(int held,int pressed,bool modifier1,bool modifier2,bool training){
  held&=63;pressed&=63;var pulses=new List<ModifiedInputAction>();int attacks=0;bool force=false;
  for(int i=0;i<6;i++){
   int bit=1<<i;bool down=(held&bit)!=0;
   if((pressed&bit)!=0||(down&&(previousHeld&bit)==0)){
    // Both modifiers together are undefined: consume without inventing a new action.
    int modifier=modifier1&&modifier2?-1:modifier1?1:modifier2?2:0;
    var action=modifier==0?(ModifiedInputAction)(i+1):modifier==1?t1[i]:modifier==2?t2[i]:ModifiedInputAction.None;
    owners[i]=action;ownerModifier[i]=modifier;
    bool trainingOnly=action==ModifiedInputAction.Record||action==ModifiedInputAction.Play||action==ModifiedInputAction.Reset;
    if(!trainingOnly||training){if(modifier==0)attacks|=bit;else if(action!=ModifiedInputAction.None&&action!=ModifiedInputAction.Force)pulses.Add(action);}
   }
   if(down&&owners[i]==ModifiedInputAction.Force)force|=ownerModifier[i]==1?modifier1:ownerModifier[i]==2&&modifier2;
   if(!down){owners[i]=ModifiedInputAction.None;ownerModifier[i]=0;}
  }
  previousHeld=held;return new ModifierFrameResult{attackMask=attacks,forceHeld=force,pulses=pulses.ToArray()};
 }
 public void Clear(){previousHeld=0;Array.Clear(owners,0,6);Array.Clear(ownerModifier,0,6);}
 public static bool ValidateBindings(string modifier1,string modifier2,string[] normal,out string error){
  error=null;if(normal==null){error="Bindings required";return false;}var m1=(modifier1??"").Trim();var m2=(modifier2??"").Trim();if(m1.Length>0&&m2.Length>0&&string.Equals(m1,m2,StringComparison.OrdinalIgnoreCase)){error="T1 and T2 must differ";return false;}
  foreach(string path in normal){string p=(path??"").Trim();if(p.Length>0&&(string.Equals(p,m1,StringComparison.OrdinalIgnoreCase)||string.Equals(p,m2,StringComparison.OrdinalIgnoreCase))){error="Modifier cannot share a normal physical binding";return false;}}return true;
 }
 public static bool ValidateChords(IEnumerable<string> contextModifierTriggerKeys,out string error){error=null;var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(var key in contextModifierTriggerKeys)if(!seen.Add((key??"").Trim())){error="Duplicate chord in context";return false;}return true;}
}
