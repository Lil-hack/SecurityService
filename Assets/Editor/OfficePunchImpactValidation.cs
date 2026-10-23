using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficePunchImpactValidation
{
    [MenuItem("Tools/Office/Validate Punch Impacts")]
    public static void Run()
    {
        if(!Application.isPlaying)throw new Exception("Enter Play mode first.");
        var scene=SceneManager.CreateScene("Punch collision test",new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        GameObject player=null;
        try
        {
            var source=UnityEngine.Object.FindFirstObjectByType<OfficeThirdPersonController>();
            player=UnityEngine.Object.Instantiate(source.gameObject);SceneManager.MoveGameObjectToScene(player,scene);
            player.transform.position=new Vector3(1000,0,0);
            var controller=player.GetComponent<OfficeThirdPersonController>();controller.enabled=false;controller.visualRoot.rotation=Quaternion.identity;
            var animator=controller.humanoidAnimator;animator.Rebind();animator.Update(0);
            int layer=animator.GetLayerIndex("Punches");animator.SetLayerWeight(layer,1);animator.Play("Punches.Jab",layer,.4f);animator.Update(0);
            var hit=player.GetComponent<OfficePunchHitboxes>() ?? player.AddComponent<OfficePunchHitboxes>();
            var p=animator.GetBoneTransform(HumanBodyBones.LeftHand).position+Vector3.forward*.04f;
            var go=new GameObject("Pinned target");SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=p;
            go.AddComponent<SphereCollider>().radius=.04f;var body=go.AddComponent<Rigidbody>();body.mass=.5f;
            var prop=go.AddComponent<OfficePushableProp>();prop.keepInitialPoseUntilGrab=true;body.isKinematic=true;
            Physics.SyncTransforms();
            hit.Sample(animator,true,.1f);if(!body.isKinematic)throw new Exception("Inactive fist hit target.");
            hit.Sample(animator,true,.4f);if(body.isKinematic || hit.HitCount!=1)throw new Exception("Active fist did not release target.");
            hit.Sample(animator,true,.45f);if(hit.HitCount!=1)throw new Exception("Duplicate impact in one swing.");
            scene.GetPhysicsScene().Simulate(.02f);if(body.linearVelocity.z<6)throw new Exception("Target did not fly forward.");
            hit.ResetSwing();body.position=p;body.linearVelocity=Vector3.zero;
            var wall=new GameObject("Blocking wall");SceneManager.MoveGameObjectToScene(wall,scene);
            var chest=player.transform.position+Vector3.up*p.y;wall.transform.position=Vector3.Lerp(chest,p,.5f);
            wall.transform.rotation=Quaternion.LookRotation(p-chest);wall.AddComponent<BoxCollider>().size=new Vector3(1,1,.02f);Physics.SyncTransforms();
            hit.Sample(animator,true,.4f);if(hit.HitCount!=0)throw new Exception("Punch passed through wall.");
            scene.GetPhysicsScene().Simulate(.02f);if(body.linearVelocity.z>.01f)throw new Exception("Blocked target received impulse.");
            var shader=Resources.Load<Shader>("PunchImpact");if(shader==null || !shader.isSupported)throw new Exception("Impact shader unavailable.");
            var fx=UnityEngine.Object.FindFirstObjectByType<OfficePunchImpact>();if(fx==null || fx.GetComponentInChildren<ParticleSystem>().particleCount==0)throw new Exception("Missing impact particles.");
            Directory.CreateDirectory("OfficePreview");File.WriteAllText("OfficePreview/PunchImpacts.txt","PASS: inactive fist ignored; pinned object released and flew forward; one impulse per swing; wall blocks punches; impact shader supported and particles emitted.");
            Debug.Log("PASS punch collision and impact validation");
        }
        finally {if(player!=null)UnityEngine.Object.DestroyImmediate(player);SceneManager.UnloadSceneAsync(scene);}
    }
}

