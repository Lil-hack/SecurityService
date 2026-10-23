using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OfficeDogPhysicsSetup
{
    const string Prefab="Assets/character/officeperson/dog/Office_Dog.prefab";
    static void Configure(GameObject dog)
    {
        var rb=dog.GetComponent<Rigidbody>();if(rb==null)rb=dog.AddComponent<Rigidbody>();
        rb.mass=12;rb.useGravity=true;rb.isKinematic=false;rb.linearDamping=1.1f;rb.angularDamping=2;
        rb.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        if(dog.GetComponent<OfficeDogPhysics>()==null)dog.AddComponent<OfficeDogPhysics>();
        EditorUtility.SetDirty(rb);
    }
    [MenuItem("Tools/Office/Enable Dog Physics")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first.");
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try{Configure(root);PrefabUtility.SaveAsPrefabAsset(root,Prefab);}finally{PrefabUtility.UnloadPrefabContents(root);}
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var dogs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Animator>(true)).Where(a=>a.name=="Office_Dog").ToArray();
        foreach(var animator in dogs){Configure(animator.gameObject);PrefabUtility.RecordPrefabInstancePropertyModifications(animator.GetComponent<Rigidbody>());}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("Dog physics configured; scene dogs: "+dogs.Length);
    }
    [MenuItem("Tools/Office/Test Dog Physics")]
    public static void Test()
    {
        if(!Application.isPlaying)throw new Exception("Enter Play first.");Application.runInBackground=true;
        var dog=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));dog.transform.position=new Vector3(1000,.03f,1000);
        var physics=dog.GetComponent<OfficeDogPhysics>();var rb=dog.GetComponent<Rigidbody>();var start=dog.transform.position;
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.1f,1000);floor.transform.localScale=new Vector3(30,.2f,30);
        var player=UnityEngine.Object.Instantiate(UnityEngine.Object.FindAnyObjectByType<OfficeThirdPersonController>().gameObject);
        var controller=player.GetComponent<OfficeThirdPersonController>();controller.enabled=false;
        player.GetComponent<CharacterController>().enabled=false;player.GetComponent<OfficePunchCombat>().enabled=false;
        player.GetComponent<OfficeFootstepDust>().enabled=false;player.transform.position=start-Vector3.forward*.65f;controller.visualRoot.rotation=Quaternion.identity;
        controller.humanoidAnimator.Rebind();controller.humanoidAnimator.Update(0);Physics.SyncTransforms();
        var fists=player.GetComponent<OfficePunchHitboxes>();if(fists==null)fists=player.AddComponent<OfficePunchHitboxes>();
        fists.Sample(controller.humanoidAnimator,true,.4f);fists.Sample(controller.humanoidAnimator,true,.45f);
        if(physics.PunchCount!=1)throw new Exception("Standing punch did not hit low dog exactly once.");
        float began=Time.time;int stage=0;GameObject ball=null;
        void Check()
        {
            if(!Application.isPlaying){EditorApplication.update-=Check;return;}
            if(stage==0 && Time.time-began>1.5f)
            {
                if(dog.transform.position.z-start.z<1)throw new Exception("Punch did not displace dog.");
                rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;rb.isKinematic=true;dog.transform.position=start;rb.position=start;rb.isKinematic=false;Physics.SyncTransforms();
                ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);ball.transform.localScale=Vector3.one*.2f;ball.transform.position=start+new Vector3(0,.4f,2);
                ball.AddComponent<Rigidbody>().mass=.7f;var prop=ball.AddComponent<OfficePushableProp>();var holder=UnityEngine.Object.FindAnyObjectByType<OfficePropGrabber>();
                prop.Grab(holder);prop.ReleaseFromPlayer(holder,Vector3.back*8,Vector3.zero);stage=1;
            }
            if(stage==1 && Time.time-began>4)
            {
                EditorApplication.update-=Check;
                try
                {
                    if(physics.PropHitCount<1 || dog.transform.position.z>start.z-.5f)throw new Exception("Thrown prop did not knock back dog. hits="+physics.PropHitCount+", position="+dog.transform.position);
                    if(dog.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime<=0)throw new Exception("Head animation stopped.");
                    Debug.Log("PASS dog punch knockback, actual thrown prop hit, upright landing and running head Animator.");
                }
                finally{UnityEngine.Object.Destroy(player);UnityEngine.Object.Destroy(dog);UnityEngine.Object.Destroy(floor);UnityEngine.Object.Destroy(ball);}
            }
        }
        EditorApplication.update+=Check;
    }
}

