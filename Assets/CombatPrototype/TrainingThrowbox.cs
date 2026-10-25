using UnityEngine;
[System.Flags]public enum TrainingThrowTarget { Ground=1, Air=2, Both=3 }
[RequireComponent(typeof(BoxCollider2D))]
public sealed class TrainingThrowbox:MonoBehaviour {
 public TrainingFighter Fighter{get;private set;}public BoxCollider2D Collider{get;private set;}
 public TrainingThrowSettings Settings=>Fighter==null||Fighter.moveSet==null?null:Fighter.moveSet.throwSettings;
 public TrainingThrowTarget Targets=>Settings==null?0:Settings.targets;
 public Vector3 WorldCenter=>transform.TransformPoint(Collider.offset);
 public Vector3 WorldHalfSize=>new Vector3(Collider.size.x*Mathf.Abs(transform.lossyScale.x)*.5f,Collider.size.y*Mathf.Abs(transform.lossyScale.y)*.5f,0);
 public void Initialize(TrainingFighter owner){Fighter=owner;Refresh();}
 public void Refresh(){if(Collider==null)Collider=GetComponent<BoxCollider2D>();Collider.isTrigger=true;var s=Settings;if(s!=null){Collider.offset=TrainingCoordinates2D.FromLegacy(s.boxCenter,Fighter.Facing);Collider.size=TrainingCoordinates2D.HalfSize(s.boxSize);}var spatial=Fighter==null?null:Fighter.SpatialSample;if(spatial!=null){Collider.offset=TrainingCoordinates2D.FromLegacy(spatial.hitCenter,Fighter.Facing);Collider.size=TrainingCoordinates2D.HalfSize(spatial.hitHalfSize)*2;}Collider.enabled=isActiveAndEnabled&&Fighter!=null&&Fighter.isActiveAndEnabled&&Fighter.ThrowVolumeActive;}
 public bool Matches(TrainingThrowHurtbox target)=>Collider!=null&&Collider.enabled&&target!=null&&target.isActiveAndEnabled&&target.Collider!=null&&target.Collider.enabled&&target.Fighter!=Fighter&&target.Fighter!=null&&target.Fighter.isActiveAndEnabled&&!target.Fighter.ThrowInvulnerable&&(Targets&target.Category)!=0;
 public bool OverlapsAndMatches(TrainingThrowHurtbox target){if(!Matches(target))return false;foreach(var hit in TrainingPhysicsQueries2D.OverlapBox(WorldCenter,WorldHalfSize,transform.eulerAngles.z))if(hit==target.Collider)return true;return false;}
 void OnEnable(){Refresh();}void OnDisable(){if(Collider!=null)Collider.enabled=false;}
}
