using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class TrainingFighter
{
    // Optional sample-character presentation only. No controller, root or combat state changes.
    bool focusedMediumPlantPending;
    Animator focusedPlantAnimator;
    SkinnedMeshRenderer focusedFootSkin;
    Transform[] focusedSkinBones;
    Matrix4x4[] focusedBindPoses, focusedSkinMatrices;
    Vector3[] focusedSkinVertices;
    BoneWeight[] focusedSkinWeights;
    int[] focusedLeftVertices, focusedRightVertices;
    Vector3 focusedLeftPole, focusedRightPole;
    Quaternion focusedFlatLeft, focusedFlatRight;
    bool focusedFlatReady;
    bool focusedFootWarning;

    void ResetFocusedMediumKickPlant()
    {
        focusedMediumPlantPending = false;
        focusedLeftPole = focusedRightPole = Vector3.zero;
        focusedFlatReady = false;
    }

    void ApplyFocusedMediumKickGrounding(string state)
    {
        if (visual == null || !grounded || sampleMotions == null || !sampleMotions.enabled ||
            sampleMotions.character != definition || !sampleMotions.mediumKickGroundFeet)
        { ResetFocusedMediumKickPlant(); return; }

        bool active = IsCrouchKickAttack && currentMove.command == TrainingCommand.CrouchMediumKick;
        Quaternion inverseVisual = Quaternion.Inverse(visual.transform.rotation);
        if (state == "CrouchIdle" || (active && !focusedFlatReady))
        {
            focusedFlatLeft = inverseVisual * visual.GetBoneTransform(HumanBodyBones.LeftFoot).rotation;
            focusedFlatRight = inverseVisual * visual.GetBoneTransform(HumanBodyBones.RightFoot).rotation;
            focusedFlatReady = true;
        }
        if (active)
        {
            focusedMediumPlantPending = true;
            focusedFlatLeft = inverseVisual * visual.GetBoneTransform(HumanBodyBones.LeftFoot).rotation;
            if (timer >= attackStartup + attackActive + attackRecovery - .05f)
                focusedFlatRight = inverseVisual * visual.GetBoneTransform(HumanBodyBones.RightFoot).rotation;
        }
        else if (!focusedMediumPlantPending) return;
        else if ((CurrentPhase != Phase.Idle && CurrentPhase != Phase.Crouch) ||
            (state != "Idle" && state != "CrouchIdle" && state != "CrouchStart" && state != "CrouchEnd"))
        { ResetFocusedMediumKickPlant(); return; }

        if (!PrepareFocusedFootSkin() || !FocusedGroundHeight(out float floor)) return;
        if (!active && focusedFlatReady)
        {
            // Keep the move's resting sole orientation instead of importing the idle toe roll.
            visual.GetBoneTransform(HumanBodyBones.LeftFoot).rotation = visual.transform.rotation * focusedFlatLeft;
            visual.GetBoneTransform(HumanBodyBones.RightFoot).rotation = visual.transform.rotation * focusedFlatRight;
        }
        float plane = floor + Mathf.Max(0f, sampleMotions.mediumKickSoleClearance);
        float landing = active ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(
            attackStartup + attackActive + attackRecovery - .1f,
            attackStartup + attackActive + attackRecovery, timer)) : 1f;
        // Skin mixes ankle/lower-leg weights, so a second solve removes the small residual.
        for (int pass = 0; pass < 2; pass++)
        {
            UpdateFocusedSkinMatrices();
            if (!active)
            {
                float drop = Mathf.Max(FocusedReachDrop(true, sampleMotions.mediumKickSupportFootXZ, plane),
                    FocusedReachDrop(false, sampleMotions.mediumKickRestFootXZ, plane));
                if (drop > 0f)
                {
                    // Only the post-move stance: remove the idle's small unreachable leg extension.
                    visual.GetBoneTransform(HumanBodyBones.Hips).position -= Vector3.up * Mathf.Min(drop, .03f);
                    UpdateFocusedSkinMatrices();
                }
            }
            PlantFocusedFoot(true, sampleMotions.mediumKickSupportFootXZ, plane);
            if (!active || landing > 0f)
            {
                UpdateFocusedSkinMatrices();
                PlantFocusedFoot(false, sampleMotions.mediumKickRestFootXZ, plane, landing);
            }
        }
    }

    bool FocusedGroundHeight(out float height)
    {
        height = float.NegativeInfinity;
        int count = Physics.RaycastNonAlloc(transform.position + Vector3.up * .2f, Vector3.down,
            floorHits, 1f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var hit = floorHits[i];
            if (hit.collider == null || hit.normal.y < .65f || hit.collider is CharacterController ||
                hit.collider.GetComponentInParent<TrainingFighter>() != null) continue;
            height = Mathf.Max(height, hit.point.y);
        }
        return !float.IsNegativeInfinity(height);
    }

    bool PrepareFocusedFootSkin()
    {
        if (focusedPlantAnimator == visual && focusedFootSkin != null) return true;
        focusedPlantAnimator = visual;
        focusedFootSkin = null;
        var left = visual.GetBoneTransform(HumanBodyBones.LeftFoot);
        var right = visual.GetBoneTransform(HumanBodyBones.RightFoot);
        if (left == null || right == null) return false;
        int largest = 0;
        foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mesh = renderer.sharedMesh;
            if (mesh == null || mesh.blendShapeCount != 0) continue;
            var bones = renderer.bones;
            var weights = mesh.boneWeights;
            if (weights.Length != mesh.vertexCount || mesh.bindposes.Length != bones.Length) continue;
            var leftIndices = new List<int>();
            var rightIndices = new List<int>();
            for (int v = 0; v < weights.Length; v++)
            {
                float l = FocusedFootWeight(weights[v], bones, left);
                float r = FocusedFootWeight(weights[v], bones, right);
                if (l > .5f) leftIndices.Add(v);
                if (r > .5f) rightIndices.Add(v);
            }
            int size = leftIndices.Count + rightIndices.Count;
            if (leftIndices.Count < 8 || rightIndices.Count < 8 || size <= largest) continue;
            largest = size;
            focusedFootSkin = renderer;
            focusedSkinBones = bones;
            focusedSkinWeights = weights;
            focusedSkinVertices = mesh.vertices;
            focusedBindPoses = mesh.bindposes;
            focusedSkinMatrices = new Matrix4x4[bones.Length];
            focusedLeftVertices = leftIndices.ToArray();
            focusedRightVertices = rightIndices.ToArray();
        }
        if (focusedFootSkin == null && !focusedFootWarning)
        {
            focusedFootWarning = true;
            Debug.LogWarning("Focused medium kick planting needs the sample's unblended skinned foot vertices.", this);
        }
        return focusedFootSkin != null;
    }

    static float FocusedFootWeight(BoneWeight w, Transform[] bones, Transform foot)
    {
        return FocusedBoneWeight(w.boneIndex0, w.weight0, bones, foot) +
            FocusedBoneWeight(w.boneIndex1, w.weight1, bones, foot) +
            FocusedBoneWeight(w.boneIndex2, w.weight2, bones, foot) +
            FocusedBoneWeight(w.boneIndex3, w.weight3, bones, foot);
    }
    static float FocusedBoneWeight(int index, float weight, Transform[] bones, Transform foot)
    {
        return weight > 0f && index < bones.Length && bones[index] != null &&
            (bones[index] == foot || bones[index].IsChildOf(foot)) ? weight : 0f;
    }
    void UpdateFocusedSkinMatrices()
    {
        for (int i = 0; i < focusedSkinBones.Length; i++)
            focusedSkinMatrices[i] = focusedSkinBones[i].localToWorldMatrix * focusedBindPoses[i];
    }
    float FocusedSoleMinimum(int[] indices)
    {
        float minimum = float.PositiveInfinity;
        foreach (int index in indices)
        {
            var v = focusedSkinVertices[index];
            var w = focusedSkinWeights[index];
            Vector3 world = focusedSkinMatrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0 +
                focusedSkinMatrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1 +
                focusedSkinMatrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2 +
                focusedSkinMatrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
            minimum = Mathf.Min(minimum, world.y);
        }
        return minimum;
    }
    void PlantFocusedFoot(bool left, Vector2 anchor, float plane, float amount = 1f)
    {
        var foot = visual.GetBoneTransform(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
        var upper = visual.GetBoneTransform(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
        var lower = visual.GetBoneTransform(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
        if (foot == null || upper == null || lower == null) return;
        float minimum = FocusedSoleMinimum(left ? focusedLeftVertices : focusedRightVertices);
        if (float.IsInfinity(minimum)) return;
        Vector3 target = visual.transform.TransformPoint(new Vector3(anchor.x, 0f, anchor.y));
        target.y = foot.position.y + plane - minimum;
        target = Vector3.Lerp(foot.position, target, amount);
        Quaternion footRotation = foot.rotation;
        Vector3 direction = (target - upper.position).normalized;
        Vector3 pole = Vector3.ProjectOnPlane(lower.position - upper.position, direction);
        if (pole.sqrMagnitude < .000001f)
            pole = left ? focusedLeftPole : focusedRightPole;
        if (pole.sqrMagnitude < .000001f) pole = visual.transform.forward;
        if (left) focusedLeftPole = pole; else focusedRightPole = pole;
        float first = Vector3.Distance(upper.position, lower.position);
        float second = Vector3.Distance(lower.position, foot.position);
        float distance = Mathf.Clamp(Vector3.Distance(upper.position, target), .001f, first + second - .001f);
        float along = (first * first - second * second + distance * distance) / (2f * distance);
        float bend = Mathf.Sqrt(Mathf.Max(0f, first * first - along * along));
        Vector3 knee = upper.position + direction * along + pole.normalized * bend;
        upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, knee - upper.position) * upper.rotation;
        lower.rotation = Quaternion.FromToRotation(foot.position - lower.position, target - lower.position) * lower.rotation;
        foot.rotation = footRotation;
    }

    float FocusedReachDrop(bool left, Vector2 anchor, float plane)
    {
        var foot = visual.GetBoneTransform(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
        var upper = visual.GetBoneTransform(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
        var lower = visual.GetBoneTransform(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
        Vector3 target = visual.transform.TransformPoint(new Vector3(anchor.x, 0f, anchor.y));
        target.y = foot.position.y + plane - FocusedSoleMinimum(left ? focusedLeftVertices : focusedRightVertices);
        float length = Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, foot.position) - .001f;
        var delta = upper.position - target;
        float horizontal = delta.x * delta.x + delta.z * delta.z;
        if (horizontal >= length * length) return 0f;
        return Mathf.Max(0f, delta.y - Mathf.Sqrt(length * length - horizontal));
    }
}
