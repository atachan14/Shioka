using System.IO;using UnityEngine;
public sealed class LocalMatchHistoryStore {
 public readonly string Path;public LocalMatchHistoryStore(string path=null){Path=System.IO.Path.GetFullPath(path??System.IO.Path.Combine(Application.persistentDataPath,"History","Matches-v1.json"));}
 public LocalMatchHistory Load(out string status){var value=LocalProfileFile.Read<LocalMatchHistory>(Path,out status);if(value!=null&&value.Valid)return value;status+="; empty local history (file preserved)";return new LocalMatchHistory();}
 public bool Append(LocalMatchRecord record,out string error){if(record==null||!record.Valid){error="Valid non-training result required";return false;}var history=Load(out _);foreach(var old in history.records)if(old.matchId==record.matchId){error="Duplicate match ID";return false;}history.records.Add(record);if(history.records.Count>1000)history.records.RemoveAt(0);return Save(history,out error);}
 public bool Save(LocalMatchHistory value,out string error){if(value==null||!value.Valid){error="Invalid bounded version1 history";return false;}return LocalProfileFile.Write(Path,value,out error);}
}
