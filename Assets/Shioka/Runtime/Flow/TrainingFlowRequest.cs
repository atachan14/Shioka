using UnityEngine;
public readonly struct TrainingFlowRequest {public readonly string fighterId;public readonly GameObject prefab;public TrainingFlowRequest(string id,GameObject source){fighterId=id;prefab=source;}}
public static class TrainingFlow {
 public static bool TryPrepare(FighterCatalog catalog,PlayerPreferences preferences,out TrainingFlowRequest request,out string status){request=default;status=string.Empty;if(catalog==null||!catalog.Validate(out status))return false;var id=preferences!=null&&preferences.Valid?preferences.selectedFighterId:catalog.FirstId;var prefab=catalog.Find(id);status="Selected fighter resolved";if(prefab==null){id=catalog.FirstId;prefab=catalog.Find(id);status="Unavailable fighter: using catalog default without overwriting preference";}request=new TrainingFlowRequest(id,prefab);return true;}
}
