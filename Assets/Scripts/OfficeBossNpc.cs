using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(200), RequireComponent(typeof(Animator))]
public class OfficeBossNpc : MonoBehaviour
{
    public enum Action { Typing, Writing, Reading, DrinkCoffee, CheckWatch, AdjustTie, StandUp, SitDown, Walk, Turn, Talk, Nod, Shrug, Wave, Surprised, Angry, PointDoor, Idle }
    public Transform player, seatAnchor, walkAnchor, keyboardTarget, paperTarget, doorTarget;
    public Transform standingAnchor;
    public bool startStanding = true;
    public float initialStandingSeconds = 2;
    public Transform mug, document, pen, chair;
    public float attentionDistance = 2.8f;
    public bool automaticRoutine = true;
    [HideInInspector] public float seatedPose = 1, poseClock;
    [HideInInspector] public float typing, writing, reading, drinking, watch, tie, talking, nodding, shrugging, waving, surprise, anger, pointing;
    public Action CurrentAction { get; private set; }
    public bool IsSeated { get; private set; } = true;
    public int GreetingCount { get; private set; }
    Animator animator;
    Transform hips, head, chestBone, leftShoulder, rightShoulder, la, ra, le, re, lh, rh, lt, rt, lk, rk, lf, rf;
    Vector3 gaze, mugHome, paperHome, chairHome, moveFrom, moveTo;
    Quaternion mugRotation, paperRotation, moveRotation;
    float elapsed, duration, chairShift;
    int routineIndex, conversationIndex = -1, reactionIndex, startupPhase = -1;
    bool initialized, noticed;
    static readonly Action[] Routine = { Action.Typing, Action.Writing, Action.Reading, Action.DrinkCoffee, Action.CheckWatch, Action.AdjustTie,
        Action.StandUp, Action.Walk, Action.Turn, Action.Idle, Action.Walk, Action.Turn, Action.SitDown };
    static readonly float[] Times = { 12, 8, 8, 7, 4, 4, 2.5f, 4.5f, 1.5f, 4, 4.5f, 1.5f, 2.5f };
    static readonly Action[] Conversation = { Action.Wave, Action.Talk, Action.Nod, Action.Shrug };
    static readonly Action[] Reactions = { Action.Surprised, Action.Angry, Action.PointDoor, Action.Nod, Action.Shrug, Action.Talk };

