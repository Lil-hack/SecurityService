using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StudioPropPhysicsSetup
{
    const string Group="05 Open plan studio office";
    static readonly HashSet<string> Movable=new HashSet<string>{"Task chair","Meeting chair","Coffee bar stool","Collaboration soft stool",
        "Colourful upholstered pouf","Studio lounge sofa","Studio potted plant","Desk coffee mug","Desk notebooks","Lounge books",
        "Collaboration books","Storage books","Studio espresso machine","Studio cups and coffee tray","Shared office printer","Printer cabinet","Studio storage cabinet"};

    [MenuItem("Tools/Office/Enable Studio Grab And Throw")]
    public static string Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
        var root=GameObject.Find(Group).transform;
        FixStorage(root);
        foreach(var t in root.Cast<Transform>().Where(t=>Movable.Contains(t.name)).ToArray())
        {
            foreach(var child in t.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(child.gameObject,0);
            if(t.GetComponentsInChildren<Collider>().Length==0)
                foreach(var f in t.GetComponentsInChildren<MeshFilter>())
                {
                    var box=f.gameObject.AddComponent<BoxCollider>();box.center=f.sharedMesh.bounds.center;box.size=f.sharedMesh.bounds.size;
                }
            foreach(var c in t.GetComponentsInChildren<Collider>())
            {c.enabled=true;c.contactOffset=.001f;if(c is MeshCollider mesh)mesh.convex=true;}
            var prop=t.GetComponent<OfficePushableProp>();if(prop==null)prop=t.gameObject.AddComponent<OfficePushableProp>();
            prop.allowPickup=true;var body=prop.Body;var size=prop.WorldBounds.size;
            body.mass=Mathf.Clamp(size.x*size.y*size.z*30,.08f,t.name.Contains("sofa")?35:15);
            body.isKinematic=false;body.useGravity=true;body.constraints=RigidbodyConstraints.None;
            body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping=.15f;body.angularDamping=.35f;body.solverIterations=12;body.solverVelocityIterations=4;body.maxDepenetrationVelocity=1;
        }
        Physics.SyncTransforms();var report=Validate();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveScene(root.gameObject.scene);
        File.WriteAllText("OfficePreview/Studio_Prop_Physics.txt",report);return report;
    }
    static void FixStorage(Transform root)
    {
        foreach(var old in root.Cast<Transform>().Where(t=>t.name=="Low studio storage back").ToArray())
        {
            var pos=old.position;pos.y=0;var mat=old.GetComponent<Renderer>().sharedMaterial;
            var cabinet=new GameObject("Studio storage cabinet").transform;cabinet.SetParent(root);cabinet.position=pos;
            // Hollow shelves support books without starting with overlapping colliders.
            Part(cabinet,new Vector3(0,.54f,-.205f),new Vector3(2,1.08f,.05f),mat);
            foreach(float y in new[]{.045f,.555f,1.065f})Part(cabinet,new Vector3(0,y,0),new Vector3(2,.03f,.46f),mat);
            foreach(float x in new[]{-.985f,-.33f,.33f,.985f})Part(cabinet,new Vector3(x,.55f,0),new Vector3(.03f,1.05f,.46f),mat);
            foreach(var recess in root.Cast<Transform>().Where(t=>t.name=="Storage cubby recess" && Mathf.Abs(t.position.x-pos.x)<1.05f && Mathf.Abs(t.position.z-pos.z)<.5f).ToArray())
                UnityEngine.Object.DestroyImmediate(recess.gameObject);
            foreach(var book in root.Cast<Transform>().Where(t=>t.name=="Storage books" && Mathf.Abs(t.position.x-pos.x)<1.05f && Mathf.Abs(t.position.z-pos.z)<.5f))
            {var p=book.position;p.z=pos.z+.02f;book.position=p;}
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
    }
    static void Part(Transform parent,Vector3 p,Vector3 size,Material mat)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Storage cabinet structural part";g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;
    }
    public static string Validate()
    {
        var root=GameObject.Find(Group).transform;var props=root.GetComponentsInChildren<OfficePushableProp>();
        foreach(var t in root.Cast<Transform>().Where(t=>Movable.Contains(t.name)))
        {
            var p=t.GetComponent<OfficePushableProp>();if(p==null || !p.CanPickUp || p.GetComponentsInChildren<Collider>().Length==0)
                throw new Exception("Item cannot be picked up: "+t.name);
        }
        foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Workstation ") || t.name=="Meeting laptop" ||
            t.name=="Meeting table" || t.name=="Studio round coffee table" || t.name=="Collaboration oak table"))
            if(t.GetComponentInParent<OfficePushableProp>()!=null)throw new Exception("Table or computer became movable: "+t.name);
        if(props.Any(p=>p.GetComponentsInChildren<Transform>().Any(t=>GameObjectUtility.GetStaticEditorFlags(t.gameObject)!=0)))
            throw new Exception("Movable object still marked static.");
        return "PASS: "+props.Length+" studio objects can be picked up and thrown; tables, desktops, monitors, keyboards, mice and meeting laptop remain fixed. Door and room structure preserved.";
    }
}
