using UnityEngine;
using UnityEngine.InputSystem;

public sealed class OfficeDoor : MonoBehaviour
{
    public Transform player;
    public BoxCollider leaf;
    public float interactionDistance=2.2f, degreesPerSecond=115;
    public bool startOpen;
    public float openAngle;
    public bool IsOpen { get; private set; }
    public bool IsMoving => Mathf.Abs(angle-(IsOpen?openAngle:90))>.05f;
    float angle=90;
    Rigidbody body;
    readonly Collider[] overlaps=new Collider[32];
    Vector3 InteractionPoint => transform.position+new Vector3(0,1.1f,-.65f);

    void Awake()
    {
        body=GetComponent<Rigidbody>();
        if(player==null){var p=GameObject.FindGameObjectWithTag("Player");if(p!=null)player=p.transform;}
        SetOpen(startOpen,true);
    }
    public bool CanInteract(Transform actor)
    {
        return actor!=null && actor.gameObject.scene==gameObject.scene &&
            Vector3.Distance(actor.position+Vector3.up*1.1f,InteractionPoint)<=interactionDistance;
    }
    public bool TryToggle(Transform actor)
    {
        if(!CanInteract(actor))return false;
        if(OfficeElevatorDoors.ButtonIsCloser(actor,InteractionPoint))return false;
        SetOpen(!IsOpen);return true;
    }
    public void SetOpen(bool open,bool immediate=false)
    {
        IsOpen=open;
        if(!immediate)return;
        angle=open?openAngle:90;
        transform.localRotation=Quaternion.Euler(0,angle,0);
        if(body==null)body=GetComponent<Rigidbody>();
        if(body!=null)body.rotation=transform.rotation;
    }
    void Update()
    {
        if(Application.isFocused && !LaptopDesktop.BlocksWorldUse && Cursor.lockState==CursorLockMode.Locked && Keyboard.current!=null && Keyboard.current.eKey.wasPressedThisFrame)
            TryToggle(player);
    }
    void FixedUpdate(){TickDoor(Time.fixedDeltaTime);}
    public void TickDoor(float dt)
    {
        float next=Mathf.MoveTowards(angle,IsOpen?openAngle:90,degreesPerSecond*Mathf.Max(0,dt));
        if(Mathf.Approximately(next,angle))return;
        // Reverse rather than pushing the player or a movable object into the jamb.
        if(Obstructed(next)){IsOpen=!IsOpen;return;}
        angle=next;
        var local=Quaternion.Euler(0,angle,0);
        if(body!=null && Application.isPlaying)body.MoveRotation(transform.parent==null?local:transform.parent.rotation*local);
        else transform.localRotation=local;
    }
    bool Obstructed(float next)
    {
        if(leaf==null)return false;
        var local=Quaternion.Euler(0,next,0);
        var world=transform.parent==null?local:transform.parent.rotation*local;
        var centre=transform.position+world*(leaf.transform.localPosition+Vector3.Scale(leaf.center,leaf.transform.localScale));
        var half=Vector3.Scale(leaf.size,leaf.transform.lossyScale)*.48f;
        int count=Physics.OverlapBoxNonAlloc(centre,half,overlaps,world*leaf.transform.localRotation,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            var hit=overlaps[i];if(hit.transform.IsChildOf(transform))continue;
            if(hit is CharacterController || (hit.attachedRigidbody!=null && !hit.attachedRigidbody.isKinematic))return true;
        }
        return false;
    }
    void OnGUI()
    {
        if(!CanInteract(player) || LaptopDesktop.BlocksWorldUse || OfficeElevatorDoors.ButtonIsCloser(player,InteractionPoint))return;
        var rect=new Rect(Screen.width*.5f-145,Screen.height-105,290,30);
        GUI.Box(rect,IsOpen?"E — закрыть дверь":"E — открыть дверь");
    }
}
