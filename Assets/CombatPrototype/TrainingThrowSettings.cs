using UnityEngine;

[CreateAssetMenu(menuName="Shioka/Training Throw Settings")]
public sealed class TrainingThrowSettings:ScriptableObject
{
    public TrainingMoveSpatialProfile spatialProfile;
    public float startupFrames=6,activeFrames=3,recoveryFrames=21,holdFrames=24;
    [HideInInspector] public float range=.85f,planeTolerance=.35f,heightTolerance=.12f; // retained serialized legacy reference values
    public TrainingThrowTarget targets=TrainingThrowTarget.Ground;
    public Vector3 boxCenter=new Vector3(0,.8f,.425f),boxSize=new Vector3(.1f,.12f,.25f);
    public float attackLevel=110,damage=16,hitGain=8;
    public float AttemptSeconds=>(startupFrames+activeFrames+recoveryFrames)/60f;
    public float HoldSeconds=>Mathf.Max(1,holdFrames)/60f;
}
