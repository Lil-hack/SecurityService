using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class OfficeElevatorDoors : MonoBehaviour
{
    public Transform leftDoor,rightDoor,outsideButton,insideButton,player;
    public TextMesh statusText;
    public Renderer outsideLamp,insideLamp;
    public float slideDistance=.90f,openingSpeed=1.15f,interactionDistance=1.35f;
    public bool IsOpen { get; private set; }
    public float OpenFraction { get; private set; }
    static readonly HashSet<OfficeElevatorDoors> instances=new HashSet<OfficeElevatorDoors>();
    Vector3 leftClosed,rightClosed;Rigidbody leftBody,rightBody;bool bound;
    readonly Collider[] occupants=new Collider[48];MaterialPropertyBlock block;
    void OnEnable(){instances.Add(this);}
    void OnDisable(){instances.Remove(this);}
    void Awake()
    {
        Bind();if(player==null){var p=GameObject.FindGameObjectWithTag("Player");if(p!=null)player=p.transform;}
        SetOpen(false,true);
    }
    void Bind()
    {
        if(bound)return;leftClosed=new Vector3(-.44f,1.24f,-.06f);rightClosed=new Vector3(.44f,1.24f,-.06f);
        leftBody=leftDoor.GetComponent<Rigidbody>();rightBody=rightDoor.GetComponent<Rigidbody>();bound=true;
    }
    public bool CanInteract(Transform actor,out float distance)
    {
        distance=float.PositiveInfinity;
        if(actor==null || !isActiveAndEnabled || actor.gameObject.scene!=gameObject.scene)return false;
        var controls=actor.GetComponent<OfficeThirdPersonController>();if(controls!=null && !controls.ControlEnabled)return false;
        var origin=actor.position+Vector3.up*1.1f;
        foreach(var button in new[]{outsideButton,insideButton})
        {
            if(button==null)continue;var delta=button.position-origin;float d=delta.magnitude;if(d>interactionDistance || d>=distance)continue;
            bool clear=true;
            foreach(var hit in Physics.RaycastAll(origin,delta.normalized,d,~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(actor) && hit.distance<d-.04f){clear=false;break;}
            if(clear)distance=d;
        }
        return distance<=interactionDistance;
    }
    public static bool ButtonIsCloser(Transform actor,Vector3 otherPoint)
    {
        if(actor==null)return false;float otherDistance=Vector3.Distance(actor.position+Vector3.up*1.1f,otherPoint);
        foreach(var lift in instances)if(lift!=null && lift.CanInteract(actor,out float distance) && distance<otherDistance)return true;
        return false;
    }
    public bool TryToggle(Transform actor)
    {
        if(!CanInteract(actor,out _))return false;SetOpen(!IsOpen);return true;
    }
    public void SetOpen(bool open,bool immediate=false)
    {
        Bind();IsOpen=open;
        if(immediate){OpenFraction=open?1:0;Place(true);}
        RefreshDisplay();
    }
    void Update()
    {
        if(Application.isFocused && Cursor.lockState==CursorLockMode.Locked && !LaptopDesktop.BlocksWorldUse && Keyboard.current!=null && Keyboard.current.eKey.wasPressedThisFrame)TryToggle(player);
    }
    void FixedUpdate(){TickDoors(Time.fixedDeltaTime);}
    public void TickDoors(float dt)
    {
        Bind();
        if(!IsOpen && OpenFraction>.01f && DoorwayOccupied()){SetOpen(true);}
        float next=Mathf.MoveTowards(OpenFraction,IsOpen?1:0,Mathf.Max(0,dt)*openingSpeed);
        if(next==OpenFraction)return;OpenFraction=next;Place(false);RefreshDisplay();
    }
    void Place(bool immediate)
    {
        var a=leftClosed+Vector3.left*(slideDistance*OpenFraction);var b=rightClosed+Vector3.right*(slideDistance*OpenFraction);
        if(Application.isPlaying && !immediate){leftBody.MovePosition(transform.TransformPoint(a));rightBody.MovePosition(transform.TransformPoint(b));}
        else {leftDoor.localPosition=a;rightDoor.localPosition=b;if(leftBody!=null)leftBody.position=leftDoor.position;if(rightBody!=null)rightBody.position=rightDoor.position;}
    }
    bool DoorwayOccupied()
    {
        int count=Physics.OverlapBoxNonAlloc(transform.TransformPoint(new Vector3(0,1.2f,-.03f)),new Vector3(.79f,1.14f,.27f),occupants,transform.rotation,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)if(!occupants[i].transform.IsChildOf(transform))return true;
        return false;
    }
    void RefreshDisplay()
    {
        if(statusText!=null)statusText.text=OpenFraction>.99f?"ДВЕРИ ОТКРЫТЫ":OpenFraction<.01f?"ДВЕРИ ЗАКРЫТЫ":IsOpen?"ОТКРЫВАЕТСЯ":"ЗАКРЫВАЕТСЯ";
        var color=IsOpen?new Color(.13f,.75f,.40f):new Color(.15f,.45f,.85f);
        if(block==null)block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);block.SetColor("_EmissionColor",color*.6f);
        if(outsideLamp!=null)outsideLamp.SetPropertyBlock(block);if(insideLamp!=null)insideLamp.SetPropertyBlock(block);
    }
    void OnGUI()
    {
        if(LaptopDesktop.BlocksWorldUse || !CanInteract(player,out _))return;
        GUI.Box(new Rect(Screen.width*.5f-170,Screen.height-105,340,30),IsOpen?"E — нажать кнопку: закрыть лифт":"E — нажать кнопку: открыть лифт");
    }
}

