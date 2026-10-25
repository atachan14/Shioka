using UnityEngine;

[DefaultExecutionOrder(1000)]
public sealed class TrainingFrameBar:MonoBehaviour
{
    public readonly TrainingFrameHistory First=new TrainingFrameHistory(),Second=new TrainingFrameHistory();
    TrainingFighter first,second;Texture2D texture;[Range(30,120)]public int visibleFrames=60;Color32[] pixels;int drawnFirst=-1,drawnSecond=-1;
    public float EstimatedAdvantage=>first==null||second==null?0:second.StunRemaining*60-first.RemainingLockFrames;
    public void Configure(TrainingFighter p,TrainingFighter o){first=p;second=o;}
    public static TrainingFrameSnapshot Snapshot(TrainingFighter f)
    {
        float attack=f.CurrentAttackClass==TrainingAttackClass.Throw&&f.moveSet!=null&&f.moveSet.throwSettings!=null?f.moveSet.throwSettings.attackLevel:f.attackLevel;
        return new TrainingFrameSnapshot(f.InputBuffer.Frame,f.CurrentPhase,f.LockKind,f.HitstopRemaining*60,f.RemainingLockFrames,attack*(f.CombatV01==null?f.Tension.Bonus:1),f.DefenseFor(f.opponent==null?TrainingAttackClass.Mid:f.opponent.CurrentAttackClass),f.LastLevelDifference);
    }
    bool firstActive=true,secondActive=true;
    void FixedUpdate(){Capture(first,First,ref firstActive);Capture(second,Second,ref secondActive);}
    static void Capture(TrainingFighter fighter,TrainingFrameHistory row,ref bool wasActive){bool active=fighter!=null&&fighter.isActiveAndEnabled;if(!active&&wasActive)row.Suspend(fighter==null?0:fighter.InputBuffer.ResetVersion,fighter==null?0:fighter.CancelCount,fighter==null?0:fighter.FdGuardCount);wasActive=active;if(active)row.Capture(Snapshot(fighter),fighter.InputBuffer.ResetVersion,fighter.CancelCount,fighter.FdGuardCount);}
    public Texture2D GetTexture()
    {
        int span=Mathf.Clamp(visibleFrames,30,TrainingFrameHistory.Capacity);if(texture!=null&&texture.width!=span){Destroy(texture);texture=null;}
        if(texture==null){pixels=new Color32[span*2];drawnFirst=drawnSecond=-1;texture=new Texture2D(span,2,TextureFormat.RGBA32,false);texture.name="Training frame history";texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;}
        if(drawnFirst!=First.Version||drawnSecond!=Second.Version){for(int row=0;row<2;row++){var history=row==0?First:Second;for(int x=0;x<span;x++){int age=span-1-x;pixels[(1-row)*span+x]=age<history.Count?ColorFor(history.Newest(age)):new Color(.06f,.075f,.1f,1);}}texture.SetPixels32(pixels);texture.Apply(false,false);drawnFirst=First.Version;drawnSecond=Second.Version;}
        return texture;
    }
    public static Color ColorFor(TrainingFrameSnapshot s)
    {
        if(s.TdcEvents>0)return Color.white;if(s.FdEvents>0)return new Color(.1f,.9f,.65f);
        Color c=new Color(.28f,.32f,.38f);
        switch(s.Lock){case TrainingLockKind.Startup:case TrainingLockKind.ThrowStartup:c=Color.yellow;break;case TrainingLockKind.Active:case TrainingLockKind.ThrowActive:c=new Color(1,.2f,.1f);break;case TrainingLockKind.Recovery:case TrainingLockKind.ThrowRecovery:c=new Color(1,.55f,.12f);break;case TrainingLockKind.Blockstun:c=new Color(.3f,.6f,1);break;case TrainingLockKind.GuardStun:c=Color.cyan;break;case TrainingLockKind.Hitstun:case TrainingLockKind.Knockdown:case TrainingLockKind.Down:case TrainingLockKind.GetUp:case TrainingLockKind.ThrowHold:case TrainingLockKind.Thrown:c=new Color(.7f,.3f,.95f);break;default:if(s.Phase==TrainingFighter.Phase.Move)c=new Color(.3f,.7f,.35f);else if(s.Phase==TrainingFighter.Phase.Air)c=new Color(.3f,.55f,.95f);break;}
        return s.Stop>0?Color.Lerp(c,Color.gray,.5f):c;
    }
    void OnDestroy(){if(texture!=null)Destroy(texture);}
}
