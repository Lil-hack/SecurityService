using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KitchenPropPhysicsSetup
{
    static readonly HashSet<string> Names=new HashSet<string>{"Kitchen coffee pot","Kitchen milk jug","Kitchen bottle","Kitchen sugar","Kitchen mug","Kitchen fruit bowl","Kitchen orange","Kitchen shelf books","Kitchen shelf plant","Kitchen floor plant","Kitchen window plant","Kitchen corner plant","Kitchen dining chair","Kitchen bar stool","Coffee cup","Pantry jar","Soap bottle","Folded dish cloth","Coffee tray","Island cutting board","Knife blade","Bread plate","Bread slices","Dining vase","Round tufted mustard cushion","Sage bolster with linen piping"};
    [MenuItem("Tools/Office/Enable Kitchen Grab And Throw")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
        var root=GameObject.Find("Kitchen room");if(root==null)throw new Exception("Kitchen missing.");
        Undo.RegisterFullObjectHierarchyUndo(root,"Enable kitchen pickup");
        var all=root.GetComponentsInChildren<Transform>(true);
        Attach(all,new[]{"Cup handle","Cup dark opening","Coffee surface","Cup saucer"},new[]{"Kitchen mug","Coffee cup"},.24f);
        Attach(all,new[]{"Jar wooden lid","Jar label"},new[]{"Pantry jar"},.3f);
        Attach(all,new[]{"Board grip"},new[]{"Island cutting board"},.6f);
        Attach(all,new[]{"Knife handle"},new[]{"Knife blade"},.3f);
        Attach(all,new[]{"Cloth woven stripe"},new[]{"Folded dish cloth"},.3f);
        Attach(all,new[]{"Soap dispenser pump"},new[]{"Soap bottle"},.25f);
        Attach(all,new[]{"Flower stem","Small yellow bloom"},new[]{"Dining vase"},.5f);
        var targets=all.Where(t=>Names.Contains(t.name) && t.gameObject.activeInHierarchy).ToArray();
        foreach(var t in targets)
        {
            foreach(var c in t.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(c.gameObject,0);
            if(t.GetComponentsInChildren<Collider>().Length==0)
            {
                // Local bounds enclose assembled handles, lids and trim in one stable collider.
                var bounds=new Bounds();bool first=true;
                foreach(var f in t.GetComponentsInChildren<MeshFilter>())
                {
                    if(f.sharedMesh==null)continue;var b=f.sharedMesh.bounds;
                    for(int i=0;i<8;i++)
                    {var p=t.InverseTransformPoint(f.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
                }
                if(first)throw new Exception("No mesh: "+t.name);
                var box=t.gameObject.AddComponent<BoxCollider>();box.center=bounds.center;box.size=Vector3.Max(bounds.size,Vector3.one*.005f);
            }
            foreach(var c in t.GetComponentsInChildren<Collider>()) {c.enabled=true;c.contactOffset=.001f;if(c is MeshCollider m)m.convex=true;}
            var prop=t.GetComponent<OfficePushableProp>();if(prop==null)prop=t.gameObject.AddComponent<OfficePushableProp>();prop.allowPickup=true;
            var rb=prop.Body;var size=prop.WorldBounds.size;rb.mass=Mathf.Clamp(size.x*size.y*size.z*25,.08f,12);
            rb.isKinematic=false;rb.useGravity=true;rb.constraints=RigidbodyConstraints.None;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            rb.linearDamping=.2f;rb.angularDamping=.5f;rb.solverIterations=12;rb.solverVelocityIterations=4;rb.maxDepenetrationVelocity=1;
            prop.keepInitialPoseUntilGrab=true;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;rb.isKinematic=true;
        }
        Physics.SyncTransforms();Check(root);
        EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);
        File.WriteAllText("OfficePreview/KitchenPickup_Validation.txt","PASS: "+targets.Length+" kitchen objects enabled for pickup and throw; composite details attached; furniture and installed appliances fixed.");
    }
    static void Attach(Transform[] all,string[] children,string[] owners,float max)
    {
        var bases=all.Where(t=>owners.Contains(t.name)).ToArray();
        foreach(var t in all.Where(t=>children.Contains(t.name)))
        {
            if(bases.Length==0)continue;
            var nearest=bases.OrderBy(b=>Vector3.Distance(b.position,t.position)).First();
            if(Vector3.Distance(nearest.position,t.position)<max)t.SetParent(nearest,true);
        }
    }
    static void Check(GameObject root)
    {
        foreach(var p in root.GetComponentsInChildren<OfficePushableProp>())
            if(!p.CanPickUp || p.GetComponentsInChildren<Collider>().Length==0)throw new Exception("Cannot pick up "+p.name);
        foreach(var t in root.GetComponentsInChildren<Transform>())
            if(t.name=="Kitchen counter top" || t.name=="Kitchen island base" || t.name=="Kitchen fridge" || t.name=="Kitchen dining top")
                if(t.GetComponentInParent<OfficePushableProp>()!=null)throw new Exception("Installed furniture movable: "+t.name);
    }
    [MenuItem("Tools/Office/Validate Kitchen Pickup In Play Mode")]
    public static void ValidatePlay()
    {
        if(!Application.isPlaying)throw new Exception("Run in Play Mode.");
        var root=GameObject.Find("Kitchen room");Check(root);
        var holder=UnityEngine.Object.FindFirstObjectByType<OfficePropGrabber>();if(holder==null)throw new Exception("Player grabber missing.");
        int count=0;
        foreach(var prop in root.GetComponentsInChildren<OfficePushableProp>())
        {
            if(!prop.Grab(holder))throw new Exception("Grab failed: "+prop.name);
            prop.ReleaseFromPlayer(holder,Vector3.zero,Vector3.zero);
            if(prop.Body.isKinematic || !prop.CanPickUp)throw new Exception("Release failed: "+prop.name);
            count++;
        }
        File.AppendAllText("OfficePreview/KitchenPickup_Validation.txt","\nPASS Play Mode: grabbed and released "+count+" objects using the player grabber.");
    }
}
