using UnityEngine;

// Retargeted human idles hold the arms too close to this character's broad jacket.
// Correct the animated pose rather than changing the source FBX or skin weights.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Animator))]
public class TripoArmClearance : MonoBehaviour
{
    [Range(0, 45)] public float minimumArmAngle = 35f;
    Animator animator;
    Transform leftArm, leftElbow, rightArm, rightElbow;

    void LateUpdate() { ApplyPose(); }

    public void ApplyPose()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (!animator.isHuman || animator.avatar == null || !animator.avatar.isValid) return;
            leftArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftElbow = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            rightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightElbow = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        }
        Correct(leftArm, leftElbow, -1);
        Correct(rightArm, rightElbow, 1);
    }

    void Correct(Transform shoulder, Transform elbow, float side)
    {
        if (shoulder == null || elbow == null) return;
        Vector3 direction = transform.InverseTransformDirection(elbow.position - shoulder.position).normalized;
        // Raised or extended arms retain their animation; only inward hanging arms need clearance.
        if (direction.y >= -.15f) return;
        float minimumOutward = -direction.y * Mathf.Tan(minimumArmAngle * Mathf.Deg2Rad);
        if (direction.x * side >= minimumOutward) return;
        Vector3 corrected = direction;
        corrected.x = minimumOutward * side;
        shoulder.rotation = Quaternion.FromToRotation(transform.TransformDirection(direction),
            transform.TransformDirection(corrected.normalized)) * shoulder.rotation;
    }
}
