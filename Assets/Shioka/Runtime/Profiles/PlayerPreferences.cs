using System;
[Serializable]
public sealed class PlayerPreferences {
 public int version=2;
 // Keep this serialized name for compatibility with the original Player-only save.
 public string selectedFighterId;
 public string cpuFighterId;
 public bool Valid => (version==1||version==2)&&IdValid(selectedFighterId)&&(string.IsNullOrEmpty(cpuFighterId)||IdValid(cpuFighterId));
 static bool IdValid(string id)=>!string.IsNullOrWhiteSpace(id)&&id.Length<=128;
 public PlayerPreferences Copy(string defaultCpu=null)=>new PlayerPreferences{selectedFighterId=selectedFighterId,cpuFighterId=string.IsNullOrEmpty(cpuFighterId)?defaultCpu??selectedFighterId:cpuFighterId};
}
