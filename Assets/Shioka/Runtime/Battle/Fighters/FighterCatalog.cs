using UnityEngine;
[CreateAssetMenu(menuName="Shioka/Fighter catalog")]
public sealed class FighterCatalog:ScriptableObject {
 public GameObject[] fighters=new GameObject[0];
 public bool Validate(out string error){error="";if(fighters==null||fighters.Length==0){error="No fighters";return false;}var ids=new System.Collections.Generic.HashSet<string>();foreach(var f in fighters){var c=Character(f);if(f==null||f.GetComponent<TrainingFighter>()==null||f.GetComponent<TrainingMotor2D>()==null||c==null||!c.Valid||string.IsNullOrWhiteSpace(c.characterId)||!ids.Add(c.characterId)){error="Unique stable character IDs and valid fighter prefabs required";return false;}}return true;}
 public static TrainingApprovedCharacter Character(GameObject prefab)=>prefab==null?null:prefab.GetComponent<ArmoredStaticPoseView>()?.characterProfile;
 public GameObject Find(string id){if(fighters!=null)foreach(var f in fighters)if(Character(f)?.characterId==id)return f;return null;}
 public string FirstId=>fighters!=null&&fighters.Length>0?Character(fighters[0])?.characterId:null;
}
