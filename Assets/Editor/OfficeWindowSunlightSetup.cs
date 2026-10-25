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

public static class OfficeWindowSunlightSetup
{
    const string Group="Window light · golden hour";
    const string CookiePath="Assets/ExecutiveOffice/Materials/WindowSunlightCookie.png";
    const string Shots=".local/WindowSunlightQA";
    [MenuItem("Tools/Office/Apply Golden Window Light")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var scene=SceneManager.GetActiveScene();if(scene.isDirty)EditorSceneManager.SaveScene(scene);
        if(scene.path!="Assets/Scenes/ExecutiveOffice.unity")scene=EditorSceneManager.OpenScene("Assets/Scenes/ExecutiveOffice.unity");
        Directory.CreateDirectory(Shots);Capture("Before");
        var old=GameObject.Find(Group);if(old!=null)Object.DestroyImmediate(old);
        var root=new GameObject(Group).transform;var cookie=CreateCookie();
        // The project uses a low-poly palette: cool diffuse fill reveals shadow detail,
        // while local window sources provide the warm key and character edge lighting.
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.66f,.75f,.91f);
        RenderSettings.ambientEquatorColor=new Color(.53f,.58f,.69f);
        RenderSettings.ambientGroundColor=new Color(.34f,.32f,.31f);
        foreach(var light in Object.FindObjectsByType<Light>())
        {
            switch(light.name)
            {
                case "Warm afternoon window light":
                    light.color=new Color(1,.84f,.64f);light.intensity=1.05f;light.shadowBias=.025f;light.shadowNormalBias=.16f;light.shadowStrength=.85f;break;
                case "Window soft daylight":light.color=new Color(.60f,.76f,1);light.intensity=2.5f;break;
                case "Soft front fill":light.color=new Color(.76f,.85f,1);light.intensity=3.6f;break;
                case "Director soft room fill":light.color=new Color(.72f,.83f,1);light.intensity=3.2f;break;
                case "Kitchen window light":light.color=new Color(.62f,.79f,1);light.intensity=3;break;
            }
            EditorUtility.SetDirty(light);
        }
        // Glass and skyline images transmit sunlight; the opaque window frames still cast shadows.
        foreach(var renderer in Object.FindObjectsByType<Renderer>())
        {
            if(renderer.name=="Window glass continuous"||renderer.name=="Kitchen window glass"||renderer.name=="City panorama outside windows"||renderer.name=="Sky beyond window")
            {renderer.shadowCastingMode=ShadowCastingMode.Off;EditorUtility.SetDirty(renderer);}
        }
        Sun(root,"Director · golden window panes",new Vector3(-3.64f,3.15f,-1.65f),new Vector3(2.1f,.03f,-3.25f),cookie,54,60,10,true);
        Sun(root,"Director · warm character edge",new Vector3(-3.15f,2.75f,1.9f),new Vector3(.2f,1.2f,-1.2f),null,8,66,6,false);
        Sun(root,"Studio · sunset panes south",new Vector3(27.92f,3.38f,-2.6f),new Vector3(23.25f,.03f,-.9f),cookie,30,76,9,true);
        Sun(root,"Studio · sunset panes north",new Vector3(27.92f,3.38f,4.5f),new Vector3(23.3f,.03f,3.1f),cookie,28,76,9,true);
        Sun(root,"Kitchen · sunset panes",new Vector3(27.92f,3.12f,12.5f),new Vector3(23.8f,.03f,10.6f),cookie,26,74,8,true);
        foreach(string path in new[]{"Assets/Settings/PC_RPAsset.asset","Assets/Settings/Windows_Balanced_RP.asset"})
        {
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);if(pipeline==null)continue;
            var so=new SerializedObject(pipeline);so.FindProperty("m_AdditionalLightShadowsSupported").boolValue=true;so.FindProperty("m_SupportsLightCookies").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Capture("After");Selection.activeGameObject=root.gameObject;
        Debug.Log("Golden window lighting saved: four shadowed window-cookie sources, warm director edge light, cool diffuse fill; glass/panoramas transmit light. Room captures: "+Shots);
    }
    static Light Sun(Transform parent,string name,Vector3 position,Vector3 target,Texture cookie,float intensity,float angle,float range,bool shadow)
    {
        var light=new GameObject(name).AddComponent<Light>();light.transform.SetParent(parent,false);light.transform.position=position;light.transform.LookAt(target);
        light.type=LightType.Spot;light.color=new Color(1,.79f,.49f);light.intensity=intensity;light.range=range;light.spotAngle=angle;light.innerSpotAngle=angle*.88f;
        light.cookie=cookie;light.shadows=shadow?LightShadows.Soft:LightShadows.None;light.shadowStrength=.82f;light.shadowBias=.02f;light.shadowNormalBias=.08f;
        light.shadowResolution=LightShadowResolution.Medium;light.renderMode=LightRenderMode.ForcePixel;
        return light;
    }
    static Texture2D CreateCookie()
    {
        const int size=512;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false,true);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=(x+.5f)/size,v=(y+.5f)/size;
            float edge=Mathf.SmoothStep(0,1,Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v))/.055f);
            float vertical=1;
            for(int bar=1;bar<5;bar++)vertical=Mathf.Min(vertical,Mathf.Abs(u-bar*.2f));
            float bars=Mathf.SmoothStep(0,1,(vertical-.014f)/.014f)*Mathf.SmoothStep(0,1,(Mathf.Abs(v-.51f)-.016f)/.012f);
            float value=edge*bars;texture.SetPixel(x,y,new Color(value,value,value,value));
        }
        texture.Apply();File.WriteAllBytes(CookiePath,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(CookiePath);
        var importer=(TextureImporter)AssetImporter.GetAtPath(CookiePath);importer.textureType=TextureImporterType.Cookie;importer.textureShape=TextureImporterShape.Texture2D;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=false;importer.sRGBTexture=false;importer.maxTextureSize=512;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(CookiePath);
    }
    [MenuItem("Tools/Office/Capture Golden Window Light")]
    public static void CaptureCurrent(){Directory.CreateDirectory(Shots);Capture("Current");}
    static void Capture(string suffix)
    {
        Shot("Director",new Vector3(3.5f,2.2f,-4.5f),new Vector3(-.3f,1.25f,.7f),suffix);
        Shot("Studio",new Vector3(21.1f,2.3f,-4.3f),new Vector3(25.6f,1.1f,1.3f),suffix);
        Shot("Kitchen",new Vector3(25.8f,2.25f,8.1f),new Vector3(23.9f,1.35f,14.1f),suffix);
    }
    static void Shot(string room,Vector3 position,Vector3 target,string suffix)
    {
        var camera=new GameObject("Window light preview"){hideFlags=HideFlags.HideAndDontSave}.AddComponent<Camera>();camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=70;camera.nearClipPlane=.05f;camera.farClipPlane=100;camera.allowHDR=true;
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask=~0;data.volumeTrigger=camera.transform;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;
        var rt=RenderTexture.GetTemporary(1400,788,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;var texture=new Texture2D(1400,788,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1400,788),0,0);texture.Apply();File.WriteAllBytes(Shots+"/"+room+"_"+suffix+".png",texture.EncodeToPNG());}
        finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(texture);Object.DestroyImmediate(camera.gameObject);}
    }
}
