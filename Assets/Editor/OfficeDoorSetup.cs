using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeDoorSetup
{
    [MenuItem("Tools/Office/Make Connecting Door Interactive")]
    public static string Apply(){return Configure(true);}
    public static string Configure(bool save)
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
        var root=GameObject.Find("05 Open plan studio office").transform;
        var leaf=GameObject.Find("Connecting oak door leaf")??GameObject.Find("Open connecting oak door leaf");
        if(leaf==null)throw new Exception("Connecting door leaf not found.");
        var hinge=root.Find("Interactive connecting door");
        if(hinge==null){hinge=new GameObject("Interactive connecting door").transform;hinge.SetParent(root,false);}
        hinge.SetPositionAndRotation(new Vector3(6.0f,0,.145f),Quaternion.identity);
        leaf.name="Connecting oak door leaf";leaf.transform.SetParent(hinge,true);
        leaf.transform.SetPositionAndRotation(new Vector3(6.695f,1.3375f,.145f),Quaternion.identity);
        leaf.transform.localScale=new Vector3(1.39f,2.665f,.09f);
        leaf.GetComponent<Renderer>().enabled=false; // Exact-size invisible collision carrier.
        var previous=hinge.Find("Paneled walnut door model");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var visual=ExecutiveOfficeBuilder.PlaceDecoration("Assets/models/meshes/data/SM_Bld/SM_Bld_Door_01.obj",hinge,Vector3.zero,180);
        visual.name="Paneled walnut door model";
        var bounds=Bounds(visual); visual.transform.localScale=Vector3.Scale(visual.transform.localScale,new Vector3(1.39f/bounds.size.x,2.665f/bounds.size.y,.12f/bounds.size.z));
        bounds=Bounds(visual);visual.transform.position+=new Vector3(6.695f,.005f,.145f)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        var walnut=AssetDatabase.LoadAssetAtPath<Material>("Assets/ExecutiveOffice/Materials/Walnut joinery.mat");
        foreach(var r in visual.GetComponentsInChildren<Renderer>())r.sharedMaterial=walnut;
        ConfigureRebates(root,walnut);
        var handle=GameObject.Find("Open door brass handle")??GameObject.Find("Connecting door brass handle");
        if(handle!=null){handle.name="Connecting door brass handle";handle.transform.SetParent(hinge,true);handle.transform.SetPositionAndRotation(new Vector3(7.22f,1.35f,.04f),Quaternion.identity);}
        foreach(var t in hinge.GetComponentsInChildren<Transform>())
        {
            GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
            var prop=t.GetComponent<OfficePushableProp>();if(prop!=null)UnityEngine.Object.DestroyImmediate(prop);
            if(t!=hinge){var rb=t.GetComponent<Rigidbody>();if(rb!=null)UnityEngine.Object.DestroyImmediate(rb);}
        }
        var body=hinge.GetComponent<Rigidbody>();if(body==null)body=hinge.gameObject.AddComponent<Rigidbody>();
        body.isKinematic=true;body.useGravity=false;body.interpolation=RigidbodyInterpolation.Interpolate;
        var door=hinge.GetComponent<OfficeDoor>();if(door==null)door=hinge.gameObject.AddComponent<OfficeDoor>();
        door.leaf=leaf.GetComponent<BoxCollider>();door.player=GameObject.FindGameObjectWithTag("Player")?.transform;
        door.startOpen=false;door.SetOpen(false,true);
        Physics.SyncTransforms();EditorUtility.SetDirty(door);
        if(save){AssetDatabase.SaveAssets();var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        return "Connecting door saved closed; E opens/closes from either side, with animated hinge and solid collision.";
    }
    static Bounds Bounds(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    static void ConfigureRebates(Transform root,Material material)
    {
        var old=root.Find("Connecting door fitted jambs");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var frame=new GameObject("Connecting door fitted jambs").transform;frame.SetParent(root,false);
        FramePart(frame,"Latch side jamb",new Vector3(6.08f,1.35f,-1.27f),new Vector3(.32f,2.70f,.09f),material);
        FramePart(frame,"Hinge side jamb",new Vector3(6.08f,1.35f,.185f),new Vector3(.32f,2.70f,.09f),material);
        FramePart(frame,"Top jamb",new Vector3(6.08f,2.70f,-.55f),new Vector3(.32f,.12f,1.55f),material);
    }
    static void FramePart(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent);g.transform.position=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
    }
}
