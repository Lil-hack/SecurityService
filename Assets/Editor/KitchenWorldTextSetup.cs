using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KitchenWorldTextSetup
{
    [MenuItem("Tools/Office/Fix Kitchen Text Occlusion")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode first.");
        var root=GameObject.Find("Kitchen room");int count=0;
        foreach(var text in root.GetComponentsInChildren<TextMesh>(true))
        {
            Undo.RecordObject(text.gameObject,"Depth-tested kitchen text");
            var world=text.GetComponent<KitchenWorldText>();if(world==null)world=Undo.AddComponent<KitchenWorldText>(text.gameObject);
            world.Refresh();Verify(text);count++;
        }
        EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);
        File.WriteAllText("OfficePreview/KitchenText_Validation.txt","PASS: "+count+" world-space signs render normally and are fully hidden behind an opaque wall (pixel comparison).");
    }
    static void Verify(TextMesh text)
    {
        var renderer=text.GetComponent<Renderer>();int oldLayer=text.gameObject.layer;
        var cameraObject=new GameObject("Text occlusion test"){hideFlags=HideFlags.HideAndDontSave};
        var camera=cameraObject.AddComponent<Camera>();var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
        var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        var target=new RenderTexture(256,256,24);var previous=RenderTexture.active;
        var pixels=new Texture2D(256,256,TextureFormat.RGB24,false);
        try
        {
            text.gameObject.layer=31;wall.layer=31;wall.GetComponent<Renderer>().sharedMaterial=mat;
            camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.blue;
            camera.orthographic=true;camera.orthographicSize=Mathf.Max(.3f,renderer.bounds.size.magnitude*.65f);camera.nearClipPlane=.01f;camera.farClipPlane=5;
            camera.transform.position=renderer.bounds.center-text.transform.forward*2;
            camera.transform.rotation=Quaternion.LookRotation(text.transform.forward,text.transform.up);
            wall.transform.position=renderer.bounds.center-text.transform.forward;
            wall.transform.rotation=text.transform.rotation;wall.transform.localScale=new Vector3(camera.orthographicSize*3,camera.orthographicSize*3,.05f);
            camera.targetTexture=target;
            Color32[] Shot(bool enabled,bool covered)
            {renderer.enabled=enabled;wall.SetActive(covered);camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,256,256),0,0);pixels.Apply();return pixels.GetPixels32();}
            int Difference(Color32[] a,Color32[] b)
            {int diff=0;for(int i=0;i<a.Length;i++)if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>8)diff++;return diff;}
            if(Difference(Shot(true,false),Shot(false,false))<5)throw new Exception("Sign did not render: "+text.text);
            if(Difference(Shot(true,true),Shot(false,true))>0)throw new Exception("Sign visible through wall: "+text.text);
        }
        finally
        {renderer.enabled=true;text.gameObject.layer=oldLayer;RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(wall);UnityEngine.Object.DestroyImmediate(mat);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
    }
}
