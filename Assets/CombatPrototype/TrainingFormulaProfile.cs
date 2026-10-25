using System;using UnityEngine;
public enum TrainingComparisonFormula { Subtraction, Attenuation }
[Serializable] public sealed class TrainingFormulaMove {
 public TrainingCommand command;
 [Min(0)] public float minimumAttack,maximumAttack,defenseMultiplier=1,threshold,guardDamageScale;
 public TrainingFormulaMove Copy()=>JsonUtility.FromJson<TrainingFormulaMove>(JsonUtility.ToJson(this));
 public bool Valid=>F(minimumAttack)&&F(maximumAttack)&&maximumAttack>=minimumAttack&&F(defenseMultiplier)&&F(threshold)&&F(guardDamageScale)&&guardDamageScale<=1&&defenseMultiplier<=1000;
 static bool F(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n)&&n>=0&&n<=1000000;
}
[CreateAssetMenu(menuName="Shioka/Developer/3P Formula Comparison")]
public sealed class TrainingFormulaProfile:ScriptableObject {
 public TrainingComparisonFormula formula;
 public TrainingFormulaMove[] moves=new TrainingFormulaMove[0];
 public bool Validate(out string error){error="";if(!Enum.IsDefined(typeof(TrainingComparisonFormula),formula)||moves==null||moves.Length!=3){error="Need exactly three valid P entries";return false;}var ids=new System.Collections.Generic.HashSet<TrainingCommand>();foreach(var m in moves)if(m==null||!m.Valid||!(m.command==TrainingCommand.Light||m.command==TrainingCommand.Medium||m.command==TrainingCommand.Heavy)||!ids.Add(m.command)){error="Finite nonnegative values, max >= min, values <= 1,000,000, defense multiplier <= 1000, chip 0..1, and unique light/medium/heavy P required";return false;}return true;}
 public TrainingFormulaMove Find(TrainingCommand command){foreach(var m in moves)if(m!=null&&m.command==command)return m;return null;}
 public TrainingComparisonRule Rule(TrainingCommand command){var m=Find(command);return m==null?TrainingComparisonRule.Legacy:new TrainingComparisonRule{formula=formula,defenseMultiplier=m.defenseMultiplier,threshold=m.threshold,guardDamageScale=m.guardDamageScale};}
}
public struct TrainingComparisonRule {
 public TrainingComparisonFormula formula;public float defenseMultiplier,threshold,guardDamageScale;
 public static TrainingComparisonRule Legacy=>new TrainingComparisonRule{defenseMultiplier=1};
 public float Defense(float d)=>d*defenseMultiplier;
 public float Final(float effectiveAttack,float adjustedDefense)=>formula==TrainingComparisonFormula.Attenuation?effectiveAttack/(1+Mathf.Max(0,adjustedDefense)/100):TrainingHitFormula.Result(effectiveAttack,adjustedDefense);
 public bool IsHit(float result)=>result>threshold;
}
public sealed class TrainingResolvedContact {
 public int partId=-1,sequence;public float rawAttack,effectiveAttack,defense,finalResult,threshold,damage,stunFrames,hitstopFrames;public bool hit,continuedCombo;public TrainingComparisonFormula formula;
}
