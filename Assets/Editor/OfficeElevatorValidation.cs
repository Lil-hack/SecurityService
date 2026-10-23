using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class OfficeElevatorValidation
{
    [MenuItem("Tools/Office/Test Elevator Buttons And Safety")]
    public static void Run()
    {
        if(!Application.isPlaying)throw new Exception("Enter Play first.");Application.runInBackground=true;EditorApplication.isPaused=false;
        var lift=UnityEngine.Object.FindAnyObjectByType<OfficeElevatorDoors>();lift.SetOpen(false,true);
        var actor=new GameObject("Temporary elevator button user");var capsule=actor.AddComponent<CharacterController>();capsule.height=1.72f;capsule.center=Vector3.up*.86f;capsule.radius=.28f;
        void Position(Vector3 local){capsule.enabled=false;actor.transform.position=lift.transform.TransformPoint(local);capsule.enabled=true;Physics.SyncTransforms();}
        Position(new Vector3(0,0,4));if(lift.TryToggle(actor.transform))throw new Exception("Elevator usable out of range.");
        Position(new Vector3(.65f,.04f,.8f));if(!lift.TryToggle(actor.transform))throw new Exception("Outside button did not open elevator.");
        float began=Time.time;int stage=0;
        void Check()
        {
            if(!Application.isPlaying){EditorApplication.update-=Check;return;}
            try
            {
                if(stage==0 && Time.time-began>1.5f)
                {
                    if(lift.OpenFraction<.99f || lift.leftDoor.localPosition.x>-.9f || lift.rightDoor.localPosition.x<.9f)throw new Exception("Door panels did not slide fully open.");
                    OfficeElevatorSetup.Capture();Position(new Vector3(0,.04f,0));lift.SetOpen(false);began=Time.time;stage=1;
                }
                else if(stage==1 && Time.time-began>.5f)
                {
                    if(!lift.IsOpen || lift.OpenFraction<.9f)throw new Exception("Door safety did not reopen for player.");
                    Position(new Vector3(0,.04f,-1.3f));if(!lift.TryToggle(actor.transform))throw new Exception("Cabin button did not close elevator.");began=Time.time;stage=2;
                }
                else if(stage==2 && Time.time-began>1.5f)
                {
                    if(lift.IsOpen || lift.OpenFraction>.01f)throw new Exception("Cabin door did not close.");
                    var rb=GameObject.CreatePrimitive(PrimitiveType.Cube);rb.name="Temporary elevator prop obstruction";rb.transform.localScale=Vector3.one*.3f;rb.transform.position=lift.transform.TransformPoint(new Vector3(0,.8f,0));
                    var body=rb.AddComponent<Rigidbody>();body.useGravity=false;lift.SetOpen(true,true);lift.SetOpen(false);lift.TickDoors(.02f);
                    bool safe=lift.IsOpen;UnityEngine.Object.Destroy(rb);if(!safe)throw new Exception("Door safety did not detect loose prop.");
                    lift.SetOpen(false,true);EditorApplication.update-=Check;UnityEngine.Object.Destroy(actor);
                    Directory.CreateDirectory("OfficePreview");File.WriteAllText("OfficePreview/ElevatorValidation.txt","PASS exterior button opens sliding doors; far interaction blocked; player prevents closure; interior button closes; loose props reopen closing doors. Cabin is accessible; starts closed.");
                    Debug.Log("PASS elevator buttons, sliding panels, inside access and player/prop obstruction safety.");
                }
            }
            catch{EditorApplication.update-=Check;UnityEngine.Object.Destroy(actor);throw;}
        }
        EditorApplication.update+=Check;
    }
}

