using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StudioLoungeShelfRemoval
{
    [MenuItem("Tools/Office/Remove Shelf Behind Lounge Sofa")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode first.");
        var root=GameObject.Find("05 Open plan studio office").transform;
        var shelf=root.Cast<Transform>().Where(t=>t.name=="Studio storage cabinet" && t.gameObject.activeSelf)
            .OrderBy(t=>Vector3.Distance(t.position,new Vector3(24.15f,0,7.28f))).First();
        if(Vector3.Distance(shelf.position,new Vector3(24.15f,0,7.28f))>1)throw new Exception("Expected lounge shelf not found.");
        var p=shelf.position;int count=0;
        foreach(var t in root.Cast<Transform>().Where(t=>t==shelf || ((t.name=="Storage books" || t.name=="Studio potted plant") && Mathf.Abs(t.position.x-p.x)<1.1f && Mathf.Abs(t.position.z-p.z)<.55f)).ToArray())
        {Undo.RecordObject(t.gameObject,"Remove shelf behind sofa");t.gameObject.SetActive(false);count++;}
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveScene(root.gameObject.scene);
        System.IO.File.WriteAllText("OfficePreview/LoungeShelfRemoval.txt","Removed lounge shelf and its books/plant at "+p+"; disabled "+count+" objects.");
    }
}

