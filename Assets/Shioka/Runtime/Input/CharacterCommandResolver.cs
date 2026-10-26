using System;using System.Collections.Generic;
// Immediate mapping only. No seconds, grace interval, pending attack, expiry or startup compensation.
public sealed class CharacterCommandResolver {
 public TrainingCommand[] Sample(long frame,int pressed,int held,CharacterAttackSlot[] slots,CharacterDirectionalMove[] directional,int direction=5){var output=new List<TrainingCommand>();slots=slots??CharacterAttackSlot.AwariDefaults();foreach(var slot in slots){if(slot==null||!slot.assigned||(pressed&(1<<(int)slot.slot))==0)continue;var overrideMove=directional==null?null:Array.Find(directional,x=>x!=null&&x.defined&&x.slot==slot.slot&&x.direction==direction);output.Add(overrideMove==null?slot.command:overrideMove.command);}return output.ToArray();}
 public void Clear(){} // ordinary input buffer still owns buffering/expiration, not this mapper.
}
