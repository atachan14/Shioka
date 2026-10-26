using UnityEngine;

[RequireComponent(typeof(TrainingFighter))]
public sealed class TrainingDummy : MonoBehaviour,ITrainingInputSource,ITrainingResettableInputSource,ITrainingReactionInputSource
{
    public enum PostureMode { Stand,Crouch,Jump }
    public PostureMode Posture;
    [Tooltip("Legacy Inspector posture setting; Option keeps this synchronized.")] public bool Crouch;
    [SerializeField] string counterCommand="";
    TrainingDummyCommand parsed;
    int observedHits;
    bool pending;
    public string CounterCommand=>counterCommand;
    public string CounterError{get;private set;}="";
    public int ReactionsIssued{get;private set;}
    public TrainingFighter Fighter=>GetComponent<TrainingFighter>();
    public float health=>Fighter.Health;
    public bool Guarding{get=>Fighter.Guarding;set=>Fighter.Guarding=value;}
    public int HitCount=>Fighter.HitCount;
    public int GuardCount=>Fighter.GuardCount;
    public float LastLevelDifference=>Fighter.LastLevelDifference;
    public float StunRemaining=>Fighter.StunRemaining;
    public string LastResult=>Fighter.LastResult;
    void Awake(){SetCounterCommand(counterCommand);observedHits=Fighter.HitCount;}
    public void SetPosture(PostureMode mode){Posture=mode;Crouch=mode==PostureMode.Crouch;}
    public bool SetCounterCommand(string value){counterCommand=value??"";bool valid=TrainingDummyCommand.TryParse(counterCommand,out parsed,out var error);CounterError=error;if(!valid||parsed==null)pending=false;return valid;}
    public void Clear(){pending=false;observedHits=Fighter.HitCount;}
    public TrainingInputFrame ReadInput()
    {
        var f=Fighter;if(f.HitCount<observedHits){pending=false;observedHits=f.HitCount;}
        if(f.HitCount>observedHits){observedHits=f.HitCount;pending=parsed!=null;}
        var frame=new TrainingInputFrame{Movement=Crouch||Posture==PostureMode.Crouch?Vector2.down:Vector2.zero};
        if(!pending&&Posture==PostureMode.Jump&&f.IsGrounded&&!f.IsDown&&f.StunRemaining<=0&&f.HitstopRemaining<=0&&(f.CurrentPhase==TrainingFighter.Phase.Idle||f.CurrentPhase==TrainingFighter.Phase.Move||f.CurrentPhase==TrainingFighter.Phase.Landing))frame.Commands=new[]{TrainingCommand.Jump};
        return frame;
    }
    public bool TryPeekReaction(out TrainingInputFrame input){input=default;if(!pending||parsed==null)return false;input=parsed.Frame(Fighter);return true;}
    public void ConfirmReaction(){pending=false;ReactionsIssued++;}
}
