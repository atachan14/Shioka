using UnityEngine;
[RequireComponent(typeof(BoxCollider2D))]
public sealed class TrainingThrowHurtbox:MonoBehaviour {
 public TrainingFighter Fighter{get;private set;}public BoxCollider2D Collider{get;private set;}
 public TrainingThrowTarget Category=>Fighter==null?0:Fighter.IsGrounded?TrainingThrowTarget.Ground:TrainingThrowTarget.Air;
 public void Initialize(TrainingFighter owner){Fighter=owner;Refresh();}
 public void Refresh(){if(Collider==null)Collider=GetComponent<BoxCollider2D>();Collider.isTrigger=true;if(Fighter!=null&&Fighter.throwHurtboxProfile!=null){Collider.offset=TrainingCoordinates2D.FromLegacy(Fighter.throwHurtboxProfile.center);Collider.size=TrainingCoordinates2D.HalfSize(Fighter.throwHurtboxProfile.size);}Collider.enabled=isActiveAndEnabled&&Fighter!=null&&Fighter.isActiveAndEnabled&&!Fighter.ThrowInvulnerable;}
 void OnEnable(){Refresh();}void OnDisable(){if(Collider!=null)Collider.enabled=false;}
}
