using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class CanonicalDevelopment {
 public static void Guard() { if(!System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath,"Shioka/Scenes/CombatFormulaComparison.unity"))) throw new Exception("Open the Shioka Unity project containing the playable scene"); }
 [MenuItem("Shioka/Development/Open adopted play scene")]
 public static void Open(){ Guard(); if(EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return; EditorSceneManager.OpenScene("Assets/Shioka/Scenes/CombatFormulaComparison.unity"); }
 public static void Play(){ Open(); if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path=="Assets/Shioka/Scenes/CombatFormulaComparison.unity") EditorApplication.isPlaying=true; }
 public static void VerifyImport(){ Guard(); var p=AssetDatabase.LoadAssetAtPath<TrainingAirDashProfile>("Assets/Shioka/調整Data/Fighter/Awari/Move/AirDashProfile.asset"); if(p==null || !p.Valid)throw new Exception("Air dash profile missing or invalid"); Debug.Log("CANONICAL_IMPORT_COMPILE_PASS air dash="+p.Speed); }
}
