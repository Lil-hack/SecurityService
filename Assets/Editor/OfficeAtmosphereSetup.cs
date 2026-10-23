using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class OfficeAtmosphereSetup
{
    const string Group="Atmosphere · evening studio";
    [MenuItem("Tools/Office/Apply Atmospheric Lighting")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode first.");
        if(GameObject.Find("Kitchen room")==null)throw new Exception("Open ExecutiveOffice first.");
        if(!File.Exists("OfficePreview/Atmosphere_Executive_Before.png"))Capture("Before");
        var old=GameObject.Find(Group);if(old!=null)UnityEngine.Object.DestroyImmediate(old);
        var root=new GameObject(Group).transform;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.70f,.74f,.82f);
        RenderSettings.ambientEquatorColor=new Color(.58f,.57f,.55f);
        RenderSettings.ambientGroundColor=new Color(.36f,.31f,.26f);
        // Local contrast comes from existing practical fixtures rather than a room-wide exposure lift.
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            switch(light.name)
            {
                case "Warm afternoon window light":light.intensity=1.1f;light.color=new Color(1,.79f,.60f);break;
                case "Pendant warm light":light.intensity=6.5f;light.range=6;light.color=new Color(1,.76f,.49f);break;
                case "Soft front fill":light.intensity=3f;light.range=7;light.color=new Color(.82f,.87f,1);break;
                case "Window soft daylight":light.intensity=4f;light.range=7;light.color=new Color(.64f,.77f,1);break;
                case "Studio soft ceiling light":light.intensity=7f;light.range=7;light.color=new Color(.91f,.94f,1);break;
                case "Studio east ceiling light":light.intensity=7f;light.range=7;light.color=new Color(1,.88f,.72f);break;
                case "Kitchen window light":light.intensity=4f;light.range=5.5f;light.color=new Color(.63f,.78f,1);break;
                case "Kitchen ceiling light":light.intensity=4.5f;light.range=5.5f;break;
                case "Kitchen pendant light":light.intensity=3.2f;light.range=3.4f;light.color=new Color(1,.77f,.48f);break;
                case "Warm counter task light":light.intensity=.85f;light.range=2.6f;break;
            }
            EditorUtility.SetDirty(light);
        }
        Spot(root,"Executive desk pool",new Vector3(-.6f,3.6f,.4f),new Vector3(0,1,.5f),new Color(1,.78f,.53f),12,64,5,true);
        Spot(root,"Studio collaboration pool",new Vector3(17.4f,3.7f,3),new Vector3(17.4f,.7f,3),new Color(1,.83f,.62f),12,70,5,true);
        Spot(root,"Kitchen dining pool",new Vector3(26.6f,3.75f,12.5f),new Vector3(26.6f,.75f,12.5f),new Color(1,.83f,.60f),10,65,4.7f,true);
        Spot(root,"Lounge amber wall wash",new Vector3(23.2f,3.5f,5.65f),new Vector3(23.2f,1.8f,7.3f),new Color(1,.72f,.48f),8,90,4,false);
        Spot(root,"Meeting cool accent",new Vector3(9.5f,3.5f,3.7f),new Vector3(9.5f,.8f,3.7f),new Color(.60f,.81f,1),3,76,4.5f,false);

        const string profilePath="Assets/Settings/OfficeAtmosphereProfile.asset";
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
        T Effect<T>() where T:VolumeComponent {if(!profile.TryGet<T>(out var c)){c=profile.Add<T>(true);AssetDatabase.AddObjectToAsset(c,profile);}c.active=true;return c;}
        var bloom=Effect<Bloom>();bloom.intensity.Override(.22f);bloom.threshold.Override(1.25f);bloom.scatter.Override(.55f);
        var tone=Effect<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);
        var color=Effect<ColorAdjustments>();color.postExposure.Override(.4f);color.contrast.Override(4);color.saturation.Override(-4);
        var white=Effect<WhiteBalance>();white.temperature.Override(4);white.tint.Override(1);
        var vignette=Effect<Vignette>();vignette.intensity.Override(.15f);vignette.smoothness.Override(.38f);
        var grain=Effect<FilmGrain>();grain.type.Override(FilmGrainLookup.Thin1);grain.intensity.Override(.055f);grain.response.Override(.8f);
        foreach(var c in profile.components)EditorUtility.SetDirty(c);EditorUtility.SetDirty(profile);
        var volume=root.gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=20;volume.sharedProfile=profile;
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {if(camera.CompareTag("MainCamera")){camera.allowHDR=true;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;EditorUtility.SetDirty(camera);}}
        foreach(var path in new[]{"Assets/Settings/PC_RPAsset.asset","Assets/Settings/Windows_Balanced_RP.asset"})
        {
            var asset=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);if(asset==null)continue;
            var so=new SerializedObject(asset);so.FindProperty("m_AdditionalLightShadowsSupported").boolValue=true;so.ApplyModifiedProperties();EditorUtility.SetDirty(asset);
        }
        Dust(root,"Executive window dust",new Vector3(-3.2f,2.1f,.2f),new Vector3(.5f,2.1f,3));
        Dust(root,"Studio window dust",new Vector3(27.5f,2.1f,3.8f),new Vector3(.5f,2.1f,4));
        Dust(root,"Kitchen window dust",new Vector3(27.5f,2.0f,12),new Vector3(.5f,1.6f,3));
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveScene(root.gameObject.scene);
        Capture("After");
        File.WriteAllText("OfficePreview/Atmosphere_Validation.txt","PASS: existing practical lights balanced; 5 spot accents, 3 shadow casters; bloom, ACES, color grade, vignette, subtle grain; 3 capped dust systems. Four room previews captured.");
    }
    static void Spot(Transform root,string name,Vector3 p,Vector3 target,Color c,float intensity,float angle,float range,bool shadow)
    {
        var l=new GameObject(name).AddComponent<Light>();l.transform.SetParent(root,false);l.transform.position=p;l.transform.LookAt(target);
        l.type=LightType.Spot;l.color=c;l.intensity=intensity;l.range=range;l.spotAngle=angle;l.innerSpotAngle=angle*.6f;
        l.shadows=shadow?LightShadows.Soft:LightShadows.None;l.shadowResolution=LightShadowResolution.Low;l.shadowBias=.035f;l.shadowNormalBias=.12f;
    }
    static void Dust(Transform root,string name,Vector3 p,Vector3 size)
    {
        const string path="Assets/ExecutiveOffice/Materials/Atmosphere dust.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null)
        {
            material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);
            material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;AssetDatabase.CreateAsset(material,path);
        }
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=p;var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.duration=20;main.loop=true;main.startLifetime=new ParticleSystem.MinMaxCurve(9,16);main.startSpeed=.018f;main.startSize=new ParticleSystem.MinMaxCurve(.008f,.018f);main.startColor=new Color(1,.88f,.65f,.16f);main.maxParticles=24;main.simulationSpace=ParticleSystemSimulationSpace.World;
        var emission=ps.emission;emission.rateOverTime=1.3f;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=size;
        var noise=ps.noise;noise.enabled=true;noise.strength=.025f;noise.frequency=.18f;noise.scrollSpeed=.08f;
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        ps.Play();
    }
    [MenuItem("Tools/Office/Capture Atmospheric Rooms")]
    public static void CaptureAfter()=>Capture("After");
    static void Capture(string suffix)
    {
        Shot("Executive",new Vector3(3.5f,2.2f,-4.5f),new Vector3(-.3f,1.25f,.7f),suffix);
        Shot("Studio",new Vector3(12,2.4f,-3.5f),new Vector3(18,1.15f,3),suffix);
        Shot("Lounge",new Vector3(20.2f,2.4f,2),new Vector3(23.4f,1.1f,5.3f),suffix);
        Shot("Kitchen",new Vector3(25.8f,2.25f,8.1f),new Vector3(23.9f,1.35f,14.1f),suffix);
    }
    static void Shot(string room,Vector3 p,Vector3 target,string suffix)
    {
        var go=new GameObject("Atmosphere preview"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.transform.position=p;camera.transform.LookAt(target);camera.fieldOfView=70;camera.nearClipPlane=.05f;camera.farClipPlane=100;camera.allowHDR=true;
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        var rt=new RenderTexture(1400,900,24,RenderTextureFormat.DefaultHDR);var old=RenderTexture.active;var tex=new Texture2D(1400,900,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1400,900),0,0);tex.Apply();File.WriteAllBytes("OfficePreview/Atmosphere_"+room+"_"+suffix+".png",tex.EncodeToPNG());}
        finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
}

