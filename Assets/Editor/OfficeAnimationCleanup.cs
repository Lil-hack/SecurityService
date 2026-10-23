using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeAnimationCleanup
{
    const string Folder="Assets/character/officeperson/FBX/";
    static string[] Unused(out string[] kept)
    {
        var candidates=Directory.GetFiles(Folder,"*.fbx",SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')).ToArray();
        var candidateSet=new HashSet<string>(candidates,StringComparer.OrdinalIgnoreCase);
        var roots=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(p) && !candidateSet.Contains(p)).ToArray();
        var used=new HashSet<string>(AssetDatabase.GetDependencies(roots,true),StringComparer.OrdinalIgnoreCase);
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);if(!scene.isLoaded)continue;
            foreach(var obj in EditorUtility.CollectDependencies(scene.GetRootGameObjects()))
            {string path=AssetDatabase.GetAssetPath(obj);if(!string.IsNullOrEmpty(path))used.Add(path);}
        }
        kept=candidates.Where(used.Contains).OrderBy(p=>p).ToArray();
        return candidates.Where(p=>!used.Contains(p)).OrderBy(p=>p).ToArray();
    }
    [MenuItem("Tools/Office/Audit Unused FBX Animations")]
    public static void Audit()
    {
        var unused=Unused(out var kept);Directory.CreateDirectory("OfficePreview");
        File.WriteAllText("OfficePreview/UnusedFbxAudit.txt","KEEP ("+kept.Length+")\n"+string.Join("\n",kept)+"\nDELETE ("+unused.Length+")\n"+string.Join("\n",unused));
        Debug.Log("FBX audit: keep "+kept.Length+", unused "+unused.Length);
    }
    [MenuItem("Tools/Office/Delete Unused FBX Animations")]
    public static void Delete()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first.");
        var unused=Unused(out var kept);
        foreach(string path in unused)if(!AssetDatabase.DeleteAsset(path))throw new Exception("Cannot delete "+path);
        AssetDatabase.Refresh();
        foreach(string path in kept)if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null)throw new Exception("Used FBX unavailable: "+path);
        File.AppendAllText("OfficePreview/UnusedFbxAudit.txt","\nDELETED "+unused.Length+" unused FBXs with metadata; preserved "+kept.Length+" referenced files.\n");
        Debug.Log("PASS deleted "+unused.Length+" unused animation FBXs, preserved "+kept.Length+" referenced FBXs.");
    }
}
