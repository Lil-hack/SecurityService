using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public sealed class OfficeStaffPhysics : MonoBehaviour
{
    public bool IsDown { get; private set; }
    public bool IsRecovering { get; private set; }
    public AnimationClip getUpFront,getUpBack;
    Animator animator;CapsuleCollider standing;Rigidbody hipsBody;
    readonly List<Rigidbody> bodies=new List<Rigidbody>();readonly List<Collider> colliders=new List<Collider>();
    Transform[] poseBones;Vector3[] restPositions,fallenPositions;Quaternion[] restRotations,fallenRotations;
    float downTime,recovery;Quaternion fallenFacing;
    public Vector3 LastFallPosition { get; private set; }
    float getUpDuration=2f;
    void Awake()=>PrepareRagdoll();
    public void PrepareRagdoll()
    {
        bodies.Clear();colliders.Clear();
        animator=GetComponentInChildren<Animator>();standing=GetComponent<CapsuleCollider>();
        if(animator==null || !animator.isHuman){enabled=false;return;}
        var bones=new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Head,
            HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,
            HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg};
        var ends=new[]{HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Neck,HumanBodyBones.LastBone,
            HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,
            HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
        var map=new Dictionary<Transform,Rigidbody>();
        for(int i=0;i<bones.Length;i++)
        {
            var bone=animator.GetBoneTransform(bones[i]);if(bone==null)continue;
            var end=ends[i]==HumanBodyBones.LastBone?null:animator.GetBoneTransform(ends[i]);
            Vector3 delta=end!=null?bone.InverseTransformPoint(end.position):bone.InverseTransformVector(Vector3.up*.20f);
            var rb=bone.GetComponent<Rigidbody>();if(rb==null)rb=bone.gameObject.AddComponent<Rigidbody>();rb.mass=i==0?9:i<4?5:2;rb.isKinematic=true;
            rb.linearDamping=.25f;rb.angularDamping=1.5f;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            var c=bone.GetComponent<CapsuleCollider>();if(c==null)c=bone.gameObject.AddComponent<CapsuleCollider>();
            float sx=Mathf.Abs(bone.lossyScale.x);float sy=Mathf.Abs(bone.lossyScale.y);float sz=Mathf.Abs(bone.lossyScale.z);
            int axis=Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?0:1;if(Mathf.Abs(delta.z)>Mathf.Abs(delta[axis]))axis=2;
            c.direction=axis;c.center=delta*.5f;c.radius=(i<4?.12f:.065f)/Mathf.Max(.001f,Mathf.Max(sx,Mathf.Max(sy,sz)));
            c.height=Mathf.Max(c.radius*2,Mathf.Abs(delta[axis]));c.enabled=false;
            map[bone]=rb;bodies.Add(rb);colliders.Add(c);if(i==0)hipsBody=rb;
        }
        foreach(var rb in bodies)
        {
            if(rb==hipsBody)continue;var parent=rb.transform.parent;
            while(parent!=null && !map.ContainsKey(parent))parent=parent.parent;
            if(parent==null)continue;
            var joint=rb.GetComponent<CharacterJoint>();if(joint==null)joint=rb.gameObject.AddComponent<CharacterJoint>();joint.connectedBody=map[parent];joint.enableProjection=true;
            joint.lowTwistLimit=new SoftJointLimit{limit=-25};joint.highTwistLimit=new SoftJointLimit{limit=25};
            joint.swing1Limit=new SoftJointLimit{limit=45};joint.swing2Limit=new SoftJointLimit{limit=30};
        }
        for(int i=0;i<colliders.Count;i++)for(int j=i+1;j<colliders.Count;j++)Physics.IgnoreCollision(colliders[i],colliders[j]);
        poseBones=animator.GetComponentsInChildren<Transform>();restPositions=new Vector3[poseBones.Length];restRotations=new Quaternion[poseBones.Length];
        fallenPositions=new Vector3[poseBones.Length];fallenRotations=new Quaternion[poseBones.Length];
        for(int i=0;i<poseBones.Length;i++){restPositions[i]=poseBones[i].localPosition;restRotations[i]=poseBones[i].localRotation;}
    }
    public bool Punch(Vector3 point,Vector3 forward)
        => KnockDown(point,forward*4.5f+Vector3.up*1.6f);
    public bool ThrownImpact(Vector3 point,Vector3 velocity,float mass)
    {
        var flat=Vector3.ProjectOnPlane(velocity,Vector3.up);
        if(flat.sqrMagnitude<.1f)flat=transform.forward;
        float speed=Mathf.Clamp(velocity.magnitude*Mathf.Sqrt(Mathf.Max(.1f,mass))*.8f,3,7);
        return KnockDown(point,flat.normalized*speed+Vector3.up*1.5f);
    }
    bool KnockDown(Vector3 point,Vector3 velocity)
    {
        if(!enabled || hipsBody==null)return false;
        var typing=GetComponent<OfficeStaffTyping>();if(typing!=null)typing.Interrupt();
        IsRecovering=false;IsDown=true;downTime=0;animator.enabled=false;standing.enabled=false;
        foreach(var c in colliders)c.enabled=true;
        foreach(var b in bodies){b.isKinematic=false;b.WakeUp();b.AddForce(velocity,ForceMode.VelocityChange);}
        hipsBody.AddForceAtPosition(Vector3.ProjectOnPlane(velocity,Vector3.up)*5,point,ForceMode.Impulse);return true;
    }
    void Update()
    {
        if(!IsDown)return;
        downTime+=Time.deltaTime;
        if(!IsRecovering && downTime>2.2f && (hipsBody.linearVelocity.magnitude<1 || downTime>5))BeginRecovery();
        if(IsRecovering)
        {
            recovery+=Time.deltaTime;
            // Reset unkeyed bones before Animator evaluates the target standing pose.
            for(int i=0;i<poseBones.Length;i++){poseBones[i].localPosition=restPositions[i];poseBones[i].localRotation=restRotations[i];}
        }
    }
    public void BeginRecovery()
    {
        if(!IsDown || IsRecovering)return;
        foreach(var b in bodies){b.linearVelocity=Vector3.zero;b.angularVelocity=Vector3.zero;b.isKinematic=true;}
        foreach(var c in colliders)c.enabled=false;
        var p=hipsBody.position;var ground=p;
        var back=Vector3.Dot(hipsBody.transform.forward,Vector3.up)>0;
        var getUp=back?getUpBack:getUpFront;getUpDuration=getUp!=null?getUp.length:2f;
        var direction=Vector3.ProjectOnPlane(animator.GetBoneTransform(HumanBodyBones.Head).position-p,Vector3.up);
        if(direction.sqrMagnitude<.01f)direction=transform.forward;
        fallenFacing=Quaternion.LookRotation(back?-direction:direction);
        bool found=false;float closest=float.PositiveInfinity;
        foreach(var hit in Physics.RaycastAll(p+Vector3.up*.15f,Vector3.down,5,~0,QueryTriggerInteraction.Ignore))
            if(!hit.transform.IsChildOf(transform) && hit.normal.y>.65f && hit.distance<closest){closest=hit.distance;ground=hit.point;found=true;}
        // Preserve the fallen body's horizontal location. Never return to its spawn.
        Vector3 destination=new Vector3(p.x,found?ground.y+.03f:transform.position.y,p.z);
        // If the body slid under furniture, use the closest clear standing space beside it.
        bool Clear(Vector3 spot)
        {
            foreach(var c in Physics.OverlapCapsule(spot+Vector3.up*(standing.radius+.04f),spot+Vector3.up*(standing.height-standing.radius),standing.radius,~0,QueryTriggerInteraction.Ignore))
                if(!c.transform.IsChildOf(transform))return false;
            return true;
        }
        if(!Clear(destination))
        {
            bool free=false;
            for(float radius=.2f;radius<=1.21f && !free;radius+=.2f)
                for(int i=0;i<16;i++)
                {
                    float angle=i*Mathf.PI/8;var candidate=destination+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                    if(!Clear(candidate))continue;
                    destination=candidate;free=true;break;
                }
            if(!free)
            {
                foreach(var c in colliders)c.enabled=true;
                foreach(var b in bodies)b.isKinematic=false;
                downTime=1.8f;return;
            }
        }
        LastFallPosition=destination;
        var worldPositions=new Vector3[poseBones.Length];var worldRotations=new Quaternion[poseBones.Length];
        for(int i=0;i<poseBones.Length;i++){worldPositions[i]=poseBones[i].position;worldRotations[i]=poseBones[i].rotation;}
        transform.SetPositionAndRotation(destination,fallenFacing);
        for(int i=0;i<poseBones.Length;i++)poseBones[i].SetPositionAndRotation(worldPositions[i],worldRotations[i]);
        for(int i=0;i<poseBones.Length;i++){fallenPositions[i]=poseBones[i].localPosition;fallenRotations[i]=poseBones[i].localRotation;poseBones[i].localPosition=restPositions[i];poseBones[i].localRotation=restRotations[i];}
        animator.enabled=true;animator.Rebind();animator.Play(back?"GetUpBack":"GetUpFront",0,0);animator.Update(0);recovery=0;IsRecovering=true;
    }
    void LateUpdate()
    {
        if(!IsRecovering)return;float t=Mathf.SmoothStep(0,1,recovery/.35f);
        // The authored kneeling pose rises gradually before returning to idle.
        float rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1.85f,recovery));
        if(getUpFront==null)hipsBody.transform.position-=Vector3.up*(standing.height*.32f*(1-rise));
        for(int i=0;i<poseBones.Length;i++)
        {
            poseBones[i].localPosition=Vector3.Lerp(fallenPositions[i],poseBones[i].localPosition,t);
            poseBones[i].localRotation=Quaternion.Slerp(fallenRotations[i],poseBones[i].localRotation,t);
        }
        if(recovery>=getUpDuration){IsDown=false;IsRecovering=false;standing.enabled=true;animator.CrossFadeInFixedTime("Idle",.15f);}
    }
}


