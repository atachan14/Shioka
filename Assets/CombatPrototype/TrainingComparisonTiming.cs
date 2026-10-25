using UnityEngine;
// Shared session timing draft for the comparison's three ground punches; existing fractional 60Hz semantics.
[System.Serializable]public sealed class TrainingComparisonTiming {
 public float startup,active,recovery;
 public float Total=>startup+active+recovery;
 public bool Validate(out string error){error="";if(!Finite(startup)||!Finite(active)||!Finite(recovery)||startup<0||active<0||recovery<0||Total<=0||Total>600){error="時間は有限・0以上、合計は0超〜600F。持続0Fは判定なし";return false;}return true;}
 static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
 public static TrainingComparisonTiming From(TrainingMove move)=>new TrainingComparisonTiming{startup=move.startupFrames,active=move.activeFrames,recovery=move.recoveryFrames};
 public void Apply(TrainingMove move){move.startupFrames=startup;move.activeFrames=active;move.recoveryFrames=recovery;}
}
