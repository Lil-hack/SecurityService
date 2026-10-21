using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class OfficePushableProp : MonoBehaviour
{
    Rigidbody body;
    bool held;
    public bool allowPickup=true;
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
            if(renderers.Length>0) bounds=renderers[0].bounds;
            foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }

    public bool CanPickUp
    {
        get
        {
            return allowPickup && isActiveAndEnabled && !held && PlayerHolder==null &&
                !Body.isKinematic && Body.constraints!=RigidbodyConstraints.FreezeAll;
        }
    }

    public bool Grab(OfficePropGrabber holder)
    {
        if(holder==null || !CanPickUp) return false;
        RestorePlayerCollisions();
        previousGravity=Body.useGravity; previousCollisionMode=Body.collisionDetectionMode;
        Body.linearVelocity=Vector3.zero; Body.angularVelocity=Vector3.zero;
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

    public void ReleaseFromPlayer(OfficePropGrabber holder,Vector3 velocity,Vector3 spin)
    {
        if(PlayerHolder!=holder || holder==null) return;
        PlayerHolder=null;
        Body.isKinematic=false; Body.useGravity=previousGravity;
        Body.collisionDetectionMode=previousCollisionMode;
        Body.linearVelocity=velocity; Body.angularVelocity=spin;
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
