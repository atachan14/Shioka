using UnityEngine;
public sealed class TrainingCancelEffect:MonoBehaviour {
 public const float Lifetime=.22f; public const int MaximumRings=4;
 sealed class Ring {public GameObject item;public LineRenderer line;public float age;public Vector3 center;}
 readonly System.Collections.Generic.List<Ring> rings=new System.Collections.Generic.List<Ring>();TrainingFighter fighter;Material material;
 public int ActiveCount=>rings.Count;public int TotalBursts{get;private set;}
 void OnEnable(){fighter=GetComponent<TrainingFighter>();if(fighter!=null)fighter.TdcPerformed+=Burst;}
 void OnDisable(){if(fighter!=null)fighter.TdcPerformed-=Burst;Clear();}
 void Burst(TrainingFighter source){if(rings.Count>=MaximumRings){Destroy(rings[0].item);rings.RemoveAt(0);}if(material==null)material=new Material(Resources.Load<Shader>("TrainingCancelRing"));var ring=new Ring();ring.item=new GameObject("TDC red ring");ring.line=ring.item.AddComponent<LineRenderer>();ring.line.sharedMaterial=material;ring.line.loop=true;ring.line.positionCount=48;ring.line.useWorldSpace=true;ring.line.widthMultiplier=.11f;ring.center=source.GetComponent<TrainingMotor2D>().bounds.center-Vector3.forward*.34f;rings.Add(ring);TotalBursts++;Draw(ring);}
 void Draw(Ring ring){float t=Mathf.Clamp01(ring.age/Lifetime);float radius=Mathf.Lerp(.12f,.9f,1-(1-t)*(1-t));var color=new Color(1,.045f,.025f,(1-t)*.72f);ring.line.startColor=ring.line.endColor=color;for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;ring.line.SetPosition(i,ring.center+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}}
 void Update(){for(int i=rings.Count-1;i>=0;i--){var ring=rings[i];ring.age+=Time.deltaTime;if(ring.age>=Lifetime){Destroy(ring.item);rings.RemoveAt(i);}else Draw(ring);}}
 public void Clear(){foreach(var ring in rings)if(ring.item!=null)Destroy(ring.item);rings.Clear();}
 void OnDestroy(){Clear();if(material!=null)Destroy(material);}
}
