using UnityEngine;
[CreateAssetMenu(menuName="Shioka/Combat Test v0.1")]
public sealed class CombatV01Profile:ScriptableObject {
 public TrainingMoveSet moves;public TrainingHurtboxProfile hurtboxes;
 [Range(-200,200)]public float playerTG,opponentTG;[Min(1)]public float hp=2300;[Range(1,120)]public int pushFrames=10;
 static bool N(float n)=>CombatV01Formula.Finite(n)&&n>=0&&n<=1000000;
 public bool Valid=>Validate(out _);
 public bool Validate(out string error){error="";if(moves==null||hurtboxes==null||!hurtboxes.HasValidIds()||hurtboxes.regions.Length==0||!CombatV01Formula.Finite(playerTG)||!CombatV01Formula.Finite(opponentTG)||System.Math.Abs(playerTG)>1000000||System.Math.Abs(opponentTG)>1000000||!N(hp)||hp<=0||pushFrames<1||pushFrames>120){error="Finite signed TG / positive HP, referenced moves/hurtboxes, unique part IDs and push frames1..120 required";return false;}foreach(var region in hurtboxes.regions)if(!N(region.highDefense)||!N(region.midDefense)||!N(region.lowDefense)||!N(region.combatPositionCorrection)){error="Invalid defense/correction";return false;}foreach(var command in new[]{TrainingCommand.Light,TrainingCommand.Medium,TrainingCommand.Heavy}){var m=moves.Find(command);if(m==null||!N(m.attackLevel)||!N(m.combatDamageMultiplier)||!N(m.combatChipMultiplier)||m.combatChipMultiplier>1||!N(m.combatPushbackScale)||!N(m.startupFrames)||!N(m.activeFrames)||!N(m.recoveryFrames)||m.Duration<=0||m.strength!=null&&m.strength.enabled&&!m.strength.IsValid){error="Invalid P intensity/timing/multipliers";return false;}if(m.useLevelCurves&&m.attackLevelCurve!=null)foreach(var k in m.attackLevelCurve.keys)if(!CombatV01Formula.Finite(k.time)||!CombatV01Formula.Finite(k.value)){error="Nonfinite intensity curve";return false;}}return true;}
}
