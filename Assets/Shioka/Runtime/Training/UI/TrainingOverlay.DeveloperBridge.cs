using UnityEngine;
public sealed partial class TrainingOverlay {
 public bool DeveloperOptionVisible{get;private set;}
 partial void CreateDeveloperOptions();partial void LayoutDeveloperOptions(float w,float h,float left,float x,float right);partial void HideDeveloperOptions();partial void ColorDeveloperHurtbox(HurtboxOutline item);
 void ColorComparisonHurtbox(HurtboxOutline item){if(player!=null&&player.CombatV01!=null&&item.hurtbox!=null){bool chosen=item.hurtbox.Fighter.LastContactHurtboxId==item.hurtbox.StableId;item.line.enabled=true;item.line.startColor=item.line.endColor=chosen?Color.yellow:item.hurtbox.Fighter==player?Color.green:Color.cyan;return;}ColorDeveloperHurtbox(item);}
}
