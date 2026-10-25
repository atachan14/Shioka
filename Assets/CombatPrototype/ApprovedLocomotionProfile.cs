using UnityEngine;

[CreateAssetMenu(menuName="Shioka/Motion defaults",fileName="ApprovedLocomotionProfile")]
public sealed class ApprovedLocomotionProfile:ScriptableObject{
 [SerializeField] int simulationHz=60;
 [SerializeField] ApprovedLocomotionSettings defaults=new ApprovedLocomotionSettings();
 public int SimulationHz=>simulationHz;
 public bool TryRead(out ApprovedLocomotionSettings value,out string error){value=null;if(simulationHz!=60){error="This motion runtime requires a 60Hz profile";return false;}if(defaults==null){error="Missing motion defaults";return false;}if(!defaults.Validate(out error))return false;value=defaults.Copy();return true;}
 // Changes memory only. Persistent adoption belongs to the explicit Editor action.
 public bool ReplaceValidated(ApprovedLocomotionSettings value,out string error){if(value==null){error="No motion values";return false;}if(!value.Validate(out error))return false;simulationHz=60;defaults=value.Copy();return true;}
}
