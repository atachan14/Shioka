using System;using System.IO;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class TrainingComparisonSetup {
 public const string Scene="Assets/Scenes/CombatFormulaComparison.unity",Folder="Assets/CombatPrototype/DeveloperComparison";
 public static void Build(){CanonicalDevelopment.Guard();if(!File.Exists(Scene))throw new FileNotFoundException("Playable scene missing",Scene);EditorSceneManager.OpenScene(Scene);}
 [MenuItem("Shioka/Developer combat/Open 3P comparison")]public static void Open(){if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;Build();}
 public static void Play(){Open();if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path==Scene)EditorApplication.isPlaying=true;}
}
