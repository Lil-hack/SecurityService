using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class StylizedPostProcessingSetup
{
    public const string MenuProfile="Assets/Settings/MainMenuPostProcessing.asset";
    public const string GameProfile="Assets/Settings/ExecutiveOfficePostProcessing.asset";
    const string Captures=".local/PostProcessingQA";

    [MenuItem("Tools/Office/Apply Stylized Post Processing")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play before applying scene profiles.");
        var original=SceneManager.GetActiveScene();string originalPath=original.path;
        if(original.isDirty)EditorSceneManager.SaveScene(original);
        Directory.CreateDirectory(Captures);
        foreach(string path in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/ExecutiveOffice.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path);bool menu=scene.name=="MainMenu";
            CaptureScene(menu,"Before");
            var profile=CreateProfile(menu);
            var volume=Object.FindObjectsByType<Volume>().FirstOrDefault(v=>v.isGlobal&&v.name=="Atmosphere · evening studio");
            if(volume==null)volume=new GameObject("Stylized office post processing").AddComponent<Volume>();
            volume.isGlobal=true;volume.weight=1;volume.priority=30;volume.sharedProfile=profile;
            foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                if(!camera.CompareTag("MainCamera")&&camera.name!="Third-person gameplay camera"&&camera.name!="Main menu camera")continue;
                var data=camera.GetUniversalAdditionalCameraData();camera.allowHDR=true;
                data.renderPostProcessing=true;data.volumeLayerMask=~0;
                data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality=AntialiasingQuality.High;
                data.stopNaN=true;data.dithering=true;
                data.requiresDepthTexture=menu&&camera.name=="Main menu camera";
                EditorUtility.SetDirty(camera);EditorUtility.SetDirty(data);
            }
            EditorUtility.SetDirty(volume);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        foreach(string path in new[]{"Assets/Settings/PC_RPAsset.asset","Assets/Settings/Windows_Balanced_RP.asset"})
        {
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);if(pipeline==null)continue;
            pipeline.supportsHDR=true;pipeline.colorGradingMode=ColorGradingMode.HighDynamicRange;EditorUtility.SetDirty(pipeline);
        }
        AssetDatabase.SaveAssets();
        foreach(string path in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/ExecutiveOffice.unity"})
        {var scene=EditorSceneManager.OpenScene(path);CaptureScene(scene.name=="MainMenu","After");}
        if(!string.IsNullOrEmpty(originalPath))EditorSceneManager.OpenScene(originalPath);
        Debug.Log("Stylized post processing saved: separate HDR menu/game profiles, warm highlights / cool shadows, bloom, SMAA, restrained lens finish; distant menu DOF only. Captures: "+Captures);
    }

    static VolumeProfile CreateProfile(bool menu)
    {
        string path=menu?MenuProfile:GameProfile;var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
        T Effect<T>() where T:VolumeComponent
        {if(!profile.TryGet<T>(out var effect)){effect=profile.Add<T>(true);AssetDatabase.AddObjectToAsset(effect,profile);}effect.active=true;return effect;}
        var tone=Effect<Tonemapping>();tone.mode.Override(menu?TonemappingMode.ACES:TonemappingMode.Neutral);
        var colors=Effect<ColorAdjustments>();colors.postExposure.Override(menu?.32f:.4f);colors.contrast.Override(menu?13:12);colors.saturation.Override(menu?10:18);colors.colorFilter.Override(menu?new Color(1,.985f,.965f):Color.white);
        var white=Effect<WhiteBalance>();white.temperature.Override(menu?7:1);white.tint.Override(menu?1.5f:0);
        var split=Effect<SplitToning>();split.active=menu;split.shadows.Override(new Color(.455f,.495f,.53f));split.highlights.Override(new Color(.555f,.52f,.475f));split.balance.Override(8);
        if(!menu)
        {
            var wheels=Effect<LiftGammaGain>();
            wheels.lift.Override(new Vector4(1,1,1,0));
            wheels.gamma.Override(new Vector4(1,1,1,.06f));
            wheels.gain.Override(new Vector4(1,1,1,-.01f));
        }
        var bloom=Effect<Bloom>();bloom.intensity.Override(menu?.48f:.30f);bloom.threshold.Override(menu?1.15f:1.1f);bloom.scatter.Override(menu?.62f:.5f);bloom.tint.Override(menu?new Color(1,.92f,.82f):new Color(1,.98f,.94f));bloom.clamp.Override(menu?8:5);bloom.highQualityFiltering.Override(true);bloom.downscale.Override(BloomDownscaleMode.Half);bloom.maxIterations.Override(5);
        var vignette=Effect<Vignette>();vignette.color.Override(new Color(.018f,.035f,.055f));vignette.intensity.Override(menu?.25f:.09f);vignette.smoothness.Override(menu?.55f:.65f);vignette.rounded.Override(true);
        var grain=Effect<FilmGrain>();grain.type.Override(FilmGrainLookup.Thin1);grain.intensity.Override(menu?.035f:0);grain.response.Override(.85f);
        var chromatic=Effect<ChromaticAberration>();chromatic.intensity.Override(menu?.035f:0);
        var lens=Effect<LensDistortion>();lens.intensity.Override(menu?-.035f:0);lens.scale.Override(menu?1.015f:1);
        var dof=Effect<DepthOfField>();dof.mode.Override(menu?DepthOfFieldMode.Gaussian:DepthOfFieldMode.Off);dof.gaussianStart.Override(10);dof.gaussianEnd.Override(22);dof.gaussianMaxRadius.Override(.65f);dof.highQualitySampling.Override(true);
        var motion=Effect<MotionBlur>();motion.intensity.Override(0);
        foreach(var component in profile.components)EditorUtility.SetDirty(component);EditorUtility.SetDirty(profile);
        return profile;
    }

    [MenuItem("Tools/Office/Apply Cartoon Executive Office")]
    public static void ApplyCartoonOffice()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var scene=SceneManager.GetActiveScene();if(scene.isDirty)EditorSceneManager.SaveScene(scene);
        if(scene.path!="Assets/Scenes/ExecutiveOffice.unity")scene=EditorSceneManager.OpenScene("Assets/Scenes/ExecutiveOffice.unity");
        Directory.CreateDirectory(Captures);CaptureScene(false,"CartoonBefore");
        var profile=CreateProfile(false);
        var volume=Object.FindObjectsByType<Volume>().FirstOrDefault(v=>v.isGlobal&&v.name=="Atmosphere · evening studio");
        if(volume==null)volume=new GameObject("Cartoon office post processing").AddComponent<Volume>();
        volume.isGlobal=true;volume.sharedProfile=profile;volume.priority=30;volume.weight=1;
        foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if(!camera.CompareTag("MainCamera")&&camera.name!="Third-person gameplay camera")continue;
            camera.allowHDR=true;var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;
            data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;
            data.dithering=true;data.stopNaN=true;
            EditorUtility.SetDirty(camera);EditorUtility.SetDirty(data);
        }
        EditorUtility.SetDirty(volume);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        CaptureScene(false,"CartoonAfter");Selection.activeObject=profile;
        Debug.Log("ExecutiveOffice cartoon look saved: clean Neutral HDR palette, saturation +18, lifted shadows, soft bloom, SMAA; grain/fringing/distortion/DOF/motion blur off.");
    }

    [MenuItem("Tools/Office/Capture Stylized Post Processing")]
    public static void CaptureCurrent(){Directory.CreateDirectory(Captures);CaptureScene(SceneManager.GetActiveScene().name=="MainMenu","Current");}
    static void CaptureScene(bool menu,string suffix)
    {
        if(menu)
        {
            var view=Object.FindObjectsByType<Camera>().First(c=>c.name=="Main menu camera");
            Shot("MainMenu",view.transform.position,view.transform.position+view.transform.forward*8,view.fieldOfView,true,suffix);
        }
        else
        {
            Shot("Office",new Vector3(9,2.15f,-4.65f),new Vector3(14.4f,1.35f,.2f),58,false,suffix);
            Shot("Boss",new Vector3(3.5f,2.2f,-4.5f),new Vector3(-.3f,1.25f,.7f),70,false,suffix);
            Shot("Kitchen",new Vector3(25.8f,2.25f,8.1f),new Vector3(23.9f,1.35f,14.1f),70,false,suffix);
        }
    }
    static void Shot(string name,Vector3 position,Vector3 target,float fov,bool menu,string suffix)
    {
        var camera=new GameObject("Post processing preview"){hideFlags=HideFlags.HideAndDontSave}.AddComponent<Camera>();
        camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=fov;camera.nearClipPlane=.05f;camera.farClipPlane=100;camera.allowHDR=true;
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask=~0;data.volumeTrigger=camera.transform;
        data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;data.requiresDepthTexture=menu;
        var rt=RenderTexture.GetTemporary(1400,788,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;var texture=new Texture2D(1400,788,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1400,788),0,0);texture.Apply();File.WriteAllBytes(Captures+"/"+name+"_"+suffix+".png",texture.EncodeToPNG());}
        finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(texture);Object.DestroyImmediate(camera.gameObject);}
    }
}
