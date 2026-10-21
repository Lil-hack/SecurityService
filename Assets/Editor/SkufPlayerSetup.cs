using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SkufPlayerSetup
{
    public const string ModelPath="Assets/character/skuf/skuf.fbx";
    public const string PrefabPath="Assets/character/skuf/Skuf_Textured.prefab";
    const string Folder="Assets/character/skuf/Textures/";
    const string MaterialPath="Assets/character/skuf/Skuf_URP.mat";
    const string ControllerPath="Assets/character/Animations/TripoLocomotion.controller";

    [MenuItem("Tools/Office/Use Textured Skuf As Player")]
    public static string Apply()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Stop Play mode before replacing the player model.");
        AssetDatabase.Refresh();
        string albedo=Find("_BaseColor.jpg"),normal=Find("_Normal_Bake.jpg"),roughness=Find("_Roughness.jpg"),metallic=Find("_Metallic.jpg");
        ConfigureTexture(albedo,true,false,4096);
        ConfigureTexture(normal,false,true,2048);
        ConfigureTexture(roughness,false,false,2048);
        ConfigureTexture(metallic,false,false,2048);
        ConfigureTexture(Folder+"Skuf_MetallicSmoothness.png",false,false,2048);
        var mat=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if(mat==null) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Skuf original PBR textures"}; AssetDatabase.CreateAsset(mat,MaterialPath); }
        mat.SetColor("_BaseColor",Color.white);
        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));
        mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
        mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Skuf_MetallicSmoothness.png"));
        mat.SetFloat("_BumpScale",1); mat.SetFloat("_Metallic",1); mat.SetFloat("_Smoothness",1);
        mat.SetFloat("_SmoothnessTextureChannel",0);
        mat.EnableKeyword("_NORMALMAP"); mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(mat);
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var materialNames=model.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.name).Distinct().ToArray();
        var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.animationType=ModelImporterAnimationType.Human;
        importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;
        foreach(string name in materialNames) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),mat);
        importer.SaveAndReimport();
        model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var sourceAnimator=model.GetComponent<Animator>();
        if(sourceAnimator==null || sourceAnimator.avatar==null || !sourceAnimator.avatar.isValid || !sourceAnimator.avatar.isHuman)
            throw new InvalidOperationException("Skuf Humanoid Avatar could not be configured automatically.");
        var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if(controller==null) throw new FileNotFoundException("Player locomotion controller is missing.");
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(model);
        try
        {
            instance.name="Skuf textured Humanoid";
            var a=instance.GetComponent<Animator>(); a.runtimeAnimatorController=controller; a.applyRootMotion=false; a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var r in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray(); r.updateWhenOffscreen=true; }
            instance.AddComponent<TripoArmClearance>().minimumArmAngle=25;
            PrefabUtility.SaveAsPrefabAsset(instance,PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
        var player=UnityEngine.Object.FindFirstObjectByType<OfficeThirdPersonController>();
        if(player==null) throw new InvalidOperationException("The office third-person controller is missing.");
        Quaternion facing=player.visualRoot!=null?player.visualRoot.rotation:player.transform.rotation;
        if(player.visualRoot!=null) UnityEngine.Object.DestroyImmediate(player.visualRoot.gameObject);
        var visual=new GameObject("Skuf animated visual").transform; visual.SetParent(player.transform,false); visual.rotation=facing;
        var character=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        character.transform.SetParent(visual,false);
        var animator=character.GetComponent<Animator>();
        animator.Rebind(); animator.SetBool("Grounded",true); animator.SetFloat("Speed",0); animator.Play("Locomotion",0,0); animator.Update(.2f);
        var direction=Vector3.Cross(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position-animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position,Vector3.up);
        direction=visual.InverseTransformDirection(direction);
        float yaw=Mathf.Round(Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg/90)*90;
        character.transform.localRotation=Quaternion.Euler(0,-yaw,0);
        var bounds=TripoHumanoidAnimationSetup.SkinnedBoundsOf(character);
        character.transform.localScale*=1.72f/bounds.size.y;
        animator.Update(0); bounds=TripoHumanoidAnimationSetup.SkinnedBoundsOf(character);
        var centre=visual.InverseTransformPoint(bounds.center);
        var bottom=visual.InverseTransformPoint(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
        character.transform.localPosition-=new Vector3(centre.x,bottom.y,centre.z);
        foreach(var t in character.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=2;
        player.visualRoot=visual; player.humanoidAnimator=animator;
        player.leftArm=null; player.rightArm=null; player.leftLeg=null; player.rightLeg=null;
        player.RefreshVisual(); player.UpdateCamera(); character.GetComponent<TripoArmClearance>().ApplyPose();
        AssetDatabase.SaveAssets();
        var scene=SceneManager.GetActiveScene(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Skuf player: original albedo/normal/metallic-smoothness textures, valid Humanoid Avatar, Idle/Walk/Run/Jump/Fall. Controller and camera preserved.";
    }

    static string Find(string suffix)
    {
        var path=Directory.GetFiles(Folder).FirstOrDefault(p=>p.EndsWith(suffix,StringComparison.OrdinalIgnoreCase));
        if(path==null) throw new FileNotFoundException("Run Tools/extract_skuf_textures.py first: missing "+suffix);
        return path.Replace('\\','/');
    }
    static void ConfigureTexture(string path,bool color,bool normal,int resolution)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        importer.convertToNormalmap=false; importer.sRGBTexture=color;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency=false; importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=resolution; importer.mipmapEnabled=true;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        importer.wrapMode=TextureWrapMode.Clamp; importer.anisoLevel=4; importer.SaveAndReimport();
    }
}
