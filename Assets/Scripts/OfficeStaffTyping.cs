using UnityEngine;

[DefaultExecutionOrder(300)]
public sealed class OfficeStaffTyping : MonoBehaviour
{
    public Transform keyboard,chair;
    public float seatHeight=.52f;
    public bool IsSeated { get; private set; }=true;
    Animator animator;OfficeStaffPhysics physics;CapsuleCollider capsule;
    float height;Vector3 center;
    void Start()
    {
        animator=GetComponentInChildren<Animator>();physics=GetComponent<OfficeStaffPhysics>();capsule=GetComponent<CapsuleCollider>();
        foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>())skin.forceMatrixRecalculationPerRender=true;
        height=capsule.height;center=capsule.center;
        capsule.height=1.25f;capsule.center=Vector3.up*.68f;
        var prop=chair!=null?chair.GetComponent<OfficePushableProp>():null;
        if(prop!=null){prop.keepInitialPoseUntilGrab=true;prop.Body.isKinematic=true;}
    }
    public void Interrupt()
    {
        IsSeated=false;
        if(capsule!=null){capsule.height=height;capsule.center=center;}
    }
    void LateUpdate()
    {
        if(!IsSeated || keyboard==null || physics!=null && physics.IsDown)return;
        if(animator==null)animator=GetComponentInChildren<Animator>();
        ApplyPose(Time.time);
    }
    public void ApplyPose(float time)
    {
        if(keyboard==null)return;if(animator==null)animator=GetComponentInChildren<Animator>();
        Transform B(HumanBodyBones b)=>animator.GetBoneTransform(b);
        var hips=B(HumanBodyBones.Hips);hips.position+=Vector3.up*(seatHeight+.09f-hips.position.y);
        var forward=transform.forward;var right=transform.right;
        foreach(bool left in new[]{true,false})
        {
            var thigh=B(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);var knee=B(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg);var foot=B(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
            var target=transform.position+forward*.40f+right*(left?-.20f:.20f)+Vector3.up*.11f;
            var footRotation=foot.rotation;
            OfficeBossNpc.Solve(thigh,knee,foot,target,forward);
            foot.rotation=footRotation;
        }
        var chest=B(HumanBodyBones.Chest);if(chest!=null)chest.rotation=Quaternion.AngleAxis(22+Mathf.Sin(time*1.4f)*1.5f,right)*chest.rotation;
        float tapping=Mathf.Sin(time*.9f)>-.85f?1:0;
        foreach(bool left in new[]{true,false})
        {
            var upper=B(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);var lower=B(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);var hand=B(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
            var shoulder=B(left?HumanBodyBones.LeftShoulder:HumanBodyBones.RightShoulder);if(shoulder!=null)shoulder.position+=forward*.12f-Vector3.up*.06f;
            upper.position+=forward*.14f-Vector3.up*.04f;
            var key=keyboard.position-forward*.13f+right*(left?-.13f:.13f)+Vector3.up*(.025f+.012f*tapping*Mathf.Sin(time*(left?19:23)));
            var forearm=hand.position-lower.position;var wrist=hand.rotation;
            OfficeBossNpc.Solve(upper,lower,hand,key,-Vector3.up+right*(left?-.5f:.5f));
            hand.rotation=Quaternion.FromToRotation(forearm,hand.position-lower.position)*wrist;
            var finger=B(left?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal);
            if(finger!=null)
            {
                var fingerDirection=finger.position-hand.position;
                if(fingerDirection.sqrMagnitude>.00001f)hand.rotation=Quaternion.FromToRotation(fingerDirection,forward)*hand.rotation;
            }
            var index=B(left?HumanBodyBones.LeftIndexProximal:HumanBodyBones.RightIndexProximal);
            var little=B(left?HumanBodyBones.LeftLittleProximal:HumanBodyBones.RightLittleProximal);
            if(index!=null && little!=null)
            {
                var palm=Vector3.Cross(index.position-hand.position,little.position-hand.position)*(left?1:-1);
                hand.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(palm,Vector3.down,forward),forward)*hand.rotation;
            }
        }
        var head=B(HumanBodyBones.Head);head.rotation=Quaternion.AngleAxis(-3+Mathf.Sin(time*.8f),right)*head.rotation;
    }
}