    void Awake() { Initialize(); }
    void Start() { if (automaticRoutine) ResetRoutine(false, startStanding); }
    Transform B(HumanBodyBones bone) { return animator.GetBoneTransform(bone); }
    public void Initialize()
    {
        if (initialized) return;
        animator = GetComponent<Animator>(); if (!animator.isHuman) return;
        hips=B(HumanBodyBones.Hips); head=B(HumanBodyBones.Head); chestBone=B(HumanBodyBones.Chest);
        leftShoulder=B(HumanBodyBones.LeftShoulder); rightShoulder=B(HumanBodyBones.RightShoulder);
        la=B(HumanBodyBones.LeftUpperArm); ra=B(HumanBodyBones.RightUpperArm); le=B(HumanBodyBones.LeftLowerArm); re=B(HumanBodyBones.RightLowerArm);
        lh=B(HumanBodyBones.LeftHand); rh=B(HumanBodyBones.RightHand);
        lt=B(HumanBodyBones.LeftUpperLeg); rt=B(HumanBodyBones.RightUpperLeg); lk=B(HumanBodyBones.LeftLowerLeg); rk=B(HumanBodyBones.RightLowerLeg);
        lf=B(HumanBodyBones.LeftFoot); rf=B(HumanBodyBones.RightFoot);
        if (player == null) { var p=GameObject.FindGameObjectWithTag("Player"); if(p!=null) player=p.transform; }
        if(mug!=null) { mugHome=mug.position; mugRotation=mug.rotation; }
        if(document!=null) { paperHome=document.position; paperRotation=document.rotation; }
        if(chair!=null) chairHome=chair.position;
        gaze=head.position+transform.forward*3; initialized=true;
    }
    void Update() { TickNpc(Time.deltaTime,true); }
    public void TickNpc(float dt,bool readInput=false)
    {
        Initialize(); if(!initialized || !automaticRoutine) return;
        bool near=PlayerNearby();
        if(!near && player!=null && Vector3.Distance(player.position,transform.position)>attentionDistance+1) noticed=false;
        if(near && !noticed && conversationIndex<0 && IsSeated && CurrentAction!=Action.StandUp && CurrentAction!=Action.SitDown && drinking<.05f && reading<.05f)
        { noticed=true; GreetingCount++; conversationIndex=0; PlayAction(Action.Wave,3); }
        if(startupPhase<0 && readInput && near && !LaptopDesktop.BlocksWorldUse && Keyboard.current!=null && Keyboard.current.eKey.wasPressedThisFrame)
        { conversationIndex=Conversation.Length; PlayAction(Reactions[reactionIndex++%Reactions.Length],4); }
        elapsed+=dt;
        if(CurrentAction==Action.Walk)
        {
            transform.position=Vector3.Lerp(moveFrom,moveTo,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/duration)));
            Vector3 d=moveTo-moveFrom; d.y=0;
            if(d.sqrMagnitude>.001f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(d),1-Mathf.Exp(-7*dt));
        }
        else if(CurrentAction==Action.Turn && seatAnchor!=null)
        {
            var target=routineIndex==8?Quaternion.LookRotation(new Vector3(-.3f,0,-1)):seatAnchor.rotation;
            transform.rotation=Quaternion.Slerp(moveRotation,target,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/duration)));
        }
        if(elapsed<duration) return;
        if(startupPhase>=0) { AdvanceStartup(); return; }
        if(CurrentAction==Action.StandUp) IsSeated=false;
        if(CurrentAction==Action.SitDown) IsSeated=true;
        if(conversationIndex>=0)
        {
            conversationIndex++;
            if(conversationIndex<Conversation.Length) PlayAction(Conversation[conversationIndex],conversationIndex==1?5:3);
            else { conversationIndex=-1; BeginRoutine(IsSeated?0:9); }
        }
        else BeginRoutine((routineIndex+1)%Routine.Length);
    }
    void BeginStartup()
    {
        IsSeated=false; seatedPose=0; startupPhase=0;
        if(standingAnchor!=null) transform.SetPositionAndRotation(standingAnchor.position,standingAnchor.rotation);
        else if(seatAnchor!=null) transform.SetPositionAndRotation(seatAnchor.position+seatAnchor.right*1.25f,seatAnchor.rotation);
        PlayAction(Action.Idle,initialStandingSeconds,false);
    }
    void AdvanceStartup()
    {
        startupPhase++;
        switch(startupPhase)
        {
            case 1:
                moveFrom=transform.position; moveTo=seatAnchor!=null?seatAnchor.position:moveFrom;
                PlayAction(Action.Walk,Mathf.Max(2.4f,Vector3.Distance(moveFrom,moveTo)/1.1f));
                break;
            case 2: PlayAction(Action.Turn,1); break;
            case 3: PlayAction(Action.SitDown,2.5f); break;
            default:
                IsSeated=true; startupPhase=-1; BeginRoutine(0);
                break;
        }
    }
    bool PlayerNearby()
    {
        if(player==null) return false;
        var d=player.position-transform.position; d.y=0;
        return d.sqrMagnitude<attentionDistance*attentionDistance && Vector3.Angle(transform.forward,d)<100;
    }
    void BeginRoutine(int index,bool blend=true)
    {
        routineIndex=index;
        if(index==0 && seatAnchor!=null && IsSeated) transform.SetPositionAndRotation(seatAnchor.position,seatAnchor.rotation);
        PlayAction(Routine[index],Times[index],blend);
        if(CurrentAction==Action.Walk) { moveFrom=transform.position; moveTo=index==7 && walkAnchor!=null?walkAnchor.position:seatAnchor.position; }
    }
    public void PlayAction(Action action,float seconds=4,bool blend=true)
    {
        Initialize(); CurrentAction=action; elapsed=0; duration=Mathf.Max(.1f,seconds); moveRotation=transform.rotation;
        if(blend) animator.CrossFadeInFixedTime(action.ToString(),.35f); else animator.Play(action.ToString(),0,0);
    }
    public void ResetRoutine(bool restoreProps=true,bool fromStanding=false)
    {
        if(restoreProps) RestoreProps();
        initialized=false; IsSeated=true; noticed=false; conversationIndex=-1; routineIndex=0; GreetingCount=0; chairShift=0; startupPhase=-1;
        Initialize(); if(fromStanding) BeginStartup(); else BeginRoutine(0,false);
    }
    [ContextMenu("Preview/Greet player")] void Greet() { conversationIndex=0; PlayAction(Action.Wave,3); }
    [ContextMenu("Preview/Surprised")] void Surprise() { conversationIndex=4; PlayAction(Action.Surprised,4); }
    [ContextMenu("Preview/Angry")] void Angry() { conversationIndex=4; PlayAction(Action.Angry,4); }
    [ContextMenu("Preview/Point at door")] void PointDoor() { conversationIndex=4; PlayAction(Action.PointDoor,4); }
    void OnAnimatorIK(int layerIndex)
    {
        if(!initialized) return;
        var target=head.position+transform.forward*3;
        if(Mathf.Max(typing,writing,reading,drinking,watch,tie)>.1f && keyboardTarget!=null) target=keyboardTarget.position+Vector3.up*.15f;
        if(PlayerNearby() && conversationIndex>=0) target=player.position+Vector3.up*1.55f;
        if(pointing>.3f && doorTarget!=null) target=doorTarget.position+Vector3.up*1.5f;
        if(watch>.2f) target=lh.position;
        gaze=Vector3.Lerp(gaze,target,1-Mathf.Exp(-3*Time.deltaTime));
        animator.SetLookAtWeight(.65f,.05f,.6f,.3f,.65f); animator.SetLookAtPosition(gaze);
    }
    void LateUpdate() { ApplyInteractionPose(); }
    public void ApplyInteractionPose()
    {
        Initialize(); if(!initialized) return;
        float sit=CurrentAction==Action.SitDown || CurrentAction==Action.StandUp?seatedPose:(IsSeated?1:0);
        var lfr=lf.rotation; var rfr=rf.rotation;
        hips.position-=Vector3.up*(.06f*sit);
        if(sit>.001f)
        {
            Solve(lt,lk,lf,Vector3.Lerp(lf.position,FootTarget(-1),sit),transform.forward);
            Solve(rt,rk,rf,Vector3.Lerp(rf.position,FootTarget(1),sit),transform.forward);
            lf.rotation=lfr; rf.rotation=rfr;
        }
        chestBone.rotation=Quaternion.AngleAxis(typing*9+writing*10+reading*4+anger*6,transform.right)*chestBone.rotation;
        // Protract the shoulders for close body gestures: the stock human reach
        // otherwise ends inside this character's much deeper chest.
        if(rightShoulder!=null) rightShoulder.position+=(transform.forward*.09f-transform.right*.07f)*Mathf.Max(tie,watch);
        if(leftShoulder!=null) leftShoulder.position+=(transform.forward*.07f+transform.right*.04f)*watch;
        var chest=(la.position+ra.position)*.5f;
        var left=lh.position; var right=rh.position; float lw=0,rw=0,time=poseClock;
        if(keyboardTarget!=null)
        {
            var key=keyboardTarget.position;
            Target(ref left,ref lw,key-transform.right*.16f+Vector3.up*(.018f*Mathf.Sin(time*19)),typing);
            Target(ref right,ref rw,key+transform.right*.16f+Vector3.up*(.018f*Mathf.Cos(time*21)),typing);
        }
        if(paperTarget!=null)
        {
            Target(ref left,ref lw,keyboardTarget.position-transform.right*.20f+Vector3.up*.01f,writing);
            Target(ref right,ref rw,paperTarget.position+transform.right*(.05f+.045f*Mathf.Sin(time*5))+transform.forward*(.025f*Mathf.Cos(time*7))+Vector3.up*.03f,writing);
        }
        var held=chest+transform.forward*.40f+Vector3.up*.01f;
        Target(ref left,ref lw,held-transform.right*.15f,reading); Target(ref right,ref rw,held+transform.right*.15f,reading);
        float lift=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,2.4f,time))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.2f,5.8f,time)));
        var cupDesk=mugHome+Vector3.up*.09f-transform.right*.035f;
        var cupMouth=head.position+transform.forward*.38f-Vector3.up*.05f-transform.right*.20f;
        Target(ref left,ref lw,Vector3.Lerp(cupDesk,cupMouth,lift),drinking);
        Target(ref left,ref lw,chest+transform.forward*.37f-transform.right*.14f-Vector3.up*.10f,watch);
        Target(ref right,ref rw,chest+transform.forward*.36f-transform.right*.04f-Vector3.up*.08f,watch);
        Target(ref right,ref rw,chest+transform.forward*.37f+Vector3.up*(.03f+Mathf.Sin(time*5)*.025f),tie);
        Target(ref left,ref lw,chest-transform.right*.3f+transform.forward*.32f-Vector3.up*(.12f+Mathf.Sin(time*3)*.045f),talking);
        Target(ref right,ref rw,chest+transform.right*.3f+transform.forward*.32f-Vector3.up*(.12f+Mathf.Cos(time*3)*.045f),talking);
        Target(ref right,ref rw,head.position+transform.right*(.38f+.06f*Mathf.Sin(time*9))+transform.forward*.13f,waving);
        Target(ref left,ref lw,chest-transform.right*.45f+transform.forward*.27f-Vector3.up*.03f,shrugging);
        Target(ref right,ref rw,chest+transform.right*.45f+transform.forward*.27f-Vector3.up*.03f,shrugging);
        if(leftShoulder!=null) leftShoulder.position+=Vector3.up*(.035f*shrugging);
        if(rightShoulder!=null) rightShoulder.position+=Vector3.up*(.035f*shrugging);
        Target(ref left,ref lw,head.position-transform.right*.42f+transform.forward*.3f-Vector3.up*.08f,surprise);
        Target(ref right,ref rw,head.position+transform.right*.42f+transform.forward*.3f-Vector3.up*.08f,surprise);
        Target(ref right,ref rw,chest+transform.right*.3f+transform.forward*.27f-Vector3.up*(.1f+Mathf.Sin(time*9)*.025f),anger);
        if(doorTarget!=null) Target(ref left,ref lw,la.position+(doorTarget.position+Vector3.up*1.5f-la.position).normalized*.42f,pointing);
        if(lw>0) Solve(la,le,lh,left,-transform.right-Vector3.up*.6f);
        if(rw>0) Solve(ra,re,rh,right,transform.right-Vector3.up*.6f);
        float flat=Mathf.Max(typing,writing);
        if(flat>.01f) { AimFingers(lh,HumanBodyBones.LeftMiddleProximal,flat); AimFingers(rh,HumanBodyBones.RightMiddleProximal,flat); }
        head.rotation=Quaternion.AngleAxis(Mathf.Sin(time*6)*9*nodding-8*anger-5*surprise,transform.right)*head.rotation;
        UpdateProps(lift);
        if(chair!=null) { chairShift=Mathf.Lerp(chairShift,sit<.2f?.38f:0,.12f); chair.position=chairHome-(seatAnchor!=null?seatAnchor.forward:transform.forward)*chairShift; }
    }
    Vector3 FootTarget(float side) { return transform.position+transform.right*(.2f*side)+transform.forward*.40f+Vector3.up*.27f; }
    static void Target(ref Vector3 p,ref float total,Vector3 target,float weight) { weight=Mathf.Clamp01(weight); p=Vector3.Lerp(p,target,weight); total=Mathf.Max(total,weight); }
    public static void Solve(Transform upper,Transform lower,Transform end,Vector3 target,Vector3 hint)
    {
        var origin=upper.position; float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,end.position);
        var d=target-origin; float distance=Mathf.Clamp(d.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f); d=d.normalized;
        var bend=Vector3.ProjectOnPlane(hint,d).normalized;
        if(bend.sqrMagnitude<.001f) bend=Vector3.ProjectOnPlane(Vector3.forward,d).normalized;
        float along=(a*a-b*b+distance*distance)/(2*distance);
        var elbow=origin+d*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(end.position-lower.position,origin+d*distance-lower.position)*lower.rotation;
    }
    void AimFingers(Transform hand,HumanBodyBones finger,float weight)
    {
        var tip=B(finger); if(tip!=null) hand.rotation=Quaternion.Slerp(hand.rotation,Quaternion.FromToRotation(tip.position-hand.position,transform.forward)*hand.rotation,weight);
    }
    void UpdateProps(float lift)
    {
        if(mug!=null)
        {
            float grip=drinking*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,.9f,poseClock))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(5.8f,6.5f,poseClock)));
            PoseProp(mug,mugHome,mugRotation,lh.position+transform.right*.04f+transform.forward*(.08f*lift)-Vector3.up*.09f,
                Quaternion.AngleAxis(-18*lift,transform.right)*mugRotation,grip);
        }
        if(document!=null)
        {
            PoseProp(document,paperHome,paperRotation,(lh.position+rh.position)*.5f+transform.forward*.035f,
                transform.rotation*Quaternion.Euler(-38,0,0),reading);
        }
        if(pen!=null) { pen.gameObject.SetActive(writing>.05f); pen.position=rh.position+transform.forward*.055f-Vector3.up*.015f; pen.rotation=transform.rotation*Quaternion.Euler(0,0,-25); }
    }
    static void PoseProp(Transform prop,Vector3 home,Quaternion homeRotation,Vector3 target,Quaternion rotation,float weight)
    {
        var physical=prop.GetComponent<OfficePushableProp>();
        if(Application.isPlaying && physical!=null) physical.PoseForNpc(home,homeRotation,target,rotation,weight);
        else prop.SetPositionAndRotation(Vector3.Lerp(home,target,weight),Quaternion.Slerp(homeRotation,rotation,weight));
    }
    void OnGUI() { if(PlayerNearby() && !LaptopDesktop.BlocksWorldUse) GUI.Label(new Rect(Screen.width*.5f-130,Screen.height-65,340,30),"E — реакция босса / продолжить разговор"); }
    void OnDisable() { RestoreProps(); }
    void RestoreProps()
    {
        if(!initialized) return;
        RestoreProp(mug,mugHome,mugRotation);
        RestoreProp(document,paperHome,paperRotation);
        if(chair!=null) chair.position=chairHome;
        if(pen!=null) pen.gameObject.SetActive(false);
    }
    static void RestoreProp(Transform prop,Vector3 home,Quaternion rotation)
    {
        if(prop==null) return;
        var physical=prop.GetComponent<OfficePushableProp>();
        if(Application.isPlaying && physical!=null) physical.Release();
        else prop.SetPositionAndRotation(home,rotation);
    }
}
