using UnityEngine;

[System.Flags]public enum TrainingMotorContact2D { None=0, Sides=1, Above=2, Below=4 }

// Kinematic motor: Unity Physics2D performs shape casts and contact geometry.
// Existing combat code remains responsible for gravity, velocities and action time.
[RequireComponent(typeof(Rigidbody2D),typeof(CapsuleCollider2D))]
public sealed class TrainingMotor2D:MonoBehaviour {
 public float skinWidth=.08f;
 Rigidbody2D rigidBody;CapsuleCollider2D capsule;
 readonly RaycastHit2D[] hits=new RaycastHit2D[32];
 public Rigidbody2D Body {get{Ensure();return rigidBody;}}
 public CapsuleCollider2D Shape {get{Ensure();return capsule;}}
 public float Height {get=>Shape.size.y;set{var s=Shape.size;s.y=Mathf.Max(.02f,value);Shape.size=s;}}
 public float Radius {get=>Shape.size.x*.5f;set{var s=Shape.size;s.x=Mathf.Max(.02f,value*2);Shape.size=s;}}
 public Vector2 Center {get=>Shape.offset;set=>Shape.offset=value;}
 public Bounds Bounds=>Shape.bounds;
 public float height {get=>Height;set=>Height=value;}public float radius {get=>Radius;set=>Radius=value;}
 public Vector3 center {get=>Center;set=>Center=value;}public Bounds bounds=>Bounds;
 public TrainingMotorContact2D Move(Vector3 displacement)=>Move((Vector2)displacement);
 void OnDisable(){if(capsule!=null)capsule.enabled=false;if(rigidBody!=null)rigidBody.simulated=false;}
 void OnEnable(){Ensure();capsule.enabled=true;rigidBody.simulated=true;rigidBody.position=transform.position;}

 void Awake(){Ensure();}
 void Ensure(){if(rigidBody==null){rigidBody=GetComponent<Rigidbody2D>();rigidBody.bodyType=RigidbodyType2D.Kinematic;rigidBody.gravityScale=0;rigidBody.constraints=RigidbodyConstraints2D.FreezeRotation;rigidBody.interpolation=RigidbodyInterpolation2D.None;}if(capsule==null){capsule=GetComponent<CapsuleCollider2D>();capsule.direction=CapsuleDirection2D.Vertical;}}
 static ContactFilter2D SolidFilter(){var filter=new ContactFilter2D();filter.SetLayerMask(Physics2D.DefaultRaycastLayers);filter.useTriggers=false;return filter;}
 public void Teleport(Vector2 position){Ensure();rigidBody.position=position;transform.position=new Vector3(position.x,position.y,0);Physics2D.SyncTransforms();}
 public TrainingMotorContact2D Move(Vector2 displacement){
  Ensure();if(!enabled||!capsule.enabled)return TrainingMotorContact2D.None;
  // Transform edits from reset/authoring are explicit and synchronized at this boundary.
  rigidBody.position=transform.position;Physics2D.SyncTransforms();var result=TrainingMotorContact2D.None;var remaining=displacement;
  for(int pass=0;pass<4&&remaining.sqrMagnitude>1e-12f;pass++){
   float separation=Mathf.Max(.001f,Physics2D.defaultContactOffset*.5f+.001f);float length=remaining.magnitude;var direction=remaining/length;int count=capsule.Cast(direction,SolidFilter(),hits,length+separation);int chosen=-1;float closest=length+separation;
   for(int i=0;i<count;i++){var hit=hits[i];if(hit.collider==null||hit.collider.attachedRigidbody==rigidBody||Vector2.Dot(direction,hit.normal)>=-.00001f)continue;
    // Fighter pushboxes are resolved symmetrically by the combat motor, not by the floor solver.
    if(hit.collider.GetComponentInParent<TrainingMotor2D>()!=null)continue;
    if(hit.distance<closest){closest=hit.distance;chosen=i;}}
   if(chosen<0){rigidBody.position+=remaining;remaining=Vector2.zero;break;}
   var contact=hits[chosen];float travel=Mathf.Clamp(contact.distance-separation,0,length);rigidBody.position+=direction*travel;
   if(contact.normal.y>.5f)result|=TrainingMotorContact2D.Below;else if(contact.normal.y<-.5f)result|=TrainingMotorContact2D.Above;else result|=TrainingMotorContact2D.Sides;
   remaining-=direction*travel;float into=Vector2.Dot(remaining,contact.normal);if(into<0)remaining-=contact.normal*into;
   transform.position=new Vector3(rigidBody.position.x,rigidBody.position.y,0);Physics2D.SyncTransforms();
  }
  transform.position=new Vector3(rigidBody.position.x,rigidBody.position.y,0);Physics2D.SyncTransforms();return result;
 }
 public bool HasFloorSupport(){Ensure();Physics2D.SyncTransforms();int count=capsule.Cast(Vector2.down,SolidFilter(),hits,.04f);for(int i=0;i<count;i++)if(hits[i].collider!=null&&hits[i].normal.y>.65f&&hits[i].collider.GetComponentInParent<TrainingMotor2D>()==null)return true;return false;}
}
