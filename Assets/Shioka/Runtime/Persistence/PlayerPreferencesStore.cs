using System.IO;using UnityEngine;
public sealed class PlayerPreferencesStore {
 public readonly string Path;public PlayerPreferencesStore(string path=null){Path=System.IO.Path.GetFullPath(path??System.IO.Path.Combine(Application.persistentDataPath,"Profiles","PlayerPreferences-v1.json"));}
 public PlayerPreferences Load(string defaultFighterId,out string status){var value=LocalProfileFile.Read<PlayerPreferences>(Path,out status);if(value!=null&&value.Valid)return value;status+="; default selection (file preserved)";return new PlayerPreferences{selectedFighterId=defaultFighterId};}
 public bool Save(PlayerPreferences value,out string error){if(value==null||!value.Valid){error="Version1 and stable fighter ID required";return false;}return LocalProfileFile.Write(Path,value,out error);}
}
