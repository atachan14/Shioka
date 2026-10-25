using System;
using UnityEngine;

public interface ITrainingReactionInputSource
{
    bool TryPeekReaction(out TrainingInputFrame input);
    void ConfirmReaction();
}
public sealed class TrainingDummyCommand
{
    public TrainingCommand Command;
    public int Direction=5;
    public static bool TryParse(string text,out TrainingDummyCommand result,out string error)
    {
        result=null;error="";
        string s=(text??"").Normalize(System.Text.NormalizationForm.FormKC).Trim().ToUpperInvariant().Replace(" ","");
        if(s.Length==0)return true;
        int direction=5;if(s[0]>='1'&&s[0]<='9'){direction=s[0]-'0';s=s.Substring(1).TrimStart('+');}
        bool legacySweep=direction==3&&(s=="強"||s=="HEAVY"||s=="L");
        TrainingCommand command;
        switch(s){
            case "弱P":case "LP":case "弱":case "J":case "LIGHT":command=TrainingCommand.Light;break;
            case "中P":case "MP":case "中":case "K":case "MEDIUM":command=TrainingCommand.Medium;break;
            case "強P":case "HP":case "強":case "L":case "HEAVY":command=TrainingCommand.Heavy;break;
            case "弱K":case "LK":case "S":case "M":case "SLASH":command=TrainingCommand.Slash;break;
            case "中K":case "MK":case "HS":case ",":case "HEAVYSLASH":command=TrainingCommand.HeavySlash;break;
            case "強K":case "HK":case ".":command=TrainingCommand.StrongKick;break;
            case "SPECIAL2":case "特殊2":command=TrainingCommand.Special2;break;
            case "投げ":case "I":case "THROW":command=TrainingCommand.Throw;break;
            case "SPECIAL1":case "特殊1":case "特殊":case "O":case "SPECIAL":command=TrainingCommand.Special;break;
            case "足払い":case "SWEEP":command=TrainingCommand.StrongKick;direction=3;break;
            case "ジャンプ":case "JUMP":command=TrainingCommand.Jump;break;
            default:error="不正コマンド。例: 弱 / 中 / 強 / 3強 / S / HS / 投げ / 特殊";return false;
        }
        if(legacySweep){var config=TrainingInputConfig.Runtime(Resources.Load<TrainingInputConfig>("TrainingInputConfig"));command=config!=null&&config.layoutVersion>=2?TrainingCommand.StrongKick:TrainingCommand.Heavy;}
        if(direction<=3){if(command==TrainingCommand.Slash)command=TrainingCommand.CrouchWeakKick;else if(command==TrainingCommand.HeavySlash)command=TrainingCommand.CrouchMediumKick;else if(command==TrainingCommand.StrongKick&&direction!=3)command=TrainingCommand.CrouchStrongKick;else if(!(direction==3&&(command==TrainingCommand.StrongKick||legacySweep&&command==TrainingCommand.Heavy))){error="しゃがみ攻撃は弱K・中K・強K、3強Kは足払いです。";return false;}}
        if(direction>=7&&command!=TrainingCommand.Jump){error="上方向との組合せはジャンプのみです。空中技は弱K・中Kを指定します。";return false;}
        result=new TrainingDummyCommand{Command=command,Direction=direction};return true;
    }
    public TrainingInputFrame Frame(TrainingFighter fighter)
    {
        int facing=fighter.opponent==null?fighter.Facing:fighter.opponent.transform.position.x>=fighter.transform.position.x?1:-1;
        float x=Direction==1||Direction==4||Direction==7?-1:Direction==3||Direction==6||Direction==9?1:0;
        float y=Direction<=3?-1:Direction>=7?1:0;
        return new TrainingInputFrame{Movement=new Vector2(x*facing,y),Commands=new[]{Command}};
    }
}
