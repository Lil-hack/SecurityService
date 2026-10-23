using UnityEngine;

[RequireComponent(typeof(OfficeStaffPhysics),typeof(Rigidbody))]
public sealed class OfficeStaffPatrol : MonoBehaviour
{
    public Vector3[] waypoints;
    public float speed=.9f,pauseSeconds=1.7f;
    Animator animator;OfficeStaffPhysics physics;Rigidbody body;CapsuleCollider capsule;
    int next;float pause,blocked;bool wasDown;
    void Start()
    {
        animator=GetComponentInChildren<Animator>();physics=GetComponent<OfficeStaffPhysics>();body=GetComponent<Rigidbody>();capsule=GetComponent<CapsuleCollider>();
        if(waypoints==null || waypoints.Length<2){enabled=false;return;}
        next=Vector3.Distance(transform.position,waypoints[0])<Vector3.Distance(transform.position,waypoints[1])?1:0;pause=.7f;
    }
    void FixedUpdate()
    {
        if(animator==null)return;
        if(physics.IsDown){animator.SetFloat("PatrolSpeed",0);wasDown=true;return;}
        if(wasDown){wasDown=false;pause=pauseSeconds;}
        if(pause>0){pause-=Time.fixedDeltaTime;animator.SetFloat("PatrolSpeed",0);return;}
        var target=waypoints[next];var delta=Vector3.ProjectOnPlane(target-transform.position,Vector3.up);
        if(delta.magnitude<.16f){next=(next+1)%waypoints.Length;pause=pauseSeconds;animator.SetFloat("PatrolSpeed",0);return;}
        var forward=delta.normalized;float step=speed*Time.fixedDeltaTime;
        bool obstacle=false;
        var origin=transform.position;
        foreach(var hit in Physics.CapsuleCastAll(origin+Vector3.up*(capsule.radius+.05f),origin+Vector3.up*(capsule.height-capsule.radius),capsule.radius*.9f,forward,step+.2f,~0,QueryTriggerInteraction.Ignore))
            if(!hit.transform.IsChildOf(transform)){obstacle=true;break;}
        if(obstacle)
        {
            animator.SetFloat("PatrolSpeed",0);blocked+=Time.fixedDeltaTime;
            if(blocked>1.2f){next=(next+1)%waypoints.Length;blocked=0;pause=.5f;}
            return;
        }
        blocked=0;
        body.MoveRotation(Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(forward),180*Time.fixedDeltaTime));
        body.MovePosition(body.position+forward*step);animator.SetFloat("PatrolSpeed",speed,.12f,Time.fixedDeltaTime);
    }
    void OnDisable(){if(animator!=null)animator.SetFloat("PatrolSpeed",0);}
}
