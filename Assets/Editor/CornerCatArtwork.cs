using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class CornerCatArtwork
{
    [MenuItem("Tools/Office/Apply Corner Money Cat Artwork")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode first.");
        var frame=GameObject.Find("Right rear wall artwork");if(frame==null)throw new Exception("Corner artwork not found.");
        var previous=frame.transform.Find("Money cat print");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var renderers=frame.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
        var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/photos/CornerMoneyCat.png");importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/photos/CornerMoneyCat.png");
        var root=new GameObject("Money cat print").transform;root.SetParent(frame.transform,true);Undo.RegisterCreatedObjectUndo(root.gameObject,"Replace corner picture contents");
        float width=b.size.z*.92f,height=b.size.y*.90f;
        var center=new Vector3(b.min.x-.004f,b.center.y,b.center.z);
        var black=Material("Corner print mat",null,new Color(.035f,.025f,.02f));var photo=Material("Corner money cat print",texture,Color.white);
        Quad(root,"Dark picture mat",center,width,height,black);
        float ratio=(float)texture.width/texture.height;float imageWidth=Mathf.Min(width,height*ratio),imageHeight=imageWidth/ratio;
        Quad(root,"Money cat image",center+Vector3.left*.002f,imageWidth,imageHeight,photo);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(frame.scene);EditorSceneManager.SaveScene(frame.scene);
        Capture(b.center);
        System.IO.File.WriteAllText("OfficePreview/CornerCatArtwork.txt","Saved supplied money-cat image in Right rear wall artwork; original frame kept, image aspect ratio preserved without cropping.");
    }
    static Material Material(string name,Texture texture,Color color)
    {string p="Assets/ExecutiveOffice/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(m,p);}m.color=color;m.mainTexture=texture;EditorUtility.SetDirty(m);return m;}
    static void Quad(Transform root,string name,Vector3 center,float w,float h,Material mat)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name=name;g.transform.SetParent(root,true);g.transform.SetPositionAndRotation(center,Quaternion.Euler(0,90,0));g.transform.localScale=Vector3.one;g.transform.localScale=new Vector3(w/g.transform.lossyScale.x,h/g.transform.lossyScale.y,1/g.transform.lossyScale.z);g.GetComponent<Renderer>().sharedMaterial=mat;g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
    static void Capture(Vector3 target)
    {
        var go=new GameObject("Corner art preview"){hideFlags=HideFlags.HideAndDontSave};var c=go.AddComponent<Camera>();c.transform.position=target+new Vector3(-2.1f,.15f,-.45f);c.transform.LookAt(target);c.fieldOfView=45;c.nearClipPlane=.05f;c.allowHDR=true;c.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        var rt=new RenderTexture(1000,1000,24,RenderTextureFormat.DefaultHDR);var previous=RenderTexture.active;var t=new Texture2D(1000,1000,TextureFormat.RGB24,false);
        try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,1000,1000),0,0);t.Apply();System.IO.File.WriteAllBytes("OfficePreview/CornerCatArtwork.png",t.EncodeToPNG());}
        finally{c.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
}
