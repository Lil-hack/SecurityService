using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController),typeof(OfficePropGrabber))]
public class OfficeThirdPersonController : MonoBehaviour
{
    public Camera followCamera;
    public Transform visualRoot, leftLeg, rightLeg, leftArm, rightArm;
    public Animator humanoidAnimator;
    public float walkSpeed=2.2f, runSpeed=4.2f, jumpHeight=.65f;
    public float cameraDistance=2.6f, mouseSensitivity=.12f;
    public float pushForce=18f;
    Vector3 pushDirection;
    float pushDeltaTime, pushSpeed;
    CharacterController motor;
    Renderer[] bodyRenderers;
    Vector3 spawn;
    float verticalVelocity, yaw, pitch=18f, gait, tutorialTime;
    const float Gravity=-22f;
    readonly RaycastHit[] cameraHits=new RaycastHit[32];
    readonly Collider[] pushContacts=new Collider[48];
    readonly Rigidbody[] pushedBodies=new Rigidbody[48];
    readonly RaycastHit[] pushObstacles=new RaycastHit[32];
    int pushedCount;

    void Awake()
    {
        motor=GetComponent<CharacterController>(); spawn=transform.position;
        RefreshVisual();
        tutorialTime=Time.time+12;
    }

    void Start()
    {
        Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false;
        UpdateCamera();
    }

