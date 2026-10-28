using UnityEngine;
public sealed class PlayerPreferencesStore {
 public readonly string Path;
 // The filename stays stable: v1 is migrated in memory and only rewritten on explicit confirmation.
 public PlayerPreferencesStore(string path=null){Path=System.IO.Path.GetFullPath(path??System.IO.Path.Combine(Application.persistentDataPath,"Profiles","PlayerPreferences-v1.json"));}
 public PlayerPreferences Load(string defaultFighterId,out string status){
  var value=LocalProfileFile.Read<PlayerPreferences>(Path,out status);
  if(value!=null&&value.Valid)return value.Copy(defaultFighterId);
  status+="; default selection (file preserved)";
  return new PlayerPreferences{selectedFighterId=defaultFighterId,cpuFighterId=defaultFighterId};
 }
 public bool Save(PlayerPreferences value,out string error){
  if(value==null||!value.Valid){error="Save failed: supported version and stable fighter IDs required";return false;}
  return LocalProfileFile.Write(Path,value.Copy(),out error);
 }
}
