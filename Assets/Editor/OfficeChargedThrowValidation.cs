using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeChargedThrowValidation
{
    public static string Run()
    {
        if(!Application.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        var source=SceneManager.GetActiveScene();
        var scene=SceneManager.CreateScene("Charged throw test",new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        try
        {
            var originalPlayer=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OfficeThirdPersonController>()).First();
            var player=UnityEngine.Object.Instantiate(originalPlayer.gameObject); SceneManager.MoveGameObjectToScene(player,scene);
            var controller=player.GetComponent<OfficeThirdPersonController>(); controller.enabled=false;
            var capsule=player.GetComponent<CharacterController>(); capsule.enabled=false;
            player.transform.position=new Vector3(20,.02f,0); player.transform.rotation=Quaternion.identity; capsule.enabled=true;
            controller.visualRoot.rotation=Quaternion.identity;
            controller.humanoidAnimator.Rebind(); controller.humanoidAnimator.Update(0);
            var grabber=player.GetComponent<OfficePropGrabber>(); grabber.enabled=false;
            var cameraObject=new GameObject("Charge test camera"); SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>(); controller.followCamera=camera;
            camera.transform.position=new Vector3(20,1.7f,-2); camera.transform.rotation=Quaternion.LookRotation(new Vector3(0,.25f,1));
            var floor=new GameObject("Charge test floor"); SceneManager.MoveGameObjectToScene(floor,scene);
            floor.transform.position=new Vector3(20,-.1f,10); floor.AddComponent<BoxCollider>().size=new Vector3(30,.2f,50);
            var original=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OfficePushableProp>()).First(p=>p.name=="Guest mug");
            var item=UnityEngine.Object.Instantiate(original.gameObject); SceneManager.MoveGameObjectToScene(item,scene);
            var prop=item.GetComponent<OfficePushableProp>();
            item.transform.position=new Vector3(20,1.1f,.6f); prop.Body.position=item.transform.position;
            prop.Body.linearVelocity=Vector3.zero; Physics.SyncTransforms();
            if(!grabber.TryPickup(prop)) throw new Exception("Cannot pick up charge test mug.");
            float weakSpeed=grabber.GetThrowVelocity().magnitude;
            if(!grabber.BeginThrowCharge() || !grabber.ThrowPreviewVisible) throw new Exception("Charge did not show trajectory.");
            grabber.TickThrowCharge(grabber.throwChargeSeconds*.5f);
            float middleSpeed=grabber.GetThrowVelocity().magnitude;
            if(Mathf.Abs(grabber.ThrowCharge01-.5f)>.001f) throw new Exception("Charge did not advance with held time.");
            grabber.TickThrowCharge(100);
            var chargedVelocity=grabber.GetThrowVelocity();
            if(grabber.ThrowCharge01!=1 || chargedVelocity.magnitude<middleSpeed || middleSpeed<=weakSpeed || grabber.HeldProp!=prop)
                throw new Exception("Full charge is not stronger, is uncapped, or auto-threw the item.");
            var points=new Vector3[241];
            foreach(float facing in new[]{0f,90f,180f,270f})
            {
                controller.visualRoot.rotation=Quaternion.Euler(0,facing,0);
                foreach(float cameraYaw in new[]{0f,90f,180f,270f})
                {
                    camera.transform.rotation=Quaternion.Euler(-15,cameraYaw,0);
                    var velocity=grabber.GetThrowVelocity(); var horizontal=velocity; horizontal.y=0;
                    if(Vector3.Dot(horizontal.normalized,controller.visualRoot.forward)<.999f || velocity.y<=0)
                        throw new Exception("Camera orbit allowed a sideways/backwards throw or removed the arc.");
                }
            }
            controller.visualRoot.rotation=Quaternion.identity;
            camera.transform.rotation=Quaternion.LookRotation(new Vector3(0,.25f,1));
            chargedVelocity=grabber.GetThrowVelocity();
            int count=grabber.PredictThrow(points,out var impact,out var normal);
            if(count<6 || normal.sqrMagnitude<.1f || impact.y>.1f) throw new Exception("Trajectory did not stop at the floor with an impact marker.");
            if(points.Take(count).Max(p=>p.y)<points[0].y+.5f) throw new Exception("Throw preview has no rising arc.");
            var centerBefore=prop.WorldBounds.center; var rootBefore=prop.Body.position;
            var predictedAtPointFive=points[10]-centerBefore;
            if(!grabber.ReleaseThrowCharge() || grabber.HeldProp!=null || grabber.IsChargingThrow || grabber.ThrowPreviewVisible || grabber.ThrowCharge01!=0)
                throw new Exception("Release did not throw and reset indicator.");
            if(Vector3.Distance(prop.Body.linearVelocity,chargedVelocity)>.001f) throw new Exception("Thrown velocity differs from preview velocity.");
            for(int i=0;i<10;i++) scene.GetPhysicsScene().Simulate(.02f);
            float error=Vector3.Distance(prop.Body.position-rootBefore,predictedAtPointFive);
            if(error>.15f) throw new Exception("Predicted free flight diverges from actual flight: "+error);
            item.transform.position=player.transform.position+new Vector3(0,1.1f,.6f); prop.Body.position=item.transform.position;
            prop.Body.linearVelocity=Vector3.zero; Physics.SyncTransforms();
            if(!grabber.TryPickup(prop)) throw new Exception("Cannot re-pick charged throw.");
            grabber.BeginThrowCharge(); grabber.TickThrowCharge(.5f); grabber.CancelThrowCharge();
            if(grabber.IsChargingThrow || grabber.ThrowPreviewVisible || grabber.HeldProp!=prop || !prop.Body.isKinematic)
                throw new Exception("Cancelling aim threw or dropped the object.");
            grabber.BeginThrowCharge(); grabber.TickThrowCharge(.5f); grabber.DropHeld();
            if(grabber.ThrowPreviewVisible || grabber.IsChargingThrow || prop.Body.isKinematic || prop.Body.linearVelocity.magnitude>.1f)
                throw new Exception("Drop during charge launched the object or left an indicator.");
            if(grabber.ReleaseThrowCharge()) throw new Exception("Cancelled mouse release threw again.");
            string report="CHARGED_THROW_PASS: weak "+weakSpeed.ToString("F1")+", half "+middleSpeed.ToString("F1")+", full "+chargedVelocity.magnitude.ToString("F1")+
                " m/s; forward-only across 16 character/camera headings, rising physical arc; full charge holds until release, preview and floor ring valid; 0.20 s flight prediction error "+error.ToString("F3")+" m; release/cancel/drop clean up charge and preview.";
            Directory.CreateDirectory("OfficePreview"); File.WriteAllText("OfficePreview/ChargedThrow_Validation.txt",report);
            return report;
        }
        finally { SceneManager.UnloadSceneAsync(scene); }
    }
}
