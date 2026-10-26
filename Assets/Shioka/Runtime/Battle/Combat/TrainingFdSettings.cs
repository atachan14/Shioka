using UnityEngine;

[CreateAssetMenu(menuName = "Shioka/Training FD Settings")]
public sealed class TrainingFdSettings : ScriptableObject
{
    [Min(0f)] public float costPerAttackLevel = .15f;
    [Min(0f)] public float pushDistance = .5f;
}
