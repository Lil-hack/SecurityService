using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class TripoTextureSetup
{
    const string ModelPath="Assets/character/tripo_rigging_45cedad5-51a7-4486-a0c7-a845906cc520.fbx";
    const string TexturePath="Assets/character/Tripo_Albedo.png";
    const string MaterialPath="Assets/character/Tripo_Textured.mat";

    [MenuItem("Tools/Office/Apply Tripo Character Texture")]
    public static string Apply()
    {
        AssetDatabase.ImportAsset(TexturePath,ImportAssetOptions.ForceSynchronousImport);
        var textureImporter=(TextureImporter)AssetImporter.GetAtPath(TexturePath);
        textureImporter.textureType=TextureImporterType.Default;
        textureImporter.sRGBTexture=true; textureImporter.npotScale=TextureImporterNPOTScale.None;
        textureImporter.mipmapEnabled=true; textureImporter.maxTextureSize=2048;
        textureImporter.textureCompression=TextureImporterCompression.Uncompressed;
        textureImporter.filterMode=FilterMode.Bilinear; textureImporter.anisoLevel=4;
        textureImporter.wrapMode=TextureWrapMode.Clamp; textureImporter.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if(material==null)
        {
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Tripo Textured Character"};
            AssetDatabase.CreateAsset(material,MaterialPath);
        }
        material.color=Color.white; material.mainTexture=texture;
        material.SetFloat("_Metallic",0); material.SetFloat("_Smoothness",.22f);
        EditorUtility.SetDirty(material);
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
        var materialNames=model.GetComponentsInChildren<Renderer>(true)
            .SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.name).Distinct().ToArray();
        foreach(var name in materialNames)
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),material);
        importer.SaveAndReimport();
        model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        int updated=0;
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
        foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if(AssetDatabase.GetAssetPath(renderer.sharedMesh)!=ModelPath) continue;
            Undo.RecordObject(renderer,"Apply Tripo texture");
            renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>material).ToArray();
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); updated++;
        }
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name="Tripo Textured Character";
        PrefabUtility.SaveAsPrefabAsset(instance,"Assets/character/Tripo_Textured.prefab");
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        var scene=SceneManager.GetActiveScene();
        if(updated>0 && !string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
        return "Material mapped to FBX; textured prefab saved; scene instances updated: "+updated;
    }

    public static void Capture()
    {
        Directory.CreateDirectory("OfficePreview");
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/character/Tripo_Textured.prefab");
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.transform.position=new Vector3(50,0,0);
        foreach(var t in instance.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=31;
        var rs=instance.GetComponentsInChildren<Renderer>(true); var bounds=rs[0].bounds;
        foreach(var r in rs) bounds.Encapsulate(r.bounds);
        var camera=new GameObject("Temporary Tripo preview camera").AddComponent<Camera>();
        camera.cullingMask=1<<31; camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.19f,.22f,.26f); camera.orthographic=true;
        camera.orthographicSize=Mathf.Max(bounds.size.y*.65f,bounds.size.z*.60f);
        camera.nearClipPlane=.01f; camera.farClipPlane=30;
        var light=new GameObject("Temporary Tripo preview light").AddComponent<Light>();
        light.type=LightType.Directional; light.intensity=1.6f; light.cullingMask=1<<31;
        light.transform.rotation=Quaternion.Euler(30,-30,0);
        var rt=new RenderTexture(1000,1100,24);
        try
        {
            foreach(var view in new[]{"Front","Side","Back","ThreeQuarter"})
            {
                Vector3 offset=view=="Front"?new Vector3(-3,.08f,0):view=="Side"?new Vector3(0,.08f,-3):view=="Back"?new Vector3(3,.08f,0):new Vector3(-2.8f,.18f,-1.6f);
                camera.transform.position=bounds.center+offset;
                camera.transform.LookAt(bounds.center);
                camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
                var texture=new Texture2D(1000,1100,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,1000,1100),0,0); texture.Apply();
                File.WriteAllBytes("OfficePreview/Tripo_"+view+".png",texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
        }
        finally
        {
            camera.targetTexture=null; RenderTexture.active=null;
            Object.DestroyImmediate(rt); Object.DestroyImmediate(light.gameObject);
            Object.DestroyImmediate(camera.gameObject); Object.DestroyImmediate(instance);
        }
    }
}
