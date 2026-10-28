using System.Collections;using UnityEngine;
[DefaultExecutionOrder(-9000)]public sealed class TrainingSceneEntryGate:MonoBehaviour {
 IEnumerator Start(){
  bool entering=TrainingFlowSession.AwaitingTrainingEntry,options=TrainingFlowSession.OpenOptionsFromTitle;
  TrainingFlowSession.AwaitingTrainingEntry=false;TrainingFlowSession.OpenOptionsFromTitle=false;
  if(!entering&&!options)yield break;
  foreach(var input in FindObjectsByType<TrainingControls>(FindObjectsSortMode.None))input.SetOptionBlocked(false);
  // Options reuse the existing product UI; pause before the first combat tick, never instantiate a second battle.
  if(options)Time.timeScale=0;
  yield return null;
  var overlay=FindAnyObjectByType<TrainingOverlay>();
  if(overlay==null){Debug.LogError("Training entry requires the existing overlay");Time.timeScale=1;yield break;}
  if(entering){Validate(overlay.player,TrainingFlowSession.PlayerFighterId,"Player");Validate(overlay.dummy.Fighter,TrainingFlowSession.CpuFighterId,"CPU");}
  if(options){overlay.ReturnToTitleOnOptionClose=true;Time.timeScale=1;overlay.SetOptionVisible(true);overlay.ShowInputOptions(true);FindAnyObjectByType<TrainingOptionNavigation>()?.SelectByName("NavInputs");}
 }
 static void Validate(TrainingFighter fighter,string id,string role){if(!string.IsNullOrEmpty(id)&&fighter.GetComponent<ArmoredStaticPoseView>()?.characterProfile?.characterId!=id)Debug.LogError(role+" selected Fighter does not match the installed Training actor");}
}
