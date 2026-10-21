using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ExecutiveOfficeBuilder
{
    const string Output="Assets/ExecutiveOffice";
    const string Data="Assets/models/meshes/data/SM_Prop/";
    const string Raccoon="Assets/models/RaccoonGame/Content/";
    static Transform room, furniture, decor;
    static Shader shader;
    static Material walnut, gold, blue, cream, black, glow;
    static readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();

    [MenuItem("Tools/Office/Build Executive Reference Office")]
    public static void Build()
    {
        Directory.CreateDirectory(Output+"/Materials"); Directory.CreateDirectory("OfficePreview");
        AssetDatabase.Refresh(); mats.Clear();
        shader=Shader.Find(GraphicsSettings.currentRenderPipeline==null?"Standard":"Universal Render Pipeline/Lit");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        for(int i=SceneManager.sceneCount-1;i>=0;i--)
        {
            var other=SceneManager.GetSceneAt(i); if(other==scene) continue;
            if(other.isDirty)
            {
                Directory.CreateDirectory("Assets/Scenes/Backups");
                string backup=AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Backups/"+other.name+"_BeforeExecutive.unity");
                if(!EditorSceneManager.SaveScene(other,backup,true)) throw new Exception("Could not preserve "+other.name);
            }
            EditorSceneManager.CloseScene(other,true);
        }
        room=new GameObject("01 Architecture and walnut joinery").transform;
        furniture=new GameObject("02 Reference furniture").transform;
        decor=new GameObject("03 Books plants and accessories").transform;
        walnut=Solid("Walnut joinery",new Color(.43f,.205f,.075f),.35f);
        gold=Solid("Brushed brass",new Color(.69f,.41f,.12f),.55f,.55f);
        blue=Solid("Navy leather",new Color(.045f,.095f,.16f),.3f);
        cream=Solid("Ivory linen",new Color(.78f,.69f,.54f),.05f);
        black=Solid("Executive leather",new Color(.025f,.03f,.038f),.34f);
        glow=Solid("Warm diffuser",new Color(1,.69f,.27f),.12f,0,2.1f);
        var wall=Solid("Blue grey wall",new Color(.12f,.19f,.245f));
        var plaster=Solid("Warm plaster",new Color(.65f,.58f,.48f));
        var oak=Solid("Honey oak parquet",Color.white,.38f);
        oak.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Raccoon+"Airship/Textures/T_WoodFloor_A.png");
        for(int x=0;x<16;x++) for(int z=0;z<14;z++)
        {
            var tile=Box("Parquet block",new Vector3(-3.75f+x*.5f,-.075f,-3.25f+z*.5f),new Vector3(.494f,.15f,.494f),oak,room);
            tile.transform.localRotation=Quaternion.Euler(0,(x+z)%2==0?0:90,0);
        }
        Box("Back blue wall",new Vector3(0,1.95f,3.6f),new Vector3(8.2f,3.9f,.2f),wall,room);
        Box("Right wall",new Vector3(4.1f,1.95f,0),new Vector3(.2f,3.9f,7.4f),plaster,room);
        // The large left window is an opening in the wall, rather than a flat decal.
        Box("Window lower wall",new Vector3(-4.1f,.43f,0),new Vector3(.2f,.86f,7.4f),plaster,room);
        Box("Window upper wall",new Vector3(-4.1f,3.63f,0),new Vector3(.2f,.54f,7.4f),plaster,room);
        foreach(float z in new[]{-3.2f,3.2f}) Box("Window side wall",new Vector3(-4.1f,2.1f,z),new Vector3(.2f,2.55f,1f),plaster,room);
        var sky=Solid("Window blue sky",new Color(.29f,.62f,.85f),.1f,0,.65f);
        Box("Sky beyond window",new Vector3(-5.5f,2.2f,0),new Vector3(.05f,3.4f,6.1f),sky,room).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        sky.SetColor("_EmissionColor",new Color(.4f,.75f,1)*2f);
        foreach(float z in new[]{-2.7f,-.9f,.9f,2.7f}) Box("Window timber mullion",new Vector3(-3.98f,2.07f,z),new Vector3(.17f,2.55f,.085f),walnut,room);
        foreach(float y in new[]{.85f,2.55f,3.35f}) Box("Window cross rail",new Vector3(-3.98f,y,0),new Vector3(.18f,.10f,5.5f),walnut,room);
        Box("Deep window sill",new Vector3(-3.85f,.85f,0),new Vector3(.50f,.12f,5.8f),walnut,room);
        // Ceiling, cornice and warm perimeter lighting.
        Box("Ceiling",new Vector3(0,4.04f,0),new Vector3(8.2f,.18f,7.4f),plaster,room);
        foreach(float x in new[]{-3.85f,3.85f})
        {
            Box("Ceiling walnut cornice",new Vector3(x,3.78f,0),new Vector3(.18f,.22f,7.2f),walnut,room);
            Box("Ceiling cove glow",new Vector3(x*.96f,3.88f,0),new Vector3(.055f,.035f,7.0f),glow,room);
        }
        Box("Back cornice",new Vector3(0,3.78f,3.42f),new Vector3(8,.22f,.18f),walnut,room);
        Box("Back cove glow",new Vector3(0,3.88f,3.28f),new Vector3(7.7f,.035f,.055f),glow,room);
        Box("Back skirting",new Vector3(0,.13f,3.43f),new Vector3(8,.26f,.12f),walnut,room);
        Box("Right skirting",new Vector3(3.96f,.13f,0),new Vector3(.12f,.26f,7),walnut,room);
        // Built-in shelving around the central artwork.
        foreach(float x in new[]{-2.85f,2.85f})
        {
            Box("Cabinet base",new Vector3(x,.42f,3.14f),new Vector3(1.85f,.84f,.62f),walnut,room);
            foreach(float dx in new[]{-.44f,.44f})
            {
                Box("Raised cabinet door",new Vector3(x+dx,.43f,2.815f),new Vector3(.82f,.68f,.045f),walnut,room);
                Sphere("Brass cabinet knob",new Vector3(x+dx*.25f,.48f,2.77f),.032f,gold,room);
            }
            var shelf=Model(Data+"SM_Prop_Bookshelf_01.obj",new Vector3(x,.84f,3.15f),180,1,walnut);
            shelf.transform.localScale=new Vector3(1.60f,1.30f,1.5f);
            foreach(float y in new[]{1.0f,1.68f,2.35f,3.0f})
                Model(Data+"SM_Prop_Book_Group_02.obj",new Vector3(x-.35f,y,3.03f),180,.8f);
        }
        Box("Back credenza",new Vector3(0,.45f,3.13f),new Vector3(3.8f,.9f,.60f),walnut,room);
        Landscape();
        Model(Data+"SM_Prop_Globe_01.obj",new Vector3(2.98f,1.68f,2.98f),0,.68f);
        Model(Data+"SM_Prop_Trophy_01.obj",new Vector3(3.25f,2.35f,3.01f),0,.7f,gold);
        Model(Data+"SM_Prop_Clock_01.obj",new Vector3(-2.45f,2.35f,2.97f),180,.8f);
        Model(Data+"SM_Prop_PotPlant_01.obj",new Vector3(-2.55f,3.08f,3.05f),0,.8f);
        Model(Data+"SM_Prop_PotPlant_01.obj",new Vector3(3.15f,3.08f,3.05f),0,.8f);
        // Desk and guest seating match the composition of the supplied image.
        var desk=Model(Data+"SM_Prop_Office_Desk_01.obj",new Vector3(0,0,.48f),0,1,walnut);
        desk.transform.localScale=new Vector3(1.72f,1.08f,1.62f);
        Box("Thick executive desktop",new Vector3(0,1.055f,.48f),new Vector3(3.95f,.14f,1.65f),walnut,furniture);
        Box("Desk front panel",new Vector3(0,.51f,-.23f),new Vector3(3.64f,.87f,.10f),walnut,furniture);
        foreach(float x in new[]{-1.65f,0,1.65f}) Box("Desk panel stile",new Vector3(x,.51f,-.295f),new Vector3(.065f,.84f,.035f),gold,furniture);
        Box("Leather desk blotter",new Vector3(0,1.13f,.55f),new Vector3(1.6f,.018f,.7f),black,decor);
        Model(Data+"SM_Prop_OfficeChair_01.obj",new Vector3(.05f,0,1.97f),180,1.4f,black);
        foreach(float x in new[]{-1.13f,1.13f})
        {
            var chair=Model(Raccoon+"Airship/Props/SM_Prop_ArmChair_01.obj",new Vector3(x,0,-1.55f),0,1.12f,blue);
        }
        // Layered rug with a navy border and fine golden inlay.
        Box("Rug navy border",new Vector3(0,.015f,-.65f),new Vector3(5.5f,.025f,3.95f),blue,furniture);
        Box("Rug gold border",new Vector3(0,.03f,-.65f),new Vector3(5.15f,.012f,3.6f),gold,furniture);
        Box("Rug ivory field",new Vector3(0,.04f,-.65f),new Vector3(4.99f,.012f,3.44f),cream,furniture);
        Model(Raccoon+"Airship/Props/SM_Prop_Laptop_01.obj",new Vector3(.95f,1.13f,.45f),180,1.15f);
        Model(Data+"SM_Prop_Study_Lamp_01.obj",new Vector3(-1.35f,1.13f,.67f),140,1.2f,gold);
        Model(Data+"SM_Prop_Trophy_01.obj",new Vector3(-.8f,1.13f,.45f),0,.45f,gold);
        Model(Data+"SM_Prop_Book_Group_02.obj",new Vector3(1.53f,1.13f,.48f),90,.8f);
        Model(Data+"SM_Prop_Book_Group_03.obj",new Vector3(-.32f,1.13f,.55f),90,.5f);
        Model(Raccoon+"Airship/Props/SM_Prop_Curtains_05.obj",new Vector3(-3.73f,.3f,0),0,.77f,cream);
        Model(Data+"SM_Prop_Plant_Palm_01.obj",new Vector3(-3.15f,0,-1.8f),0,1.05f);
        Model(Data+"SM_Prop_PotPlant_01.obj",new Vector3(-3.73f,.92f,1.2f),0,1f);
        Model(Raccoon+"AssetPacks/PolygonOffice/Meshes/Props/Misc/SM_Prop_CoatRack_01.obj",new Vector3(-2.15f,0,2.68f),0,.9f);
        Model(Data+"SM_Prop_PotPlant_01.obj",new Vector3(1.40f,.93f,3.0f),0,.85f);
        // Window-side storage and shaded table lamp.
        Box("Window sideboard",new Vector3(-3.45f,.43f,.15f),new Vector3(.72f,.86f,2.55f),walnut,furniture);
        Model(Data+"SM_Prop_Lamp_01.obj",new Vector3(-3.4f,.87f,-.5f),0,.9f);
        Model(Data+"SM_Prop_Book_Group_02.obj",new Vector3(-3.4f,.87f,.35f),0,.7f);
        // Warm pendant assembled around an existing hanging light mesh.
        Model(Data+"SM_Prop_Light_Pendant_01.obj",new Vector3(0,3.03f,.35f),0,.8f,gold);
        Cylinder("Pendant diffuser",new Vector3(0,3.27f,.35f),new Vector3(.85f,.18f,.85f),glow,decor);
        Cylinder("Pendant upper rim",new Vector3(0,3.47f,.35f),new Vector3(.91f,.025f,.91f),gold,decor);
        Cylinder("Pendant lower rim",new Vector3(0,3.07f,.35f),new Vector3(.91f,.025f,.91f),gold,decor);
        foreach(float x in new[]{-.38f,.38f}) Box("Pendant brass cage",new Vector3(x,3.27f,.35f),new Vector3(.025f,.42f,.025f),gold,decor);
        Point("Pendant warm light",new Vector3(0,3.0f,.35f),2.5f,7,new Color(1,.69f,.38f));
        Point("Desk lamp glow",new Vector3(-1.35f,1.67f,.55f),.7f,2,new Color(1,.73f,.38f));
        Point("Window-side lamp",new Vector3(-3.3f,1.50f,-.5f),.8f,3,new Color(1,.73f,.41f));
        Point("Soft front fill",new Vector3(1,2.7f,-2.9f),2.2f,10,new Color(1,.89f,.75f));
        Point("Window soft daylight",new Vector3(-3.5f,2.7f,0),3f,10,new Color(1,.89f,.71f));
        var sun=new GameObject("Warm afternoon window light").AddComponent<Light>();
        sun.type=LightType.Directional; sun.intensity=1.35f; sun.color=new Color(1,.85f,.65f);
        sun.transform.rotation=Quaternion.Euler(35,65,0); sun.shadows=LightShadows.Soft; sun.shadowNormalBias=.15f;
        RenderSettings.skybox=null; RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.78f,.79f,.80f);
        RenderSettings.ambientEquatorColor=new Color(.65f,.60f,.52f);
        RenderSettings.ambientGroundColor=new Color(.38f,.31f,.23f);
        var camera=new GameObject("Reference composition camera").AddComponent<Camera>();
        camera.tag="MainCamera"; camera.transform.position=new Vector3(3.25f,2.25f,-4.75f);
        camera.transform.LookAt(new Vector3(-.25f,1.15f,.85f)); camera.fieldOfView=57;
        camera.nearClipPlane=.05f; camera.farClipPlane=100; camera.backgroundColor=new Color(.16f,.2f,.25f);
        camera.gameObject.AddComponent<AudioListener>(); camera.allowHDR=true;
        QualitySettings.pixelLightCount=8; QualitySettings.antiAliasing=4;
        FixShelfDecor();
        if(File.Exists("Assets/photos/artforboss.png")) ApplyBossArtwork();
        ExpandRoomAndDoor();
        DressBossDesk();
        ExecutiveWindowGlass.Apply();
        OfficeColliderSetup.Apply();
        AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene,"Assets/Scenes/ExecutiveOffice.unity");
        var scenes=EditorBuildSettings.scenes.ToList();
        if(!scenes.Any(s=>s.path==scene.path)) { scenes.Add(new EditorBuildSettingsScene(scene.path,true)); EditorBuildSettings.scenes=scenes.ToArray(); }
        SceneView.lastActiveSceneView?.LookAt(camera.transform.position+camera.transform.forward*5,camera.transform.rotation,5,false,true);
        OfficeThirdPersonSetup.Apply();
        if(File.Exists("Assets/character/Animations/TripoLocomotion.controller")) TripoHumanoidAnimationSetup.Apply();
        ExecutiveOfficeEnclosure.Apply();
        if(File.Exists(SkufPlayerSetup.PrefabPath)) SkufPlayerSetup.Apply();
        OfficePropPhysicsSetup.Apply();
        Debug.Log("EXECUTIVE_OFFICE_SAVED");
    }

    [MenuItem("Tools/Office/Apply Boss Artwork")]
    public static void ApplyBossArtwork()
    {
        const string source="Assets/photos/artforboss.png";
        var importer=AssetImporter.GetAtPath(source) as TextureImporter;
        if(importer!=null && importer.npotScale!=TextureImporterNPOTScale.None)
        {
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(source);
        if(texture==null) throw new FileNotFoundException(source);
        var scene=SceneManager.GetActiveScene();
        var parent=scene.GetRootGameObjects().First(g=>g.name=="03 Books plants and accessories").transform;
        // Replace just the picture contents; keep the existing cabinetry and lighting.
        foreach(var t in parent.Cast<Transform>().Where(t=>t.name=="Landscape sky" || t.name=="Mountain range" || t.name=="Valley" || t.name=="Alpine lake" || t.name.StartsWith("Pine silhouette") || t.name=="Boss landscape artwork").ToArray())
            UnityEngine.Object.DestroyImmediate(t.gameObject);
        const float width=3.08f;
        float height=width*texture.height/texture.width;
        var outer=parent.Find("Landscape gilded frame");
        var inner=parent.Find("Landscape inner wood frame");
        outer.localScale=new Vector3(width+.28f,height+.28f,.12f);
        inner.localScale=new Vector3(width+.14f,height+.14f,.035f);
        var material=new Material(Shader.Find(GraphicsSettings.currentRenderPipeline==null?"Unlit/Texture":"Universal Render Pipeline/Unlit")){name="Boss landscape artforboss",mainTexture=texture,color=Color.white};
        const string materialPath=Output+"/Materials/Boss landscape artforboss.mat";
        var existing=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(existing!=null) { EditorUtility.CopySerialized(material,existing); UnityEngine.Object.DestroyImmediate(material); material=existing; }
        else AssetDatabase.CreateAsset(material,materialPath);
        var picture=GameObject.CreatePrimitive(PrimitiveType.Quad); picture.name="Boss landscape artwork";
        picture.transform.SetParent(parent,false); picture.transform.localPosition=new Vector3(outer.localPosition.x,outer.localPosition.y,3.30f);
        picture.transform.localScale=new Vector3(width,height,1);
        UnityEngine.Object.DestroyImmediate(picture.GetComponent<Collider>());
        var renderer=picture.GetComponent<MeshRenderer>(); renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
        float top=outer.localPosition.y+(height+.28f)*.5f;
        foreach(var name in new[]{"Picture light brass bar","Picture light diffuser"})
        {
            var light=parent.Find(name); var position=light.localPosition;
            position.y=top+(name.EndsWith("bar")?.09f:.06f); light.localPosition=position;
        }
        var point=GameObject.Find("Picture warm light");
        if(point!=null) point.transform.position=new Vector3(outer.position.x,top-.15f,2.75f);
        AssetDatabase.SaveAssets(); if(!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/Office/Dress Boss Desktop")]
    public static void DressBossDesk()
    {
        var scene=SceneManager.GetActiveScene();
        decor=scene.GetRootGameObjects().First(g=>g.name=="03 Books plants and accessories").transform;
        shader=Shader.Find(GraphicsSettings.currentRenderPipeline==null?"Standard":"Universal Render Pipeline/Lit");
        walnut=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/Walnut joinery.mat");
        gold=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/Brushed brass.mat");
        black=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/Executive leather.mat");
        blue=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/Navy leather.mat");
        var old=decor.Find("Boss desktop accessories");
        if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var group=new GameObject("Boss desktop accessories").transform; group.SetParent(decor,false);
        var top=GameObject.Find("Thick executive desktop").GetComponent<Renderer>().bounds;
        float surface=top.max.y;
        // Remove the original rows of standing books, retaining shelf decorations.
        foreach(var item in decor.Cast<Transform>().Where(t=>new[]{"SM_Prop_Book_Group_02","SM_Prop_Book_Group_03","SM_Prop_Trophy_01"}.Contains(t.name)
            && Mathf.Abs(t.position.x-top.center.x)<top.extents.x && Mathf.Abs(t.position.y-surface)<.15f && t.position.z>top.min.z && t.position.z<top.max.z).ToArray())
            UnityEngine.Object.DestroyImmediate(item.gameObject);
        var mat=GameObject.Find("Leather desk blotter").transform;
        mat.position=new Vector3(top.center.x,surface+.011f,.60f); mat.localScale=new Vector3(2.03f,.018f,.91f);
        var laptop=decor.Cast<Transform>().First(t=>t.name=="SM_Prop_Laptop_01");
        SeatOnDesk(laptop,top.center.x,.65f,surface+.021f);
        var lamp=decor.Cast<Transform>().First(t=>t.name=="SM_Prop_Study_Lamp_01");
        SeatOnDesk(lamp,-1.60f,.98f,surface+.003f);
        var warmLight=GameObject.Find("Desk lamp glow");
        if(warmLight!=null) warmLight.transform.position=new Vector3(-1.60f,surface+.55f,.92f);
        DeskModel(Raccoon+"Airship/Props/SM_Prop_Phone_Rotary_01.obj",-1.38f,.35f,surface+.003f,180,.85f,group,black);
        DeskModel(Data+"SM_Prop_Trophy_01.obj",-.97f,1.06f,surface+.003f,0,.36f,group,gold);
        DeskModel(Data+"SM_Prop_BookOpen_01.obj",-.64f,.48f,surface+.022f,165,.66f,group);
        var mobile=DeskModel(Data+"SM_Prop_Computer_Smartphone_01.obj",.68f,.27f,surface+.022f,168,.80f,group);
        mobile.transform.localRotation=Quaternion.Euler(-90,168,0);
        SeatOnDesk(mobile.transform,.68f,.27f,surface+.022f);
        // Low two-tier document organizer, built at a real desktop height.
        foreach(float y in new[]{surface+.02f,surface+.115f})
        {
            Box("Document tray leather base",new Vector3(1.43f,y,.87f),new Vector3(.55f,.025f,.45f),blue,group);
            foreach(float x in new[]{1.165f,1.695f})
                Box("Document tray brass side",new Vector3(x,y+.028f,.87f),new Vector3(.018f,.06f,.45f),gold,group);
            Box("Document tray rear rim",new Vector3(1.43f,y+.028f,1.085f),new Vector3(.55f,.06f,.018f),gold,group);
            DeskModel(Data+"SM_Prop_Computer_Printer_Paper_01.obj",1.43f,.85f,y+.014f,0,.85f,group);
        }
        foreach(float x in new[]{1.18f,1.68f})
            Box("Tray support",new Vector3(x,surface+.07f,1.06f),new Vector3(.018f,.14f,.018f),gold,group);
        // A closed journal and pencil cup balance the right-hand side.
        Box("Navy journal cover",new Vector3(1.40f,surface+.025f,.24f),new Vector3(.43f,.05f,.30f),blue,group);
        Box("Journal pages",new Vector3(1.40f,surface+.026f,.237f),new Vector3(.405f,.030f,.28f),Solid("Journal ivory pages",new Color(.88f,.82f,.65f)),group);
        Cylinder("Brass pencil cup",new Vector3(.97f,surface+.065f,1.02f),new Vector3(.12f,.065f,.12f),gold,group);
        for(int i=0;i<3;i++)
        {
            var pencil=DeskModel("Assets/models/meshes/data/SM_Item/SM_Item_Pencil_01.obj",.94f+i*.025f,1.02f+(i%2)*.018f,surface+.075f,20+i*33,1,group);
            pencil.transform.Rotate(0,0,(i-1)*7);
        }
        Cylinder("Coffee coaster",new Vector3(.72f,surface+.009f,.93f),new Vector3(.22f,.009f,.22f),black,group);
        DeskModel(Raccoon+"Airship/Props/SM_Item_Mug_01.obj",.72f,.93f,surface+.020f,30,.85f,group);
        // Small blank nameplate at the visitor edge, clear of the laptop keyboard.
        Box("Nameplate walnut plinth",new Vector3(0,surface+.022f,-.16f),new Vector3(.63f,.044f,.14f),walnut,group);
        var plate=Box("Nameplate brass face",new Vector3(0,surface+.072f,-.18f),new Vector3(.56f,.095f,.018f),gold,group);
        plate.transform.localRotation=Quaternion.Euler(-12,0,0);
        AssetDatabase.SaveAssets(); if(!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
    }

    static GameObject DeskModel(string path,float x,float z,float surface,float yaw,float scale,Transform parent,Material replacement=null)
    {
        var go=Model(path,new Vector3(x,surface,z),yaw,scale,replacement);
        go.transform.SetParent(parent,true); SeatOnDesk(go.transform,x,z,surface); return go;
    }

    static void SeatOnDesk(Transform item,float x,float z,float surface)
    {
        var bounds=ObjectBounds(item);
        item.position+=new Vector3(x-bounds.center.x,surface-bounds.min.y,z-bounds.center.z);
    }

    [MenuItem("Tools/Office/Fix Executive Shelf Decorations")]
    public static void FixShelfDecor()
    {
        var scene=SceneManager.GetActiveScene();
        var decoration=scene.GetRootGameObjects().First(g=>g.name=="03 Books plants and accessories").transform;
        var shelves=decoration.Cast<Transform>().Where(t=>t.name=="SM_Prop_Bookshelf_01").OrderBy(t=>t.position.x).ToArray();
        foreach(var shelf in shelves)
        {
            var items=decoration.Cast<Transform>().Where(t=>t!=shelf && Mathf.Abs(t.position.x-shelf.position.x)<.9f && t.position.z>2.7f).ToArray();
            var books=items.Where(t=>t.name=="SM_Prop_Book_Group_02").OrderBy(t=>t.position.y).ToArray();
            for(int i=0;i<books.Length;i++) SeatOnShelf(books[i],shelf,Mathf.Min(i,3),-.35f);
            foreach(var item in items.Where(t=>t.name=="SM_Prop_Globe_01")) SeatOnShelf(item,shelf,2,.40f);
            foreach(var item in items.Where(t=>t.name=="SM_Prop_Trophy_01")) SeatOnShelf(item,shelf,3,.40f);
            foreach(var item in items.Where(t=>t.name=="SM_Prop_Clock_01")) SeatOnShelf(item,shelf,3,.40f);
            foreach(var item in items.Where(t=>t.name=="SM_Prop_PotPlant_01"))
            {
                item.localScale=Vector3.one*.50f;
                SeatOnShelf(item,shelf,4,.10f);
            }
        }
        if(!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
    }

    static void SeatOnShelf(Transform item,Transform shelf,int level,float horizontalOffset)
    {
        // Upper faces measured from SM_Prop_Bookshelf_01.obj; both the model and its
        // wrapper have a bottom-centred pivot. Apply the actual cabinet scale.
        float[] upperFaces={.041f,.4739f,.9019f,1.3299f,1.7579f,2.1851f};
        float top=shelf.TransformPoint(new Vector3(0,upperFaces[level],0)).y;
        var support=ObjectBounds(shelf);
        var bounds=ObjectBounds(item);
        item.position+=new Vector3(shelf.position.x+horizontalOffset-bounds.center.x,
            top+.002f-bounds.min.y,support.min.z+.055f-bounds.min.z);
    }

    static Bounds ObjectBounds(Transform root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>();
        var bounds=renderers[0].bounds;
        foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    [MenuItem("Tools/Office/Enlarge Executive Office and Add Door")]
    public static void ExpandRoomAndDoor()
    {
        var scene=SceneManager.GetActiveScene();
        room=scene.GetRootGameObjects().First(g=>g.name=="01 Architecture and walnut joinery").transform;
        decor=scene.GetRootGameObjects().First(g=>g.name=="03 Books plants and accessories").transform;
        shader=Shader.Find(GraphicsSettings.currentRenderPipeline==null?"Standard":"Universal Render Pipeline/Lit");
        walnut=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/Walnut joinery.mat");
        gold=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/Brushed brass.mat");
        glow=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/Warm diffuser.mat");
        var plaster=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/Warm plaster.mat");
        var oak=AssetDatabase.LoadAssetAtPath<Material>(Output+"/Materials/Honey oak parquet.mat");
        string[] replace={"Parquet block","Right wall","Right skirting","Ceiling","Ceiling walnut cornice","Ceiling cove glow","Back cornice","Back cove glow","Right wall front","Right wall rear","Right door lintel","Right door casing","Door header","Door threshold","Door brass handle","Door brass plate","Door timber leaf","Door raised panel"};
        foreach(var t in room.Cast<Transform>().Where(t=>replace.Contains(t.name) || t.name=="Continuous parquet collision").ToArray()) UnityEngine.Object.DestroyImmediate(t.gameObject);
        foreach(var t in decor.Cast<Transform>().Where(t=>t.name=="Right entry door").ToArray()) UnityEngine.Object.DestroyImmediate(t.gameObject);
        // Expand the shell from 8 x 7 m to 10 x 9 m; furniture keeps its scale.
        for(int x=0;x<20;x++) for(int z=0;z<18;z++)
        {
            var tile=Box("Parquet block",new Vector3(-3.75f+x*.5f,-.075f,-5.25f+z*.5f),new Vector3(.494f,.15f,.494f),oak,room);
            tile.transform.localRotation=Quaternion.Euler(0,(x+z)%2==0?0:90,0);
        }
        Resize("Back blue wall",new Vector3(1,1.95f,3.6f),new Vector3(10.2f,3.9f,.2f));
        Resize("Back skirting",new Vector3(1,.13f,3.43f),new Vector3(10,.26f,.12f));
        Resize("Window lower wall",new Vector3(-4.1f,.43f,-1),new Vector3(.2f,.86f,9.4f));
        Resize("Window upper wall",new Vector3(-4.1f,3.63f,-1),new Vector3(.2f,.54f,9.4f));
        var frontWindowSide=room.Cast<Transform>().Where(t=>t.name=="Window side wall").OrderBy(t=>t.localPosition.z).First();
        frontWindowSide.localPosition=new Vector3(-4.1f,2.1f,-4.2f); frontWindowSide.localScale=new Vector3(.2f,2.55f,3f);
        Box("Ceiling",new Vector3(1,4.04f,-1),new Vector3(10.2f,.18f,9.4f),plaster,room);
        foreach(float x in new[]{-3.85f,5.85f})
        {
            Box("Ceiling walnut cornice",new Vector3(x,3.78f,-1),new Vector3(.18f,.22f,9.2f),walnut,room);
            Box("Ceiling cove glow",new Vector3(x+(x<0?.15f:-.15f),3.88f,-1),new Vector3(.055f,.035f,9f),glow,room);
        }
        Box("Back cornice",new Vector3(1,3.78f,3.42f),new Vector3(10,.22f,.18f),walnut,room);
        Box("Back cove glow",new Vector3(1,3.88f,3.28f),new Vector3(9.7f,.035f,.055f),glow,room);
        // Real opening in the right wall, 1.5 m wide and 2.7 m high.
        Box("Right wall front",new Vector3(6.1f,1.95f,-3.425f),new Vector3(.2f,3.9f,4.35f),plaster,room);
        Box("Right wall rear",new Vector3(6.1f,1.95f,1.875f),new Vector3(.2f,3.9f,3.45f),plaster,room);
        Box("Right door lintel",new Vector3(6.1f,3.3f,-.55f),new Vector3(.2f,1.2f,1.4f),plaster,room);
        foreach(float z in new[]{-1.30f,.20f})
            Box("Right door casing",new Vector3(5.95f,1.36f,z),new Vector3(.19f,2.72f,.12f),walnut,room);
        Box("Door header",new Vector3(5.95f,2.73f,-.55f),new Vector3(.19f,.15f,1.62f),walnut,room);
        Box("Door threshold",new Vector3(6,.018f,-.55f),new Vector3(.34f,.035f,1.4f),gold,room);
        var door=Model("Assets/models/meshes/data/SM_Bld/SM_Bld_House_Door_01.obj",new Vector3(6,0,-.55f),270,1.05f,walnut);
        door.name="Right entry door";
        Box("Door timber leaf",new Vector3(5.99f,1.28f,-.55f),new Vector3(.08f,2.52f,1.30f),walnut,room);
        foreach(float y in new[]{.48f,1.28f,2.08f}) foreach(float z in new[]{-.87f,-.23f})
            Box("Door raised panel",new Vector3(5.935f,y,z),new Vector3(.035f,.64f,.49f),walnut,room);
        var collider=door.AddComponent<BoxCollider>(); collider.center=new Vector3(0,1.224f,0); collider.size=new Vector3(1.28f,2.448f,.367f);
        Sphere("Door brass handle",new Vector3(5.77f,1.12f,-.08f),.055f,gold,room);
        Box("Door brass plate",new Vector3(5.795f,1.8f,-.55f),new Vector3(.018f,.14f,.34f),gold,room);
        Box("Right skirting",new Vector3(5.96f,.13f,-3.425f),new Vector3(.12f,.26f,4.35f),walnut,room);
        Box("Right skirting",new Vector3(5.96f,.13f,1.875f),new Vector3(.12f,.26f,3.45f),walnut,room);
        var camera=Camera.main;
        camera.transform.position=new Vector3(2.3f,2.65f,-6.5f);
        camera.transform.LookAt(new Vector3(1.1f,1.48f,.9f)); camera.fieldOfView=68;
        AssetDatabase.SaveAssets();
        if(!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
        SceneView.lastActiveSceneView?.LookAt(camera.transform.position+camera.transform.forward*6,camera.transform.rotation,6,false,true);
    }

    static void Resize(string name,Vector3 pos,Vector3 size)
    {
        var t=room.Cast<Transform>().First(o=>o.name==name); t.localPosition=pos; t.localScale=size;
    }

    static Material Solid(string name,Color color,float smooth=.2f,float metal=0,float emission=0)
    {
        var m=new Material(shader){name=name,color=color};
        if(m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness",smooth);
        if(m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",smooth);
        if(m.HasProperty("_Metallic")) m.SetFloat("_Metallic",metal);
        if(emission>0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",color*emission); }
        string path=Output+"/Materials/"+name+".mat"; var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old!=null) { EditorUtility.CopySerialized(m,old); UnityEngine.Object.DestroyImmediate(m); return old; }
        AssetDatabase.CreateAsset(m,path); return m;
    }

    static GameObject Box(string name,Vector3 pos,Vector3 size,Material mat,Transform parent)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false);
        g.transform.localPosition=pos; g.transform.localScale=size; g.GetComponent<Renderer>().sharedMaterial=mat; return g;
    }
    static void Sphere(string name,Vector3 pos,float radius,Material mat,Transform parent)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name=name; g.transform.SetParent(parent,false);
        g.transform.localPosition=pos; g.transform.localScale=Vector3.one*radius*2; g.GetComponent<Renderer>().sharedMaterial=mat;
    }
    static void Cylinder(string name,Vector3 pos,Vector3 size,Material mat,Transform parent)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name=name; g.transform.SetParent(parent,false);
        g.transform.localPosition=pos; g.transform.localScale=size; g.GetComponent<Renderer>().sharedMaterial=mat;
    }
    static void Point(string name,Vector3 pos,float intensity,float range,Color color)
    {
        var l=new GameObject(name).AddComponent<Light>(); l.transform.position=pos; l.type=LightType.Point;
        l.intensity=intensity; l.range=range; l.color=color;
    }

    static GameObject Model(string path,Vector3 pos,float yaw=0,float scale=1,Material replacement=null)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(asset==null) throw new FileNotFoundException(path);
        var root=new GameObject(Path.GetFileNameWithoutExtension(path)); var g=(GameObject)PrefabUtility.InstantiatePrefab(asset);
        g.transform.SetParent(root.transform,false); var rs=g.GetComponentsInChildren<MeshRenderer>();
        var bindings=new Dictionary<string,string>(); string key="";
        foreach(var l in File.ReadLines(Path.ChangeExtension(path,".mtl")))
        {
            if(l.StartsWith("newmtl ")) { key=l.Substring(7).Trim(); bindings[key]=null; }
            if(l.StartsWith("map_Kd "))
            {
                var p=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path),l.Substring(7).Trim())).Replace('\\','/');
                bindings[key]=p.Substring(p.IndexOf("Assets/",StringComparison.Ordinal));
            }
        }
        foreach(var r in rs) r.sharedMaterials=r.sharedMaterials.Select(m=>replacement??Atlas(m==null?bindings.Keys.First():m.name,bindings)).ToArray();
        var b=rs[0].bounds; foreach(var r in rs) b.Encapsulate(r.bounds);
        g.transform.localPosition=-new Vector3(b.center.x,b.min.y,b.center.z);
        root.transform.SetParent(decor,false); root.transform.localPosition=pos;
        root.transform.localRotation=Quaternion.Euler(0,yaw,0); root.transform.localScale=Vector3.one*scale; return root;
    }
    public static GameObject PlaceDecoration(string path,Transform parent,Vector3 position,float yaw=0)
    {
        shader=Shader.Find(GraphicsSettings.currentRenderPipeline==null?"Standard":"Universal Render Pipeline/Lit");
        var previous=decor; decor=parent;
        try { return Model(path,position,yaw); }
        finally { decor=previous; }
    }
    static Material Atlas(string name,Dictionary<string,string> bindings)
    {
        name=name.Replace(" (Instance)",""); bindings.TryGetValue(name,out string texture);
        if(texture==null && name.Contains("Office")) texture=Raccoon+"AssetPacks/PolygonOffice/Textures/T_PolygonOffice_01_A.png";
        string key=texture??name; if(mats.TryGetValue(key,out var m)) return m;
        m=Solid("Atlas_"+Path.GetFileNameWithoutExtension(key),Color.white);
        if(texture!=null) { m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texture); if(m.mainTexture==null) throw new FileNotFoundException(texture); }
        mats[key]=m; return m;
    }

    static void Landscape()
    {
        Box("Landscape gilded frame",new Vector3(0,2.32f,3.42f),new Vector3(3.36f,1.84f,.12f),gold,decor);
        Box("Landscape inner wood frame",new Vector3(0,2.32f,3.34f),new Vector3(3.22f,1.70f,.035f),walnut,decor);
        var sky=Solid("Painting sky",new Color(.30f,.58f,.73f),0,0,.22f);
        Box("Landscape sky",new Vector3(0,2.32f,3.31f),new Vector3(3.08f,1.56f,.018f),sky,decor);
        var far=Solid("Painting distant peaks",new Color(.35f,.43f,.58f));
        var near=Solid("Painting forest",new Color(.12f,.29f,.20f));
        var lake=Solid("Painting lake",new Color(.25f,.56f,.57f));
        Poly("Mountain range",new[]{new Vector2(-1.53f,1.55f),new Vector2(-1.53f,2.08f),new Vector2(-1.1f,2.7f),new Vector2(-.65f,2.19f),new Vector2(-.1f,2.85f),new Vector2(.4f,2.23f),new Vector2(.9f,2.75f),new Vector2(1.53f,2.1f),new Vector2(1.53f,1.55f)},3.28f,far);
        Poly("Valley",new[]{new Vector2(-1.53f,1.55f),new Vector2(-1.53f,2.05f),new Vector2(-.7f,1.88f),new Vector2(0,1.77f),new Vector2(.8f,2.01f),new Vector2(1.53f,2.12f),new Vector2(1.53f,1.55f)},3.26f,near);
        Poly("Alpine lake",new[]{new Vector2(-.8f,1.55f),new Vector2(-.2f,1.78f),new Vector2(.25f,1.82f),new Vector2(.9f,1.55f)},3.24f,lake);
        foreach(var mat in new[]{sky,far,near,lake})
        {
            var color=mat.color;
            mat.shader=Shader.Find(GraphicsSettings.currentRenderPipeline==null?"Unlit/Color":"Universal Render Pipeline/Unlit");
            mat.color=color;
            EditorUtility.SetDirty(mat);
        }
        for(int i=0;i<7;i++)
        {
            float x=i<4?-1.4f+i*.22f:.85f+(i-4)*.22f;
            Poly("Pine silhouette "+i,new[]{new Vector2(x-.13f,1.67f),new Vector2(x,2.4f-(i%3)*.12f),new Vector2(x+.13f,1.67f)},3.22f,near);
        }
        Box("Picture light brass bar",new Vector3(0,3.37f,3.1f),new Vector3(.9f,.055f,.08f),gold,decor);
        Box("Picture light diffuser",new Vector3(0,3.34f,3.09f),new Vector3(.8f,.025f,.06f),glow,decor);
        Point("Picture warm light",new Vector3(0,3.1f,2.75f),.7f,2,new Color(1,.78f,.48f));
    }
    static void Poly(string name,Vector2[] points,float z,Material material)
    {
        var go=new GameObject(name); go.transform.SetParent(decor,false);
        var mesh=new Mesh(){name=name}; var vertices=new List<Vector3>();
        var triangles=new List<int>();
        // Fan is sufficient for the intentionally simple landscape silhouettes.
        vertices.AddRange(points.Select(p=>new Vector3(p.x,p.y,z)));
        for(int i=1;i<points.Length-1;i++) { triangles.Add(0); triangles.Add(i); triangles.Add(i+1); triangles.Add(0); triangles.Add(i+1); triangles.Add(i); }
        mesh.vertices=vertices.ToArray(); mesh.triangles=triangles.ToArray(); mesh.normals=Enumerable.Repeat(Vector3.back,vertices.Count).ToArray();
        string path=Output+"/"+name+".asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old!=null) { EditorUtility.CopySerialized(mesh,old); UnityEngine.Object.DestroyImmediate(mesh); mesh=old; }
        else AssetDatabase.CreateAsset(mesh,path);
        go.AddComponent<MeshFilter>().sharedMesh=mesh; var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
    }
    public static void Capture()
    {
        var c=Camera.main; var rt=new RenderTexture(1680,1050,24); c.targetTexture=rt; c.Render(); RenderTexture.active=rt;
        var t=new Texture2D(1680,1050,TextureFormat.RGB24,false); t.ReadPixels(new Rect(0,0,1680,1050),0,0); t.Apply();
        File.WriteAllBytes("OfficePreview/ExecutiveOffice.png",t.EncodeToPNG()); c.targetTexture=null; RenderTexture.active=null;
        UnityEngine.Object.DestroyImmediate(t); UnityEngine.Object.DestroyImmediate(rt);
    }
}
