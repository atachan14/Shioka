using System;using UnityEngine;using UnityEditor;
// Development scene entry only. Does not alter input bindings, simulation speed, or saved scenes.
[InitializeOnLoad]public static class ShiokaGameViewFocus {
 static ShiokaGameViewFocus(){EditorApplication.playModeStateChanged+=state=>{if(state!=PlayModeStateChange.EnteredPlayMode)return;string scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;if(scene!="Assets/Scenes/CombatFormulaComparison.unity"&&scene!="Assets/Scenes/ArmoredStaticPoseTrial.unity")return;if(SessionState.GetBool("CanonicalQAPending",false)||SessionState.GetBool("BackstepNineRunning",false))return;EditorApplication.delayCall+=Focus;};}
 [MenuItem("Shioka/Development/Focus Game view")]public static void Focus(){var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");if(type!=null)EditorWindow.GetWindow(type).Focus();}
}
