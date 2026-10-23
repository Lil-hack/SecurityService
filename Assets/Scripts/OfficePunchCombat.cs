using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(350),RequireComponent(typeof(OfficeThirdPersonController))]
public sealed class OfficePunchCombat : MonoBehaviour
{
    public const float PlaybackSpeed=1.85f;
    public AnimationClip jab,cross;
    public bool useImportedPose;
    [Range(0,.25f)] public float punchLift=.12f;
    [Range(0,.3f)] public float windupWidth=.16f;
    public bool IsPunching { get; private set; }
    public bool LastPunchWasLeft { get; private set; }
    OfficeThirdPersonController controller;
    OfficePropGrabber grabber;
    Animator animator;
    int layer=-1;
    bool nextLeft=true,queued,hadCursor;
    float remaining,weight;
    OfficePunchHitboxes hitboxes;
    void Awake(){controller=GetComponent<OfficeThirdPersonController>();grabber=GetComponent<OfficePropGrabber>();}
    bool Allowed => controller!=null && controller.ControlEnabled && !LaptopDesktop.SessionActive && (grabber==null || grabber.HeldProp==null);
    void Bind()
    {
        if(animator==controller.humanoidAnimator)return;
        if(animator!=null && layer>=0)animator.SetLayerWeight(layer,0);
        animator=controller.humanoidAnimator;layer=animator==null?-1:animator.GetLayerIndex("Punches");Cancel();nextLeft=true;
    }
    public bool TryPunch()
    {
        Bind();if(!Allowed || animator==null || layer<0 || jab==null || cross==null)return false;
        if(IsPunching){if(remaining<.22f)queued=true;return false;}
        LastPunchWasLeft=nextLeft;nextLeft=!nextLeft;
        var clip=LastPunchWasLeft?jab:cross;
        animator.CrossFadeInFixedTime(LastPunchWasLeft?"Punches.Jab":"Punches.Cross",.045f,layer,0);
        remaining=clip.length/PlaybackSpeed;IsPunching=true;queued=false;return true;
    }
    void Update()
    {
        Bind();
        bool active=Application.isFocused && Cursor.lockState==CursorLockMode.Locked && Allowed;
        if(!active){Cancel();hadCursor=false;return;}
        var mouse=Mouse.current;
        if(hadCursor && mouse!=null && mouse.leftButton.wasPressedThisFrame)TryPunch();
        if(IsPunching)
        {
            remaining-=Time.deltaTime;
            if(remaining<=0){IsPunching=false;if(queued || mouse!=null && mouse.leftButton.isPressed)TryPunch();}
        }
        weight=Mathf.MoveTowards(weight,IsPunching?1:0,Time.deltaTime*(IsPunching?16:10));
        if(animator!=null && layer>=0)animator.SetLayerWeight(layer,weight);
        hadCursor=true;
    }
    void LateUpdate()
    {
        if(!IsPunching || animator==null)return;
        float duration=(LastPunchWasLeft?jab:cross).length/PlaybackSpeed;
        ApplyPunchStyle(Mathf.Clamp01(1-remaining/duration));
        if(hitboxes==null){hitboxes=GetComponent<OfficePunchHitboxes>();if(hitboxes==null)hitboxes=gameObject.AddComponent<OfficePunchHitboxes>();}
        hitboxes.Sample(animator,LastPunchWasLeft,Mathf.Clamp01(1-remaining/duration));
    }
    public void ApplyPunchStyle(float phase)
    {
        if(useImportedPose)return;
        if(animator==null || !animator.isHuman || controller.visualRoot==null)return;
        var forward=controller.visualRoot.forward;var right=controller.visualRoot.right;
        float side=LastPunchWasLeft?-1:1;
        var upper=animator.GetBoneTransform(LastPunchWasLeft?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
        var lower=animator.GetBoneTransform(LastPunchWasLeft?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
        var hand=animator.GetBoneTransform(LastPunchWasLeft?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
        if(upper==null || lower==null || hand==null)return;
        // Preserve the ready-made jab/cross path: no sideways windup or hook.
        float reach=Vector3.Distance(upper.position,lower.position)+Vector3.Distance(lower.position,hand.position);
        var target=hand.position+Vector3.up*(punchLift*Mathf.Sin(Mathf.Clamp01(phase)*Mathf.PI));
        target=upper.position+Vector3.ClampMagnitude(target-upper.position,reach*.97f);
        var wrist=hand.rotation;
        var forearm=hand.position-lower.position;
        OfficeBossNpc.Solve(upper,lower,hand,target,-Vector3.up+right*side*.35f+forward*.1f);
        // Rotate the wrist with the forearm instead of retaining the old straight-punch
        // world orientation, which bent the hand unnaturally during the side swing.
        hand.rotation=Quaternion.FromToRotation(forearm,hand.position-lower.position)*wrist;
    }
    public void Cancel(){IsPunching=false;queued=false;remaining=0;weight=0;if(animator!=null && layer>=0)animator.SetLayerWeight(layer,0);if(hitboxes!=null)hitboxes.ResetSwing();}
    void OnDisable(){Cancel();hadCursor=false;}
}

