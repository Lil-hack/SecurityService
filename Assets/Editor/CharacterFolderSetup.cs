using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CharacterFolderSetup
{
    const string ControllerPath="Assets/character/boss/Animations/TripoLocomotion.controller";

    [MenuItem("Tools/Office/Prepare Character Folders")]
    public static string ApplyAll()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Stop Play mode before preparing characters.");
        AssetDatabase.Refresh();
        return string.Join("\n",new[]{"altgirl","tubik","shtrih","boss"}.Select(Apply));
    }

    public static string Apply(string folderName)
    {
        string folder="Assets/character/"+folderName;
        string model=Directory.GetFiles(folder,"*.fbx").Select(p=>p.Replace('\\','/')).Single();
        string textures=folder+"/Textures/";
        string title=char.ToUpper(folderName[0])+folderName.Substring(1);
        bool boss=folderName=="boss";
        string materialPath=boss?folder+"/Tripo_Textured.mat":folder+"/"+title+"_URP.mat";
        string prefabPath=boss?folder+"/Tripo_Textured.prefab":folder+"/"+title+"_Textured.prefab";
        string albedo=Find(textures,"_BaseColor.jpg")??Find(textures,".jpg");
        string normal=Find(textures,"_Normal_Bake.jpg");
        string roughness=Find(textures,"_Roughness.jpg");
        string metallic=Find(textures,"_Metallic.jpg");
        if(albedo==null) throw new FileNotFoundException("No albedo in "+textures);
        ConfigureTexture(albedo,true,false,4096);
        if(normal!=null) ConfigureTexture(normal,false,true,2048);
        if(roughness!=null) ConfigureTexture(roughness,false,false,2048);
        if(metallic!=null) ConfigureTexture(metallic,false,false,2048);
        string packed=null;
        if(metallic!=null && roughness!=null)
        {
            packed=textures+title+"_MetallicSmoothness.png";
            PackMetallicSmoothness(metallic,roughness,packed);
            ConfigureTexture(packed,false,false,2048);
        }
        var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(mat==null)
        {
            mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=title+" original textures"};
            AssetDatabase.CreateAsset(mat,materialPath);
        }
        mat.SetColor("_BaseColor",Color.white);
        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));
        mat.SetTexture("_BumpMap",normal==null?null:AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
        mat.SetTexture("_MetallicGlossMap",packed==null?null:AssetDatabase.LoadAssetAtPath<Texture2D>(packed));
        mat.SetFloat("_BumpScale",1);
        mat.SetFloat("_Metallic",packed==null?0:1);
        mat.SetFloat("_Smoothness",packed==null?.35f:1);
        mat.SetFloat("_SmoothnessTextureChannel",0);
        if(normal!=null) mat.EnableKeyword("_NORMALMAP"); else mat.DisableKeyword("_NORMALMAP");
        if(packed!=null) mat.EnableKeyword("_METALLICSPECGLOSSMAP"); else mat.DisableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(mat);
        var importer=(ModelImporter)AssetImporter.GetAtPath(model);
        importer.animationType=ModelImporterAnimationType.Human;
        importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>(model);
        var materialNames=preview.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.name).Distinct().ToArray();
        foreach(string name in materialNames) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),mat);
        importer.SaveAndReimport();
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(model);
        var sourceAnimator=source.GetComponent<Animator>();
        if(sourceAnimator==null || sourceAnimator.avatar==null || !sourceAnimator.avatar.isValid || !sourceAnimator.avatar.isHuman)
            throw new InvalidOperationException(title+" Humanoid Avatar could not be configured.");
        var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if(controller==null) throw new FileNotFoundException("Player locomotion controller is missing.");
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);
        try
        {
            instance.name=title+"_Textured";
            var animator=instance.GetComponent<Animator>();
            animator.runtimeAnimatorController=controller;
            animator.applyRootMotion=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>mat).ToArray();
                renderer.updateWhenOffscreen=true;
            }
            if(instance.GetComponent<TripoArmClearance>()==null) instance.AddComponent<TripoArmClearance>().minimumArmAngle=25;
            PrefabUtility.SaveAsPrefabAsset(instance,prefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
        AssetDatabase.SaveAssets();
        string maps=packed!=null?"albedo, normal, metallic-smoothness":"albedo";
        return title+": "+maps+", Humanoid prefab "+prefabPath;
    }

    static void PackMetallicSmoothness(string metallicPath,string roughnessPath,string outputPath)
    {
        var metallic=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        var roughness=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        if(!metallic.LoadImage(File.ReadAllBytes(metallicPath)) || !roughness.LoadImage(File.ReadAllBytes(roughnessPath)))
            throw new InvalidOperationException("Could not read metallic or roughness JPEG.");
        if(metallic.width!=roughness.width || metallic.height!=roughness.height)
            throw new InvalidOperationException("Metallic and roughness sizes differ.");
        var metal=metallic.GetPixels32();
        var rough=roughness.GetPixels32();
        var packed=new Color32[metal.Length];
        for(int i=0;i<packed.Length;i++)
            packed[i]=new Color32(metal[i].r,0,0,(byte)(255-rough[i].r));
        var texture=new Texture2D(metallic.width,metallic.height,TextureFormat.RGBA32,false,true);
        texture.SetPixels32(packed);
        texture.Apply(false);
        File.WriteAllBytes(outputPath,texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(metallic);
        UnityEngine.Object.DestroyImmediate(roughness);
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(outputPath,ImportAssetOptions.ForceSynchronousImport);
    }

    static string Find(string folder,string suffix)
    {
        if(!Directory.Exists(folder)) return null;
        return Directory.GetFiles(folder).Select(p=>p.Replace('\\','/')).FirstOrDefault(p=>p.EndsWith(suffix,StringComparison.OrdinalIgnoreCase));
    }

    static void ConfigureTexture(string path,bool color,bool normal,int resolution)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        importer.convertToNormalmap=false;
        importer.sRGBTexture=color;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency=false;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=resolution;
        importer.mipmapEnabled=true;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.anisoLevel=4;
        importer.SaveAndReimport();
    }
}
