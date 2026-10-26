using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
[InitializeOnLoad] public static class KeyConfigVisual {
 const string Pending="KeyConfigVisualPending";static KeyConfigVisual(){if(SessionState.GetBool(Pending,false))EditorApplication.update+=Show;}
 [MenuItem("Shioka/Development/Preview KeyConfig")]public static void Open(){if(EditorApplication.isPlaying){ShowNow();return;}if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty){Debug.LogWarning("Save or cancel the current scene before opening preview");return;}EditorSceneManager.OpenScene("Assets/Shioka/Scenes/CombatFormulaComparison.unity");SessionState.SetBool(Pending,true);EditorApplication.update-=Show;EditorApplication.update+=Show;EditorApplication.EnterPlaymode();}
 static void Show(){if(!EditorApplication.isPlaying||Time.frameCount<10)return;SessionState.SetBool(Pending,false);EditorApplication.update-=Show;ShowNow();}
 static void ShowNow(){var hud=UnityEngine.Object.FindAnyObjectByType<TrainingOverlay>();if(hud==null)return;hud.SetOptionVisible(true);hud.ShowInputOptions(true);Debug.Log("KEYCONFIG_VISUAL_READY");}
}
