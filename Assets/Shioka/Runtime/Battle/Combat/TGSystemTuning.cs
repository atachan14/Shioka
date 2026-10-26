using System;using UnityEngine;
[Serializable]public sealed class TGSystemValues {public double positiveDivisor=200,negativeDivisor=100;public bool Valid=>CombatV01Formula.Finite(positiveDivisor)&&positiveDivisor>0&&CombatV01Formula.Finite(negativeDivisor)&&negativeDivisor>0;}
[CreateAssetMenu(menuName="Shioka/System/TG")]
public sealed class TGSystemTuning:ScriptableObject {public TGSystemValues values=new TGSystemValues();public bool Valid=>values!=null&&values.Valid;}
