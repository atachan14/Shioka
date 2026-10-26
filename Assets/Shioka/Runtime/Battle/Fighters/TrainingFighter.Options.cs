using UnityEngine;

public sealed partial class TrainingFighter
{
    public bool OptionInputSuspended{get;private set;}
    public void SetOptionInputSuspended(bool suspended)
    {
        OptionInputSuspended=suspended;
        // Keep dummy reaction episode and all simulation clocks/velocities intact.
        if(inputSource is ITrainingResettableInputSource resettable && !(inputSource is ITrainingReactionInputSource))resettable.Clear();
        InputBuffer.ClearPending();movement=Vector2.zero;dash=previousDash=jumpRequested=attackRequested=false;
        guardHeld=fdHeld=false;guardWindowStart=-1;
    }
    private void TryQueueConfiguredReaction()
    {
        if(TrainingSession!=null&&TrainingSession.SuppressDummyReaction(this))return;
        if(OptionInputSuspended||!(inputSource is ITrainingReactionInputSource source)||!source.TryPeekReaction(out var input))return;
        var previous=movement;movement=input.Movement;
        var command=DirectionalCommand(input.Commands[0]);
        if(!CanAccept(command)){movement=previous;return;}
        InputBuffer.Push(command,LockKind);source.ConfirmReaction();
    }
}
