using UnityEngine;
public static class TrainingFlowSession {
 public const string TitleScene="Assets/Shioka/Scenes/Title.unity",TrainingScene="Assets/Shioka/Scenes/CombatFormulaComparison.unity";
 public static bool AwaitingTrainingEntry,OpenOptionsFromTitle;
 public static string PlayerFighterId,CpuFighterId;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){AwaitingTrainingEntry=OpenOptionsFromTitle=false;PlayerFighterId=CpuFighterId=null;}
}
