using UnityEngine;
using UnityEngine.UI;

public sealed partial class TrainingOverlay
{
    public TrainingLevelDisplaySettings levelDisplay = new TrainingLevelDisplaySettings();
    private Font defenseFont;
    private Mesh defenseQuad;
    // The combat contact routine remains the sole authority for effective defense.
    public float DisplayDefense(TrainingHurtbox part,TrainingAttackClass kind) => part.Fighter.ContactDefenseFor(kind,part);
    private void CreateDefenseDisplay(HurtboxOutline item)
    {
        if(defenseQuad==null){defenseQuad=new Mesh();defenseQuad.name="Defense visualization quad";defenseQuad.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};defenseQuad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};defenseQuad.triangles=new[]{0,1,2,0,2,3};defenseQuad.RecalculateBounds();}
        var fill=new GameObject("Effective defense fill (display only)",typeof(MeshFilter),typeof(MeshRenderer));fill.transform.SetParent(item.line.transform,false);fill.GetComponent<MeshFilter>().sharedMesh=defenseQuad;
        item.fill=fill.GetComponent<MeshRenderer>();item.fill.sharedMaterial=new Material(Shader.Find("Shioka/TrainingDefenseFill"));item.fill.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;item.fill.receiveShadows=false;
        if(defenseFont==null)defenseFont=Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic","Meiryo","Arial"},levelDisplay.labelFontSize);
        item.labels=new Text[3];
        for(int i=0;i<3;i++){var go=new GameObject("Defense value "+i,typeof(RectTransform),typeof(Text),typeof(Outline));go.layer=5;go.transform.SetParent(hudRoot.transform,false);var t=go.GetComponent<Text>();t.font=defenseFont;t.fontSize=levelDisplay.labelFontSize;t.alignment=TextAnchor.MiddleLeft;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;go.GetComponent<Outline>().effectColor=Color.black;go.GetComponent<Outline>().effectDistance=new Vector2(1,-1);item.labels[i]=t;}
    }
    private void DestroyDefenseDisplay(HurtboxOutline item)
    {
        if(item.labels!=null)foreach(var t in item.labels)if(t!=null)Destroy(t.gameObject);
        if(item.fill!=null)Destroy(item.fill.sharedMaterial);
    }
    private void UpdateDefenseDisplay(HurtboxOutline item,bool visible)
    {
        if(item.labels==null)return;
        foreach(var t in item.labels)t.gameObject.SetActive(visible&&ActorsAvailable&&!HudHelpVisible&&!HudHistoryVisible&&(DisplaySettings==null||DisplaySettings.showWorldDefenseLabels));
        if(!visible||worldCamera==null)return;
        var bounds=item.hurtbox.Collider.bounds;var cam=worldCamera;var right=cam.transform.right;var up=cam.transform.up;
        float width=2*(Mathf.Abs(right.x)*bounds.extents.x+Mathf.Abs(right.y)*bounds.extents.y+Mathf.Abs(right.z)*bounds.extents.z);
        float height=2*(Mathf.Abs(up.x)*bounds.extents.x+Mathf.Abs(up.y)*bounds.extents.y+Mathf.Abs(up.z)*bounds.extents.z);
        item.fill.transform.position=bounds.center;item.fill.transform.rotation=cam.transform.rotation;item.fill.transform.localScale=new Vector3(width,height,1);
        var kinds=new[]{TrainingAttackClass.Mid,TrainingAttackClass.High,TrainingAttackClass.Low};var names=new[]{"中","上","下"};
        for(int i=0;i<3;i++){float value=DisplayDefense(item.hurtbox,kinds[i]);item.fill.sharedMaterial.SetColor(i==0?"_Mid":i==1?"_High":"_Low",levelDisplay.LevelColor(value));item.labels[i].text=(i==0?(item.hurtbox.Fighter==player?"1P":"2P")+(item.hurtbox.Fighter.Hurtboxes.Length>1?"#"+item.hurtbox.StableId:"")+" ":"")+names[i]+" "+value.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture);item.labels[i].color=levelDisplay.TextColor(value);item.labels[i].fontSize=levelDisplay.labelFontSize;}
        var a=cam.WorldToViewportPoint(bounds.center);float x=HudWorldRect.x+a.x*HudWorldRect.width;float y=HudWorldRect.y+(1-a.y)*HudWorldRect.height;
        float halfWidth=width/(2*cam.orthographicSize*cam.aspect)*HudWorldRect.width*.5f;float labelWidth=110;
        bool first=DefenseLabelOnLeft(item.hurtbox.Fighter);float left=first?x-halfWidth-labelWidth-6:x+halfWidth+6;
        left=Mathf.Clamp(left,HudWorldRect.x+4,HudWorldRect.xMax-labelWidth-4);float row=levelDisplay.labelFontSize+5;float top=Mathf.Clamp(y-row*1.5f,HudWorldRect.y+3,Mathf.Max(HudWorldRect.y+3,HudWorldRect.yMax-row*3-3));
        // Multiple regions receive separate label blocks, ordered from highest volume to lowest.
        // This also avoids overlap in a compact training viewport; geometry and defenses are untouched.
        var regions=item.hurtbox.Fighter.Hurtboxes;int count=0,rank=0;
        foreach(var region in regions)if(region!=null&&region.Collider.enabled&&region.gameObject.activeInHierarchy){count++;if(region.Collider.bounds.center.y>bounds.center.y||(Mathf.Approximately(region.Collider.bounds.center.y,bounds.center.y)&&region.StableId<item.hurtbox.StableId))rank++;}
        if(count>1){float block=row*3+4;float total=block*count-4;var actorPoint=cam.WorldToViewportPoint(item.hurtbox.Fighter.GetComponent<TrainingMotor2D>().bounds.center);float center=HudWorldRect.y+(1-actorPoint.y)*HudWorldRect.height;float start=Mathf.Clamp(center-total*.5f,HudWorldRect.y+3,Mathf.Max(HudWorldRect.y+3,HudWorldRect.yMax-total-3));top=start+rank*block;}
        for(int i=0;i<3;i++){item.labels[i].alignment=first?TextAnchor.MiddleRight:TextAnchor.MiddleLeft;var rt=item.labels[i].rectTransform;rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(left,-top-i*row);rt.sizeDelta=new Vector2(labelWidth,row);}
    }

    bool firstLabelsLeft=true;
    public bool DefenseLabelOnLeft(TrainingFighter fighter)
    {
        if(worldCamera!=null&&player!=null&&player.opponent!=null){float a=worldCamera.WorldToViewportPoint(player.transform.position).x*HudWorldRect.width,b=worldCamera.WorldToViewportPoint(player.opponent.transform.position).x*HudWorldRect.width;if(a<b-8)firstLabelsLeft=true;else if(a>b+8)firstLabelsLeft=false;}
        return fighter==player?firstLabelsLeft:!firstLabelsLeft;
    }
    Rect DefenseProjectedRect(Bounds b)
    {
        var cam=worldCamera;var point=cam.WorldToViewportPoint(b.center);var right=cam.transform.right;var up=cam.transform.up;
        float w=2*(Mathf.Abs(right.x)*b.extents.x+Mathf.Abs(right.y)*b.extents.y+Mathf.Abs(right.z)*b.extents.z)/(2*cam.orthographicSize*cam.aspect)*HudWorldRect.width;
        float h=2*(Mathf.Abs(up.x)*b.extents.x+Mathf.Abs(up.y)*b.extents.y+Mathf.Abs(up.z)*b.extents.z)/(2*cam.orthographicSize)*HudWorldRect.height;
        return new Rect(HudWorldRect.x+point.x*HudWorldRect.width-w*.5f,HudWorldRect.y+(1-point.y)*HudWorldRect.height-h*.5f,w,h);
    }
    Rect DefenseBlockRect(HurtboxOutline item){var r=item.labels[0].rectTransform;return new Rect(r.anchoredPosition.x,-r.anchoredPosition.y,r.sizeDelta.x,(levelDisplay.labelFontSize+5)*3);}
    public Rect DefenseLabelRectFor(TrainingHurtbox part){foreach(var item in hurtboxes)if(item.hurtbox==part&&item.labels!=null)return DefenseBlockRect(item);return default;}
    void ResolveDefenseLabelOverlaps()
    {
        if(worldCamera==null||player==null||player.opponent==null)return;
        var bodies=new[]{DefenseProjectedRect(player.GetComponent<TrainingMotor2D>().bounds),DefenseProjectedRect(player.opponent.GetComponent<TrainingMotor2D>().bounds)};
        var occupied=new System.Collections.Generic.List<Rect>();var ordered=new System.Collections.Generic.List<HurtboxOutline>(hurtboxes);
        ordered.Sort((a,b)=>{int actor=(a.hurtbox.Fighter==player?0:1).CompareTo(b.hurtbox.Fighter==player?0:1);return actor!=0?actor:a.hurtbox.StableId.CompareTo(b.hurtbox.StableId);});
        foreach(var item in ordered){if(item.labels==null||!item.labels[0].gameObject.activeInHierarchy)continue;var original=DefenseBlockRect(item);var owner=DefenseProjectedRect(item.hurtbox.Fighter.GetComponent<TrainingMotor2D>().bounds);Rect chosen=original;bool found=false;
            var xs=new[]{original.x,DefenseLabelOnLeft(item.hurtbox.Fighter)?HudWorldRect.x+4:HudWorldRect.xMax-original.width-4};
            foreach(float x in xs){var ys=new System.Collections.Generic.List<float>{original.y,owner.yMin-original.height-6,owner.yMax+6};for(float y=HudWorldRect.y+3;y<=HudWorldRect.yMax-original.height-3;y+=4)ys.Add(y);
                foreach(float y in ys){var candidate=new Rect(Mathf.Clamp(x,HudWorldRect.x+4,HudWorldRect.xMax-original.width-4),Mathf.Clamp(y,HudWorldRect.y+3,HudWorldRect.yMax-original.height-3),original.width,original.height);bool clear=true;foreach(var r in occupied)if(candidate.Overlaps(r))clear=false;foreach(var r in bodies)if(candidate.Overlaps(r))clear=false;if(clear){chosen=candidate;found=true;break;}}if(found)break;}
            if(!found){for(float y=HudWorldRect.y+3;y<=HudWorldRect.yMax-original.height-3;y+=4){var candidate=new Rect(original.x,y,original.width,original.height);bool clear=true;foreach(var r in occupied)if(candidate.Overlaps(r))clear=false;if(clear){chosen=candidate;break;}}}
            chosen.y=Mathf.Round(chosen.y);float row=levelDisplay.labelFontSize+5;for(int i=0;i<3;i++)item.labels[i].rectTransform.anchoredPosition=new Vector2(chosen.x,-chosen.y-row*i);occupied.Add(chosen);
        }
    }
}
