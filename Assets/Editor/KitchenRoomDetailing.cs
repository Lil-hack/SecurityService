using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class KitchenRoomDetailing
{
    static KitchenRoomDetailing() { EditorApplication.update += ProcessRequest; }

    static void ProcessRequest()
    {
        const string request = "Temp/KitchenDetail.request";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || Application.isPlaying || !File.Exists(request)) return;
        string action = File.ReadAllText(request).Trim();
        File.Delete(request);
        try
        {
            if (GameObject.Find("Kitchen room") == null) throw new InvalidOperationException("Open ExecutiveOffice with Kitchen room first.");
            if (action == "apply") Apply();
            Capture();
            File.WriteAllText("OfficePreview/Kitchen_Result.txt", "OK " + action);
        }
        catch (Exception e) { File.WriteAllText("OfficePreview/Kitchen_Result.txt", e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Tools/Office/Detail Kitchen Room %#k")]
    public static void Apply()
    {
        if (UnityEngine.Application.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        var kitchen = GameObject.Find("Kitchen room");
        if (kitchen == null) throw new InvalidOperationException("Kitchen room is missing.");
        Undo.RegisterFullObjectHierarchyUndo(kitchen, "Detail kitchen");
        var old = kitchen.transform.Find("Kitchen crafted details");
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("Kitchen crafted details").transform;
        root.SetParent(kitchen.transform, false);
        var oak = Mat("Kitchen honey oak"); var white = Mat("Kitchen porcelain");
        var ink = Mat("Kitchen graphite"); var blue = Mat("Kitchen ocean");
        var teal = Mat("Kitchen teal"); var yellow = Mat("Kitchen sunflower");
        var orange = Mat("Kitchen tangerine"); var green = Mat("Kitchen leaf");
        var glow = Mat("Kitchen pendant glow");
        var metal = NewMat("Kitchen brushed steel", new Color(.42f,.47f,.48f), .72f, .8f);
        var fabric = NewMat("Kitchen linen", new Color(.74f,.68f,.51f), .05f);
        var coffee = NewMat("Kitchen coffee", new Color(.085f,.035f,.015f), .4f);

        // The window bench replaces the eastern chair; the island aisle stays open.
        int chair = 0;
        foreach (Transform t in kitchen.transform)
        {
            if (t.name == "Kitchen dining top") t.position = new Vector3(26.65f,.74f,12.55f);
            if (t.name == "Kitchen dining leg") t.position = new Vector3(26.65f,.36f,12.55f);
            if (t.name == "Kitchen dining chair")
            {
                var spots = new[]{new Vector3(26.65f,0,11.78f),new Vector3(27.35f,0,12.55f),new Vector3(26.65f,0,13.32f),new Vector3(25.88f,0,12.55f)};
                t.position = spots[chair]; t.gameObject.SetActive(chair != 1); chair++;
            }
            if (t.name.StartsWith("Kitchen bench")) t.position = new Vector3(t.position.x,t.position.y,12.55f);
            if (t.name == "Kitchen corner plant") t.position = new Vector3(20.98f,0,13.35f);
            if (t.name == "Kitchen counter doors") t.gameObject.SetActive(false);
        }
        for (int i=0;i<6;i++)
        {
            float x=21.4f+i*.86f;
            Cube(root,"Individual cabinet front",new Vector3(x,.48f,14.45f),new Vector3(.835f,.78f,.045f),blue);
            Cube(root,"Brushed cabinet handle",new Vector3(x,.78f,14.405f),new Vector3(.26f,.025f,.035f),metal);
        }
        Cube(root,"Recessed dark plinth",new Vector3(23.55f,.07f,14.45f),new Vector3(5.08f,.12f,.035f),ink);
        for(int i=0;i<28;i++)
            Cube(root,"Island oak fluting",new Vector3(21.98f+i*.116f,.48f,10.614f),new Vector3(.065f,.78f,.035f),oak);
        Cube(root,"Island foot rail",new Vector3(23.55f,.21f,10.38f),new Vector3(3.1f,.035f,.035f),metal);
        for(int i=0;i<3;i++) Cube(root,"Rail bracket",new Vector3(22.2f+i*1.35f,.21f,10.5f),new Vector3(.03f,.03f,.25f),metal);

        // Raised sink with a dark inset and a sculpted mixer, keeping the original counter.
        Cube(root,"Sink steel surround",new Vector3(23.25f,1.008f,14.75f),new Vector3(.78f,.025f,.51f),metal);
        Cube(root,"Sink dark basin",new Vector3(23.25f,1.024f,14.75f),new Vector3(.64f,.009f,.39f),ink);
        foreach(float x in new[]{22.9f,23.6f}) Cube(root,"Sink lip",new Vector3(x,1.035f,14.75f),new Vector3(.04f,.035f,.47f),metal);
        Rod(root,"Tap upright",new Vector3(23.25f,1.02f,15.02f),new Vector3(23.25f,1.38f,15.02f),.025f,metal);
        Rod(root,"Tap spout",new Vector3(23.25f,1.38f,15.02f),new Vector3(23.25f,1.38f,14.76f),.025f,metal);
        Rod(root,"Tap nozzle",new Vector3(23.25f,1.38f,14.76f),new Vector3(23.25f,1.32f,14.76f),.029f,metal);
        Cube(root,"Soap bottle",new Vector3(23.82f,1.10f,14.89f),new Vector3(.09f,.18f,.08f),teal);
        Rod(root,"Soap dispenser pump",new Vector3(23.82f,1.20f,14.89f),new Vector3(23.88f,1.20f,14.89f),.012f,metal);
        Cube(root,"Folded dish cloth",new Vector3(24.04f,1.014f,14.55f),new Vector3(.26f,.025f,.22f),fabric);
        for(int i=0;i<4;i++) Cube(root,"Cloth woven stripe",new Vector3(23.95f+i*.05f,1.028f,14.55f),new Vector3(.012f,.003f,.22f),teal);

        Cube(root,"Coffee tray",new Vector3(22.03f,1.015f,14.56f),new Vector3(.64f,.025f,.28f),oak);
        Cup(root,new Vector3(21.9f,1.03f,14.54f),white,coffee);
        Cup(root,new Vector3(22.17f,1.03f,14.56f),teal,coffee);
        for(int i=0;i<3;i++)
        {
            float x=24.05f+i*.29f;
            Disc(root,"Pantry jar",new Vector3(x,2.37f,15.16f),.17f,.23f,new[]{white,teal,orange}[i]);
            Disc(root,"Jar wooden lid",new Vector3(x,2.49f,15.16f),.18f,.025f,oak);
            Cube(root,"Jar label",new Vector3(x,2.37f,15.065f),new Vector3(.105f,.065f,.006f),fabric);
        }
        // Small rim and handle details make the original shelf cylinders read as cups.
        foreach(Transform t in kitchen.transform)
            if(t.name=="Kitchen mug")
            {
                Disc(root,"Cup dark opening",t.position+Vector3.up*.051f,.072f,.003f,coffee);
                Ring(root,"Cup handle",t.position+new Vector3(.062f,0,0),.036f,.009f,t.GetComponent<Renderer>().sharedMaterial);
            }
        Cube(root,"Island cutting board",new Vector3(24.48f,1.018f,11.2f),new Vector3(.65f,.035f,.42f),oak);
        Cube(root,"Board grip",new Vector3(24.87f,1.018f,11.2f),new Vector3(.19f,.035f,.13f),oak);
        Cube(root,"Knife blade",new Vector3(24.48f,1.044f,11.27f),new Vector3(.25f,.012f,.035f),metal);
        Cube(root,"Knife handle",new Vector3(24.68f,1.044f,11.27f),new Vector3(.15f,.025f,.045f),ink);
        Disc(root,"Bread plate",new Vector3(24.42f,1.046f,11.06f),.25f,.012f,white);
        for(int i=0;i<3;i++) Ball(root,"Bread slices",new Vector3(24.34f+i*.065f,1.08f,11.06f),new Vector3(.09f,.05f,.14f),fabric);
        Cube(root,"Island linen runner",new Vector3(22.55f,1.008f,11.15f),new Vector3(.46f,.009f,1.2f),teal);
        Cup(root,new Vector3(22.52f,1.02f,10.92f),yellow,coffee);

        Disc(root,"Dining woven mat",new Vector3(26.65f,.786f,12.55f),.8f,.007f,fabric);
        Cup(root,new Vector3(26.4f,.795f,12.4f),white,coffee);
        Cup(root,new Vector3(26.8f,.795f,12.75f),orange,coffee);
        Disc(root,"Dining vase",new Vector3(26.72f,.88f,12.48f),.11f,.17f,teal);
        for(int i=0;i<3;i++)
        {
            var flower=new Vector3(26.65f+i*.055f,1.17f+(i%2)*.08f,12.48f);
            Rod(root,"Flower stem",new Vector3(26.72f,.93f,12.48f),flower,.006f,green);
            Ball(root,"Small yellow bloom",flower,new Vector3(.08f,.06f,.08f),yellow);
        }
        for(int i=0;i<2;i++)
        {
            var cushion=Cube(root,"Bench accent cushion",new Vector3(27.55f,.79f,12.0f+i*1.05f),new Vector3(.18f,.38f,.4f),i==0?yellow:fabric);
            cushion.transform.rotation=Quaternion.Euler(0,0,-12f);
        }
        // Narrow warm shelf strips and supports give the wall depth.
        for(int i=0;i<3;i++)
        {
            float y=1.72f+i*.48f;
            Cube(root,"Warm under shelf strip",new Vector3(23.5f,y-.035f,15.14f),new Vector3(4.5f,.012f,.035f),glow);
            foreach(float x in new[]{21.4f,25.5f}) Cube(root,"Shelf steel bracket",new Vector3(x,y-.09f,15.25f),new Vector3(.035f,.16f,.13f),ink);
        }
        var task=new GameObject("Warm counter task light").AddComponent<Light>();
        task.transform.SetParent(root); task.transform.position=new Vector3(23.3f,1.6f,14.65f);
        task.type=LightType.Point; task.color=new Color(1,.85f,.65f); task.range=3.2f; task.intensity=.65f;
        var clock=Disc(root,"Wall clock rim",new Vector3(25.55f,3.3f,15.24f),.48f,.035f,ink); clock.transform.rotation=Quaternion.Euler(90,0,0);
        var face=Disc(root,"Wall clock face",new Vector3(25.55f,3.3f,15.215f),.43f,.01f,fabric); face.transform.rotation=Quaternion.Euler(90,0,0);
        Cube(root,"Clock minute hand",new Vector3(25.55f,3.37f,15.2f),new Vector3(.014f,.15f,.01f),ink);
        Cube(root,"Clock hour hand",new Vector3(25.6f,3.3f,15.198f),new Vector3(.11f,.018f,.01f),ink);
        EditorSceneManager.MarkSceneDirty(kitchen.scene);
        EditorSceneManager.SaveScene(kitchen.scene);
        AssetDatabase.SaveAssets();
    }

    static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/ExecutiveOffice/Materials/"+name+".mat");
    static Material NewMat(string name, Color color, float smooth, float metallic=0)
    {
        var m=Mat(name);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,"Assets/ExecutiveOffice/Materials/"+name+".mat");}
        m.color=color; m.SetFloat("_Smoothness",smooth); m.SetFloat("_Metallic",metallic); EditorUtility.SetDirty(m); return m;
    }
    static GameObject Shape(Transform root,string name,Vector3 pos,Vector3 scale,Material mat,PrimitiveType type)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);go.transform.position=pos;go.transform.localScale=scale;
        go.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    static GameObject Cube(Transform r,string n,Vector3 p,Vector3 s,Material m)=>Shape(r,n,p,s,m,PrimitiveType.Cube);
    static GameObject Disc(Transform r,string n,Vector3 p,float diameter,float height,Material m)=>Shape(r,n,p,new Vector3(diameter,height*.5f,diameter),m,PrimitiveType.Cylinder);
    static void Ball(Transform r,string n,Vector3 p,Vector3 s,Material m)=>Shape(r,n,p,s,m,PrimitiveType.Sphere);
    static void Rod(Transform r,string n,Vector3 a,Vector3 b,float radius,Material m)
    {
        var go=Disc(r,n,(a+b)*.5f,radius*2,(b-a).magnitude,m);go.transform.up=(b-a).normalized;
    }
    static void Ring(Transform r,string n,Vector3 p,float radius,float thickness,Material m)
    {
        const int count=16;
        for(int i=0;i<count;i++)
        {
            float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;
            Rod(r,n,p+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0),p+new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,0),thickness,m);
        }
    }
    static void Cup(Transform r,Vector3 bottom,Material body,Material coffee)
    {
        Disc(r,"Cup saucer",bottom+Vector3.up*.006f,.18f,.012f,body);
        Disc(r,"Coffee cup",bottom+Vector3.up*.068f,.11f,.11f,body);
        Disc(r,"Coffee surface",bottom+Vector3.up*.124f,.085f,.003f,coffee);
        Ring(r,"Cup handle",bottom+new Vector3(.073f,.07f,0),.037f,.009f,body);
    }

    [MenuItem("Tools/Office/Capture Kitchen %#j")]
    public static void Capture()
    {
        Directory.CreateDirectory("OfficePreview");
        Shot("Kitchen_Entrance", new Vector3(25.8f, 2.25f, 8.1f), new Vector3(23.9f, 1.35f, 14.1f));
        Shot("Kitchen_Details", new Vector3(26.65f, 2.1f, 13.2f), new Vector3(23.1f, 1.55f, 14.8f));
    }

    static void Shot(string name, Vector3 position, Vector3 target)
    {
        var go = new GameObject("Kitchen preview camera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = go.AddComponent<Camera>();
        camera.transform.position = position;
        camera.transform.LookAt(target);
        camera.fieldOfView = 72f;
        camera.nearClipPlane = .05f;
        camera.farClipPlane = 100f;
        camera.clearFlags = CameraClearFlags.Skybox;
        var rt = new RenderTexture(1600, 1000, 24);
        var previous = RenderTexture.active;
        var texture = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
            texture.Apply();
            File.WriteAllBytes("OfficePreview/" + name + ".png", texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
