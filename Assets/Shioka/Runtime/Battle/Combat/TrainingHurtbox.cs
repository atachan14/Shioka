using UnityEngine;
[RequireComponent(typeof(BoxCollider2D))]
public sealed class TrainingHurtbox:MonoBehaviour {
 public TrainingFighter Fighter{get;private set;}public TrainingHurtboxRegion Region{get;private set;}
 public int StableId=>Region.stableId;public bool Upper=>Region.shape==TrainingHurtboxShape.BodyUpper;
 public BoxCollider2D Collider{get;private set;}
 public void Initialize(TrainingFighter fighter,bool upper)=>Initialize(fighter,new TrainingHurtboxRegion{stableId=upper?0:1,shape=upper?TrainingHurtboxShape.BodyUpper:TrainingHurtboxShape.BodyLower});
 public void Initialize(TrainingFighter fighter,TrainingHurtboxRegion region){Fighter=fighter;Region=region;Collider=GetComponent<BoxCollider2D>();Collider.isTrigger=true;Refresh();}
 public void Refresh(){if(Fighter==null||Collider==null)return;var body=Fighter.GetComponent<TrainingMotor2D>();if(body==null)return;bool down=Fighter.IsDown&&Fighter.IsGrounded;
  if(!down&&Fighter.SpatialHurtGeometry(StableId,out var center,out var size)){Collider.offset=center;Collider.size=size;}
  else if(!down&&Fighter.MotionHurtGeometry(StableId,out var motionCenter,out var motionSize)){Collider.offset=motionCenter;Collider.size=motionSize;}
  else if(Region.shape==TrainingHurtboxShape.Box&&!down){Collider.offset=TrainingCoordinates2D.FromLegacy(Region.center,Fighter.Facing);Collider.size=TrainingCoordinates2D.HalfSize(Region.size)*2*.5f;}
  else {float height=down?.35f:body.height,split=down?height*.5f:Mathf.Min(.75f,height*.6f),bottom=down?0:body.center.y-body.height*.5f;bool whole=Region.shape==TrainingHurtboxShape.BodyWhole;float partHeight=whole?height:Upper?height-split:split;Collider.offset=new Vector2(0,bottom+(!whole&&Upper?split:0)+partHeight*.5f);Collider.size=new Vector2(down?1.2f:body.radius*2,partHeight);}
  var moves=Fighter.moveSet;Collider.enabled=!Fighter.IsDown||(moves!=null&&moves.allowDownFollowup&&Fighter.CurrentPhase==TrainingFighter.Phase.Down&&Fighter.DownFollowupCount<Mathf.Max(0,moves.maxDownFollowups));
 }
}
