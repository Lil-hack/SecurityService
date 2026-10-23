using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class DirectorCornerFloorLamp
{
    [MenuItem("Tools/Office/Add Director Corner Floor Lamp")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode first.");
        var parent=GameObject.Find("04 Enclosed office and guest lounge").transform;
        var old=parent.Find("Director corner floor lamp");if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
        var source=GameObject.Find("Guest reading floor lamp");
        if(source==null)throw new Exception("Existing floor lamp model missing.");
        var root=UnityEngine.Object.Instantiate(source,parent);root.name="Director corner floor lamp";
        Undo.RegisterCreatedObjectUndo(root,"Add director corner floor lamp");
        var bounds=Bounds(root);root.transform.localScale*=1.95f/bounds.size.y;
        var bottom=new Vector3(4.5f,.015f,3.01f);
        bounds=Bounds(root);root.transform.position+=bottom-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        foreach(var t in root.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        if(root.GetComponentsInChildren<Collider>().Length==0)
        {
            // A narrow base collider blocks walking through the stand without obstructing the corner.
            var collider=new GameObject("Floor lamp base collider");collider.transform.SetParent(root.transform,false);collider.transform.position=bottom+Vector3.up*.10f;
            var box=collider.AddComponent<BoxCollider>();box.size=new Vector3(.35f,.20f,.35f);collider.transform.localScale=new Vector3(1/root.transform.lossyScale.x,1/root.transform.lossyScale.y,1/root.transform.lossyScale.z);
        }
        var glow=AssetDatabase.LoadAssetAtPath<Material>("Assets/ExecutiveOffice/Materials/Director lamp warm bulb.mat");
        if(glow==null)
        {
            glow=new Material(Shader.Find("Universal Render Pipeline/Lit"));glow.color=new Color(1,.86f,.57f);glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.66f,.30f)*1.8f);
            AssetDatabase.CreateAsset(glow,"Assets/ExecutiveOffice/Materials/Director lamp warm bulb.mat");
        }
        var bulb=GameObject.CreatePrimitive(PrimitiveType.Sphere);bulb.name="Warm floor lamp bulb";bulb.transform.SetParent(root.transform,true);bulb.transform.position=bottom+Vector3.up*1.64f;bulb.transform.localScale=Vector3.one*(.08f/root.transform.lossyScale.x);bulb.GetComponent<Renderer>().sharedMaterial=glow;UnityEngine.Object.DestroyImmediate(bulb.GetComponent<Collider>());
        var light=new GameObject("Corner floor lamp warm light").AddComponent<Light>();light.transform.SetParent(root.transform,true);light.transform.position=bottom+Vector3.up*1.65f;
        light.type=LightType.Point;light.color=new Color(1,.80f,.53f);light.intensity=3.3f;light.range=2.7f;light.shadows=LightShadows.None;
        var wash=new GameObject("Corner floor lamp wall wash").AddComponent<Light>();wash.transform.SetParent(root.transform,true);wash.transform.position=bottom+new Vector3(0,1.68f,-.08f);wash.transform.LookAt(new Vector3(4.7f,2.7f,3.5f));
        wash.type=LightType.Spot;wash.color=new Color(1,.82f,.60f);wash.intensity=4;wash.range=2.6f;wash.spotAngle=100;wash.innerSpotAngle=65;wash.shadows=LightShadows.Soft;wash.shadowResolution=LightShadowResolution.Low;wash.shadowNormalBias=.1f;wash.shadowBias=.03f;
        EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);EditorSceneManager.SaveScene(parent.gameObject.scene);AssetDatabase.SaveAssets();
        Capture();File.WriteAllText("OfficePreview/DirectorCornerLamp.txt","Saved 1.95 m floor lamp beside rear corner plant at "+bottom+"; warm local light and shadowed wall wash.");
    }
    static Bounds Bounds(GameObject go)
    {var renderers=go.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
    static void Capture()
    {
        var go=new GameObject("Corner lamp preview"){hideFlags=HideFlags.HideAndDontSave};var c=go.AddComponent<Camera>();c.transform.position=new Vector3(2.2f,2.05f,-.9f);c.transform.LookAt(new Vector3(4.65f,1.4f,2.85f));c.fieldOfView=59;c.nearClipPlane=.05f;c.allowHDR=true;c.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        var rt=new RenderTexture(1200,1000,24,RenderTextureFormat.DefaultHDR);var old=RenderTexture.active;var t=new Texture2D(1200,1000,TextureFormat.RGB24,false);
        try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,1200,1000),0,0);t.Apply();File.WriteAllBytes("OfficePreview/DirectorCornerLamp.png",t.EncodeToPNG());}
        finally{c.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
}
