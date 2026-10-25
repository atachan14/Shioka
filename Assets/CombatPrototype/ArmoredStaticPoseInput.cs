using UnityEngine;using System.Collections.Generic;
// Only this separate scene maps grounded StrongKick to the existing jump -> airborne StrongKick.
public sealed class ArmoredStaticPoseInput:MonoBehaviour,ITrainingInputSource,ITrainingResettableInputSource,ITrainingPendingChordSource{
 public TrainingFighter fighter;public MonoBehaviour sourceBehaviour;bool pending;int pendingTicks;
 public TrainingInputFrame ReadInput(){var actor=fighter.TrainingSession==null?fighter:fighter.TrainingSession.InputTargetFor(fighter);var source=sourceBehaviour as ITrainingInputSource;var frame=source!=null?source.ReadInput():default;
  if(frame.Reset){pending=false;pendingTicks=0;return frame;}var commands=new List<TrainingCommand>();if(frame.Commands!=null)foreach(var command in frame.Commands){if(command==TrainingCommand.StrongKick&&actor.IsGrounded&&frame.Movement.y>-.5f&&(actor.CurrentPhase==TrainingFighter.Phase.Idle||actor.CurrentPhase==TrainingFighter.Phase.Move)){commands.Add(TrainingCommand.Jump);pending=true;pendingTicks=0;}else commands.Add(command);}
  if(pending){pendingTicks++;if(actor.StunRemaining>0||actor.IsDown||pendingTicks>80){pending=false;}else if(actor.CurrentPhase==TrainingFighter.Phase.Air){commands.Add(TrainingCommand.AirStrongKick);pending=false;}}
  frame.Commands=commands.ToArray();return frame;}
 public void Clear(){pending=false;pendingTicks=0;(sourceBehaviour as ITrainingResettableInputSource)?.Clear();}
 public void ClearPendingChordInput(){pending=false;(sourceBehaviour as ITrainingPendingChordSource)?.ClearPendingChordInput();}
}
