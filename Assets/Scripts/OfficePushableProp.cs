using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class OfficePushableProp : MonoBehaviour
{
    Rigidbody body;
    bool held;
    public bool allowPickup=true;
    public bool keepInitialPoseUntilGrab;
    bool initialPoseReleased;
    public bool InitialPosePinned => keepInitialPoseUntilGrab && !initialPoseReleased;
    public Vector3 InitialPosition { get; private set; }
    public Quaternion InitialRotation { get; private set; }
    void Awake()
    {
        InitialPosition=transform.position;InitialRotation=transform.rotation;
        if(InitialPosePinned){Body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;Body.isKinematic=true;}
    }
    public OfficePropGrabber PlayerHolder { get; private set; }
    public Rigidbody Body { get { if(body==null) body=GetComponent<Rigidbody>(); return body; } }
    readonly List<Collider[]> ignoredPairs=new List<Collider[]>();
    CollisionDetectionMode previousCollisionMode;
    bool previousGravity;
    float npcBlockedUntil;

    public Bounds WorldBounds
    {
        get
        {
            var renderers=GetComponentsInChildren<Renderer>();
            var bounds=new Bounds(transform.position,Vector3.zero);
            bool first=true;
            foreach(var renderer in renderers)
            {
                // Steam and trails are visual effects, not the solid pickup footprint.
                if(renderer is ParticleSystemRenderer || renderer is TrailRenderer)continue;
                if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }
    }

    public bool CanPickUp
    {
        get
        {
            return allowPickup && isActiveAndEnabled && !held && PlayerHolder==null &&
                (!Body.isKinematic || InitialPosePinned) && Body.constraints!=RigidbodyConstraints.FreezeAll;
        }
    }

    public bool Grab(OfficePropGrabber holder)
    {
        if(holder==null || !CanPickUp) return false;
        RestorePlayerCollisions();
        previousGravity=Body.useGravity; previousCollisionMode=InitialPosePinned?CollisionDetectionMode.ContinuousDynamic:Body.collisionDetectionMode;
        initialPoseReleased=true;
        if(!Body.isKinematic){Body.linearVelocity=Vector3.zero; Body.angularVelocity=Vector3.zero;}
        Body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        Body.isKinematic=true; Body.useGravity=false;
        PlayerHolder=holder;
        foreach(var propCollider in GetComponentsInChildren<Collider>())
            foreach(var playerCollider in holder.GetComponentsInChildren<Collider>())
                if(!Physics.GetIgnoreCollision(propCollider,playerCollider))
                {
                    Physics.IgnoreCollision(propCollider,playerCollider,true);
                    ignoredPairs.Add(new[]{propCollider,playerCollider});
                }
        return true;
    }

    public bool ApplyPunchImpulse(Vector3 point,Vector3 impulse)
    {
        if(!allowPickup || held || PlayerHolder!=null || !isActiveAndEnabled || (Body.isKinematic && !InitialPosePinned))return false;
        initialPoseReleased=true;Body.isKinematic=false;Body.useGravity=true;
        Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        Body.AddForceAtPosition(impulse,point,ForceMode.Impulse);Body.WakeUp();npcBlockedUntil=Time.time+1;
        return true;
    }

    float thrownUntil;
    Vector3 thrownVelocity;
    void OnCollisionEnter(Collision collision)
    {
        if(Time.time>thrownUntil || PlayerHolder!=null || collision.contactCount==0)return;
        var npc=collision.collider.GetComponentInParent<OfficeStaffPhysics>();
        if(npc==null || collision.relativeVelocity.magnitude<1.5f)return;
        if(npc.ThrownImpact(collision.GetContact(0).point,thrownVelocity,Body.mass))
        {thrownUntil=0;OfficePunchImpact.Spawn(collision.GetContact(0).point,-thrownVelocity.normalized);}
    }

    public void ReleaseFromPlayer(OfficePropGrabber holder,Vector3 velocity,Vector3 spin)
    {
        if(PlayerHolder!=holder || holder==null) return;
        PlayerHolder=null;
        Body.isKinematic=false; Body.useGravity=previousGravity;
        Body.collisionDetectionMode=previousCollisionMode;
        Body.linearVelocity=velocity; Body.angularVelocity=spin;
        thrownUntil=Time.time+5;thrownVelocity=velocity;
        Body.WakeUp(); npcBlockedUntil=Time.time+.5f;
    }

    void FixedUpdate()
    {
        if(PlayerHolder!=null || ignoredPairs.Count==0) return;
        foreach(var pair in ignoredPairs)
            if(pair[0]!=null && pair[1]!=null && pair[0].bounds.Intersects(pair[1].bounds)) return;
        RestorePlayerCollisions();
    }

    void RestorePlayerCollisions()
    {
        foreach(var pair in ignoredPairs)
            if(pair[0]!=null && pair[1]!=null) Physics.IgnoreCollision(pair[0],pair[1],false);
        ignoredPairs.Clear();
    }

    // Scripted NPC interactions temporarily own a prop only while it is in reach.
    // A cup knocked off the desk must never teleport back during the coffee routine.
    public void PoseForNpc(Vector3 home,Quaternion homeRotation,Vector3 target,Quaternion rotation,float weight)
    {
        if(PlayerHolder!=null || Time.time<npcBlockedUntil) return;
        if(body==null) body=GetComponent<Rigidbody>();
        if(!held)
        {
            if(weight<=.001f || Vector3.Distance(transform.position,home)>.12f ||
                Quaternion.Angle(transform.rotation,homeRotation)>30f || body.linearVelocity.sqrMagnitude>.04f) return;
            body.isKinematic=true;
            held=true;
        }
        transform.SetPositionAndRotation(Vector3.Lerp(home,target,weight),Quaternion.Slerp(homeRotation,rotation,weight));
        if(weight<=.001f) Release();
    }

    public void Release()
    {
        if(!held) return;
        held=false;
        if(body!=null) body.isKinematic=false;
    }

    void OnDisable()
    {
        if(PlayerHolder!=null) PlayerHolder.DropHeld();
        Release(); RestorePlayerCollisions();
    }
}
