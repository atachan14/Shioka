using System;using System.IO;using UnityEngine;
public static class LocalProfileFile {
 public static T Read<T>(string path,out string status)where T:class{try{if(!File.Exists(path)){status="No saved file";return null;}if(new FileInfo(path).Length>4194304)throw new Exception("Too large");var value=JsonUtility.FromJson<T>(File.ReadAllText(path));status=value==null?"Empty data":"Loaded";return value;}catch(Exception e){status="Read fallback: "+e.GetType().Name;return null;}}
 public static bool Write(string path,object data,out string error){error=null;try{Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));string temp=path+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(data,true));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);return true;}catch(Exception e){error="Save failed: "+e.GetType().Name;return false;}}
}
