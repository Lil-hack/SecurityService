using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OfficeElevatorSetup
{
    const string Folder="Assets/OfficeExtension/Elevator/";
    static Material Mat(string name,Color color,float metallic=0,float emission=0)
    {
        string path=Folder+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.color=color;mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",metallic>0?.55f:.25f);
        if(emission>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*emission);}EditorUtility.SetDirty(mat);return mat;
    }
    static GameObject Box(Transform root,string name,Vector3 p,Vector3 size,Material mat,bool solid=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!solid)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    static TextMesh Text(Transform root,string name,string content,Vector3 p,float width,float height,Color color)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=p;go.transform.localRotation=Quaternion.Euler(0,180,0);
        var text=go.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=64;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontStyle=FontStyle.Bold;text.color=color;text.text=content;
        text.font.RequestCharactersInTexture(content,64,text.fontStyle);float max=0;foreach(var line in content.Split('\n')){float advance=0;foreach(char c in line){text.font.GetCharacterInfo(c,out var glyph,64,text.fontStyle);advance+=glyph.advance;}max=Mathf.Max(max,advance);}
        float lineHeight=text.font.lineHeight*64f/text.font.fontSize;text.characterSize=Mathf.Min(width/Mathf.Max(1,max),height/Mathf.Max(1,lineHeight*content.Split('\n').Length))*10;
        go.AddComponent<KitchenWorldText>().Refresh();return text;
    }
    static Transform Button(Transform root,string name,Vector3 p,float yaw,Material steel,Material black,Material glow,out Renderer lamp)
    {
        var panel=new GameObject(name).transform;panel.SetParent(root,false);panel.localPosition=p;panel.localRotation=Quaternion.Euler(0,yaw,0);
        Box(panel,"Brushed button plate",Vector3.zero,new Vector3(.27f,.44f,.04f),steel,false);
        Text(panel,"Button label","ДВЕРИ",new Vector3(0,.115f,.025f),.21f,.05f,Color.white);
        var rim=GameObject.CreatePrimitive(PrimitiveType.Cylinder);rim.name="Button chrome rim";rim.transform.SetParent(panel,false);rim.transform.localPosition=new Vector3(0,-.05f,.028f);rim.transform.localRotation=Quaternion.Euler(90,0,0);rim.transform.localScale=new Vector3(.14f,.012f,.14f);rim.GetComponent<Renderer>().sharedMaterial=black;UnityEngine.Object.DestroyImmediate(rim.GetComponent<Collider>());
        var face=GameObject.CreatePrimitive(PrimitiveType.Cylinder);face.name="Illuminated push button";face.transform.SetParent(panel,false);face.transform.localPosition=new Vector3(0,-.05f,.046f);face.transform.localRotation=Quaternion.Euler(90,0,0);face.transform.localScale=new Vector3(.105f,.009f,.105f);lamp=face.GetComponent<Renderer>();lamp.sharedMaterial=glow;UnityEngine.Object.DestroyImmediate(face.GetComponent<Collider>());
        return panel;
    }
    static Transform Door(Transform root,string name,float x,Material steel,Material black)
    {
        var door=new GameObject(name).transform;door.SetParent(root,false);door.localPosition=new Vector3(x,1.24f,-.06f);
        Box(door,"Metal door leaf",Vector3.zero,new Vector3(.88f,2.48f,.075f),steel,false);
        Box(door,"Centre door gasket",new Vector3(x<0?.435f:-.435f,0,.042f),new Vector3(.009f,2.40f,.005f),black,false);
        Box(door,"Interior recessed panel",new Vector3(0,0,-.043f),new Vector3(.76f,2.27f,.018f),black,false);
        Box(door,"Interior brushed panel",new Vector3(0,0,-.056f),new Vector3(.72f,2.23f,.012f),steel,false);
        foreach(float y in new[]{-.82f,.70f})
            Box(door,"Interior horizontal joint",new Vector3(0,y,-.064f),new Vector3(.71f,.012f,.006f),black,false);
        foreach(float edge in new[]{-1f,1f})
            Box(door,"Interior vertical trim",new Vector3(edge*.40f,0,-.055f),new Vector3(.018f,2.40f,.025f),steel,false);
        for(int i=0;i<5;i++)Box(door,"Lower ventilation slot",new Vector3(0,-.98f+i*.025f,-.065f),new Vector3(.38f,.009f,.005f),black,false);
        var collider=door.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(.88f,2.48f,.075f);
        var rb=door.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        return door;
    }
    [MenuItem("Tools/Office/Replace Landing Wall With Elevator")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first.");
        var landing=GameObject.Find("South studio doorway").transform;var wall=landing.Find("Landing back wall");
        var previousLift=landing.GetComponentInChildren<OfficeElevatorDoors>(true);var previous=previousLift!=null?previousLift.transform:null;
        if(wall==null && previous==null)throw new Exception("Landing back wall not found.");
        var bounds=wall!=null?wall.GetComponent<Renderer>().bounds:new Bounds();
        var position=wall!=null?new Vector3(bounds.center.x,bounds.min.y,bounds.center.z):previous.position;
        var rotation=wall!=null?wall.rotation:previous.rotation;
        var plaster=wall!=null?wall.GetComponent<Renderer>().sharedMaterial:previous.Find("Upper landing infill").GetComponent<Renderer>().sharedMaterial;
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var steel=Mat("Brushed elevator steel",new Color(.48f,.53f,.55f),.65f);var dark=Mat("Elevator charcoal",new Color(.055f,.075f,.085f));
        var panels=Mat("Warm cabin panels",new Color(.66f,.60f,.48f),.12f);var floor=Mat("Elevator slate floor",new Color(.13f,.15f,.17f));var brass=Mat("Elevator brass rail",new Color(.56f,.40f,.19f),.65f);
        var glow=Mat("Elevator warm ceiling",new Color(1,.89f,.65f),0,1.5f);var button=Mat("Elevator button glow",new Color(.15f,.45f,.85f),.2f,.5f);
        var root=new GameObject("Studio elevator");var t=root.transform;var script=root.AddComponent<OfficeElevatorDoors>();
        try
        {
            Box(t,"Cabin floor",new Vector3(0,-.05f,-1.625f),new Vector3(3.716f,.1f,3.25f),floor);
            Box(t,"Cabin rear wall",new Vector3(0,1.35f,-3.27f),new Vector3(3.816f,2.7f,.14f),panels);
            foreach(float side in new[]{-1f,1f})
            {
                Box(t,"Cabin side wall",new Vector3(side*1.838f,1.35f,-1.625f),new Vector3(.14f,2.7f,3.25f),panels);
                Box(t,"Door portal upright",new Vector3(side*1.06f,1.25f,.06f),new Vector3(.36f,2.5f,.24f),steel);
                Box(t,"Cabin horizontal handrail",new Vector3(side*1.728f,.92f,-1.8f),new Vector3(.04f,.045f,2.45f),brass);
                Box(t,"Cabin skirting",new Vector3(side*1.753f,.10f,-1.68f),new Vector3(.022f,.14f,3.0f),dark,false);
            }
            Box(t,"Cabin ceiling",new Vector3(0,2.70f,-1.625f),new Vector3(3.816f,.12f,3.45f),dark);
            Box(t,"Soft ceiling light",new Vector3(0,2.625f,-1.68f),new Vector3(2.665f,.025f,2.1f),glow,false);
            Box(t,"Rear handrail",new Vector3(0,.92f,-3.15f),new Vector3(3.336f,.045f,.04f),brass);
            var mirrorMat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Live mirror.mat");
            if(mirrorMat==null){mirrorMat=new Material(Shader.Find("Office/ElevatorMirror"));AssetDatabase.CreateAsset(mirrorMat,Folder+"Live mirror.mat");}
            Box(t,"Mirror metal frame",new Vector3(0,1.78f,-3.175f),new Vector3(2.912f,1.19f,.045f),steel,false);
            var mirror=Box(t,"Live cabin mirror",new Vector3(0,1.78f,-3.145f),new Vector3(2.802f,1.08f,.012f),mirrorMat,false);
            mirror.AddComponent<ElevatorMirror>().surface=mirror.GetComponent<Renderer>();
            foreach(float side in new[]{-1f,1f})
                Box(t,"Inner front return panel",new Vector3(side*1.334f,1.25f,-.18f),new Vector3(.888f,2.5f,.16f),steel);
            Box(t,"Door portal lintel",new Vector3(0,2.60f,.06f),new Vector3(2.5f,.26f,.24f),steel);
            Box(t,"Upper landing infill",new Vector3(0,3.47f,0),new Vector3(2.7f,1.50f,.20f),plaster);
            Box(t,"Floor display black glass",new Vector3(0,2.61f,.196f),new Vector3(1.34f,.20f,.025f),dark,false);
            script.statusText=Text(t,"Door status","ДВЕРИ ЗАКРЫТЫ",new Vector3(0,2.61f,.214f),1.14f,.08f,new Color(.71f,1,.86f));
            Text(t,"Elevator header","ЛИФТ  /  01",new Vector3(0,2.97f,.12f),1.6f,.18f,new Color(.13f,.17f,.18f));
            Box(t,"Threshold sill",new Vector3(0,.012f,0),new Vector3(1.78f,.024f,.30f),steel);
            foreach(float z in new[]{-.10f,-.025f,.05f})Box(t,"Door track groove",new Vector3(0,.026f,z),new Vector3(1.7f,.004f,.012f),dark,false);
            script.leftDoor=Door(t,"Left elevator door",-.44f,steel,dark);script.rightDoor=Door(t,"Right elevator door",.44f,steel,dark);
            script.outsideButton=Button(t,"Landing call button",new Vector3(1.06f,1.20f,.205f),0,steel,dark,button,out var outer);script.outsideLamp=outer;
            script.insideButton=Button(t,"Cabin door button",new Vector3(1.688f,1.20f,-1.15f),-90,steel,dark,button,out var inner);script.insideLamp=inner;
            var light=new GameObject("Cabin warm light").AddComponent<Light>();light.transform.SetParent(t,false);light.transform.localPosition=new Vector3(0,2.35f,-1.35f);light.type=LightType.Point;light.color=new Color(1,.90f,.74f);light.intensity=2.2f;light.range=4;light.shadows=LightShadows.None;
            script.SetOpen(false,true);PrefabUtility.SaveAsPrefabAsset(root,Folder+"Studio_Elevator.prefab");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Studio_Elevator.prefab"));instance.name="Studio elevator";instance.transform.SetParent(landing,false);instance.transform.SetPositionAndRotation(position,rotation);PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        if(wall!=null)UnityEngine.Object.DestroyImmediate(wall.gameObject);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(landing.gameObject.scene);EditorSceneManager.SaveScene(landing.gameObject.scene);Capture();
        Debug.Log("Landing back wall replaced with elevator cabin, two sliding doors and interior/exterior buttons.");
    }
    [MenuItem("Tools/Office/Capture Elevator Interior")]
    public static void CaptureInterior()
    {
        var lift=UnityEngine.Object.FindAnyObjectByType<OfficeElevatorDoors>();
        var camera=new GameObject("Interior preview").AddComponent<Camera>();camera.nearClipPlane=.03f;camera.fieldOfView=75;
        var rt=new RenderTexture(1100,1000,24);camera.targetTexture=rt;
        try
        {
            for(int i=0;i<2;i++)
            {
                camera.transform.position=lift.transform.TransformPoint(i==0?new Vector3(.65f,1.7f,-.7f):new Vector3(-.7f,1.7f,-2.7f));
                camera.transform.LookAt(lift.transform.TransformPoint(i==0?new Vector3(0,1.5f,-3.2f):new Vector3(0,1.35f,0)));
                lift.GetComponentInChildren<ElevatorMirror>().RenderFor(camera);
                camera.Render();RenderTexture.active=rt;var image=new Texture2D(1100,1000,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1100,1000),0,0);image.Apply();File.WriteAllBytes("OfficePreview/ElevatorInterior"+i+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            }
        }
        finally{camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camera.gameObject);}
    }
    [MenuItem("Tools/Office/Capture Elevator")]
    public static void Capture()
    {
        Directory.CreateDirectory("OfficePreview");var lift=UnityEngine.Object.FindAnyObjectByType<OfficeElevatorDoors>();var t=lift.transform;
        var camera=new GameObject("Elevator preview").AddComponent<Camera>();camera.nearClipPlane=.03f;camera.fieldOfView=68;camera.transform.position=t.TransformPoint(new Vector3(-.16f,1.55f,2.35f));camera.transform.LookAt(t.TransformPoint(new Vector3(0,1.4f,-.55f)));
        var rt=new RenderTexture(1100,1000,24);camera.targetTexture=rt;
        try{camera.Render();RenderTexture.active=rt;var image=new Texture2D(1100,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1100,1000),0,0);image.Apply();File.WriteAllBytes("OfficePreview/Elevator_"+(lift.IsOpen?"Open":"Closed")+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
        finally{camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camera.gameObject);}
    }
}


