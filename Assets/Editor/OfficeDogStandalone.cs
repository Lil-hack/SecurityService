using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class OfficeDogStandalone
{
    const string Folder="Assets/character/officeperson/dog/";
    const string Prefab=Folder+"Office_Dog.prefab";
    const string Source=Folder+"tripo_rigging_671adc53-cf71-4c6f-b018-1e0965674dde_meshopt.glb";
    [MenuItem("Tools/Office/Extract Dog Assets And Remove GLB")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first.");
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        try
        {
            PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            Directory.CreateDirectory(Folder+"Meshes");AssetDatabase.Refresh();
            var meshes=new Dictionary<Mesh,Mesh>();int index=0;
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var source=skin.sharedMesh;if(source==null)throw new Exception("Missing dog mesh.");
                if(!meshes.TryGetValue(source,out var mesh))
                {
                    mesh=UnityEngine.Object.Instantiate(source);mesh.name="Dog_SkinnedMesh"+(index==0?"":"_"+index);
                    AssetDatabase.CreateAsset(mesh,Folder+"Meshes/"+mesh.name+".asset");meshes[source]=mesh;index++;
                }
                skin.sharedMesh=mesh;
            }
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter.sharedMesh==null)continue;
                if(!meshes.TryGetValue(filter.sharedMesh,out var mesh))
                {mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name="Dog_Mesh_"+index++;AssetDatabase.CreateAsset(mesh,Folder+"Meshes/"+mesh.name+".asset");meshes[filter.sharedMesh]=mesh;}
                filter.sharedMesh=mesh;
            }
            var rig=root.transform.Find("Dog Rig");if(rig==null)throw new Exception("Dog rig root missing.");
            PrefabUtility.SaveAsPrefabAssetAndConnect(rig.gameObject,Folder+"Dog_Rig.prefab",InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);AssetDatabase.SaveAssets();
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        Validate();
        foreach(string path in new[]{Prefab,Folder+"Dog_Rig.prefab"})
            if(AssetDatabase.GetDependencies(path,true).Any(p=>p.EndsWith(".glb",StringComparison.OrdinalIgnoreCase)))throw new Exception("GLB dependency remains: "+path);
        if(File.Exists(Source) && !AssetDatabase.DeleteAsset(Source))throw new Exception("Could not delete source GLB.");
        AssetDatabase.Refresh();AssetDatabase.ImportAsset(Prefab,ImportAssetOptions.ForceUpdate);
        Validate();
        File.WriteAllText(Folder+"README.md","Office_Dog.prefab: standalone dog with Generic Avatar, collider and URP texture.\nDog_Rig.prefab: reusable original 17-bone skeleton and skinned renderer.\nMeshes/: native Unity mesh assets with bind poses and skin weights.\nTextures/Dog_Albedo.jpg: original embedded texture.\nSource GLB removed after verifying zero GLB dependencies and bone deformation. No source animation clips.\n");
        Debug.Log("PASS dog prefab and rig independent; GLB deleted; skin deformation verified after deletion.");
    }
    [MenuItem("Tools/Office/Validate Standalone Dog")]
    public static void Validate()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        if(prefab==null)throw new Exception("Dog prefab missing.");
        var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            var animator=root.GetComponent<Animator>();if(animator.avatar==null || !animator.avatar.isValid)throw new Exception("Invalid dog Avatar.");
            var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length==0)throw new Exception("No skin.");
            foreach(var skin in skins)
            {
                if(skin.sharedMesh==null || skin.bones.Length!=17 || skin.bones.Any(b=>b==null) || skin.sharedMesh.bindposes.Length!=17)throw new Exception("Broken bone or mesh bindings.");
                if(skin.sharedMaterials.Any(m=>m==null || m.mainTexture==null))throw new Exception("Missing dog material/texture.");
                var bone=skin.bones.First(b=>b.childCount>0);var rotation=bone.localRotation;var a=new Mesh();var b=new Mesh();
                try
                {
                    skin.BakeMesh(a);bone.localRotation*=Quaternion.Euler(0,20,0);skin.BakeMesh(b);bone.localRotation=rotation;
                    var av=a.vertices;var bv=b.vertices;float displacement=0;for(int i=0;i<av.Length;i++)displacement=Mathf.Max(displacement,Vector3.Distance(av[i],bv[i]));
                    if(displacement<.0001f)throw new Exception("Dog skin stopped deforming.");
                }
                finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
