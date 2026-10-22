using UnityEngine;

// Retargeted humanoid clips hold the arms inside these wide jackets.
// Push the upper arm and forearm outward without replacing the swing.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Animator))]
public class TripoArmClearance : MonoBehaviour
{
    [Range(0, 45)] public float minimumArmAngle = 35f;
    Animator animator;
    Transform leftArm, leftElbow, leftHand, rightArm, rightElbow, rightHand;

    void LateUpdate() { ApplyPose(); }

    public void ApplyPose()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (!animator.isHuman || animator.avatar == null || !animator.avatar.isValid) return;
            leftArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftElbow = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightElbow = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }
        Correct(leftArm, leftElbow, leftHand, -1f);
        Correct(rightArm, rightElbow, rightHand, 1f);
    }

    void Correct(Transform shoulder, Transform elbow, Transform hand, float side)
    {
        if (shoulder == null || elbow == null) return;
        Vector3 direction = transform.InverseTransformDirection(elbow.position - shoulder.position);
        if (direction.sqrMagnitude < .0001f) return;
        direction.Normalize();
        float raised = Mathf.Clamp01(direction.y);
        float hang = Mathf.Max(0f, -direction.y);
        float reach = Mathf.Tan(minimumArmAngle * Mathf.Deg2Rad);
        float minimumOutward = Mathf.Max(hang * reach, reach * (1f - raised));
        if (direction.x * side < minimumOutward)
        {
            Vector3 corrected = direction;
            corrected.x = minimumOutward * side;
            Aim(shoulder, direction, corrected);
        }
        if (hand == null) return;
        Vector3 forearm = transform.InverseTransformDirection(hand.position - elbow.position);
        if (forearm.sqrMagnitude < .0001f) return;
        forearm.Normalize();
        float forearmOut = reach * .62f * (1f - Mathf.Clamp01(forearm.y));
        if (forearm.x * side < forearmOut)
        {
            Vector3 correctedForearm = forearm;
            correctedForearm.x = forearmOut * side;
            Aim(elbow, forearm, correctedForearm);
        }
    }

    void Aim(Transform bone, Vector3 from, Vector3 to)
    {
        to.Normalize();
        bone.rotation = Quaternion.FromToRotation(transform.TransformDirection(from), transform.TransformDirection(to)) * bone.rotation;
    }
}
