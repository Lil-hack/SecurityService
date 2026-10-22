using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(300),RequireComponent(typeof(CharacterController))]
public class OfficePropGrabber : MonoBehaviour
{
    public float pickupReach=1.8f, throwSpeed=9f;
    public float maximumThrowSpeed=18f, throwChargeSeconds=3f;
    public bool IsChargingThrow { get; private set; }
    public float ThrowCharge01 { get { return Mathf.Clamp01(chargeElapsed/Mathf.Max(.1f,throwChargeSeconds)); } }
    public bool ThrowPreviewVisible { get { return throwPreview!=null && throwPreview.Visible; } }
    float chargeElapsed;
    OfficeThrowPreview throwPreview;
    public OfficePushableProp HeldProp { get; private set; }
    public OfficePushableProp Target { get; private set; }
    OfficeThirdPersonController controller;
    CharacterController motor;
    Vector3 localBoundsCenter;
    Vector3 localBoundsExtents;
    Quaternion heldRotation;
    float radius;
    bool hadLockedCursor;
    readonly OfficePickupHighlight highlight=new OfficePickupHighlight();
    SkinnedMeshRenderer[] carryRenderers;
    bool[] previousSkinning;
    readonly RaycastHit[] hits=new RaycastHit[64];
    readonly RaycastHit[] blockers=new RaycastHit[64];
    readonly Collider[] overlaps=new Collider[64];
    Camera View { get { return controller==null?null:controller.followCamera; } }

    void Awake() { controller=GetComponent<OfficeThirdPersonController>(); motor=GetComponent<CharacterController>(); }

    void Update()
    {
        if(controller==null) controller=GetComponent<OfficeThirdPersonController>();
        if(controller!=null && !controller.ControlEnabled) { CancelThrowCharge(); SetTarget(null); hadLockedCursor=false; return; }
        bool active=Application.isFocused && Cursor.lockState==CursorLockMode.Locked;
        if(active) RefreshTarget(); else SetTarget(null);
        if(active)
        {
            if(Keyboard.current!=null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                if(HeldProp!=null) DropHeld();
                else if(Target!=null) TryPickup(Target);
            }
            var mouse=Mouse.current;
            if(hadLockedCursor && mouse!=null && mouse.leftButton.wasPressedThisFrame && HeldProp!=null) BeginThrowCharge();
            if(IsChargingThrow && mouse!=null)
            {
                if(mouse.leftButton.isPressed) TickThrowCharge(Time.deltaTime);
                if(mouse.leftButton.wasReleasedThisFrame) ReleaseThrowCharge();
            }
        }
        else CancelThrowCharge();
        hadLockedCursor=active;
    }

    public void RefreshTarget() { SetTarget(HeldProp==null?FindTarget():null); }

    void SetTarget(OfficePushableProp target)
    {
        if(Target==target) return;
        highlight.Clear(); Target=target;
        if(Target!=null) highlight.Show(Target);
    }

    bool ClearPath(Vector3 origin,Vector3 point,OfficePushableProp prop)
    {
        var delta=point-origin;
        if(delta.sqrMagnitude<.0001f) return true;
        int count=gameObject.scene.GetPhysicsScene().Raycast(origin,delta.normalized,blockers,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            var hit=blockers[i];
            if(hit.transform.IsChildOf(transform) || hit.collider.attachedRigidbody==prop.Body) continue;
            if(hit.distance<delta.magnitude-.01f) return false;
        }
        return true;
    }

    public OfficePushableProp FindTarget()
    {
        if(View==null) return null;
        var ray=View.ViewportPointToRay(new Vector3(.5f,.5f,0));
        var physics=gameObject.scene.GetPhysicsScene();
        float range=Vector3.Distance(ray.origin,transform.position)+pickupReach;
        int count=physics.SphereCast(ray.origin,.12f,ray.direction,hits,range,~0,QueryTriggerInteraction.Ignore);
        OfficePushableProp best=null; float distance=float.PositiveInfinity;
        for(int i=0;i<count;i++)
        {
            var hit=hits[i];
            var prop=hit.collider.attachedRigidbody==null?null:hit.collider.attachedRigidbody.GetComponent<OfficePushableProp>();
            if(prop==null || !prop.CanPickUp || hit.distance>=distance) continue;
            var point=hit.collider.ClosestPoint(transform.position+Vector3.up*1.1f);
            if(Vector3.Distance(transform.position+Vector3.up*1.1f,point)>pickupReach ||
                !ClearPath(ray.origin,prop.WorldBounds.center,prop) || !ClearPath(transform.position+Vector3.up*1.1f,point,prop)) continue;
            best=prop; distance=hit.distance;
        }
        return best;
    }

