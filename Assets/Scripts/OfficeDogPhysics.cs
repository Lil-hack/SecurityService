using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class OfficeDogPhysics : MonoBehaviour
{
    public float punchSpeed=4.8f;
    public int PunchCount { get; private set; }
    public int PropHitCount { get; private set; }
    Rigidbody body;float lastPropHit=-10;
    void Awake()
    {
        body=GetComponent<Rigidbody>();body.mass=12;body.useGravity=true;body.isKinematic=false;
        body.linearDamping=1.1f;body.angularDamping=2;
        body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        // Keep the dog on its paws while allowing flight, sliding and yaw turns.
        body.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
    }
    public bool Punch(Vector3 point,Vector3 direction)
    {
        if(!isActiveAndEnabled)return false;
        Kick(point,direction,punchSpeed);PunchCount++;return true;
    }
    void Kick(Vector3 point,Vector3 direction,float speed)
    {
        if(body==null)body=GetComponent<Rigidbody>();
        direction=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
        if(direction.sqrMagnitude<.1f)direction=transform.forward;
        var velocity=body.linearVelocity;velocity.y=Mathf.Min(velocity.y,2);body.linearVelocity=Vector3.ClampMagnitude(velocity,5);
        body.AddForce(direction*speed+Vector3.up*1.7f,ForceMode.VelocityChange);
        body.AddTorque(Vector3.up*Mathf.Clamp(Vector3.Cross(point-body.worldCenterOfMass,direction).y,-.4f,.4f),ForceMode.VelocityChange);
        body.WakeUp();
    }
    void OnCollisionEnter(Collision collision)
    {
        var prop=collision.collider.GetComponentInParent<OfficePushableProp>();
        if(prop==null || prop.PlayerHolder!=null || Time.time-lastPropHit<.2f || collision.contactCount==0)return;
        float speed=collision.relativeVelocity.magnitude;if(speed<1.5f)return;
        var contact=collision.GetContact(0);var direction=body.worldCenterOfMass-prop.Body.worldCenterOfMass;
        Kick(contact.point,direction,Mathf.Clamp(speed*Mathf.Sqrt(Mathf.Max(.1f,prop.Body.mass))*.65f,2.8f,6));
        lastPropHit=Time.time;PropHitCount++;OfficePunchImpact.Spawn(contact.point,contact.normal);
    }
}

