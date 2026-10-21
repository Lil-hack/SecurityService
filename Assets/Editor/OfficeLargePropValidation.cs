using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeLargePropValidation
{
    public static string Run()
    {
        if(!Application.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        var source=SceneManager.GetActiveScene();
        var scene=SceneManager.CreateScene("Large prop pickup test",new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        try
        {
            var originalPlayer=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OfficeThirdPersonController>()).First();
            var player=UnityEngine.Object.Instantiate(originalPlayer.gameObject); SceneManager.MoveGameObjectToScene(player,scene);
            var controller=player.GetComponent<OfficeThirdPersonController>(); controller.enabled=false; controller.followCamera=null;
            var capsule=player.GetComponent<CharacterController>(); capsule.enabled=false;
            player.transform.position=new Vector3(20,.03f,0); player.transform.rotation=Quaternion.identity; capsule.enabled=true;
            controller.visualRoot.rotation=Quaternion.identity;
            var grabber=player.GetComponent<OfficePropGrabber>(); grabber.enabled=false;
            if(grabber.throwChargeSeconds<3) throw new Exception("Scene still uses the old fast charge.");
            var floor=new GameObject("Large pickup floor"); SceneManager.MoveGameObjectToScene(floor,scene);
            floor.transform.position=new Vector3(20,-.1f,4); floor.AddComponent<BoxCollider>().size=new Vector3(20,.2f,20);
            var report=new List<string>();
            var originals=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OfficePushableProp>()).ToArray();
            foreach(string name in new[]{"SM_Prop_ArmChair_01","SM_Prop_Plant_Palm_01","Front corner floor plant","Rear right corner floor plant"})
            {
                var original=originals.First(p=>p.name==name);
                var item=UnityEngine.Object.Instantiate(original.gameObject); SceneManager.MoveGameObjectToScene(item,scene);
                var prop=item.GetComponent<OfficePushableProp>();
                prop.Body.interpolation=RigidbodyInterpolation.None;
                var bounds=prop.WorldBounds;
                item.transform.position+=new Vector3(20-bounds.center.x,.01f-bounds.min.y,1.05f-bounds.center.z);
                prop.Body.position=item.transform.position; prop.Body.linearVelocity=prop.Body.angularVelocity=Vector3.zero;
                controller.humanoidAnimator.Rebind(); controller.humanoidAnimator.Update(0); Physics.SyncTransforms();
                if(!prop.CanPickUp || !grabber.TryPickup(prop)) throw new Exception("Freestanding prop cannot be picked up: "+name+
                    "; eligible="+prop.CanPickUp+"; center="+prop.WorldBounds.center+"; player="+player.transform.position);
                grabber.UpdateHeldPose();
                if(!prop.Body.isKinematic || Vector3.Dot(prop.WorldBounds.center-player.transform.position,controller.visualRoot.forward)<.25f)
                    throw new Exception("Large prop did not stay in front of the player: "+name);
                grabber.BeginThrowCharge(); grabber.TickThrowCharge(1.3f);
                if(grabber.ThrowCharge01>=.5f || grabber.HeldProp!=prop) throw new Exception("Charge still fills too quickly.");
                grabber.TickThrowCharge(1.7f);
                var velocity=grabber.GetThrowVelocity();
                if(velocity.y/new Vector2(velocity.x,velocity.z).magnitude<.62f) throw new Exception("Throw is not sufficiently lofted.");
                if(!grabber.ReleaseThrowCharge() || prop.Body.isKinematic || !prop.Body.useGravity) throw new Exception("Large prop did not throw: "+name);
                var start=prop.Body.position;
                for(int i=0;i<20;i++) scene.GetPhysicsScene().Simulate(.02f);
                float distance=Vector3.Distance(start,prop.Body.position);
                if(distance<1f) throw new Exception("Large thrown prop did not fly: "+name);
                report.Add(name+": "+distance.ToString("F2")+" m in 0.4 s");
                item.SetActive(false);
            }
            string result="LARGE_PROP_PASS: "+string.Join("; ",report)+". Charge takes 3 seconds, remains below half after 1.3 s; lofted forward throws restore gravity.";
            Directory.CreateDirectory("OfficePreview"); File.WriteAllText("OfficePreview/LargeProp_Validation.txt",result);
            return result;
        }
        finally { SceneManager.UnloadSceneAsync(scene); }
    }
}