    public bool TryPickup(OfficePushableProp prop)
    {
        if(controller==null) controller=GetComponent<OfficeThirdPersonController>();
        if(HeldProp!=null || prop==null || !prop.CanPickUp || prop.gameObject.scene!=gameObject.scene) return false;
        var bounds=prop.WorldBounds;
        var chest=transform.position+Vector3.up*1.1f;
        if(Vector3.Distance(chest,bounds.ClosestPoint(chest))>pickupReach || !ClearPath(chest,bounds.center,prop)) return false;
        var localBounds=CaptureLocalBounds(prop);
        localBoundsCenter=localBounds.center; localBoundsExtents=localBounds.extents;
        radius=Mathf.Max(bounds.extents.x,bounds.extents.y,bounds.extents.z)+.025f;
        heldRotation=Quaternion.Inverse(CarryYaw())*prop.transform.rotation;
        if(!prop.Grab(this)) return false;
        HeldProp=prop; SetTarget(null); BeginCarrySkinning();
        UpdateHeldPose();
        return true;
    }

    Quaternion ViewYaw()
    {
        var forward=View==null?transform.forward:View.transform.forward;
        forward.y=0;
        return Quaternion.LookRotation(forward.sqrMagnitude>.001f?forward.normalized:transform.forward);
    }

    Quaternion CarryYaw()
    {
        var forward=controller!=null && controller.visualRoot!=null?controller.visualRoot.forward:transform.forward;
        forward.y=0;
        return Quaternion.LookRotation(forward.sqrMagnitude>.001f?forward.normalized:transform.forward);
    }

