using UnityEngine;
[CreateAssetMenu(menuName="Shioka/Skill data")]
public sealed class TrainingMoveData:ScriptableObject {public TrainingMove data=new TrainingMove();}
[System.Serializable]public sealed class TrainingMoveReference {public string role;public TrainingMoveData asset;}
