using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class WindowsBuildProfileSetup
{
    public const string ProfilePath="Assets/Settings/Build Profiles/Windows Balanced Release.asset";
    const string QualityName="Windows Balanced";
    const string PipelinePath="Assets/Settings/Windows_Balanced_RP.asset";
    const string RendererPath="Assets/Settings/Windows_Balanced_Renderer.asset";
    const BindingFlags Internal=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

    [MenuItem("Tools/Build/Configure Windows Balanced Release")]
    public static string Apply()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Stop Play mode before configuring the build profile.");
        if(!File.Exists(ProfilePath)) AssetDatabase.CopyAsset("Assets/Settings/Build Profiles/Windows.asset",ProfilePath);
        if(!File.Exists(RendererPath)) AssetDatabase.CopyAsset("Assets/Settings/PC_Renderer.asset",RendererPath);
        if(!File.Exists(PipelinePath)) AssetDatabase.CopyAsset("Assets/Settings/PC_RPAsset.asset",PipelinePath);
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        // In Unity 6.6 even inactive features run Create(). Remove SSAO entirely:
        // its stripped resources otherwise make the standalone pipeline fail to start.
        foreach(var feature in AssetDatabase.LoadAllAssetsAtPath(RendererPath).OfType<ScriptableRendererFeature>().ToArray())
        {
            if(feature.GetType().Name!="ScreenSpaceAmbientOcclusion") continue;
            int index=renderer.rendererFeatures.IndexOf(feature);
            if(index>=0)
            {
                var rendererSO=new SerializedObject(renderer);
                rendererSO.FindProperty("m_RendererFeatures").DeleteArrayElementAtIndex(index);
                // Object references require a second delete when the first only nulls them.
                if(rendererSO.FindProperty("m_RendererFeatures").arraySize==renderer.rendererFeatures.Count)
                    rendererSO.FindProperty("m_RendererFeatures").DeleteArrayElementAtIndex(index);
                var map=rendererSO.FindProperty("m_RendererFeatureMap");
                if(index<map.arraySize) map.DeleteArrayElementAtIndex(index);
                rendererSO.ApplyModifiedPropertiesWithoutUndo();
            }
            UnityEngine.Object.DestroyImmediate(feature,true);
        }
        renderer.SetDirty();
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        var pipelineSO=new SerializedObject(pipeline);
        pipelineSO.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;
        pipelineSO.FindProperty("m_SoftShadowQuality").intValue=2;
        pipelineSO.FindProperty("m_UseSRPBatcher").boolValue=true;
        // Golden window spotlights need dynamic character/furniture shadows in the player.
        pipelineSO.FindProperty("m_AdditionalLightShadowsSupported").boolValue=true;
        pipelineSO.ApplyModifiedPropertiesWithoutUndo();
        pipeline.renderScale=.9f; pipeline.msaaSampleCount=1;
        pipeline.supportsCameraOpaqueTexture=false;
        pipeline.supportsCameraDepthTexture=false;
        pipeline.supportsHDR=true;
        pipeline.shadowDistance=18; pipeline.shadowCascadeCount=2;
        pipeline.mainLightShadowmapResolution=1024;
        EditorUtility.SetDirty(pipeline); EditorUtility.SetDirty(renderer);
        ConfigureQuality(pipeline);

        var profile=AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
        profile.name="Windows Balanced Release";
        profile.overrideGlobalScenes=true;
        profile.scenes=File.Exists(OfficeMainMenuSetup.ScenePath)?new[]{new EditorBuildSettingsScene(OfficeMainMenuSetup.ScenePath,true),new EditorBuildSettingsScene("Assets/Scenes/ExecutiveOffice.unity",true)}:new[]{new EditorBuildSettingsScene("Assets/Scenes/ExecutiveOffice.unity",true)};
        var settings=new SerializedObject(profile);
        settings.FindProperty("m_BuildTarget").intValue=(int)BuildTarget.StandaloneWindows64;
        settings.FindProperty("m_Subtarget").intValue=(int)StandaloneBuildSubtarget.Player;
        foreach(string key in new[]{"m_Development","m_ConnectProfiler","m_BuildWithDeepProfilingSupport","m_BuildWithCodeCoverage","m_AllowDebugging","m_WaitForManagedDebugger","m_CreateSolution","m_CopyPDBFiles"})
        {
            var property=settings.FindProperty("m_PlatformBuildProfile."+key);
            if(property==null) throw new InvalidOperationException("Unity build profile field missing: "+key);
            property.boolValue=false;
        }
        settings.FindProperty("m_PlatformBuildProfile.m_Architecture").intValue=0; // x64
        settings.FindProperty("m_PlatformBuildProfile.m_CompressionType").intValue=2; // LZ4HC
        settings.ApplyModifiedPropertiesWithoutUndo();
        ConfigureQualityOverride(profile);
        if(typeof(BuildProfile).GetProperty("playerSettings",Internal).GetValue(profile)==null)
            Invoke(profile,"CreatePlayerSettingsFromGlobal");
        EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
        BuildProfile.SetActiveBuildProfile(profile);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone,ManagedStrippingLevel.Minimal);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
        PlayerSettings.SetStaticBatchingForPlatform(BuildTarget.StandaloneWindows64,true);
        PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.defaultIsNativeResolution=false; PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.resizableWindow=true; PlayerSettings.runInBackground=false;
        PlayerSettings.useFlipModelSwapchain=true;
        var playerSettings=(UnityEngine.Object)typeof(BuildProfile).GetProperty("playerSettings",Internal).GetValue(profile);
        var playerSO=new SerializedObject(playerSettings);
        playerSO.FindProperty("gcIncremental").boolValue=true;
        playerSO.FindProperty("stripEngineCode").boolValue=true;
        playerSO.ApplyModifiedPropertiesWithoutUndo();
        Invoke(profile,"SerializePlayerSettings");
        EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
        QualitySettings.SetQualityLevel(Array.IndexOf(QualitySettings.names,QualityName),true);
        Selection.activeObject=profile;
        return Validate();
    }

    static int ConfigureQuality(RenderPipelineAsset pipeline)
    {
        var asset=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
        var so=new SerializedObject(asset); var levels=so.FindProperty("m_QualitySettings");
        int index=Array.IndexOf(QualitySettings.names,QualityName);
        if(index<0) { index=levels.arraySize; levels.InsertArrayElementAtIndex(index); }
        var level=levels.GetArrayElementAtIndex(index);
        level.FindPropertyRelative("name").stringValue=QualityName;
        level.FindPropertyRelative("customRenderPipeline").objectReferenceValue=pipeline;
        level.FindPropertyRelative("vSyncCount").intValue=0;
        level.FindPropertyRelative("antiAliasing").intValue=0;
        level.FindPropertyRelative("pixelLightCount").intValue=4;
        level.FindPropertyRelative("shadowDistance").floatValue=18;
        level.FindPropertyRelative("shadowCascades").intValue=2;
        level.FindPropertyRelative("lodBias").floatValue=1;
        level.FindPropertyRelative("skinWeights").intValue=(int)SkinWeights.FourBones;
        level.FindPropertyRelative("streamingMipmapsActive").boolValue=true;
        level.FindPropertyRelative("streamingMipmapsMemoryBudget").floatValue=512;
        level.FindPropertyRelative("asyncUploadTimeSlice").intValue=2;
        level.FindPropertyRelative("asyncUploadBufferSize").intValue=16;
        so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(asset);
        return index;
    }

    static void ConfigureQualityOverride(BuildProfile profile)
    {
        // Unity 6.6 stores this built-in override as a profile subasset. Its public
        // component API only supports user settings, so keep the version-specific bridge here.
        var type=typeof(BuildProfile).Assembly.GetType("UnityEditor.Build.Profile.BuildProfileQualitySettings",true);
        var quality=AssetDatabase.LoadAllAssetsAtPath(ProfilePath).FirstOrDefault(a=>a.GetType()==type) as ScriptableObject;
        if(quality==null)
        {
            quality=ScriptableObject.CreateInstance(type); quality.name="Quality Settings";
            quality.hideFlags=HideFlags.HideInHierarchy|HideFlags.HideInInspector;
            AssetDatabase.AddObjectToAsset(quality,profile);
        }
        var so=new SerializedObject(quality); so.FindProperty("m_DefaultQualityLevel").stringValue=QualityName;
        var levels=so.FindProperty("m_QualityLevels"); levels.arraySize=1; levels.GetArrayElementAtIndex(0).stringValue=QualityName;
        so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(quality);
        typeof(BuildProfile).GetField("qualitySettings",Internal).SetValue(profile,quality);
    }
    static void Invoke(BuildProfile profile,string method) { typeof(BuildProfile).GetMethod(method,Internal).Invoke(profile,null); }

    public static string Validate()
    {
        var profile=BuildProfile.GetActiveBuildProfile();
        if(profile==null || AssetDatabase.GetAssetPath(profile)!=ProfilePath) throw new InvalidOperationException("Optimized profile is not active.");
        var scenePaths=profile.GetScenesForBuild().Where(s=>s.enabled).Select(s=>s.path).ToArray();
        var expectedScenes=File.Exists(OfficeMainMenuSetup.ScenePath)?new[]{OfficeMainMenuSetup.ScenePath,"Assets/Scenes/ExecutiveOffice.unity"}:new[]{"Assets/Scenes/ExecutiveOffice.unity"};
        if(!scenePaths.SequenceEqual(expectedScenes))throw new InvalidOperationException("Unexpected scene list.");
        if(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone)!=ScriptingImplementation.Mono2x) throw new InvalidOperationException("Unexpected scripting backend.");
        var pipeline=QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if(pipeline==null || AssetDatabase.GetAssetPath(pipeline)!=PipelinePath) throw new InvalidOperationException("Balanced render pipeline is not active.");
        if(AssetDatabase.LoadAllAssetsAtPath(RendererPath).OfType<ScriptableRendererFeature>().Any(f=>f.GetType().Name=="ScreenSpaceAmbientOcclusion"))
            throw new InvalidOperationException("Remove SSAO from the balanced renderer before building: inactive SSAO crashes this Unity version after resource stripping. Run Configure Windows Balanced Release.");
        return "Active: Windows Balanced Release; x64, Mono, DX11, LZ4HC, release; MainMenu then ExecutiveOffice; 1600x900 resizable; URP 90%, 18m shadows/1024/2 cascades, SSAO off; static architecture batching on build.";
    }

    [MenuItem("Tools/Build/Build Windows Balanced Release")]
    public static void Build()
    {
        Validate(); Directory.CreateDirectory("Builds/Windows");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions
        {
            buildProfile=BuildProfile.GetActiveBuildProfile(),
            locationPathName="Builds/Windows/SecurityService.exe"
        });
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Windows build failed: "+report.summary.result);
        Debug.Log("Windows release build: "+report.summary.outputPath);
    }
}

public sealed class WindowsArchitectureBatching : IProcessSceneWithReport
{
    public int callbackOrder=>0;
    public void OnProcessScene(Scene scene,BuildReport report)
    {
        if(report==null || report.summary.platform!=BuildTarget.StandaloneWindows64 ||
            AssetDatabase.GetAssetPath(BuildProfile.GetActiveBuildProfile())!=WindowsBuildProfileSetup.ProfilePath) return;
        foreach(var root in scene.GetRootGameObjects().Where(g=>g.name=="01 Architecture and walnut joinery" || g.name=="04 Enclosed office and guest lounge"))
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if(renderer.name.StartsWith("Door") || renderer.name.StartsWith("Right door")) continue;
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)|StaticEditorFlags.BatchingStatic);
        }
    }
}
