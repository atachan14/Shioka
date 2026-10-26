using System;
using UnityEngine;
public enum BattleAttackSlot { A, B, C, X, Y, Z }
[Serializable] public sealed class CharacterAttackSlot {
 public BattleAttackSlot slot; public TrainingCommand command; public string displayName;
 public static CharacterAttackSlot[] AwariDefaults()=>new[]{
  new CharacterAttackSlot{slot=BattleAttackSlot.A,command=TrainingCommand.Light,displayName="LP"},
  new CharacterAttackSlot{slot=BattleAttackSlot.B,command=TrainingCommand.Medium,displayName="MP"},
  new CharacterAttackSlot{slot=BattleAttackSlot.C,command=TrainingCommand.Heavy,displayName="HP"},
  new CharacterAttackSlot{slot=BattleAttackSlot.X,command=TrainingCommand.Slash,displayName="LK"},
  new CharacterAttackSlot{slot=BattleAttackSlot.Y,command=TrainingCommand.HeavySlash,displayName="MK"},
  new CharacterAttackSlot{slot=BattleAttackSlot.Z,command=TrainingCommand.StrongKick,displayName="HK"}};
 public static bool Validate(CharacterAttackSlot[] slots,out string error){error=null;if(slots==null||slots.Length!=6){error="Exactly six slots required";return false;}int seen=0;foreach(var item in slots){if(item==null||(int)item.slot<0||(int)item.slot>5||string.IsNullOrWhiteSpace(item.displayName)||!Enum.IsDefined(typeof(TrainingCommand),item.command)){error="Invalid slot, command or display name";return false;}int bit=1<<(int)item.slot;if((seen&bit)!=0){error="Duplicate logical slot";return false;}seen|=bit;}return true;}
}
