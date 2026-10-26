using System;using UnityEngine;
[Serializable]public sealed class CombatFormulaValues {public double defenseDivisor=100,stunDivisor=10,hitstopDivisor=40,hitPushRatio=1.2,blockPushDivisor=3;public bool Valid=>P(defenseDivisor)&&P(stunDivisor)&&P(hitstopDivisor)&&P(blockPushDivisor)&&CombatV01Formula.Finite(hitPushRatio)&&hitPushRatio>=0;static bool P(double d)=>CombatV01Formula.Finite(d)&&d>0;}
[CreateAssetMenu(menuName="Shioka/System/Combat formula")]
public sealed class CombatFormulaTuning:ScriptableObject {public CombatFormulaValues values=new CombatFormulaValues();public bool Valid=>values!=null&&values.Valid;}
