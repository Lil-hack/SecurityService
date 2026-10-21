using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficePropPhysicsSetup
{
    static readonly HashSet<string> Models=new HashSet<string>
    {
        "SM_Prop_Book_Group_02", "SM_Prop_Book_Group_03", "SM_Prop_BookOpen_01",
        "SM_Prop_Globe_01", "SM_Prop_Trophy_01", "SM_Prop_Clock_01",
        "SM_Prop_PotPlant_01", "SM_Prop_Study_Lamp_01", "SM_Prop_Lamp_01",
        "SM_Prop_Phone_Rotary_01", "SM_Prop_Computer_Smartphone_01",
        "SM_Prop_Computer_Printer_Paper_01", "SM_Item_Pencil_01", "SM_Item_Mug_01",
        "Guest table books", "Guest coffee tray", "Guest service tray", "Guest mug",
        "Console book collection", "Console plant", "Guest coffee machine",
        "Coffee coaster", "Brass pencil cup", "Boss working document",
        "SM_Prop_ArmChair_01", "SM_Prop_CoatRack_01", "SM_Prop_Plant_Palm_01",
        "Front corner floor plant", "Rear right corner floor plant", "Guest reading floor lamp",
        "Entry water dispenser"
    };

    [MenuItem("Tools/Office/Enable Small Prop Physics")]
    public static void Apply()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Stop Play mode before configuring prop physics.");
        var scene=SceneManager.GetActiveScene();
        if(scene.name!="ExecutiveOffice") throw new InvalidOperationException("Open ExecutiveOffice first.");
        var desktop=All(scene).FirstOrDefault(t=>t.name=="Boss desktop accessories");
        if(desktop!=null)
        {
            Group(desktop,"Pushable document organizer",t=>t.name.StartsWith("Document tray ") || t.name=="Tray support");
            Group(desktop,"Pushable journal",t=>t.name=="Navy journal cover" || t.name=="Journal pages");
            Group(desktop,"Pushable nameplate",t=>t.name.StartsWith("Nameplate "));
        }
        foreach(var t in All(scene).ToArray())
        {
            if(!Models.Contains(t.name) && !t.name.StartsWith("Pushable ")) continue;
            if(t.GetComponentInParent<OfficePushableProp>()!=null) continue;
            var renderers=t.GetComponentsInChildren<MeshRenderer>(true);
            if(renderers.Length==0) continue;
            var bounds=renderers[0].bounds;
            foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            foreach(var child in t.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(child.gameObject,0);
            foreach(var collider in t.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled=true;
                collider.contactOffset=.001f;
                if(collider is MeshCollider mesh) mesh.convex=true;
            }
            if(t.GetComponentsInChildren<Collider>(true).Length==0)
            {
                foreach(var filter in t.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(filter.sharedMesh==null) continue;
                    var box=t.gameObject.AddComponent<BoxCollider>();
                    var local=new Bounds(t.InverseTransformPoint(filter.transform.TransformPoint(filter.sharedMesh.bounds.center)),Vector3.zero);
                    var b=filter.sharedMesh.bounds;
                    for(int i=0;i<8;i++) local.Encapsulate(t.InverseTransformPoint(filter.transform.TransformPoint(b.center+Vector3.Scale(b.extents,
                        new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)))));
                    box.center=local.center; box.size=local.size; box.contactOffset=.001f;
                }
            }
            if(t.name=="Brass pencil cup") HollowCup(t);
            var body=t.GetComponent<Rigidbody>();
            if(body==null) body=t.gameObject.AddComponent<Rigidbody>();
            body.mass=t.name=="SM_Prop_ArmChair_01"?8f:Mathf.Clamp(bounds.size.x*bounds.size.y*bounds.size.z*180f,.08f,4f);
            body.useGravity=true; body.isKinematic=false;
            body.interpolation=RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping=.15f; body.angularDamping=.35f;
            body.solverIterations=12; body.solverVelocityIterations=4;
            body.maxDepenetrationVelocity=1f;
            t.gameObject.AddComponent<OfficePushableProp>();
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log(Validate());
    }

    static IEnumerable<Transform> All(Scene scene) { return scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)); }

    static void Group(Transform parent,string name,Func<Transform,bool> select)
    {
        if(parent.Find(name)!=null) return;
        var parts=parent.Cast<Transform>().Where(select).ToArray();
        if(parts.Length==0) return;
        var group=new GameObject(name).transform;
        group.SetParent(parent,false); group.position=parts[0].position;
        foreach(var part in parts) part.SetParent(group,true);
    }

    static void HollowCup(Transform cup)
    {
        foreach(var collider in cup.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
        var bottom=cup.gameObject.AddComponent<BoxCollider>();
        bottom.center=new Vector3(0,-.95f,0); bottom.size=new Vector3(.7f,.1f,.7f); bottom.contactOffset=.001f;
        for(int i=0;i<8;i++)
        {
            var wall=new GameObject("Pencil cup collision wall").transform;
            wall.SetParent(cup,false); wall.localRotation=Quaternion.Euler(0,i*45f,0);
            wall.localPosition=wall.localRotation*new Vector3(0,0,.46f);
            var box=wall.gameObject.AddComponent<BoxCollider>();
            box.size=new Vector3(.39f,2f,.08f); box.contactOffset=.001f;
        }
    }

    public static string Validate()
    {
        var all=All(SceneManager.GetActiveScene()).ToArray();
        var props=all.Select(t=>t.GetComponent<OfficePushableProp>()).Where(p=>p!=null).ToArray();
        foreach(var prop in props)
        {
            var body=prop.GetComponent<Rigidbody>();
            if(body.isKinematic || !body.useGravity || prop.GetComponentsInChildren<Collider>().Length==0 ||
                prop.GetComponentsInChildren<MeshCollider>().Any(c=>!c.convex))
                throw new Exception("Invalid prop physics: "+prop.name);
        }
        string[] fixedNames={"SM_Prop_Laptop_01","SM_Prop_Laptop_01 (1)","SM_Prop_Office_Desk_01","Thick executive desktop","Guest coffee table","SM_Prop_OfficeChair_01"};
        foreach(var t in all.Where(t=>fixedNames.Contains(t.name)))
            if(t.GetComponentInParent<Rigidbody>()!=null) throw new Exception("Excluded object is dynamic: "+t.name);
        return "PROP_PHYSICS_PASS: "+props.Length+" pushable props; valid convex/compound colliders; laptops, tables and boss chair remain fixed.";
    }

    public static void ApplyBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/ExecutiveOffice.unity");
        Apply();
        Directory.CreateDirectory("OfficePreview");
        File.WriteAllText("OfficePreview/PropPhysics_Validation.txt",Validate());
    }

    public static string ValidateSimulation()
    {
        if(!Application.isPlaying) throw new InvalidOperationException("Enter Play mode to run the isolated physics validation.");
        var source=SceneManager.GetActiveScene();
        var test=SceneManager.CreateScene("Prop physics validation",new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        var simulation=test.GetPhysicsScene();
        try
        {
            foreach(var root in source.GetRootGameObjects())
            {
                var copy=UnityEngine.Object.Instantiate(root);
                SceneManager.MoveGameObjectToScene(copy,test);
            }
            var props=test.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OfficePushableProp>()).ToArray();
            for(int i=0;i<250;i++) simulation.Simulate(.02f);
            foreach(var prop in props)
            {
                var renderers=prop.GetComponentsInChildren<Renderer>();
                if(renderers.Length>0 && renderers.All(r=>r.bounds.max.y<-.1f))
                    throw new Exception("Prop fell through the room: "+prop.name);
            }
            var floor=new GameObject("Push test floor"); SceneManager.MoveGameObjectToScene(floor,test);
            floor.transform.position=new Vector3(22,-.1f,0);
            floor.AddComponent<BoxCollider>().size=new Vector3(12,.2f,4);
            var results=new List<string>();
            foreach(string name in new[]{"Guest coffee machine","SM_Prop_Study_Lamp_01","SM_Prop_Computer_Smartphone_01","Guest table books","SM_Prop_ArmChair_01"})
            {
                var original=All(source).First(t=>t.name==name);
                var item=UnityEngine.Object.Instantiate(original.gameObject);
                SceneManager.MoveGameObjectToScene(item,test);
                var body=item.GetComponent<Rigidbody>();
                if(body==null) throw new Exception("Prop still static: "+name);
                var b=item.GetComponentsInChildren<Renderer>()[0].bounds;
                foreach(var r in item.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                item.transform.position+=new Vector3(21.1f-b.center.x,.01f-b.min.y,-b.center.z);
                body.linearVelocity=Vector3.zero; body.angularVelocity=Vector3.zero;
                var originalPlayer=All(source).Select(t=>t.GetComponent<OfficeThirdPersonController>()).First(c=>c!=null);
                var player=UnityEngine.Object.Instantiate(originalPlayer.gameObject);
                SceneManager.MoveGameObjectToScene(player,test);
                var controller=player.GetComponent<OfficeThirdPersonController>();
                controller.enabled=false; controller.humanoidAnimator=null;
                var capsule=player.GetComponent<CharacterController>();
                capsule.enabled=false;
                player.transform.position=new Vector3(19.8f,.03f,0);
                capsule.enabled=true;
                Physics.SyncTransforms();
                for(int i=0;i<30;i++) simulation.Simulate(.02f);
                float start=item.transform.position.x;
                for(int i=0;i<110;i++) { controller.TickMotor(Vector2.right,false,false,.02f); simulation.Simulate(.02f); }
                float moved=item.transform.position.x-start;
                results.Add(name+": "+moved.ToString("F2")+" m");
                player.SetActive(false); item.SetActive(false);
                if(moved<.2f) throw new Exception("Actual player failed to push "+name+" ("+body.mass+" kg): "+moved+" m");
            }
            string report=Validate()+" Five seconds without floor penetration. Actual player capsule and scene prop models: "+string.Join("; ",results);
            Directory.CreateDirectory("OfficePreview");
            File.WriteAllText("OfficePreview/PropPhysics_Validation.txt",report);
            Debug.Log(report);
            return report;
        }
        finally { SceneManager.UnloadSceneAsync(test); }
    }
}
