using System;
// Stable IDs preserve the existing six slots. Display order is independent of serialized numeric IDs.
public enum BattleAttackSlot { A=0,B=1,C=2,J=6,K=7,L=8,X=3,Y=4,Z=5 }
[Serializable] public sealed class CharacterAttackSlot {
 public BattleAttackSlot slot;public bool assigned=true;public TrainingCommand command;public string displayName;
 public static readonly BattleAttackSlot[] DisplayOrder={BattleAttackSlot.A,BattleAttackSlot.B,BattleAttackSlot.C,BattleAttackSlot.J,BattleAttackSlot.K,BattleAttackSlot.L,BattleAttackSlot.X,BattleAttackSlot.Y,BattleAttackSlot.Z};
 public static CharacterAttackSlot[] AwariDefaults()=>new[]{
 new CharacterAttackSlot{slot=BattleAttackSlot.A,command=TrainingCommand.Light,displayName="LP"},new CharacterAttackSlot{slot=BattleAttackSlot.B,command=TrainingCommand.Medium,displayName="MP"},new CharacterAttackSlot{slot=BattleAttackSlot.C,command=TrainingCommand.Heavy,displayName="HP"},new CharacterAttackSlot{slot=BattleAttackSlot.X,command=TrainingCommand.Slash,displayName="LK"},new CharacterAttackSlot{slot=BattleAttackSlot.Y,command=TrainingCommand.HeavySlash,displayName="MK"},new CharacterAttackSlot{slot=BattleAttackSlot.Z,command=TrainingCommand.StrongKick,displayName="HK"},new CharacterAttackSlot{slot=BattleAttackSlot.J,assigned=false,displayName=""},new CharacterAttackSlot{slot=BattleAttackSlot.K,assigned=false,displayName=""},new CharacterAttackSlot{slot=BattleAttackSlot.L,assigned=false,displayName=""}};
 public static bool Validate(CharacterAttackSlot[] slots,out string error){error=null;if(slots==null||slots.Length!=9){error="Nine slots required";return false;}int seen=0;foreach(var x in slots){if(x==null||(int)x.slot<0||(int)x.slot>8||x.assigned&&(string.IsNullOrWhiteSpace(x.displayName)||!Enum.IsDefined(typeof(TrainingCommand),x.command))){error="Invalid slot or assigned move";return false;}int bit=1<<(int)x.slot;if((seen&bit)!=0){error="Duplicate slot";return false;}seen|=bit;}return true;}
}
[Serializable] public sealed class CharacterDirectionalMove {
 public BattleAttackSlot slot;public int direction=5;public bool defined;public TrainingCommand command;public string displayName;
}
