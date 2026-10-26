using UnityEngine;
[DefaultExecutionOrder(500)]public sealed class CombatV01ContactBatch:MonoBehaviour {public CombatTestV01 owner;void FixedUpdate(){owner.ResolveBatch();}}
