using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Builds an editable office using the supplied OBJ furniture and its texture atlases.</summary>
[InitializeOnLoad]
public static class OfficeSceneBuilder
{
    const string ModelRoot = "Assets/models/meshes/data/SM_Prop/";
    const string Output = "Assets/Office";
    static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
    static Transform architecture, furniture, details;
    static Shader shader;

    static OfficeSceneBuilder()
    {
        // A one-shot file lets a running Editor perform the build after its next script reload.
        if (File.Exists("Temp/BuildOffice.request"))
            EditorApplication.delayCall += BuildRequestedScene;
    }

    static void BuildRequestedScene()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BuildRequestedScene;
            return;
        }
        File.Delete("Temp/BuildOffice.request");
        try { Build(); }
        catch (Exception error)
        {
            Directory.CreateDirectory("OfficePreview");
            File.WriteAllText("OfficePreview/build-error.txt", error.ToString());
            Debug.LogException(error);
        }
    }

    [MenuItem("Tools/Office/Build Office Scene")]
    public static void Build()
    {
        Directory.CreateDirectory(Output + "/Materials");
        AssetDatabase.Refresh();
        Materials.Clear();
        shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null ? "Standard" : "Universal Render Pipeline/Lit");
        ModelCount = 0;
        var previous = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Office.unity");
        if (previous.IsValid() && previous.isLoaded)
            EditorSceneManager.CloseScene(previous, true);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        // Saved scenes can be closed safely; retain any unsaved scene the user is editing.
        for (int i = UnityEngine.SceneManagement.SceneManager.sceneCount - 1; i >= 0; i--)
        {
            var other = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (other != scene && !other.isDirty) EditorSceneManager.CloseScene(other, true);
        }
        architecture = new GameObject("01 Architecture").transform;
        furniture = new GameObject("02 Furniture from models").transform;
        details = new GameObject("03 Interior and lighting").transform;

        var plaster = Solid("Warm plaster", new Color(.84f,.85f,.81f));
        var teal = Solid("Deep teal", new Color(.055f,.19f,.21f));
        var oak = Solid("Oak", new Color(.48f,.30f,.16f));
        var trim = Solid("Graphite", new Color(.075f,.10f,.12f));
        var carpet = Solid("Carpet", new Color(.22f,.28f,.30f));
        var tile = Solid("Carpet alternate", new Color(.245f,.305f,.32f));
        var rug = Solid("Lounge rug", new Color(.63f,.39f,.22f));
        var lightMat = Solid("Warm light", new Color(.94f,.89f,.69f), 1.6f);
        var blueGlass = Solid("Window daylight", new Color(.48f,.72f,.83f), .4f);

        Box("Foundation", new Vector3(0,-.19f,0), new Vector3(18.4f,.36f,14.4f), trim, architecture);
        for(int x=0;x<18;x++) for(int z=0;z<14;z++)
            Box("Carpet tile",new Vector3(x-8.5f,-.015f,z-6.5f),new Vector3(.995f,.03f,.995f), (x+z)%3==0?tile:carpet,architecture,false);
        Box("Back wall",new Vector3(0,1.7f,7.1f),new Vector3(18.4f,3.4f,.2f),plaster,architecture);
        Box("Left wall",new Vector3(-9.1f,1.7f,0),new Vector3(.2f,3.4f,14.4f),plaster,architecture);
        // Cutaway front and right sides keep every desk visible from the overview camera.
        Box("Front low wall left",new Vector3(-5.15f,.24f,-7.1f),new Vector3(7.9f,.48f,.2f),plaster,architecture);
        Box("Front low wall right",new Vector3(5.15f,.24f,-7.1f),new Vector3(7.9f,.48f,.2f),plaster,architecture);
        Box("Right low wall",new Vector3(9.1f,.24f,0),new Vector3(.2f,.48f,14.4f),plaster,architecture);
        Box("Back skirting",new Vector3(0,.08f,6.95f),new Vector3(18,.16f,.06f),trim,architecture);
        Box("Left skirting",new Vector3(-8.95f,.08f,0),new Vector3(.06f,.16f,14),trim,architecture);
        Box("Brand wall",new Vector3(-5.5f,1.77f,6.96f),new Vector3(6.6f,3.1f,.06f),teal,architecture);
        Label("NORTH / OFFICE",new Vector3(-7.9f,2.5f,6.89f),.075f,Color.white,Quaternion.identity);
        Label("WORKSPACE     /     08",new Vector3(-7.85f,2.06f,6.88f),.03f,new Color(.65f,.81f,.79f),Quaternion.identity);
        for(int i=0;i<4;i++)
        {
            float z=-4.8f+i*3.1f;
            Box("Window frame",new Vector3(-8.94f,2.03f,z),new Vector3(.10f,2.05f,2.48f),trim,architecture);
            Box("Daylight glazing",new Vector3(-8.87f,2.03f,z),new Vector3(.035f,1.87f,2.30f),blueGlass,architecture);
            Box("Window mullion",new Vector3(-8.83f,2.03f,z),new Vector3(.065f,1.9f,.055f),trim,architecture);
            Box("Oak sill",new Vector3(-8.77f,1.03f,z),new Vector3(.42f,.08f,2.60f),oak,architecture);
        }

        int station=0;
        foreach(float z in new[]{-2.8f,2.2f})
        foreach(float x in new[]{-5.5f,-2.8f})
        foreach(int side in new[]{-1,1})
        {
            var root=new GameObject("Workstation "+(++station).ToString("00")).transform;
            root.SetParent(furniture);
            root.position=new Vector3(x,0,z+side*.60f);
            root.rotation=Quaternion.Euler(0,side<0?0:180,0);
            Place("Office_Desk_01",new Vector3(0,0,0),0,root,.84f);
            // Desktop is .8 m above the floor after scaling the original desk.
            Place("Computer_Monitor_01",new Vector3(-.15f,.805f,.19f),180,root,1f);
            Place("Computer_Keyboard_01",new Vector3(-.15f,.805f,-.18f),0,root,.8f);
            Place("Computer_Mouse_01",new Vector3(.30f,.805f,-.19f),0,root,1f);
            Place("Computer_Tower_02",new Vector3(.68f,.805f,.10f),0,root,.85f);
            Place("Cafe_Chair_01",new Vector3(0,0,-1.05f),0,root,1f);
            Place("Cafe_CoffeeCup_01",new Vector3(-.69f,.805f,-.12f),0,root,.85f);
            if(station%2==0) Place("Props_Papers_01",new Vector3(-.66f,.805f,.15f),12,root,.65f, "SM_");
        }
        foreach(float z in new[]{-2.8f,2.2f})
        {
            Box("Desk divider",new Vector3(-4.15f,1.04f,z),new Vector3(4.65f,.44f,.075f),teal,furniture);
            Box("Divider cap",new Vector3(-4.15f,1.27f,z),new Vector3(4.7f,.035f,.09f),oak,furniture);
        }

        // A lounge occupies the right side, separated by an open timber screen.
        Box("Lounge rug",new Vector3(5,.015f,2.4f),new Vector3(5.4f,.035f,5.2f),rug,details,false);
        Place("Sofa_01",new Vector3(5,.04f,4.3f),180,furniture,1.05f);
        Place("Sofa_01",new Vector3(7,.04f,2),270,furniture,.95f);
        Place("CoffeeTable_01",new Vector3(4.9f,.04f,2.35f),0,furniture,1.35f);
        Place("Cafe_CoffeeCup_01",new Vector3(4.6f,.62f,2.35f),0,details);
        Place("Book_Group_02",new Vector3(5.2f,.62f,2.4f),0,details,.7f);
        for(int i=0;i<8;i++) Box("Timber screen slat",new Vector3(1.8f,1.3f,1+i*.48f),new Vector3(.10f,2.6f,.11f),oak,architecture);
        Box("Screen base",new Vector3(1.8f,.1f,2.7f),new Vector3(.26f,.2f,4.05f),trim,architecture);
        Place("Bookshelf_01",new Vector3(.25f,0,6.62f),180,furniture);
        Place("Bookshelf_01",new Vector3(1.55f,0,6.62f),180,furniture);
        Place("PotPlant_01",new Vector3(-7.8f,0,5.9f),0,furniture,1.9f);
        Place("PotPlant_01",new Vector3(-7.8f,0,-5.9f),0,furniture,1.9f);
        Place("PotPlant_01",new Vector3(8,0,6),0,furniture,2.1f);
        Place("PotPlant_01",new Vector3(2.2f,0,-5.9f),0,furniture,1.6f);
        Place("PotPlant_01",new Vector3(1.8f,0,.15f),0,furniture,1.3f);

        Box("Coffee counter",new Vector3(5.9f,.45f,-5.8f),new Vector3(3.8f,.9f,.85f),teal,furniture);
        Box("Coffee countertop",new Vector3(5.9f,.93f,-5.8f),new Vector3(3.95f,.08f,.98f),oak,furniture);
        Place("CoffeeMachine_01",new Vector3(4.8f,.975f,-5.8f),0,furniture,1.2f);
        Place("Computer_Printer_01",new Vector3(6.9f,.975f,-5.8f),0,furniture,.9f);
        Place("Cafe_CoffeeCup_01",new Vector3(5.45f,.975f,-5.7f),0,details);
        Place("WaterCooler_01",new Vector3(8.2f,0,-5.7f),180,furniture);
        Label("COFFEE / PRINT",new Vector3(4.05f,.35f,-5.30f),.025f,Color.white,Quaternion.identity);
        // A compact collaboration table in the open area.
        Box("Meeting tabletop",new Vector3(4.8f,.79f,-2.7f),new Vector3(2.8f,.1f,1.25f),oak,furniture);
        foreach(float x in new[]{3.7f,5.9f}) foreach(float z in new[]{-3.1f,-2.3f})
            Box("Meeting table leg",new Vector3(x,.38f,z),new Vector3(.08f,.76f,.08f),trim,furniture);
        foreach(float x in new[]{4.1f,5.5f}) foreach(int side in new[]{-1,1})
            Place("Cafe_Chair_01",new Vector3(x,0,-2.7f+side*1.15f),side<0?0:180,furniture,1f);
        Place("Book_Group_02",new Vector3(4.8f,.85f,-2.7f),0,details,.7f);

        var lighting=new GameObject("04 Lighting").transform;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.66f,.74f,.80f);
        RenderSettings.ambientEquatorColor=new Color(.46f,.51f,.53f);
        RenderSettings.ambientGroundColor=new Color(.27f,.29f,.29f);
        RenderSettings.skybox=null;
        var sun=new GameObject("Afternoon daylight").AddComponent<Light>();
        sun.transform.SetParent(lighting);
        sun.type=LightType.Directional; sun.intensity=1.15f; sun.color=new Color(1,.94f,.83f);
        sun.transform.rotation=Quaternion.Euler(48,-35,0); sun.shadows=LightShadows.Soft;
        sun.shadowBias=.035f; sun.shadowNormalBias=.25f;
        foreach(float x in new[]{-5f,4.5f}) foreach(float z in new[]{-2.8f,2.4f})
        {
            Box("Suspended light housing",new Vector3(x,3.25f,z),new Vector3(2.6f,.10f,.35f),trim,lighting,false);
            Box("Light diffuser",new Vector3(x,3.19f,z),new Vector3(2.45f,.025f,.26f),lightMat,lighting,false);
            foreach(float offset in new[]{-.95f,.95f})
                Box("Suspension cable",new Vector3(x+offset,3.51f,z),new Vector3(.018f,.44f,.018f),trim,lighting,false);
        }
        var camera=new GameObject("Office Overview Camera").AddComponent<Camera>();
        camera.tag="MainCamera"; camera.transform.position=new Vector3(19,20,-23);
        camera.transform.LookAt(new Vector3(0,.3f,0)); camera.orthographic=true; camera.orthographicSize=10.6f;
        camera.nearClipPlane=.1f; camera.farClipPlane=150;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.095f,.13f,.16f);
        camera.gameObject.AddComponent<AudioListener>();
        camera.gameObject.AddComponent<OfficeCameraOrbit>();
        QualitySettings.shadows=ShadowQuality.All; QualitySettings.shadowResolution=ShadowResolution.High;
        QualitySettings.shadowDistance=70; QualitySettings.antiAliasing=4;

        AssetDatabase.SaveAssets();
        const string scenePath="Assets/Scenes/Office.unity";
        EditorSceneManager.SaveScene(scene,scenePath);
        var scenes=EditorBuildSettings.scenes.ToList();
        if(!scenes.Any(s=>s.path==scenePath)) { scenes.Insert(0,new EditorBuildSettingsScene(scenePath,true)); EditorBuildSettings.scenes=scenes.ToArray(); }
        SceneView.lastActiveSceneView?.LookAt(new Vector3(0,.3f,0),camera.transform.rotation,22);
        Directory.CreateDirectory("OfficePreview");
        var hiddenRoots = new List<GameObject>();
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
        {
            var other=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if(other==scene) continue;
            foreach(var root in other.GetRootGameObjects()) if(root.activeSelf) { hiddenRoots.Add(root); root.SetActive(false); }
        }
        var rt=new RenderTexture(1600,1100,24);
        camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
        var image=new Texture2D(1600,1100,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1600,1100),0,0); image.Apply();
        File.WriteAllBytes("OfficePreview/Office.png",image.EncodeToPNG());
        camera.targetTexture=null; RenderTexture.active=null;
        foreach(var root in hiddenRoots) root.SetActive(true);
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
        File.WriteAllText("OfficePreview/build-result.txt","Office scene saved. Workstations: "+station+". Model instances: "+ModelCount+". Scene: "+scenePath);
        Debug.Log("OFFICE_BUILD_SUCCESS: "+scenePath);
    }

    static int ModelCount;
    static GameObject Place(string name,Vector3 position,float yaw,Transform parent,float scale=1,string prefix="SM_Prop_")
    {
        string path=ModelRoot+prefix+name+".obj";
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(model==null) throw new FileNotFoundException("Missing office model: "+path);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(model);
        go.name=name; go.transform.SetParent(parent,false);
        go.transform.localPosition=position; go.transform.localRotation=Quaternion.Euler(0,yaw,0); go.transform.localScale=Vector3.one*scale;
        var material=AtlasMaterial(path);
        foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>material).ToArray();
            var collider=renderer.gameObject.AddComponent<BoxCollider>();
            var bounds=renderer.GetComponent<MeshFilter>().sharedMesh.bounds;
            collider.center=bounds.center; collider.size=bounds.size;
        }
        ModelCount++;
        return go;
    }

    static Material AtlasMaterial(string objPath)
    {
        string mtlPath=Path.ChangeExtension(objPath,".mtl");
        string line=File.ReadLines(mtlPath).First(l=>l.StartsWith("map_Kd "));
        string texturePath=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(mtlPath),line.Substring(7).Trim())).Replace('\\','/');
        texturePath=texturePath.Substring(texturePath.IndexOf("Assets/",StringComparison.Ordinal));
        string key=Path.GetFileNameWithoutExtension(texturePath);
        if(Materials.TryGetValue(key,out var cached)) return cached;
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if(texture==null) throw new FileNotFoundException("Missing atlas: "+texturePath);
        var mat=new Material(shader) { name=key, mainTexture=texture };
        if(mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",.2f);
        if(mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness",.2f);
        SaveMaterial(mat,key); return Materials[key];
    }

    static Material Solid(string name,Color color,float emission=0)
    {
        var mat=new Material(shader) { name=name, color=color };
        if(mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",.25f);
        if(mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness",.25f);
        if(emission>0) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor",color*emission); }
        SaveMaterial(mat,name); return Materials[name];
    }

    static void SaveMaterial(Material mat,string key)
    {
        string path=Output+"/Materials/"+key+".mat";
        var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(existing!=null) { EditorUtility.CopySerialized(mat,existing); UnityEngine.Object.DestroyImmediate(mat); Materials[key]=existing; }
        else { AssetDatabase.CreateAsset(mat,path); Materials[key]=mat; }
    }

    static GameObject Box(string name,Vector3 position,Vector3 scale,Material material,Transform parent,bool collider=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false);
        go.transform.localPosition=position; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=material;
        if(!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static void Label(string text,Vector3 position,float size,Color color,Quaternion rotation)
    {
        var go=new GameObject(text); go.transform.SetParent(details); go.transform.position=position; go.transform.rotation=rotation;
        var label=go.AddComponent<TextMesh>(); label.text=text; label.characterSize=size; label.fontSize=64; label.color=color;
    }
}
