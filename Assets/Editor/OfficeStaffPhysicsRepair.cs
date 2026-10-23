using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeStaffPhysicsRepair
{
    [MenuItem("Tools/Office/Audit Staff Physics")]
    public static void Audit()
    {
        Directory.CreateDirectory("OfficePreview");
        File.WriteAllText("OfficePreview/StaffPhysicsAudit.txt",string.Join("\n",Object.FindObjectsByType<OfficeStaffPhysics>().Select(n=>{
            var a=n.GetComponentInChildren<Animator>();var b=TripoHumanoidAnimationSetup.SkinnedBoundsOf(a.gameObject);
            return n.name+" root="+n.transform.position+" mesh="+b.center+" collider="+n.GetComponent<CapsuleCollider>().bounds.center+" childLocal="+a.transform.localPosition+" rigidbodies="+n.GetComponentsInChildren<Rigidbody>().Length;
        })));
    }
    [MenuItem("Tools/Office/Test Repaired Staff Punch Contacts")]
    public static void TestContacts()
    {
        if(!Application.isPlaying)throw new System.Exception("Enter Play first.");
        var originals=Object.FindObjectsByType<OfficeStaffPhysics>().Where(n=>n.GetComponent<OfficeStaffTyping>()==null).ToArray();
        var player=Object.Instantiate(Object.FindAnyObjectByType<OfficeThirdPersonController>().gameObject);
        try
        {
            var controller=player.GetComponent<OfficeThirdPersonController>();controller.enabled=false;player.GetComponent<CharacterController>().enabled=false;
            player.GetComponent<OfficePunchCombat>().enabled=false;player.GetComponent<OfficeFootstepDust>().enabled=false;
            var fists=player.GetComponent<OfficePunchHitboxes>();if(fists==null)fists=player.AddComponent<OfficePunchHitboxes>();
            foreach(var original in originals)
            {
                var npc=Object.Instantiate(original.gameObject).GetComponent<OfficeStaffPhysics>();
                try
                {
                    var patrol=npc.GetComponent<OfficeStaffPatrol>();if(patrol!=null)patrol.enabled=false;
                    npc.transform.SetPositionAndRotation(new Vector3(1000,.03f,1000),Quaternion.identity);
                    player.transform.position=npc.transform.position+new Vector3(.15f,0,-.56f);controller.visualRoot.rotation=Quaternion.identity;
                    var animator=controller.humanoidAnimator;animator.Rebind();int layer=animator.GetLayerIndex("Punches");animator.SetLayerWeight(layer,1);animator.Play("Punches.Jab",layer,.4f);animator.Update(0);
                    Physics.SyncTransforms();fists.ResetSwing();fists.Sample(animator,true,.4f);
                    if(!npc.IsDown || fists.HitCount!=1)throw new System.Exception("Fist did not reach repaired collider: "+original.name);
                }
                finally{Object.DestroyImmediate(npc.gameObject);}
            }
            Debug.Log("PASS actual fist hitboxes knock down all "+originals.Length+" standing office NPCs.");
        }
        finally{Object.DestroyImmediate(player);}
    }
    static void Configure(OfficeStaffPhysics npc,bool align)
    {
        var animator=npc.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
        if(align && npc.GetComponent<OfficeStaffTyping>()==null)
        {
            var b=TripoHumanoidAnimationSetup.SkinnedBoundsOf(animator.gameObject);
            var position=animator.transform.position;var rotation=animator.transform.rotation;
            npc.transform.SetPositionAndRotation(new Vector3(b.center.x,b.min.y,b.center.z),rotation);
            animator.transform.SetPositionAndRotation(position,rotation);
            var capsule=npc.GetComponent<CapsuleCollider>();capsule.center=npc.transform.InverseTransformPoint(b.center);capsule.height=b.size.y;capsule.radius=Mathf.Clamp(Mathf.Min(b.size.x,b.size.z)*.55f,.22f,.4f);capsule.enabled=true;
        }
        var rootBody=npc.GetComponent<Rigidbody>();if(rootBody==null)rootBody=npc.gameObject.AddComponent<Rigidbody>();
        rootBody.isKinematic=true;rootBody.useGravity=false;rootBody.mass=70;
        npc.enabled=true;npc.PrepareRagdoll();
        foreach(var skin in npc.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.updateWhenOffscreen=true;skin.forceMatrixRecalculationPerRender=true;}
        EditorUtility.SetDirty(npc);EditorUtility.SetDirty(npc.transform);EditorUtility.SetDirty(animator.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(npc.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(animator.transform);
    }
    [MenuItem("Tools/Office/Repair Staff Physics")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new System.Exception("Stop Play first.");
        foreach(var folder in new[]{"girl1","girl2","man1","man2"})
        {
            string path="Assets/character/officeperson/"+folder+"/Staff.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
            try{Configure(prefab.GetComponent<OfficeStaffPhysics>(),true);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
        foreach(var npc in Object.FindObjectsByType<OfficeStaffPhysics>())Configure(npc,true);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Audit();
        Debug.Log("Staff colliders aligned with visible characters; root Rigidbody and persistent ragdolls saved to scene and four prefabs.");
    }
}

