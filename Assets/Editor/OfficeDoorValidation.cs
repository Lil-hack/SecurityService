using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OfficeDoorValidation
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Run validation outside Play mode.");
        var door=UnityEngine.Object.FindAnyObjectByType<OfficeDoor>();var actor=door.player;
        var home=actor.position;var motor=actor.GetComponent<CharacterController>();bool enabled=motor.enabled;
        try
        {
            Place(motor,new Vector3(5.2f,.05f,-.55f));door.SetOpen(false,true);Physics.SyncTransforms();
            for(int i=0;i<30;i++)motor.Move(new Vector3(.1f,-.01f,0));
            if(actor.position.x>6.05f)throw new Exception("Closed door did not block the player.");
            Place(motor,new Vector3(5.2f,.05f,-.55f));
            if(!door.TryToggle(actor))throw new Exception("Cannot open from boss office.");
            Animate(door);
            if(!door.IsOpen || door.IsMoving)throw new Exception("Door did not finish opening.");
            Physics.SyncTransforms();for(int i=0;i<30;i++)motor.Move(new Vector3(.1f,-.01f,0));
            if(actor.position.x<7.8f)throw new Exception("Open door still blocks doorway.");
            if(!door.TryToggle(actor))throw new Exception("Cannot close from studio office.");
            Animate(door);
            if(door.IsOpen || door.IsMoving)throw new Exception("Door did not finish closing.");
            door.SetOpen(true,true);Place(motor,new Vector3(6.1f,.05f,-.55f));door.SetOpen(false);Animate(door);
            if(!door.IsOpen)throw new Exception("Door did not reverse when the player blocked closing.");
            string report="PASS: initially closed; blocks player; opens and closes from both rooms; open doorway traversable; reverses on player obstruction.";
            File.WriteAllText("OfficePreview/Door_Validation.txt",report);return report;
        }
        finally
        {
            Place(motor,home);motor.enabled=enabled;door.SetOpen(false,true);Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(actor.gameObject.scene);EditorSceneManager.SaveScene(actor.gameObject.scene);
        }
    }
    static void Place(CharacterController c,Vector3 p){c.enabled=false;c.transform.position=p;c.enabled=true;Physics.SyncTransforms();}
    static void Animate(OfficeDoor d){for(int i=0;i<100;i++){d.TickDoor(.02f);Physics.SyncTransforms();}}
}
