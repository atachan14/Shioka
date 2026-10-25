using System;using System.IO;using UnityEngine;

// Frame coordinates are half-open [start,end), with input sampled at frame 0.
[Serializable]public sealed class ApprovedActionTimeline{
 public int inputFrame,actionStartFrame,invulnerabilityStartFrame,invulnerabilityEndFrame,actionEndFrame;
 public bool invulnerabilityEnabled;
 public bool Validate(out string error){error=null;if(inputFrame<0||actionStartFrame<inputFrame||actionEndFrame<=actionStartFrame){error="Invalid input/start/end ordering";return false;}if(invulnerabilityStartFrame<actionStartFrame||invulnerabilityEndFrame<invulnerabilityStartFrame||invulnerabilityEndFrame>actionEndFrame){error="Reserved invulnerability interval must lie inside action";return false;}if(invulnerabilityEnabled){error="Invulnerability is not implemented or enabled in this trial";return false;}return true;}
}
[Serializable]public sealed class ApprovedLocomotionSettings{
 public int version=1,backstepFrames=13,pose1EndFrame=4,pose2EndFrame=9,jumpAirFrames=54;
 public float backstepDistance=1.08f,jumpHeight=1.8f;
 public ApprovedActionTimeline Timeline=>new ApprovedActionTimeline{inputFrame=0,actionStartFrame=0,actionEndFrame=backstepFrames};
 public ApprovedLocomotionSettings Copy()=>JsonUtility.FromJson<ApprovedLocomotionSettings>(JsonUtility.ToJson(this));
 public bool Validate(out string error){
  error=null;if(version!=1)error="Unsupported settings version";
  else if(backstepFrames<6||backstepFrames>90)error="Backstep: 6..90 simulation frames";
  else if(!Finite(backstepDistance)||backstepDistance<.1f||backstepDistance>5)error="Distance: 0.1..5 world units";
  else if(pose1EndFrame<1||pose2EndFrame<=pose1EndFrame||pose2EndFrame>=backstepFrames)error="Pose boundaries must satisfy 0 < end1 < end2 < total (end3)";
  else if(jumpAirFrames<24||jumpAirFrames>180)error="Jump airtime: 24..180 simulation frames";
  else if(!Finite(jumpHeight)||jumpHeight<.2f||jumpHeight>4)error="Jump height: 0.2..4 world units";
  return error==null&&Timeline.Validate(out error);
 }
 static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
}
[DefaultExecutionOrder(-1200)]public sealed class ApprovedLocomotionTuning:MonoBehaviour{
 public const int SimulationHz=60;public const float SimulationStep=1f/SimulationHz;
 public ApprovedLocomotionProfile canonicalProfile;
 [Tooltip("Developer tools only; always disabled in a non-development player build.")]public bool enableDeveloperTuning=true;
 [Tooltip("Development only. Off means every Play starts from the canonical SO; Load local resumes a draft.")]public bool loadSavedDraftOnStart;
 public static bool AllowDeveloperTools(bool editor,bool developmentBuild,bool configured)=>configured&&(editor||developmentBuild);
 public bool DeveloperTuningEnabled=>AllowDeveloperTools(Application.isEditor,Debug.isDebugBuild,enableDeveloperTuning);
 public TrainingFighter Fighter{get;private set;}public ApprovedLocomotionSettings Requested{get;private set;}public ApprovedLocomotionSettings Applied{get;private set;}
 public string DraftSource{get;private set;}public string Status{get;private set;}public bool Pending=>JsonUtility.ToJson(Requested)!=JsonUtility.ToJson(Applied);
 public event Action Changed;TrainingCharacterDefinition original,runtime;float priorStep;
 public string SettingsPath=>Path.Combine(Application.persistentDataPath,"ShiokaApprovedLocomotion-v1.json");
 public bool TryReadCanonical(out ApprovedLocomotionSettings value,out string error){if(canonicalProfile!=null)return canonicalProfile.TryRead(out value,out error);value=null;error="No canonical motion SO assigned";return false;}
 void Awake(){Fighter=GetComponent<TrainingFighter>();original=Fighter.definition;runtime=Instantiate(original);runtime.name=original.name+" (motion runtime copy)";runtime.hideFlags=HideFlags.DontSave;Fighter.definition=runtime;priorStep=Time.fixedDeltaTime;Time.fixedDeltaTime=SimulationStep;bool valid=TryReadCanonical(out var defaults,out var error);Requested=valid?defaults:new ApprovedLocomotionSettings();Commit();DraftSource=valid?"Canonical SO: "+canonicalProfile.name:"Compatibility fallback";if(!valid)Status=error+"; using safe fallback (build validation requires a valid SO)";if(DeveloperTuningEnabled&&loadSavedDraftOnStart&&File.Exists(SettingsPath)&&!Load(out error))Status=error;}
 void Update(){if(Pending&&CanCommit())Commit();}
 bool CanCommit()=>Fighter!=null&&Fighter.IsGrounded&&(Fighter.CurrentPhase==TrainingFighter.Phase.Idle||Fighter.CurrentPhase==TrainingFighter.Phase.Move||Fighter.CurrentPhase==TrainingFighter.Phase.Crouch);
 public bool Apply(ApprovedLocomotionSettings value,out string error){if(!DeveloperTuningEnabled){error="Runtime draft editing is development-only";return false;}if(value==null){error="No settings";return false;}if(!value.Validate(out error))return false;Requested=value.Copy();DraftSource="Unsaved runtime draft";if(CanCommit())Commit();else Status="Preview updated; motor changes at next grounded neutral state";Changed?.Invoke();return true;}
 void Commit(){Applied=Requested.Copy();runtime.backstepSeconds=Applied.backstepFrames*SimulationStep-.000001f;runtime.backstepSpeed=Applied.backstepDistance/(Applied.backstepFrames*SimulationStep);float t=Applied.jumpAirFrames*SimulationStep;runtime.gravity=-8*Applied.jumpHeight/(t*t);runtime.jumpVelocity=4*Applied.jumpHeight/t-.5f*runtime.gravity*SimulationStep;Status="Applied to runtime character copy";}
 public void ResetDefaults(){if(!TryReadCanonical(out var defaults,out var error)){Status="Reset refused: "+error;return;}Requested=defaults;DraftSource="Canonical SO: "+canonicalProfile.name;if(CanCommit())Commit();Status=Pending?"SO defaults queued for neutral":"SO defaults restored; JSON unchanged";Changed?.Invoke();}
 public bool Save(out string error){error=null;if(!DeveloperTuningEnabled){error="Local draft Save is development-only";return false;}if(!Requested.Validate(out error))return false;try{Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));File.WriteAllText(SettingsPath,JsonUtility.ToJson(Requested,true));Status="Saved developer JSON draft; canonical SO unchanged";Changed?.Invoke();return true;}catch(Exception e){error="Save failed: "+e.Message;return false;}}
 public bool Load(out string error){error=null;if(!DeveloperTuningEnabled){error="Local draft Load is development-only";return false;}try{if(!File.Exists(SettingsPath)){error="No saved local draft";return false;}var next=JsonUtility.FromJson<ApprovedLocomotionSettings>(File.ReadAllText(SettingsPath));if(!Apply(next,out error))return false;DraftSource="Saved JSON override";Status=Pending?"Loaded draft; motor applies at neutral":"Loaded developer draft; canonical SO unchanged";Changed?.Invoke();return true;}catch(Exception e){error="Load failed: "+e.Message;return false;}}
 public bool Load()=>Load(out _);
 public int PoseIndex(float elapsedSeconds){var s=Applied;float f=elapsedSeconds*SimulationHz+.0001f;return f<s.pose1EndFrame?0:f<s.pose2EndFrame?1:2;}
 void OnDestroy(){if(Fighter!=null&&original!=null)Fighter.definition=original;if(runtime!=null){Destroy(runtime);Time.fixedDeltaTime=priorStep;}}
}