    static Bounds CaptureLocalBounds(OfficePushableProp prop)
    {
        var bounds=new Bounds(); bool first=true;
        foreach(var filter in prop.GetComponentsInChildren<MeshFilter>())
        {
            if(filter.sharedMesh==null) continue;
            var mesh=filter.sharedMesh.bounds;
            for(int i=0;i<8;i++)
            {
                var corner=mesh.center+Vector3.Scale(mesh.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var point=prop.transform.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if(first) { bounds=new Bounds(point,Vector3.zero); first=false; } else bounds.Encapsulate(point);
            }
        }
        return first?new Bounds(prop.transform.InverseTransformPoint(prop.WorldBounds.center),Vector3.one*.1f):bounds;
    }

    void LateUpdate()
    {
        if(HeldProp!=null) UpdateHeldPose();
        if(throwPreview!=null)
        {
            if(IsChargingThrow && HeldProp!=null) throwPreview.Refresh(this);
            else throwPreview.Hide();
        }
    }

    public void UpdateHeldPose()
    {
        if(HeldProp==null) return;
        var yaw=CarryYaw(); var forward=yaw*Vector3.forward; var right=yaw*Vector3.right;
        var animator=controller==null?null:controller.humanoidAnimator;
        float height=1.1f;
        if(animator!=null && animator.isHuman)
        {
            var left=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var rightArm=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if(left!=null && rightArm!=null) height=(left.position.y+rightArm.position.y)*.5f-transform.position.y-.12f;
        }
        // Carry space belongs to the character, so orbiting or tilting the camera
        // cannot move the item beside the head or behind the hands.
        var anchor=transform.position+Vector3.up*height;
        var rotation=yaw*heldRotation;
        var extents=Vector3.Scale(localBoundsExtents,HeldProp.transform.lossyScale);
        float halfDepth=Mathf.Abs(Vector3.Dot(forward,rotation*Vector3.right))*extents.x+
            Mathf.Abs(Vector3.Dot(forward,rotation*Vector3.up))*extents.y+Mathf.Abs(Vector3.Dot(forward,rotation*Vector3.forward))*extents.z;
        // Bulky plants/chairs stay outside the torso while their near face
        // remains within reach of both hands.
        float carryDistance=Mathf.Max(.34f+radius*.25f,halfDepth+motor.radius+.06f);
        var desired=anchor+forward*carryDistance;
        var delta=desired-anchor;
        float distance=delta.magnitude;
        float carryProbe=Mathf.Clamp(radius*.5f,.04f,.28f);
        int count=gameObject.scene.GetPhysicsScene().SphereCast(anchor,carryProbe,delta.normalized,hits,distance,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
            if(!hits[i].transform.IsChildOf(transform) && hits[i].collider.attachedRigidbody!=HeldProp.Body)
                distance=Mathf.Min(distance,Mathf.Max(0,hits[i].distance-.035f));
        var center=anchor+delta.normalized*distance;
        var position=center-rotation*Vector3.Scale(localBoundsCenter,HeldProp.transform.lossyScale);
        HeldProp.transform.SetPositionAndRotation(position,rotation);
        HeldProp.Body.rotation=rotation;
        HeldProp.Body.position=position;
        ResolveHeldObstacles();
        center=HeldProp.transform.TransformPoint(localBoundsCenter);
        if(animator!=null && animator.isHuman)
        {
            var bounds=HeldProp.WorldBounds;
            float halfWidth=Mathf.Clamp(Vector3.Dot(bounds.extents,new Vector3(Mathf.Abs(right.x),Mathf.Abs(right.y),Mathf.Abs(right.z))),.045f,.30f);
            float gripDepth=halfDepth>.3f?Mathf.Max(0,halfDepth-.04f):Mathf.Min(radius*.25f,.08f);
            var gripCenter=center-forward*gripDepth-Vector3.up*Mathf.Min(bounds.extents.y*.35f,.12f);
            PoseHand(animator,true,gripCenter-right*halfWidth,-right-Vector3.up*.5f,forward+right*.45f);
            PoseHand(animator,false,gripCenter+right*halfWidth,right-Vector3.up*.5f,forward-right*.45f);
        }
    }

    static void PoseHand(Animator animator,bool left,Vector3 target,Vector3 hint,Vector3 fingersDirection)
    {
        var upper=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
        var lower=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
        var hand=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
        if(upper==null || lower==null || hand==null) return;
        OfficeBossNpc.Solve(upper,lower,hand,target,hint);
        var finger=animator.GetBoneTransform(left?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal);
        if(finger!=null) hand.rotation=Quaternion.FromToRotation(finger.position-hand.position,fingersDirection)*hand.rotation;
    }

    void BeginCarrySkinning()
    {
        RestoreCarrySkinning();
        if(controller==null || controller.visualRoot==null) return;
        carryRenderers=controller.visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
        previousSkinning=new bool[carryRenderers.Length];
        for(int i=0;i<carryRenderers.Length;i++)
        {
            previousSkinning[i]=carryRenderers[i].forceMatrixRecalculationPerRender;
            // LateUpdate IK must reach the skinning matrices before each render.
            carryRenderers[i].forceMatrixRecalculationPerRender=true;
        }
    }

    void RestoreCarrySkinning()
    {
        if(carryRenderers==null) return;
        for(int i=0;i<carryRenderers.Length;i++)
            if(carryRenderers[i]!=null) carryRenderers[i].forceMatrixRecalculationPerRender=previousSkinning[i];
        carryRenderers=null; previousSkinning=null;
    }

    void ResolveHeldObstacles()
    {
        var shapes=HeldProp.GetComponentsInChildren<Collider>();
        var physics=gameObject.scene.GetPhysicsScene();
        // A large prop can extend beyond the movement capsule next to a wall.
        // Resolve its actual shapes rather than letting a kinematic prop clip through it.
        for(int pass=0;pass<3;pass++)
        {
            bool corrected=false;
            var center=HeldProp.transform.TransformPoint(localBoundsCenter);
            int count=physics.OverlapSphere(center,radius*1.75f,overlaps,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var obstacle=overlaps[i];
                if(obstacle.transform.IsChildOf(transform) || obstacle.attachedRigidbody==HeldProp.Body) continue;
                foreach(var shape in shapes)
                {
                    if(!shape.enabled || shape.isTrigger) continue;
                    if(!Physics.ComputePenetration(shape,shape.transform.position,shape.transform.rotation,
                        obstacle,obstacle.transform.position,obstacle.transform.rotation,out var direction,out float distance)) continue;
                    var position=HeldProp.transform.position+direction*(distance+.003f);
                    HeldProp.transform.position=position; HeldProp.Body.position=position; corrected=true;
                }
            }
            if(!corrected) break;
        }
    }

    public bool BeginThrowCharge()
    {
        if(HeldProp==null) return false;
        IsChargingThrow=true; chargeElapsed=0;
        if(throwPreview==null) throwPreview=new OfficeThrowPreview(transform);
        throwPreview.Refresh(this);
        return true;
    }

    public void TickThrowCharge(float deltaTime)
    {
        if(!IsChargingThrow || HeldProp==null) return;
        chargeElapsed=Mathf.Min(Mathf.Max(.1f,throwChargeSeconds),chargeElapsed+Mathf.Max(0,deltaTime));
        if(throwPreview!=null) throwPreview.Refresh(this);
    }

    public bool ReleaseThrowCharge() { return IsChargingThrow && ThrowHeld(); }

    public void CancelThrowCharge()
    {
        IsChargingThrow=false; chargeElapsed=0;
        if(throwPreview!=null) throwPreview.Hide();
    }

    public Vector3 GetThrowVelocity()
    {
        if(HeldProp==null) return Vector3.zero;
        var forward=CarryYaw()*Vector3.forward;
        float cameraElevation=View==null?0:Mathf.Asin(Mathf.Clamp(View.transform.forward.y,-1,1))*Mathf.Rad2Deg;
        // Camera orbit never changes the character's throwing side. Elevation
        // stays positive so the actual flight and its preview share a rising arc.
        float elevation=Mathf.Clamp(42f+cameraElevation*.35f,32f,62f)*Mathf.Deg2Rad;
        var direction=forward*Mathf.Cos(elevation)+Vector3.up*Mathf.Sin(elevation);
        float speed=Mathf.Lerp(throwSpeed,Mathf.Max(throwSpeed,maximumThrowSpeed),ThrowCharge01);
        float inherited=Mathf.Clamp(Vector3.Dot(motor.velocity,forward),0,3)*.4f;
        return direction*speed+forward*inherited;
    }

    public int PredictThrow(Vector3[] points,out Vector3 impactPoint,out Vector3 impactNormal)
    {
        impactPoint=Vector3.zero; impactNormal=Vector3.up;
        if(HeldProp==null || points==null || points.Length<2) return 0;
        var position=HeldProp.WorldBounds.center;
        var velocity=GetThrowVelocity();
        var physics=gameObject.scene.GetPhysicsScene();
        float step=.02f, probeRadius=Mathf.Clamp(radius*.65f,.025f,.6f);
        points[0]=position;
        for(int i=1;i<points.Length;i++)
        {
            velocity=(velocity+Physics.gravity*step)*Mathf.Max(0,1-HeldProp.Body.linearDamping*step);
            var next=position+velocity*step;
            var delta=next-position; float nearest=delta.magnitude;
            bool collided=false;
            int count=physics.SphereCast(position,probeRadius,delta.normalized,hits,nearest,~0,QueryTriggerInteraction.Ignore);
            for(int j=0;j<count;j++)
            {
                var hit=hits[j];
                if(hit.distance>nearest || hit.transform.IsChildOf(transform) || hit.collider.attachedRigidbody==HeldProp.Body) continue;
                nearest=hit.distance; impactPoint=hit.point; impactNormal=hit.normal; collided=true;
            }
            if(collided) { points[i]=position+delta.normalized*nearest; return i+1; }
            points[i]=next; position=next;
        }
        impactPoint=position; impactNormal=Vector3.zero;
        return points.Length;
    }

    public bool ThrowHeld()
    {
        if(HeldProp==null) return false;
        var velocity=GetThrowVelocity();
        ReleaseHeld(velocity,Random.insideUnitSphere*3f);
        return true;
    }

    public void DropHeld() { ReleaseHeld(Vector3.ClampMagnitude(motor==null?Vector3.zero:motor.velocity,3),Vector3.zero); }

    void ReleaseHeld(Vector3 velocity,Vector3 spin)
    {
        CancelThrowCharge();
        if(HeldProp==null) return;
        var prop=HeldProp; HeldProp=null;
        prop.ReleaseFromPlayer(this,velocity,spin);
        RestoreCarrySkinning();
    }

    void OnDisable() { DropHeld(); RestoreCarrySkinning(); SetTarget(null); highlight.Clear(); hadLockedCursor=false; }
    void OnApplicationFocus(bool focused) { if(!focused) CancelThrowCharge(); }
    void OnDestroy() { if(throwPreview!=null) throwPreview.Dispose(); }

    void OnGUI()
    {
        if(Cursor.lockState!=CursorLockMode.Locked) return;
        OfficeThrowHud.Draw(HeldProp!=null,Target!=null,IsChargingThrow,ThrowCharge01);
    }
}
