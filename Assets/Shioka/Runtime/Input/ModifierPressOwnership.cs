using System;using System.Collections.Generic;
// Per-device sampled chord edges. No grace timer and no cancellation of earlier normal presses.
public enum ModifiedInputAction { None,A,B,C,X,Y,Z,[UnityEngine.InspectorName("Hold")] Throw,TRC,[UnityEngine.InspectorName("Mania")] Force,Dash,Record,Play,Reset,J,K,L }
public struct ModifierFrameResult {public int attackMask;public bool forceHeld,dashHeld;public ModifiedInputAction[] pulses;}
public sealed class ModifierPressOwnership {
 int previousHeld,previousChord1,previousChord2;
 public readonly ModifiedInputAction[] t1={ModifiedInputAction.None,ModifiedInputAction.None,ModifiedInputAction.None,ModifiedInputAction.Dash,ModifiedInputAction.Throw,ModifiedInputAction.TRC,ModifiedInputAction.A,ModifiedInputAction.B,ModifiedInputAction.C};
 public readonly ModifiedInputAction[] t2=new ModifiedInputAction[9];
 public ModifierFrameResult Sample(int held,int pressed,bool m1,bool m2,bool training)=>SampleAtBoundary(held,pressed,m1,m2,m1,m2,training);
 public ModifierFrameResult SampleAtBoundary(int held,int pressed,bool decision1,bool decision2,bool m1,bool m2,bool training){held&=511;pressed&=511;int attacks=0;bool force=false,dash=false;var pulses=new List<ModifiedInputAction>();for(int i=0;i<9;i++){int bit=1<<i;bool down=(held&bit)!=0,newPress=(pressed&bit)!=0||(down&&(previousHeld&bit)==0);if(!decision1&&!decision2&&newPress)attacks|=bit;if(decision1&&decision2)continue;int modifier=decision1?1:decision2?2:0;if(modifier==0)continue;var action=modifier==1?t1[i]:t2[i];int previous=modifier==1?previousChord1:previousChord2;bool candidate=((held|pressed)&bit)!=0;bool edge=candidate&&(((previous&bit)==0)||newPress);bool trainingOnly=action==ModifiedInputAction.Record||action==ModifiedInputAction.Play||action==ModifiedInputAction.Reset;if(edge&&(!trainingOnly||training)&&action!=ModifiedInputAction.None&&action!=ModifiedInputAction.Force)pulses.Add(action);bool active=down&&(modifier==1?m1:m2);force|=active&&action==ModifiedInputAction.Force;dash|=active&&action==ModifiedInputAction.Dash;}previousHeld=held;previousChord1=m1&&!m2?held:0;previousChord2=m2&&!m1?held:0;return new ModifierFrameResult{attackMask=attacks,forceHeld=force,dashHeld=dash,pulses=pulses.ToArray()};}
 public void Clear(){previousHeld=previousChord1=previousChord2=0;}
 public static bool ValidateBindings(string modifier1,string modifier2,string[] normal,out string error){
  error=null;if(normal==null){error="Bindings required";return false;}var m1=(modifier1??"").Trim();var m2=(modifier2??"").Trim();if(m1.Length>0&&m2.Length>0&&string.Equals(m1,m2,StringComparison.OrdinalIgnoreCase)){error="T1 and T2 must differ";return false;}
  foreach(string path in normal){string p=(path??"").Trim();if(p.Length>0&&(string.Equals(p,m1,StringComparison.OrdinalIgnoreCase)||string.Equals(p,m2,StringComparison.OrdinalIgnoreCase))){error="Modifier cannot share a normal physical binding";return false;}}return true;
 }
 public static bool ValidateChords(IEnumerable<string> contextModifierTriggerKeys,out string error){error=null;var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(var key in contextModifierTriggerKeys)if(!seen.Add((key??"").Trim())){error="Duplicate chord in context";return false;}return true;}
}
