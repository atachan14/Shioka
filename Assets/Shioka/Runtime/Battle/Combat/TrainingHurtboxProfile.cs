using UnityEngine;
public enum TrainingHurtboxShape { BodyUpper, BodyLower, Box, BodyWhole }
[System.Serializable]
public sealed class TrainingHurtboxRegion {
 public int stableId;
 [Min(0)]public float combatPositionCorrection=1;
 public int selectionPriority;
 public TrainingHurtboxShape shape;
 public Vector3 center=new Vector3(0,.85f,0),size=new Vector3(.6f,1.7f,.6f);
 public float highDefense=50f,midDefense=50f,lowDefense=50f;
 public float BaseDefense(TrainingAttackClass kind)=>kind==TrainingAttackClass.High?highDefense:kind==TrainingAttackClass.Low?lowDefense:midDefense;
}
[CreateAssetMenu(menuName="Shioka/Hurtbox Profile")]
public sealed class TrainingHurtboxProfile:ScriptableObject {
 public TrainingHurtboxRegion[] regions={new TrainingHurtboxRegion{stableId=0,shape=TrainingHurtboxShape.BodyWhole}};
 public bool HasValidIds(){var ids=new System.Collections.Generic.HashSet<int>();if(regions==null)return false;foreach(var r in regions)if(r==null||!ids.Add(r.stableId)||float.IsNaN(r.highDefense)||float.IsInfinity(r.highDefense)||float.IsNaN(r.midDefense)||float.IsInfinity(r.midDefense)||float.IsNaN(r.lowDefense)||float.IsInfinity(r.lowDefense))return false;return true;}
}