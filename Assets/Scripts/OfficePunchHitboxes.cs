using System.Collections.Generic;
using UnityEngine;

public sealed class OfficePunchHitboxes : MonoBehaviour
{
    public float fistRadius=.13f;
    readonly HashSet<UnityEngine.Object> struck=new HashSet<UnityEngine.Object>();
    readonly Collider[] overlaps=new Collider[48];readonly RaycastHit[] casts=new RaycastHit[48],sight=new RaycastHit[48];
    SphereCollider left,right;
    bool tracking,lastLeft;float lastPhase;Vector3 previous;
    int effects;
    public int HitCount { get; private set; }
    SphereCollider Fist(bool isLeft)
    {
        var result=isLeft?left:right;if(result!=null)return result;
        var go=new GameObject(isLeft?"Left fist hitbox":"Right fist hitbox");go.transform.SetParent(transform,false);go.layer=2;
        result=go.AddComponent<SphereCollider>();result.radius=fistRadius;result.isTrigger=true;result.enabled=false;
        if(isLeft)left=result;else right=result;return result;
    }
    public void ResetSwing(){tracking=false;struck.Clear();effects=0;HitCount=0;if(left!=null)left.enabled=false;if(right!=null)right.enabled=false;}
    void LateUpdate(){var combat=GetComponent<OfficePunchCombat>();if(combat==null || !combat.IsPunching)ResetSwing();}
    void OnDisable(){ResetSwing();}
    public void Sample(Animator animator,bool isLeft,float phase)
    {
        var hand=animator.GetBoneTransform(isLeft?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);if(hand==null)return;
        var player=GetComponent<OfficeThirdPersonController>();var forward=player.visualRoot.forward;
        var p=hand.position+forward*.04f;
        if(!tracking || lastLeft!=isLeft || phase<lastPhase){ResetSwing();tracking=true;previous=p;lastLeft=isLeft;}
        var active=Fist(isLeft);Fist(!isLeft).enabled=false;active.transform.position=p;
        active.enabled=phase>=.16f && phase<=.72f;
        if(active.enabled)
        {
            var physics=gameObject.scene.GetPhysicsScene();var delta=p-previous;
            if(delta.sqrMagnitude>.000001f)
            {
                int count=physics.SphereCast(previous,fistRadius,delta.normalized,casts,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<count;i++)Contact(casts[i].collider,casts[i].point,p,forward,physics);
            }
            int nearby=physics.OverlapSphere(p,fistRadius,overlaps,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<nearby;i++)Contact(overlaps[i],overlaps[i].ClosestPoint(p),p,forward,physics);
            // Small animals are below the standing fist path. Only accept dogs in
            // close reach; Contact still checks walls and one hit per swing.
            if(phase>=.3f && phase<=.6f)
            {
                int low=physics.OverlapSphere(transform.position+forward*.65f+Vector3.up*.35f,.35f,overlaps,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<low;i++)if(overlaps[i].GetComponentInParent<OfficeDogPhysics>()!=null)
                    Contact(overlaps[i],overlaps[i].ClosestPoint(p),p,forward,physics);
            }
        }
        previous=p;lastPhase=phase;
    }
    void Contact(Collider collider,Vector3 point,Vector3 fist,Vector3 forward,PhysicsScene physics)
    {
        if(collider==null || collider.transform.IsChildOf(transform) || collider.isTrigger)return;
        var prop=collider.GetComponentInParent<OfficePushableProp>();
        var npc=collider.GetComponentInParent<OfficeStaffPhysics>();
        var dog=collider.GetComponentInParent<OfficeDogPhysics>();
        UnityEngine.Object id=dog!=null?(UnityEngine.Object)dog:npc!=null?(UnityEngine.Object)npc:prop!=null?(UnityEngine.Object)prop:collider;if(struck.Contains(id))return;
        var origin=transform.position+Vector3.up*(fist.y-transform.position.y);
        var delta=point-origin;int n=physics.Raycast(origin,delta.normalized,sight,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<n;i++)
            if(sight[i].collider!=collider && !sight[i].transform.IsChildOf(transform) && (npc==null || sight[i].collider.GetComponentInParent<OfficeStaffPhysics>()!=npc) && (prop==null || sight[i].collider.GetComponentInParent<OfficePushableProp>()!=prop) && sight[i].distance<delta.magnitude-.03f)return;
        struck.Add(id);
        if(dog!=null){if(!dog.Punch(point,forward))return;HitCount++;}
        if(npc!=null){if(!npc.Punch(point,forward))return;HitCount++;}
        if(prop!=null)
        {
            float speed=Mathf.Lerp(8f,2.8f,Mathf.InverseLerp(.1f,12,prop.Body.mass));
            var impulse=Vector3.ClampMagnitude((forward*speed+Vector3.up*1.25f)*prop.Body.mass,24);
            if(!prop.ApplyPunchImpulse(point,impulse))return;
            HitCount++;
        }
        if(effects++<3)OfficePunchImpact.Spawn(point,-forward);
    }
}


