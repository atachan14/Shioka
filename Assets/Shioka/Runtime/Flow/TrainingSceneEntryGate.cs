using UnityEngine;
[DefaultExecutionOrder(-9000)]public sealed class TrainingSceneEntryGate:MonoBehaviour {
 void Start(){if(!TrainingFlowSession.AwaitingTrainingEntry)return;TrainingFlowSession.AwaitingTrainingEntry=false;foreach(var input in FindObjectsByType<TrainingControls>(FindObjectsSortMode.None))input.SetOptionBlocked(false);}
}
