using UnityEngine;
[CreateAssetMenu(menuName="Shioka/Character Body Calibration")]
public sealed class TrainingBodyCalibration:ScriptableObject {
 [Min(.1f)]public float standingHeight=1.55f;
 public Sprite referenceSprite;
 [Tooltip("Author-estimated anatomical skull crown, top-left source pixel coordinates; excludes ornaments and hair crest.")]public Vector2 skullPixel=new Vector2(625,145);
 [Tooltip("Supporting boot sole reference, top-left source pixel coordinates.")]public Vector2 solePixel=new Vector2(550,1500);
 public bool IsValid=>referenceSprite!=null&&Finite(standingHeight)&&standingHeight>0&&Finite(skullPixel.y)&&Finite(solePixel.y)&&solePixel.y>skullPixel.y&&solePixel.y<=referenceSprite.rect.height&&skullPixel.y>=0;
 public float VisualScale=>IsValid?standingHeight*referenceSprite.pixelsPerUnit/(solePixel.y-skullPixel.y):1;
 public float ReferenceBodyHeight=>IsValid?(solePixel.y-skullPixel.y)/referenceSprite.pixelsPerUnit*VisualScale:0;
 static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
}