    void Update()
    {
        var keyboard=Keyboard.current; var mouse=Mouse.current;
        if(keyboard!=null && keyboard.escapeKey.wasPressedThisFrame)
        { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        if(mouse!=null && mouse.leftButton.wasPressedThisFrame && Application.isFocused)
        { Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; }
        if(mouse!=null && Cursor.lockState==CursorLockMode.Locked)
        {
            var delta=mouse.delta.ReadValue(); yaw+=delta.x*mouseSensitivity;
            pitch=Mathf.Clamp(pitch-delta.y*mouseSensitivity,-12,65);
            cameraDistance=Mathf.Clamp(cameraDistance-mouse.scroll.ReadValue().y*.003f,1.1f,3.5f);
        }
        Vector2 input=Vector2.zero;
        if(keyboard!=null && Application.isFocused)
        {
            input.x=(keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed?1:0)-(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed?1:0);
            input.y=(keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed?1:0)-(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed?1:0);
        }
        bool sprint=keyboard!=null && (keyboard.leftShiftKey.isPressed||keyboard.rightShiftKey.isPressed);
        bool jump=keyboard!=null && keyboard.spaceKey.wasPressedThisFrame;
        TickMotor(input,sprint,jump,Time.deltaTime);
    }

    // The same motor entry point can also be driven by a gamepad, automation or scripted movement.
    public CollisionFlags TickMotor(Vector2 input,bool sprint,bool jump,float deltaTime)
    {
        if(motor==null) motor=GetComponent<CharacterController>();
        float dt=Mathf.Clamp(deltaTime,0,.05f);
        input=Vector2.ClampMagnitude(input,1);
        Vector3 direction=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y);
        if(motor.isGrounded && verticalVelocity<0) verticalVelocity=-2f;
        if(jump && motor.isGrounded) verticalVelocity=Mathf.Sqrt(-2*Gravity*jumpHeight);
        verticalVelocity+=Gravity*dt;
        var previousPosition=transform.position;
        pushDirection=direction;
        pushDeltaTime=dt;
        pushSpeed=sprint?runSpeed:walkSpeed;
        pushedCount=0;
        PushNearbyProps();
        var flags=motor.Move((direction*(sprint?runSpeed:walkSpeed)+Vector3.up*verticalVelocity)*dt);
        if((flags&CollisionFlags.Above)!=0 && verticalVelocity>0) verticalVelocity=0;
        if((flags&CollisionFlags.Below)!=0 && verticalVelocity<0) verticalVelocity=-2;
        if(direction.sqrMagnitude>.001f)
            visualRoot.rotation=Quaternion.Slerp(visualRoot.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-14*dt));
        if(humanoidAnimator!=null)
        {
            var displacement=transform.position-previousPosition; displacement.y=0;
            float speed=dt>0?displacement.magnitude/dt:0;
            humanoidAnimator.SetFloat("Speed",speed,.12f,dt);
            humanoidAnimator.SetBool("Grounded",(flags&CollisionFlags.Below)!=0 || motor.isGrounded);
            humanoidAnimator.SetFloat("VerticalSpeed",verticalVelocity);
        }
        else if(leftLeg!=null && rightLeg!=null && leftArm!=null && rightArm!=null)
        {
            float amount=input.magnitude;
            gait+=dt*(sprint?13f:8f)*amount;
            float swing=Mathf.Sin(gait)*(sprint?36:25)*amount;
            leftLeg.localRotation=Quaternion.Euler(swing,0,0); rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
            leftArm.localRotation=Quaternion.Euler(-swing*.8f,0,-5); rightArm.localRotation=Quaternion.Euler(swing*.8f,0,5);
            visualRoot.localPosition=Vector3.up*(Mathf.Abs(Mathf.Sin(gait*2))*.018f*amount);
        }
        if(transform.position.y< -3)
        {
            motor.enabled=false; transform.position=spawn; motor.enabled=true; verticalVelocity=0;
        }
        return flags;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var body=hit.collider.attachedRigidbody;
        if(body==null || body.isKinematic || pushDirection.sqrMagnitude<.001f || hit.normal.y>.5f) return;
        PushBody(body,hit.point);
    }

    void PushNearbyProps()
    {
        if(pushDirection.sqrMagnitude<.001f) return;
        // The movement capsule steps over tiny objects and is narrower than the
        // character's arms. Detect a short body contact before resolving the step.
        var physics=gameObject.scene.GetPhysicsScene();
        var bounds=motor.bounds;
        var offset=pushDirection.normalized*.08f;
        var bottom=new Vector3(bounds.center.x,bounds.min.y+motor.radius,bounds.center.z)+offset;
        var top=new Vector3(bounds.center.x,bounds.max.y-motor.radius,bounds.center.z)+offset;
        int count=physics.OverlapCapsule(bottom,top,motor.radius+.15f,pushContacts,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            var contact=pushContacts[i];
            var body=contact.attachedRigidbody;
            if(body==null || body.isKinematic || body.GetComponent<OfficePushableProp>()==null ||
                contact.transform.IsChildOf(transform) || Vector3.Dot(body.worldCenterOfMass-transform.position,pushDirection)<0) continue;
            var origin=new Vector3(bounds.center.x,Mathf.Clamp(contact.bounds.center.y,bounds.min.y+.06f,bounds.max.y-.06f),bounds.center.z);
            var point=contact.ClosestPoint(origin);
            var delta=point-origin;
            bool blocked=false;
            int obstacles=physics.Raycast(origin,delta.normalized,pushObstacles,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
            for(int j=0;j<obstacles;j++)
            {
                var obstacle=pushObstacles[j];
                if(obstacle.collider.attachedRigidbody!=body && !obstacle.transform.IsChildOf(transform) &&
                    obstacle.distance<delta.magnitude-.005f) { blocked=true; break; }
            }
            if(!blocked) PushBody(body,point);
        }
    }

    void PushBody(Rigidbody body,Vector3 point)
    {
        for(int i=0;i<pushedCount;i++) if(pushedBodies[i]==body) return;
        if(pushedCount>=pushedBodies.Length) return;
        var direction=pushDirection.normalized;
        float remainingSpeed=pushSpeed-Vector3.Dot(body.linearVelocity,direction);
        if(remainingSpeed<=0) return;
        // Scale force with mass so floor friction cannot pin heavier props in place.
        float impulse=body.mass*Mathf.Min(pushForce*pushDeltaTime,remainingSpeed);
        body.AddForceAtPosition(direction*impulse,point,ForceMode.Impulse);
        pushedBodies[pushedCount++]=body;
    }

    void LateUpdate() { UpdateCamera(); }

    public void RefreshVisual()
    {
        bodyRenderers=visualRoot==null?new Renderer[0]:visualRoot.GetComponentsInChildren<Renderer>();
    }

    public void UpdateCamera()
    {
        if(followCamera==null) return;
        Vector3 pivot=transform.position+Vector3.up*1.42f;
        Vector3 backwards=Quaternion.Euler(pitch,yaw,0)*Vector3.back;
        float distance=cameraDistance;
        int count=Physics.SphereCastNonAlloc(pivot,.15f,backwards,cameraHits,distance,~(1<<2),QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
            if(cameraHits[i].collider!=null && !cameraHits[i].collider.transform.IsChildOf(transform))
                distance=Mathf.Min(distance,Mathf.Max(.12f,cameraHits[i].distance-.06f));
        followCamera.transform.SetPositionAndRotation(pivot+backwards*distance,Quaternion.LookRotation(-backwards));
        if(bodyRenderers==null && visualRoot!=null) bodyRenderers=visualRoot.GetComponentsInChildren<Renderer>();
        if(bodyRenderers!=null) foreach(var renderer in bodyRenderers) if(renderer!=null) renderer.enabled=distance>.55f;
    }

    void OnApplicationFocus(bool focused)
    {
        if(!focused) { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
    }
    void OnDisable() { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }

    void OnGUI()
    {
        if(Time.time>tutorialTime && Cursor.lockState==CursorLockMode.Locked) return;
        GUI.Box(new Rect(16,16,620,76),"WASD / стрелки — движение    Shift — бег    Пробел — прыжок\nМышь — обзор    Колесо — расстояние камеры    Esc — курсор\nF — взять / отпустить    Зажми ЛКМ — сила броска, отпусти — бросок");
    }
}
