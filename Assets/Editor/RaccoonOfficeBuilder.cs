using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class RaccoonOfficeBuilder
{
    const string Root = "Assets/models/RaccoonGame/Content/";
    const string Output = "Assets/Office2";
    const string ScenePath = "Assets/Scenes/Office2_Raccoon.unity";
    const string Props = "Airship/Props/";
    const string Office = "AssetPacks/PolygonOffice/Meshes/Props/";
    const string Wall = "Airship/Building/SM_Bld_House_Wall_01.obj";
    const string Floor = "Models/LevelTiles/House/Tile_House_Floor_01.obj";
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static readonly HashSet<string> sources = new HashSet<string>();
    static Transform architecture, work, meeting, lobby, lounge;
    static Shader shader;

    [MenuItem("Tools/Office/Build Raccoon Office 2")]
    public static void Build()
    {
        Directory.CreateDirectory(Output + "/Materials");
        Directory.CreateDirectory("OfficePreview");
        AssetDatabase.Refresh();
        materials.Clear(); sources.Clear();
        shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null ? "Standard" : "Universal Render Pipeline/Lit");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        // Preserve any unsaved work before isolating the new office.
        for(int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            var other = SceneManager.GetSceneAt(i);
            if(other == scene) continue;
            if(other.isDirty)
            {
                Directory.CreateDirectory("Assets/Scenes/Backups");
                string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Backups/" + other.name + "_BeforeOffice2.unity");
                if(!EditorSceneManager.SaveScene(other, backup, true)) throw new Exception("Could not preserve " + other.name);
            }
            EditorSceneManager.CloseScene(other, true);
        }
        architecture = Group("01 L-shaped architecture — Raccoon models");
        work = Group("02 Studio — 6 workstations");
        meeting = Group("03 Meeting room — 6 seats");
        lobby = Group("04 Reception and waiting");
        lounge = Group("05 Break room and billiards");

        // 20 x 12 studio, with a 12 x 4 reception wing at the entrance.
        CreateFloor(architecture);
        for(int i=0;i<4;i++)
            Fit(Wall,new Vector3(-7.5f+i*5,1.6f,8.1f),new Vector3(.24f,3.2f,5),architecture,90);
        for(int i=0;i<3;i++)
            Fit("Airship/Building/SM_Bld_House_Wall_Window_02.obj",new Vector3(-10.1f,1.6f,-5.33f+i*5.33f),new Vector3(.28f,3.2f,5.33f),architecture);
        Fit(Wall,new Vector3(10.1f,.34f,2),new Vector3(.24f,.68f,12),architecture);
        Fit(Wall,new Vector3(6,.34f,-4.1f),new Vector3(.24f,.68f,8),architecture,90);
        Fit(Wall,new Vector3(2.1f,.34f,-6),new Vector3(.24f,.68f,4),architecture);
        // Entrance opening is 2 m wide on the front of the reception wing.
        Fit(Wall,new Vector3(-8,.34f,-8.1f),new Vector3(.24f,.68f,4),architecture,90);
        Fit(Wall,new Vector3(-1,.34f,-8.1f),new Vector3(.24f,.68f,6),architecture,90);
        // Meeting room walls are cut away on the viewer-facing side.
        Fit(Wall,new Vector3(2.2f,1.35f,5.6f),new Vector3(.18f,2.7f,4.8f),architecture);
        Fit(Wall,new Vector3(2.2f,.48f,1.0f),new Vector3(.18f,.96f,1.6f),architecture);
        Fit(Wall,new Vector3(6.1f,.48f,.1f),new Vector3(.18f,.96f,7.8f),architecture,90);

        int n=0;
        foreach(float z in new[]{.7f,3.0f,5.3f}) foreach(int side in new[]{-1,1})
        {
            var station=Group("Workstation "+(++n).ToString("00"),work);
            station.position=new Vector3(side<0?-7.05f:-2.85f,0,z);
            station.rotation=Quaternion.Euler(0,side<0?90:270,0);
            Place(Props+"SM_Prop_Desk_04.obj",Vector3.zero,station);
            Place(Props+"SM_Prop_Chair_03.obj",new Vector3(0,0,-1.02f),station);
            Place(Office+"Desk/SM_Prop_Computer_Monitor_01.obj",new Vector3(-.12f,.755f,.20f),station,180);
            Place(Office+"Desk/SM_Prop_Computer_Keyboard_01.obj",new Vector3(-.12f,.755f,-.18f),station,180,.85f);
            Place(Office+"Desk/SM_Prop_Computer_Mouse_01.obj",new Vector3(.30f,.755f,-.17f),station);
            Place(Office+"Desk/SM_Prop_Computer_Tower_02.obj",new Vector3(.75f,0,.1f),station);
            Place(Props+"SM_Prop_DeskLamp_06.obj",new Vector3(-.75f,.755f,.21f),station,150,.8f);
            Place(Office+"Desk/SM_Prop_Photo_01.obj",new Vector3(.67f,.755f,.22f),station,180,.8f);
            Place(Office+"Desk/SM_Prop_Book_Group_03.obj",new Vector3(.57f,.755f,-.12f),station,0,.6f);
        }
        // Low shelves and greenery form a central spine instead of desk dividers.
        foreach(float z in new[]{1.8f,4.6f})
        {
            Place(Office+"Furnature/SM_Prop_Shelf_05_StandingFix.obj",new Vector3(-4.95f,0,z),work,90,.82f);
            Place(Office+"Misc/SM_Prop_Plant_10.obj",new Vector3(-4.95f,1.25f,z),work,0,1.05f);
        }
        Place(Props+"SM_Prop_Whiteboard_02_Combined.obj",new Vector3(-.25f,0,6.7f),work,180);
        Place(Office+"Misc/SM_Prop_Plant_01.obj",new Vector3(-8.8f,0,6.9f),work,0,1.25f);

        // Six-seat conference room with projection screen and a sideboard.
        Place(Office+"Misc/SM_Prop_Rug_02.obj",new Vector3(6.1f,.015f,4.2f),meeting,0,1.4f);
        var table=Place(Props+"SM_Prop_Table_01.obj",new Vector3(6.0f,.04f,4.15f),meeting);
        table.transform.localScale=new Vector3(1.35f,1,1.15f);
        foreach(float x in new[]{4.9f,6,7.1f}) foreach(int side in new[]{-1,1})
            Place(Props+"SM_Prop_Chair_03.obj",new Vector3(x,0,4.15f+side*1.23f),meeting,side<0?0:180);
        Place(Office+"Wall/SM_Prop_ProjectorScreen_01.obj",new Vector3(6.1f,1.4f,7.87f),meeting,180);
        Place(Office+"Desk/SM_Prop_Book_Group_02.obj",new Vector3(5.4f,.86f,4.15f),meeting,0,.65f);
        Place(Office+"Desk/SM_Prop_Cellphone_01.obj",new Vector3(6.6f,.86f,4.15f),meeting);
        Place(Office+"Furnature/SM_Prop_Shelf_03_StandingFix.obj",new Vector3(9.4f,0,5.7f),meeting,270);
        Place(Office+"Misc/SM_Prop_Plant_13.obj",new Vector3(8.9f,0,7),meeting);

        // Reception is in a separate wing, with a curved desk and a waiting nook.
        Place(Props+"SM_Prop_Desk_06.obj",new Vector3(-2.1f,0,-5.7f),lobby,180);
        Place(Props+"SM_Prop_Chair_03.obj",new Vector3(-2.1f,0,-4.55f),lobby,180);
        Place(Office+"Desk/SM_Prop_Computer_Monitor_02.obj",new Vector3(-2.25f,.935f,-5.7f),lobby);
        Place(Office+"Desk/SM_Prop_Computer_Keyboard_01.obj",new Vector3(-2.25f,.935f,-5.42f),lobby);
        Place(Office+"Desk/SM_Prop_NamePlate_01.obj",new Vector3(-1.3f,.935f,-5.85f),lobby);
        Place(Props+"SM_Prop_Couch_04.obj",new Vector3(-8.7f,0,-5.7f),lobby,90);
        Place(Office+"Furnature/SM_Prop_CoffeeTable_03.obj",new Vector3(-7.1f,0,-5.7f),lobby,90);
        Place(Office+"Desk/SM_Prop_Book_Magazine_01.obj",new Vector3(-7.1f,.46f,-5.7f),lobby,0,.8f);
        Place(Office+"Misc/SM_Prop_CoatRack_01.obj",new Vector3(.8f,0,-7.1f),lobby);
        Place(Office+"Misc/SM_Prop_Plant_01.obj",new Vector3(-9,0,-7.2f),lobby,0,1.25f);
        Place(Props+"SM_Prop_LobbyBoard_01.obj",new Vector3(-9.87f,1.35f,-3.1f),lobby,90,.75f);

        // Break room gets different decorations: a pool table, fridge, and small cafe table.
        Place(Office+"Misc/SM_Prop_PoolTable_01.obj",new Vector3(6.2f,0,-2.05f),lounge,90,.9f);
        Place(Office+"Misc/SM_Prop_PoolTriangle_01.obj",new Vector3(6.2f,.80f,-2.05f),lounge,0,.9f);
        Place(Office+"Kitchen/SM_Prop_Fridge_01_Combined.obj",new Vector3(9,0,-.75f),lounge,270,.8f);
        Place(Props+"SM_Prop_Table_Round_02.obj",new Vector3(.1f,0,-1.8f),lounge,0,1.1f);
        Place(Props+"SM_Prop_DiningChair_01.obj",new Vector3(.1f,0,-3.05f),lounge);
        Place(Props+"SM_Prop_DiningChair_01.obj",new Vector3(1.3f,0,-1.8f),lounge,270);
        Place(Props+"SM_Prop_CoffeeMachine_02.obj",new Vector3(.1f,.83f,-1.8f),lounge,180,.8f);
        Place(Office+"Misc/SM_Prop_Plant_13.obj",new Vector3(8.9f,0,-3.25f),lounge);

        var light= new GameObject("Daylight").AddComponent<Light>();
        light.type=LightType.Directional; light.intensity=1.2f; light.color=new Color(1,.94f,.84f);
        light.transform.rotation=Quaternion.Euler(48,-32,0); light.shadows=LightShadows.Soft;
        RenderSettings.skybox=null; RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.73f,.80f,.86f);
        RenderSettings.ambientEquatorColor=new Color(.52f,.57f,.62f);
        RenderSettings.ambientGroundColor=new Color(.3f,.32f,.36f);
        var camera=new GameObject("Office 2 Overview").AddComponent<Camera>();
        camera.tag="MainCamera"; camera.transform.position=new Vector3(22,23,-27);
        camera.transform.LookAt(new Vector3(0,.3f,0)); camera.orthographic=true; camera.orthographicSize=11.5f;
        camera.nearClipPlane=.1f; camera.farClipPlane=150;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.065f,.085f,.12f);
        camera.gameObject.AddComponent<AudioListener>(); camera.gameObject.AddComponent<OfficeCameraOrbit>();
        ValidateSources();
        AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene,ScenePath);
        var scenes=EditorBuildSettings.scenes.ToList();
        if(!scenes.Any(s=>s.path==ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath,true)); EditorBuildSettings.scenes=scenes.ToArray(); }
        File.WriteAllLines("OfficePreview/Office2-model-sources.txt",sources.OrderBy(s=>s));
        SceneView.lastActiveSceneView?.LookAt(new Vector3(0,.4f,0),camera.transform.rotation,24);
        Debug.Log("OFFICE2_BUILD_SUCCESS: only RaccoonGame meshes; "+sources.Count+" source models.");
    }

    static Transform Group(string name,Transform parent=null)
    {
        var t=new GameObject(name).transform; if(parent!=null) t.SetParent(parent,false); return t;
    }

    [MenuItem("Tools/Office/Update Raccoon Office Floor")]
    public static void UpdateFloor()
    {
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.IsValid() || !scene.isLoaded) throw new Exception("Open Office2_Raccoon first.");
        SceneManager.SetActiveScene(scene);
        shader=Shader.Find(GraphicsSettings.currentRenderPipeline==null?"Standard":"Universal Render Pipeline/Lit");
        architecture=scene.GetRootGameObjects().First(g=>g.name.StartsWith("01 L-shaped architecture")).transform;
        var old=architecture.GetComponentsInChildren<Transform>(true)
            .Where(t=>t.name=="Tile_House_Floor_01" && t.parent==architecture || t.name=="Zoned floor").ToArray();
        foreach(var item in old) UnityEngine.Object.DestroyImmediate(item.gameObject);
        CreateFloor(architecture);
        AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
        Debug.Log("Office 2 zoned floor saved. " + ValidateSources());
    }

    static void CreateFloor(Transform parent)
    {
        var root=Group("Zoned floor",parent);
        var blue=FloorMaterial("Studio blue tile", "AssetPacks/PolygonOffice/Textures/T_OfficePlanks1.png",new Color(.65f,.85f,.99f));
        var blueAlt=FloorMaterial("Studio blue alternate", "AssetPacks/PolygonOffice/Textures/T_OfficePlanks1.png",new Color(.61f,.80f,.92f));
        var oak=FloorMaterial("Meeting oak parquet", "Airship/Textures/T_WoodFloor_A.png",new Color(1.18f,1.12f,1.02f));
        var oakAlt=FloorMaterial("Meeting oak alternate", "Airship/Textures/T_WoodFloor_A.png",new Color(1.08f,1.03f,.94f));
        var ivory=FloorMaterial("Reception ivory tile", "AssetPacks/PolygonOffice/Textures/T_OfficePlanks1.png",new Color(1.8f,1.73f,1.55f));
        var charcoal=FloorMaterial("Billiards charcoal tile", "AssetPacks/PolygonOffice/Textures/T_OfficePlanks1.png",new Color(.55f,.59f,.61f));
        var border=FloorMaterial("Floor brass inlay",null,new Color(.62f,.40f,.18f));
        for(int x=-10;x<10;x++) for(int z=-8;z<8;z++)
        {
            if(z < -4 && x>=2) continue;
            Material material;
            bool alternating=(x+z)%2==0;
            if(z>=0 && x<2) material=alternating?blue:blueAlt;
            else if(z>=0) material=alternating?oak:oakAlt;
            else if(x>=2) material=charcoal;
            else material=ivory;
            Fit(Floor,new Vector3(x+.5f,-.12f,z+.5f),new Vector3(.997f,.24f,.997f),root,alternating?0:90);
            foreach(var r in root.GetChild(root.childCount-1).GetComponentsInChildren<MeshRenderer>())
                r.sharedMaterials=r.sharedMaterials.Select(_=>material).ToArray();
        }
        // Inlaid borders follow the room thresholds, without a raised step.
        FloorBorder(new Vector3(-4,.006f,0),new Vector3(12,.012f,.045f),root,border);
        FloorBorder(new Vector3(2,.006f,4),new Vector3(.045f,.012f,8),root,border);
        FloorBorder(new Vector3(6,.006f,0),new Vector3(8,.012f,.045f),root,border);
        FloorBorder(new Vector3(-4,.006f,-4),new Vector3(12,.012f,.045f),root,border);
        // Timber frame around the reception wing makes the entry read as a separate zone.
        FloorBorder(new Vector3(-9.6f,.006f,-6),new Vector3(.16f,.012f,3.2f),root,oak);
        FloorBorder(new Vector3(1.6f,.006f,-6),new Vector3(.16f,.012f,3.2f),root,oak);
        FloorBorder(new Vector3(-4,.006f,-7.6f),new Vector3(11.36f,.012f,.16f),root,oak);
    }

    static void FloorBorder(Vector3 center,Vector3 size,Transform parent,Material material)
    {
        Fit(Floor,center,size,parent);
        var go=parent.GetChild(parent.childCount-1); go.name="Floor inlay";
        foreach(var r in go.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterials=r.sharedMaterials.Select(_=>material).ToArray();
        foreach(var c in go.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
    }

    static Material FloorMaterial(string name,string texture,Color tint)
    {
        var mat=new Material(shader){name=name,color=tint};
        if(texture!=null)
        {
            mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+texture);
            if(mat.mainTexture==null) throw new FileNotFoundException(Root+texture);
        }
        if(mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness",.24f);
        if(mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",.24f);
        string path=Output+"/Materials/"+name+".mat";
        var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old!=null) { EditorUtility.CopySerialized(mat,old); UnityEngine.Object.DestroyImmediate(mat); return old; }
        AssetDatabase.CreateAsset(mat,path); return mat;
    }

    static GameObject Place(string source,Vector3 pos,Transform parent,float yaw=0,float scale=1)
    {
        string path=Root+source;
        if(!path.StartsWith("Assets/models/RaccoonGame/",StringComparison.Ordinal)) throw new Exception("Outside RaccoonGame: "+path);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(prefab==null) throw new FileNotFoundException(path);
        sources.Add(path);
        var wrapper=new GameObject(Path.GetFileNameWithoutExtension(path));
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(wrapper.transform,false);
        var renderers=instance.GetComponentsInChildren<MeshRenderer>();
        var bindings=ReadBindings(path);
        foreach(var r in renderers)
        {
            r.sharedMaterials=r.sharedMaterials.Select(m=>MaterialFor(m==null?bindings.Keys.First():m.name,bindings,path)).ToArray();
            var filter=r.GetComponent<MeshFilter>();
            if(filter!=null && filter.sharedMesh!=null)
            {
                var box=r.gameObject.AddComponent<BoxCollider>(); box.center=filter.sharedMesh.bounds.center; box.size=filter.sharedMesh.bounds.size;
            }
        }
        var bounds=renderers[0].bounds;
        foreach(var r in renderers) bounds.Encapsulate(r.bounds);
        instance.transform.position=-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        wrapper.transform.SetParent(parent,false); wrapper.transform.localPosition=pos;
        wrapper.transform.localRotation=Quaternion.Euler(0,yaw,0); wrapper.transform.localScale=Vector3.one*scale;
        return wrapper;
    }

    static void Fit(string source,Vector3 center,Vector3 size,Transform parent,float yaw=0)
    {
        var go=Place(source,Vector3.zero,parent);
        var rs=go.GetComponentsInChildren<Renderer>(); var b=rs[0].bounds;
        foreach(var r in rs) b.Encapsulate(r.bounds);
        go.transform.localScale=new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);
        go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        go.transform.localPosition=center-Vector3.up*size.y*.5f;
    }

    static Dictionary<string,string> ReadBindings(string obj)
    {
        var result=new Dictionary<string,string>(); string key="";
        foreach(var line in File.ReadLines(Path.ChangeExtension(obj,".mtl")))
        {
            if(line.StartsWith("newmtl ")) { key=line.Substring(7).Trim(); result[key]=null; }
            if(line.StartsWith("map_Kd ") && key.Length>0)
            {
                string absolute=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(obj),line.Substring(7).Trim())).Replace('\\','/');
                result[key]=absolute.Substring(absolute.IndexOf("Assets/",StringComparison.Ordinal));
            }
        }
        return result;
    }

    static Material MaterialFor(string name,Dictionary<string,string> bindings,string source)
    {
        name=name.Replace(" (Instance)","");
        string texture=null;
        if(!bindings.TryGetValue(name,out texture))
        {
            var match=bindings.FirstOrDefault(k=>name.StartsWith(k.Key,StringComparison.Ordinal));
            texture=match.Value;
        }
        if(texture==null && (name.Contains("PolygonOffice") || name.Contains("Office")))
            texture=Root+"AssetPacks/PolygonOffice/Textures/T_PolygonOffice_01_A.png";
        string key=texture??name;
        if(materials.TryGetValue(key,out var cached)) return cached;
        if(texture!=null && !texture.StartsWith("Assets/models/RaccoonGame/",StringComparison.Ordinal)) throw new Exception("Outside texture: "+texture);
        var mat=new Material(shader){name=Path.GetFileNameWithoutExtension(texture??name)};
        if(texture!=null)
        {
            mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texture);
            if(mat.mainTexture==null) throw new FileNotFoundException(texture+" for "+source);
        }
        else mat.color = name.Contains("Glow")?new Color(1,.91f,.65f):new Color(.87f,.87f,.84f);
        if(mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness",.18f);
        if(mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",.18f);
        if(name.Contains("Screen") || name.Contains("Glow"))
        {
            mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor",new Color(.3f,.3f,.3f));
            if(mat.mainTexture!=null) mat.SetTexture("_EmissionMap",mat.mainTexture);
        }
        string asset=Output+"/Materials/"+mat.name+".mat";
        var old=AssetDatabase.LoadAssetAtPath<Material>(asset);
        if(old!=null) { EditorUtility.CopySerialized(mat,old); UnityEngine.Object.DestroyImmediate(mat); mat=old; }
        else AssetDatabase.CreateAsset(mat,asset);
        materials[key]=mat; return mat;
    }

    public static string ValidateSources()
    {
        var meshes=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshFilter>(true)).ToArray();
        var invalid=meshes.Where(m=>!AssetDatabase.GetAssetPath(m.sharedMesh).StartsWith("Assets/models/RaccoonGame/",StringComparison.Ordinal)).ToArray();
        if(invalid.Length>0) throw new Exception("Meshes outside RaccoonGame: "+string.Join(", ",invalid.Select(m=>m.name)));
        return "Verified "+meshes.Length+" meshes, all from RaccoonGame";
    }

    public static void Capture()
    {
        var c=Camera.main; var rt=new RenderTexture(1700,1200,24);
        c.targetTexture=rt; c.Render(); RenderTexture.active=rt;
        var image=new Texture2D(1700,1200,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1700,1200),0,0); image.Apply();
        File.WriteAllBytes("OfficePreview/Office2_Raccoon.png",image.EncodeToPNG());
        c.targetTexture=null; RenderTexture.active=null;
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
    }
}
