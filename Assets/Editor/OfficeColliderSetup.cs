using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeColliderSetup
{
    // Backdrop photography and reflection streaks are visual effects rather than objects.
    static bool VisualOnly(Transform t)
    {
        return t.name=="Sky beyond window" || t.name=="Soft diagonal sheen" || t.name=="Fine diagonal glint";
    }

    [MenuItem("Tools/Office/Add Appropriate Scene Colliders")]
    public static string Apply()
    {
        var scene=SceneManager.GetActiveScene();
        if(!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open the office scene first.");
        Undo.SetCurrentGroupName("Add appropriate office colliders");
        int added=0, corrected=0, visualRemoved=0;
        var filters=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).ToArray();
        // One continuous floor shape avoids seams between individual parquet blocks
        // and replaces hundreds of otherwise identical static box colliders.
        var floor=filters.Where(f=>f.name=="Parquet block").ToArray();
        if(floor.Length>0)
        {
            var parent=floor[0].transform;
            while(parent.parent!=null && parent.name!="01 Architecture and walnut joinery") parent=parent.parent;
            var floorGroup=parent.Find("Continuous parquet collision");
            if(floorGroup==null)
            {
                var group=new GameObject("Continuous parquet collision");
                Undo.RegisterCreatedObjectUndo(group,"Continuous floor collider");
                floorGroup=group.transform; floorGroup.SetParent(parent,false);
            }
            var bounds=floor[0].GetComponent<Renderer>().bounds;
            foreach(var block in floor) bounds.Encapsulate(block.GetComponent<Renderer>().bounds);
            var box=floorGroup.GetComponent<BoxCollider>();
            if(box==null) box=Undo.AddComponent<BoxCollider>(floorGroup.gameObject);
            Undo.RecordObject(box,"Floor collision dimensions");
            box.center=floorGroup.InverseTransformPoint(bounds.center);
            box.size=new Vector3(Mathf.Ceil(bounds.size.x*2f)/2f,bounds.size.y,Mathf.Ceil(bounds.size.z*2f)/2f);
            foreach(var block in floor)
            {
                foreach(var c in block.GetComponents<Collider>()) Undo.DestroyObjectImmediate(c);
                if(block.transform.parent!=floorGroup) Undo.SetTransformParent(block.transform,floorGroup,"Group parquet collision");
            }
        }
        foreach(var filter in filters)
        {
            if(filter.sharedMesh==null || filter.GetComponent<Renderer>()==null) continue;
            var go=filter.gameObject;
            if(VisualOnly(filter.transform))
            {
                foreach(var c in go.GetComponents<Collider>()) { Undo.DestroyObjectImmediate(c); visualRemoved++; }
                continue;
            }
            // Unity's default cylinder collider is a capsule, which is unsuitable for
            // thin coasters and trays: its rounded ends extend above the visible object.
            if(filter.sharedMesh.name=="Cylinder")
            {
                foreach(var c in go.GetComponents<CapsuleCollider>()) { Undo.DestroyObjectImmediate(c); corrected++; }
            }
            if(HasCollider(filter.transform)) continue;
            string objectName=go.transform.parent==null?go.name:go.transform.parent.name;
            bool simpleBox=filter.sharedMesh.name=="Cube" || filter.sharedMesh.name=="Quad"
                || objectName.Contains("Book_Group") || objectName.Contains("Printer_Paper")
                || objectName.Contains("Papers_");
            if(simpleBox)
            {
                var box=Undo.AddComponent<BoxCollider>(go);
                var bounds=filter.sharedMesh.bounds;
                box.center=bounds.center;
                box.size=new Vector3(Mathf.Max(bounds.size.x,.008f),Mathf.Max(bounds.size.y,.008f),Mathf.Max(bounds.size.z,.008f));
            }
            else if(filter.sharedMesh.name=="Sphere")
            {
                var sphere=Undo.AddComponent<SphereCollider>(go);
                sphere.center=filter.sharedMesh.bounds.center;
                sphere.radius=filter.sharedMesh.bounds.extents.x;
            }
            else
            {
                // Furniture retains its openings: a chair, shelf or desk does not become
                // a solid bounding box. Static mesh colliders require no Rigidbody.
                var mesh=Undo.AddComponent<MeshCollider>(go);
                mesh.sharedMesh=filter.sharedMesh; mesh.convex=false;
            }
            added++;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if(!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
        var report="Added "+added+" colliders; corrected "+corrected+" cylinder shapes; removed "+visualRemoved+" visual-only colliders. "+Validate();
        Debug.Log(report); return report;
    }

    static bool HasCollider(Transform t)
    {
        for(var current=t;current!=null;current=current.parent)
            if(current.GetComponents<Collider>().Any(c=>c.enabled && !c.isTrigger)) return true;
        return false;
    }

    public static string Validate()
    {
        var scene=SceneManager.GetActiveScene();
        var filters=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true))
            .Where(f=>f.sharedMesh!=null && f.GetComponent<Renderer>()!=null && !VisualOnly(f.transform)).ToArray();
        var missing=filters.Where(f=>!HasCollider(f.transform)).Select(f=>f.name).ToArray();
        var colliders=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).ToArray();
        var invalid=colliders.OfType<MeshCollider>().Where(c=>c.sharedMesh==null).ToArray();
        if(missing.Length>0 || invalid.Length>0)
            throw new InvalidOperationException("Missing colliders: "+string.Join(", ",missing)+"; invalid mesh colliders: "+invalid.Length);
        return "Verified "+filters.Length+" physical meshes, "+colliders.Length+" colliders, no missing or empty shapes.";
    }
}
