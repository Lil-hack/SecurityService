using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Bake only silhouette geometry. Never replace an actor's mesh, materials or colliders.
public static class OfficeOutlineSetup
{
    const string Folder="Assets/ExecutiveOffice/Outlines";
    static OfficeOutlineSettings settings;
    static Material material;

    [MenuItem("Tools/Office/Apply Cartoon Silhouettes")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play before baking outlines.");
        string original=SceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();
        Directory.CreateDirectory(Folder+"/Meshes");AssetDatabase.Refresh();
        settings=AssetDatabase.LoadAssetAtPath<OfficeOutlineSettings>("Assets/Settings/OfficeOutlineSettings.asset");
        if(settings==null){settings=ScriptableObject.CreateInstance<OfficeOutlineSettings>();AssetDatabase.CreateAsset(settings,"Assets/Settings/OfficeOutlineSettings.asset");}
        material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Silhouette.mat");
        if(material==null){material=new Material(Shader.Find("Office/Toon Silhouette"));AssetDatabase.CreateAsset(material,Folder+"/Silhouette.mat");}
        string[] prefabs={"Assets/character/skuf/Skuf_Textured.prefab","Assets/character/altgirl/Altgirl_Textured.prefab","Assets/character/tubik/Tubik_Textured.prefab","Assets/character/shtrih/Shtrih_Textured.prefab","Assets/character/boss/Tripo_Textured.prefab","Assets/character/officeperson/man1/Staff.prefab","Assets/character/officeperson/man2/Staff.prefab","Assets/character/officeperson/girl1/Staff.prefab","Assets/character/officeperson/girl2/Staff.prefab","Assets/character/officeperson/dog/Office_Dog.prefab"};
        int actors=0,props=0;
        foreach(string path in prefabs)
        {
            if(!File.Exists(path))throw new FileNotFoundException(path);
            var root=PrefabUtility.LoadPrefabContents(path);
            try{foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(Original).ToArray()){SetupSkin(skin);actors++;}PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(string path in new[]{"Assets/Scenes/ExecutiveOffice.unity","Assets/Scenes/MainMenu.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path);
            foreach(var skin in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(Original).ToArray()){SetupSkin(skin);actors++;}
            foreach(var prop in Object.FindObjectsByType<OfficePushableProp>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!prop.allowPickup||!Important(prop.name))continue;
                if(SetupProp(prop.gameObject))props++;
            }
            foreach(var laptop in Object.FindObjectsByType<LaptopDesktop>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(SetupProp(laptop.gameObject))props++;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        Selection.activeObject=settings;
        Debug.Log("Cartoon silhouettes saved: "+actors+" actor meshes (including prefabs), "+props+" interactive props. Face/hand width mask, shared smoothed meshes, depth-tested ink.");
    }

    static bool Original(Renderer renderer)=>renderer.sharedMaterial!=material&&!renderer.name.StartsWith("Toon silhouette");
    static bool Important(string name)
    {
        name=name.ToLowerInvariant();
        return new[]{"mug","coffee cup","guest mug","bottle","orange","fruit bowl","pantry jar","journal","notebook","document","smartphone","nameplate","coffee tray","pouf","cushion","bolster","printer","globe","trophy","coffee pot"}.Any(name.Contains);
    }
    static void SetupSkin(SkinnedMeshRenderer skin)
    {
        var source=skin.sharedMesh;if(source==null)return;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id);
        string path=Folder+"/Meshes/Skin_"+guid+"_"+id+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            mesh=Object.Instantiate(source);mesh.name=source.name+" · smooth silhouette";
            var vertices=ReadVertices(source);Smooth(mesh,vertices);
            var colors=Enumerable.Repeat(Color.white,vertices.Length).ToArray();
            var binds=source.bindposes;var bones=skin.bones;
            float size=source.bounds.size.magnitude;
            for(int b=0;b<Math.Min(bones.Length,binds.Length);b++)
            {
                if(bones[b]==null)continue;string name=bones[b].name.ToLowerInvariant();
                bool head=name.Contains("head"),hand=name.Contains("hand")&&!name.Contains("finger");if(!head&&!hand)continue;
                var center=binds[b].inverse.MultiplyPoint3x4(Vector3.zero);float radius=size*(head?.13f:.055f);
                for(int i=0;i<vertices.Length;i++){float t=Mathf.SmoothStep(0,1,(Vector3.Distance(vertices[i],center)-radius*.75f)/(radius*.4f));colors[i].a=Mathf.Min(colors[i].a,t);}
            }
            mesh.colors=colors;AssetDatabase.CreateAsset(mesh,path);
        }
        Configure(skin.gameObject,mesh,new Renderer[]{skin},skin);
    }
    static bool SetupProp(GameObject root)
    {
        var sources=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>Original(r)&&r.GetComponent<TextMesh>()==null&&r.GetComponent<MeshFilter>()?.sharedMesh!=null&&r.sharedMaterials.All(m=>m!=null&&m.renderQueue<3000)).ToArray();
        if(sources.Length==0)return false;
        var bounds=sources[0].bounds;foreach(var r in sources)bounds.Encapsulate(r.bounds);
        if(bounds.size.magnitude>3.5f||Mathf.Min(bounds.size.x,bounds.size.y,bounds.size.z)<.008f)return false;
        var vertices=new List<Vector3>();var triangles=new List<int>();
        foreach(var renderer in sources)
        {
            var source=renderer.GetComponent<MeshFilter>().sharedMesh;
            var transform=root.transform.worldToLocalMatrix*renderer.localToWorldMatrix;
            using(var data=MeshUtility.AcquireReadOnlyMeshData(source))
            {
                var info=data[0];var local=new NativeArray<Vector3>(info.vertexCount,Allocator.Temp);
                try{info.GetVertices(local);foreach(var v in local)vertices.Add(transform.MultiplyPoint3x4(v));}
                finally{local.Dispose();}
                int offset=vertices.Count-info.vertexCount;
                for(int s=0;s<info.subMeshCount;s++)
                {
                    var descriptor=info.GetSubMesh(s);if(descriptor.topology!=MeshTopology.Triangles)continue;
                    var indices=new NativeArray<int>(descriptor.indexCount,Allocator.Temp);
                    try{info.GetIndices(indices,s,true);foreach(int index in indices)triangles.Add(index+offset);}
                    finally{indices.Dispose();}
                }
            }
        }
        if(triangles.Count==0)return false;
        // Geometry hashing also reuses identical procedural cups and upholstered stools.
        var key=new StringBuilder("silhouette-v1");foreach(var v in vertices)key.Append(Mathf.RoundToInt(v.x*10000)).Append(',').Append(Mathf.RoundToInt(v.y*10000)).Append(',').Append(Mathf.RoundToInt(v.z*10000)).Append(';');foreach(int index in triangles)key.Append(index).Append(',');
        string path=Folder+"/Meshes/Prop_"+Hash128.Compute(key.ToString())+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            mesh=new Mesh{name="Composite prop silhouette",indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();Smooth(mesh,vertices.ToArray());
            mesh.colors=Enumerable.Repeat(Color.white,vertices.Count).ToArray();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        Configure(root,mesh,sources,null);return true;
    }
    static Vector3[] ReadVertices(Mesh mesh)
    {
        using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))
        {
            var vertices=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp);
            try{data[0].GetVertices(vertices);return vertices.ToArray();}finally{vertices.Dispose();}
        }
    }
    static void Smooth(Mesh mesh,Vector3[] vertices)
    {
        Vector3[] normals;
        using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))
        {
            var values=new NativeArray<Vector3>(vertices.Length,Allocator.Temp);
            try{data[0].GetNormals(values);normals=values.ToArray();}finally{values.Dispose();}
        }
        var groups=new Dictionary<Vector3Int,Vector3>();var keys=new Vector3Int[vertices.Length];
        for(int i=0;i<vertices.Length;i++){var v=vertices[i];var key=new Vector3Int(Mathf.RoundToInt(v.x*10000),Mathf.RoundToInt(v.y*10000),Mathf.RoundToInt(v.z*10000));keys[i]=key;groups.TryGetValue(key,out var normal);groups[key]=normal+normals[i];}
        for(int i=0;i<vertices.Length;i++)normals[i]=groups[keys[i]].normalized;
        mesh.normals=normals;
    }
    static void Configure(GameObject root,Mesh mesh,Renderer[] sources,SkinnedMeshRenderer skin)
    {
        var outline=root.GetComponent<OfficeToonOutline>();if(outline==null)outline=root.AddComponent<OfficeToonOutline>();
        outline.settings=settings;outline.outlineMaterial=material;outline.outlineMesh=mesh;outline.sources=sources;outline.animatedSource=skin;outline.character=skin!=null;outline.Rebuild();
        EditorUtility.SetDirty(outline);if(PrefabUtility.IsPartOfPrefabInstance(outline))PrefabUtility.RecordPrefabInstancePropertyModifications(outline);
    }
}
