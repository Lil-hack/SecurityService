using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeGrabValidation
{
    public static string Run()
    {
        if(!Application.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        var source=SceneManager.GetActiveScene();
        var test=SceneManager.CreateScene("Grab and throw validation",new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        try
        {
            var physics=test.GetPhysicsScene();
            var floor=new GameObject("Grab test floor"); SceneManager.MoveGameObjectToScene(floor,test);
            floor.transform.position=new Vector3(20,-.1f,0); floor.AddComponent<BoxCollider>().size=new Vector3(12,.2f,12);
            var table=new GameObject("Grab test table"); SceneManager.MoveGameObjectToScene(table,test);
            table.transform.position=new Vector3(20,.5f,1); table.AddComponent<BoxCollider>().size=new Vector3(2,1,1);
            var originalPlayer=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OfficeThirdPersonController>()).First();
            var player=UnityEngine.Object.Instantiate(originalPlayer.gameObject); SceneManager.MoveGameObjectToScene(player,test);
            var controller=player.GetComponent<OfficeThirdPersonController>(); controller.enabled=false;
            controller.visualRoot.rotation=Quaternion.identity;
            controller.humanoidAnimator.Rebind(); controller.humanoidAnimator.Update(0);
            var capsule=player.GetComponent<CharacterController>(); capsule.enabled=false;
            player.transform.position=new Vector3(20,.02f,0); player.transform.rotation=Quaternion.identity; capsule.enabled=true;
            var grabber=player.GetComponent<OfficePropGrabber>(); grabber.enabled=false;
            var cameraObject=new GameObject("Grab test camera"); SceneManager.MoveGameObjectToScene(cameraObject,test);
            var camera=cameraObject.AddComponent<Camera>(); controller.followCamera=camera;
            camera.transform.position=new Vector3(20,1.7f,-2);
            var original=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OfficePushableProp>()).First(p=>p.name=="Guest mug");
            var item=UnityEngine.Object.Instantiate(original.gameObject); SceneManager.MoveGameObjectToScene(item,test);
            var prop=item.GetComponent<OfficePushableProp>(); var body=prop.Body;
            var b=prop.WorldBounds;
            item.transform.position+=new Vector3(20-b.center.x,1.01f-b.min.y,1-b.center.z);
            body.linearVelocity=Vector3.zero; body.angularVelocity=Vector3.zero;
            camera.transform.LookAt(prop.WorldBounds.center); Physics.SyncTransforms();
            if(grabber.FindTarget()!=prop) throw new Exception("Crosshair did not select the actual mug on the table.");
            var selectedRenderer=prop.GetComponentInChildren<Renderer>();
            var playerSkin=controller.visualRoot.GetComponentInChildren<SkinnedMeshRenderer>();
            bool originalSkinning=playerSkin.forceMatrixRecalculationPerRender;
            var originalTint=new MaterialPropertyBlock(); selectedRenderer.GetPropertyBlock(originalTint,0);
            grabber.RefreshTarget();
            var selectedTint=new MaterialPropertyBlock(); selectedRenderer.GetPropertyBlock(selectedTint,0);
            if(grabber.Target!=prop || selectedTint.isEmpty) throw new Exception("Available target was not highlighted.");
            if(!grabber.TryPickup(prop) || grabber.HeldProp!=prop || !body.isKinematic || body.useGravity)
                throw new Exception("Pickup did not transfer ownership to the player.");
            var restoredTint=new MaterialPropertyBlock(); selectedRenderer.GetPropertyBlock(restoredTint,0);
            if(restoredTint.isEmpty!=originalTint.isEmpty || grabber.Target!=null) throw new Exception("Pickup did not clear highlighting.");
            if(!playerSkin.forceMatrixRecalculationPerRender) throw new Exception("Two-hand IK does not reach the rendered skin.");
            float maxHandGap=0;
            foreach(float angle in new[]{0f,90f,180f,270f})
            {
                controller.visualRoot.rotation=Quaternion.Euler(0,angle,0);
                controller.humanoidAnimator.Rebind(); controller.humanoidAnimator.Update(0); grabber.UpdateHeldPose();
                var center=prop.WorldBounds.center;
                var relative=controller.visualRoot.InverseTransformPoint(center);
                if(relative.z<.15f || Mathf.Abs(relative.x)>.08f || center.y-player.transform.position.y>1.35f)
                    throw new Exception("Item is not in front of the character at waist/chest height: "+relative);
                foreach(var bone in new[]{HumanBodyBones.LeftHand,HumanBodyBones.RightHand})
                {
                    var hand=controller.humanoidAnimator.GetBoneTransform(bone);
                    float gap=Vector3.Distance(hand.position,prop.WorldBounds.ClosestPoint(hand.position));
                    maxHandGap=Mathf.Max(maxHandGap,gap);
                    if(gap>.12f) throw new Exception("Hand does not reach the held item: "+bone+" "+gap+" m");
                }
                var savedCamera=camera.transform.position; var savedRotation=camera.transform.rotation;
                camera.transform.position=player.transform.position+new Vector3(2,2,0); camera.transform.LookAt(player.transform.position+Vector3.up);
                grabber.UpdateHeldPose();
                if(Vector3.Distance(center,prop.WorldBounds.center)>.01f) throw new Exception("Camera orbit moved the held item.");
                camera.transform.SetPositionAndRotation(savedCamera,savedRotation);
            }
            controller.visualRoot.rotation=Quaternion.identity;
            controller.humanoidAnimator.Rebind(); controller.humanoidAnimator.Update(0); grabber.UpdateHeldPose();
            var carryStart=player.transform.position;
            for(int i=0;i<35;i++)
            {
                controller.TickMotor(Vector2.down,true,false,.02f);
                controller.humanoidAnimator.Update(.02f); grabber.UpdateHeldPose(); physics.Simulate(.02f);
                var relative=controller.visualRoot.InverseTransformPoint(prop.WorldBounds.center);
                if(relative.z<.15f || Mathf.Abs(relative.x)>.08f) throw new Exception("Sprint moved the item out of the hands: "+relative);
                foreach(var bone in new[]{HumanBodyBones.LeftHand,HumanBodyBones.RightHand})
                {
                    var hand=controller.humanoidAnimator.GetBoneTransform(bone);
                    float gap=Vector3.Distance(hand.position,prop.WorldBounds.ClosestPoint(hand.position));
                    maxHandGap=Mathf.Max(maxHandGap,gap);
                    if(gap>.12f) throw new Exception("Sprint detached hand from item: "+bone+" "+gap);
                }
            }
            capsule.enabled=false; player.transform.position=carryStart; capsule.enabled=true;
            controller.visualRoot.rotation=Quaternion.identity;
            controller.humanoidAnimator.Rebind(); controller.humanoidAnimator.Update(0); grabber.UpdateHeldPose();
            var propCollider=prop.GetComponentInChildren<Collider>();
            if(!Physics.GetIgnoreCollision(propCollider,capsule)) throw new Exception("Held prop collides with its holder.");
            var held=body.position;
            prop.PoseForNpc(Vector3.zero,Quaternion.identity,Vector3.one*100,Quaternion.identity,1);
            if(body.position!=held) throw new Exception("NPC stole the held prop.");
            player.transform.position+=Vector3.right*.25f; grabber.UpdateHeldPose(); Physics.SyncTransforms();
            if(Vector3.Distance(held,body.position)<.15f) throw new Exception("Held prop did not follow the player.");
            var closeWall=new GameObject("Hold test wall"); SceneManager.MoveGameObjectToScene(closeWall,test);
            closeWall.transform.position=player.transform.position+new Vector3(0,1.2f,.55f);
            var closeWallCollider=closeWall.AddComponent<BoxCollider>(); closeWallCollider.size=new Vector3(2,2,.12f);
            Physics.SyncTransforms(); grabber.UpdateHeldPose(); Physics.SyncTransforms();
            foreach(var shape in prop.GetComponentsInChildren<Collider>())
                if(Physics.ComputePenetration(shape,shape.transform.position,shape.transform.rotation,closeWallCollider,
                    closeWall.transform.position,closeWall.transform.rotation,out _,out float penetration) && penetration>.005f)
                    throw new Exception("Held prop clips through nearby wall.");
            closeWall.SetActive(false); grabber.UpdateHeldPose(); Physics.SyncTransforms();
            if(!grabber.ThrowHeld() || grabber.HeldProp!=null || body.isKinematic || !body.useGravity || body.linearVelocity.magnitude<7)
                throw new Exception("Throw did not restore dynamic physics and launch velocity.");
            if(playerSkin.forceMatrixRecalculationPerRender!=originalSkinning) throw new Exception("Throw did not restore the original skinning mode.");
            var launch=body.position;
            for(int i=0;i<40;i++) physics.Simulate(.02f);
            float flight=Vector3.Distance(launch,body.position);
            if(flight<.5f) throw new Exception("Thrown object did not fly.");
            typeof(OfficePushableProp).GetMethod("FixedUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(prop,null);
            if(Physics.GetIgnoreCollision(propCollider,capsule)) throw new Exception("Player collisions were not restored after flight.");
            item.transform.position=player.transform.position+Vector3.forward*6+Vector3.up;
            body.position=item.transform.position; Physics.SyncTransforms();
            if(grabber.TryPickup(prop)) throw new Exception("Distant thrown prop can be picked up.");
            // Return to a reachable spot to exercise drop and owner disable cleanup.
            body.linearVelocity=Vector3.zero; body.angularVelocity=Vector3.zero;
            item.transform.position=player.transform.position+new Vector3(.1f,1.15f,.55f); body.position=item.transform.position; Physics.SyncTransforms();
            if(!grabber.TryPickup(prop)) throw new Exception("Thrown object cannot be picked up again; eligible="+prop.CanPickUp+", bounds="+prop.WorldBounds+", player="+player.transform.position);
            grabber.DropHeld();
            if(body.isKinematic || !body.useGravity || prop.PlayerHolder!=null) throw new Exception("Drop left the prop held.");
            body.linearVelocity=Vector3.zero;
            if(!grabber.TryPickup(prop)) throw new Exception("Dropped object cannot be picked up again.");
            grabber.enabled=true; grabber.enabled=false;
            if(grabber.HeldProp!=null || body.isKinematic || prop.PlayerHolder!=null) throw new Exception("Disabling player left a frozen prop.");
            var wall=new GameObject("Pickup obstruction"); SceneManager.MoveGameObjectToScene(wall,test);
            wall.transform.position=player.transform.position+new Vector3(0,1,.4f); wall.AddComponent<BoxCollider>().size=new Vector3(2,2,.1f);
            item.transform.position=player.transform.position+new Vector3(0,1.1f,.9f); body.position=item.transform.position; Physics.SyncTransforms();
            if(grabber.TryPickup(prop)) throw new Exception("Player picked up an object through a wall.");
            var chair=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OfficePushableProp>()).First(p=>p.name=="SM_Prop_ArmChair_01");
            if(!chair.CanPickUp) throw new Exception("Freestanding guest chair cannot be picked up.");
            prop.allowPickup=false;
            if(prop.CanPickUp) throw new Exception("Explicitly fixed prop can be picked up.");
            prop.allowPickup=true;
            foreach(var fixedName in new[]{"SM_Prop_Laptop_01","SM_Prop_OfficeChair_01","Thick executive desktop"})
                if(source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>()).Where(t=>t.name==fixedName)
                    .Any(t=>t.GetComponentInParent<OfficePushableProp>()!=null)) throw new Exception("Fixed furniture became pickable: "+fixedName);
            string result="GRAB_THROW_PASS: actual mug targeted, picked up, followed hand, ignored holder collision, resisted NPC ownership, thrown "+flight.ToString("F2")+
                " m, picked up again, dropped, released on disable; held wall penetration prevented, player collisions restored; wall/distant pickup and fixed furniture rejected, freestanding guest chair allowed. Highlight restored; rendered two-hand carry at four headings and during sprint, camera orbit independent; max hand gap "+maxHandGap.ToString("F3")+" m.";
            Directory.CreateDirectory("OfficePreview"); File.WriteAllText("OfficePreview/GrabThrow_Validation.txt",result);
            return result;
        }
        finally { SceneManager.UnloadSceneAsync(test); }
    }
}
