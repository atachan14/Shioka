using UnityEngine;
[DefaultExecutionOrder(1800)]public sealed class TrainingSessionAfterTick:MonoBehaviour {public TrainingSession owner;void FixedUpdate(){if(owner!=null)owner.AfterTick();}}
