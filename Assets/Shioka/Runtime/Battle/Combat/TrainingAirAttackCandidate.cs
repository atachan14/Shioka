using UnityEngine;

// Measured pose data only; no input binding, movement rule or damaging attack.
[CreateAssetMenu(menuName = "Shioka/Air Attack Candidate")]
public sealed class TrainingAirAttackCandidate : ScriptableObject
{
    public TrainingMoveSpatialProfile spatialProfile;
    public AnimationClip clip;
    [Min(0f)] public float standaloneDurationSeconds;
    public float DurationSeconds => clip != null ? clip.length : standaloneDurationSeconds;
    public Vector3 poseOffset;
    public float sourceStartSeconds, sourceEndSeconds, sourceContactSeconds;
    [Range(0f, 1f)] public float contactNormalized;
    [Range(0f, 1f)] public float extendedPoseStartNormalized, extendedPoseEndNormalized;
    public Vector3 measuredContactCenter;
    public Vector3 proposedHalfSize;
    public bool connectedToCombat;
    // Trial timings bracket the measured 0.375 contact pose; not balance decisions.
    public float ProposedStartupSeconds => SampleTime(0.25f);
    public float ProposedActiveSeconds => SampleTime(0.25f);
    public float ProposedRecoverySeconds => SampleTime(0.5f);
    public Vector3 ContactCenterFor(Transform physicsRoot, int facing)
    {
        return TrainingCoordinates2D.World(physicsRoot.position,measuredContactCenter,facing);
    }
    public Vector3 PoseOffsetFor(Quaternion visualRotation, bool mirrored)
    {
        var offset = poseOffset;
        if (mirrored) offset.x = -offset.x;
        return visualRotation * offset;
    }
    public float SampleTime(float normalized) => Mathf.Clamp01(normalized) * DurationSeconds;
    public bool IsExtendedPose(float normalized) => normalized >= extendedPoseStartNormalized && normalized <= extendedPoseEndNormalized;
}
