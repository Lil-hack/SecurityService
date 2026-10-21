using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StudioPropValidation
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new Exception("Enter Play mode first.");
        var source=GameObject.Find("05 Open plan studio office");
        var test=SceneManager.CreateScene("Studio physics validation",new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        try
        {
            var copy=UnityEngine.Object.Instantiate(source);SceneManager.MoveGameObjectToScene(copy,test);
            foreach(var script in copy.GetComponentsInChildren<MonoBehaviour>())script.enabled=false;
            var physics=test.GetPhysicsScene();var props=copy.GetComponentsInChildren<OfficePushableProp>();
            for(int i=0;i<250;i++)physics.Simulate(.02f);
            foreach(var p in props)
                if(!float.IsFinite(p.transform.position.y) || p.WorldBounds.max.y<-.1f)throw new Exception("Object fell through room: "+p.name);
            copy.SetActive(false);
            var player=UnityEngine.Object.Instantiate(GameObject.FindGameObjectWithTag("Player"));SceneManager.MoveGameObjectToScene(player,test);
            foreach(var script in player.GetComponentsInChildren<MonoBehaviour>())script.enabled=false;
            var motor=player.GetComponent<CharacterController>();motor.enabled=false;player.transform.position=new Vector3(40,.03f,0);motor.enabled=true;
            var grabber=player.GetComponent<OfficePropGrabber>();var controller=player.GetComponent<OfficeThirdPersonController>();
            var cameraObject=new GameObject("Studio carry test camera");SceneManager.MoveGameObjectToScene(cameraObject,test);
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(40,1.7f,-2);controller.followCamera=camera;
            foreach(string name in new[]{"Desk coffee mug","Task chair","Studio potted plant","Storage books","Studio storage cabinet"})
            {
                var original=source.GetComponentsInChildren<OfficePushableProp>().First(p=>p.name==name);
                var item=UnityEngine.Object.Instantiate(original.gameObject);SceneManager.MoveGameObjectToScene(item,test);
                var prop=item.GetComponent<OfficePushableProp>();prop.enabled=true;
                item.transform.position+=new Vector3(40,1.1f,.8f)-prop.WorldBounds.center;
                camera.transform.LookAt(prop.WorldBounds.center);Physics.SyncTransforms();
                if(!grabber.TryPickup(prop))throw new Exception("Cannot pick up studio object: "+name);
                grabber.UpdateHeldPose();
                if(!grabber.ThrowHeld() || prop.Body.isKinematic || !prop.Body.useGravity || prop.Body.linearVelocity.magnitude<5)
                    throw new Exception("Studio object throw failed: "+name);
                item.SetActive(false);
            }
            string report="PASS: "+props.Length+" studio props simulated for five seconds without floor penetration; actual mug, rolling chair, plant, shelf books and cabinet picked up and thrown with the existing player controller.";
            File.WriteAllText("OfficePreview/Studio_Grab_Validation.txt",report);return report;
        }
        finally{SceneManager.UnloadSceneAsync(test);}
    }
}
