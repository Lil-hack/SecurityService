using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class OpenPlanOfficeExtension
{
    const string Group="05 Open plan studio office";
    const string Folder="Assets/OfficeExtension/Materials/";
    const string Data="Assets/models/meshes/data/SM_Prop/";
    const string Air="Assets/models/RaccoonGame/Content/Airship/Props/";
    const string Computers="Assets/models/RaccoonGame/Content/AssetPacks/PolygonOffice/Meshes/Props/Desk/";
    static Transform root;
    static Material oak,cream,ink,teal,blue,yellow,orange,glass,white,emission;

    [MenuItem("Tools/Office/Replace Studio Chairs And Desktop Computers")]
    public static string RefreshEquipment()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        root=GameObject.Find(Group).transform;
        bool hadPropPhysics=root.GetComponentInChildren<OfficePushableProp>()!=null;
        var chairs=root.Cast<Transform>().Where(t=>t.name=="Task chair" || t.name=="Meeting chair")
            .Select(t=>new {t.name,bottom=new Vector3(Bounds(t.gameObject).center.x,Bounds(t.gameObject).min.y,Bounds(t.gameObject).center.z)}).ToArray();
        foreach(var t in root.Cast<Transform>().Where(t=>t.name=="Task chair" || t.name=="Meeting chair" || t.name.StartsWith("Workstation monitor") ||
            t.name.StartsWith("Workstation desktop") || t.name=="Workstation keyboard" || t.name=="Workstation mouse" || t.name.StartsWith("Monitor ") || t.GetComponent<TextMesh>()!=null).ToArray())
            UnityEngine.Object.DestroyImmediate(t.gameObject);
        foreach(var chair in chairs)
        {
            var target=chair.bottom;
            target.z=chair.name=="Meeting chair"?5.35f:(target.z<.45f?-.04f:.94f);
            OfficeChair(chair.name,chair.bottom,target);
        }
        foreach(var desk in root.Cast<Transform>().Where(t=>t.name.StartsWith("Workstation oak desktop")).ToArray())
        {
            var p=desk.position; int side=p.z<.45f?-1:1; float yaw=side<0?180:0;
            Model(Computers+"SM_Prop_Computer_Monitor_02.obj","Workstation desktop monitor",new Vector3(p.x-.14f,.835f,p.z-side*.14f),yaw,.78f);
            Model(Computers+"SM_Prop_Computer_Keyboard_01.obj","Workstation keyboard",new Vector3(p.x-.14f,.835f,p.z+side*.20f),yaw,.47f);
            Model(Computers+"SM_Prop_Computer_Mouse_01.obj","Workstation mouse",new Vector3(p.x+.26f,.835f,p.z+side*.21f),yaw,.10f);
            Model(Computers+"SM_Prop_Computer_Tower_02.obj","Workstation desktop system unit",new Vector3(p.x-.64f,.035f,p.z),yaw,.60f);
        }
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>()) if(r.GetComponentInParent<OfficeDoor>()==null && r.GetComponentInParent<OfficePushableProp>()==null) GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic);
        if(hadPropPhysics)StudioPropPhysicsSetup.Apply();
        AssetDatabase.SaveAssets(); var scene=SceneManager.GetActiveScene(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Wall lettering removed; 14 rolling office chairs face their tables; eight different desktop monitors with separate system units installed.";
    }

    [MenuItem("Tools/Office/Tile Open Plan Studio Floor")]
    public static string RetileStudioFloor()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        root=GameObject.Find(Group)?.transform;
        if(root==null) throw new InvalidOperationException("Open the ExecutiveOffice scene first.");
        int bossParquet=CountParquet();
        foreach(var t in root.Cast<Transform>().Where(t=>t.name=="Oak parquet plank" || t.name=="Oak staggered end plank" || t.name=="Studio floor tile").ToArray())
            UnityEngine.Object.DestroyImmediate(t.gameObject);
        var slab=root.Cast<Transform>().First(t=>t.name=="Studio floor structural slab");
        slab.GetComponent<Renderer>().sharedMaterial=TileMaterials(out var light, out var cool);
        PlaceTiles(light,cool);
        foreach(var tile in root.Cast<Transform>().Where(t=>t.name=="Studio floor tile"))
            GameObjectUtility.SetStaticEditorFlags(tile.gameObject,StaticEditorFlags.BatchingStatic);
        if(CountParquet()!=bossParquet) throw new InvalidOperationException("Boss office parquet changed.");
        AssetDatabase.SaveAssets();
        var scene=SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Studio floor tiled with "+root.Cast<Transform>().Count(t=>t.name=="Studio floor tile")+" porcelain tiles. Boss office parquet unchanged: "+bossParquet+".";
    }

    [MenuItem("Tools/Office/Add Open Plan Studio Extension")]
    public static string Apply()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        var scene=SceneManager.GetActiveScene();
        if(scene.name!="ExecutiveOffice") throw new InvalidOperationException("Open ExecutiveOffice first.");
        var old=GameObject.Find(Group); if(old!=null) UnityEngine.Object.DestroyImmediate(old);
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        root=new GameObject(Group).transform;
        oak=Mat("Honey oak",new Color(.66f,.43f,.21f));
        cream=Mat("Warm plaster",new Color(.84f,.83f,.73f));
        white=Mat("Porcelain white",new Color(.89f,.91f,.88f));
        ink=Mat("Graphite steel",new Color(.055f,.075f,.085f));
        teal=Mat("Teal fabric",new Color(.055f,.38f,.40f));
        blue=Mat("Ocean blue",new Color(.06f,.30f,.51f));
        yellow=Mat("Sunflower upholstery",new Color(.96f,.60f,.065f));
        orange=Mat("Tangerine upholstery",new Color(.88f,.25f,.035f));
        glass=Mat("Meeting room glass",new Color(.42f,.74f,.78f,.13f));
        glass.SetFloat("_Surface",1); glass.SetFloat("_Blend",0);
        glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        glass.SetFloat("_ZWrite",0); glass.SetFloat("_Smoothness",.88f);
        glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); glass.renderQueue=3000;
        emission=Mat("Linear warm diffuser",new Color(1,.91f,.72f));
        emission.EnableKeyword("_EMISSION"); emission.SetColor("_EmissionColor",new Color(1,.85f,.59f)*2);
        Architecture(); Workstations(); Meeting(); Lounge(); Refreshments(); Decorate(); OpenConnection();
        OfficeDoorSetup.Configure(false);
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
            if(r.GetComponentInParent<OfficeDoor>()==null)GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return Validate();
    }

    static void Architecture()
    {
        TileFloor();
        Box("Studio south wall",new Vector3(13.65f,2.1f,-5.6f),new Vector3(15.1f,4.2f,.2f),cream);
        Box("Studio north teal wall",new Vector3(13.65f,2.1f,7.6f),new Vector3(15.1f,4.2f,.2f),teal);
        Box("Studio west return",new Vector3(6.1f,2.1f,5.6f),new Vector3(.2f,4.2f,4),cream);
        Box("Studio ceiling",new Vector3(13.65f,4.27f,1),new Vector3(15.1f,.18f,13.2f),ink);
        Box("Window lower wall",new Vector3(21.2f,.45f,1),new Vector3(.2f,.9f,13.2f),white);
        Box("Window upper transom",new Vector3(21.2f,4.02f,1),new Vector3(.2f,.4f,13.2f),ink);
        var city=Mat("City view",Color.white,true); city.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/photos/msksity.png");
        Box("City panorama outside windows",new Vector3(21.42f,2.38f,1),new Vector3(.035f,3.1f,13),city,false);
        for(int i=0;i<=8;i++)
            Box("Black window mullion",new Vector3(21.08f,2.38f,-5.4f+i*1.6f),new Vector3(.14f,3.1f,.065f),ink,false);
        Box("Window glass continuous",new Vector3(21.02f,2.38f,1),new Vector3(.025f,3.1f,13),glass);
        Box("Continuous oak window sill",new Vector3(20.93f,.98f,1),new Vector3(.43f,.10f,13),oak);
        foreach(float z in new[]{-5.43f,7.43f}) Box("Studio black skirting",new Vector3(13.65f,.10f,z),new Vector3(15,.20f,.07f),ink,false);
        foreach(float x in new[]{9.1f,13.7f,18.4f}) foreach(float z in new[]{-2.8f,2.4f,5.7f})
        {
            Box("Suspended linear light housing",new Vector3(x,3.65f,z),new Vector3(2.9f,.12f,.27f),ink,false);
            Box("Suspended linear light diffuser",new Vector3(x,3.58f,z),new Vector3(2.8f,.025f,.24f),emission,false);
            foreach(float dx in new[]{-1.1f,1.1f}) Box("Light suspension cable",new Vector3(x+dx,3.94f,z),new Vector3(.012f,.56f,.012f),ink,false);
        }
        foreach(float x in new[]{10,17.5f})
        {
            var duct=GameObject.CreatePrimitive(PrimitiveType.Cylinder); duct.name="Exposed ventilation duct"; duct.transform.SetParent(root);
            duct.transform.SetPositionAndRotation(new Vector3(x,3.98f,1),Quaternion.Euler(90,0,0)); duct.transform.localScale=new Vector3(.28f,6.35f,.28f);
            duct.GetComponent<Renderer>().sharedMaterial=Mat("Galvanized ventilation",new Color(.42f,.46f,.46f)); UnityEngine.Object.DestroyImmediate(duct.GetComponent<Collider>());
        }
        foreach(var p in new[]{new Vector3(10,3.35f,-2),new Vector3(16.2f,3.35f,-2),new Vector3(17,3.35f,4.8f),new Vector3(9,3.35f,5.3f)})
        {
            var l=new GameObject("Studio soft ceiling light").AddComponent<Light>(); l.transform.SetParent(root); l.transform.position=p;
            l.type=LightType.Point; l.intensity=1.8f; l.range=8; l.color=new Color(1,.90f,.73f); l.shadows=LightShadows.None;
        }
    }

    static void TileFloor()
    {
        Box("Studio floor structural slab",new Vector3(13.65f,-.10f,1),new Vector3(15.1f,.20f,13.2f),TileMaterials(out var light, out var cool));
        PlaceTiles(light,cool);
    }

    static int CountParquet()
    {
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Count(t=>t.name=="Parquet block");
    }

    static Material TileMaterials(out Material light, out Material cool)
    {
        var grout=Mat("Studio tile grout",new Color(.22f,.215f,.20f));
        light=Mat("Studio porcelain tile",new Color(.90f,.89f,.86f));
        cool=Mat("Studio porcelain tile cool",new Color(.74f,.78f,.80f));
        if(grout.HasProperty("_Smoothness")) grout.SetFloat("_Smoothness",.08f);
        foreach(var material in new[]{light,cool})
        {
            if(material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",.55f);
            if(material.HasProperty("_Metallic")) material.SetFloat("_Metallic",0f);
        }
        return grout;
    }

    static void PlaceTiles(Material light, Material cool)
    {
        const float depth=13.2f, pitch=.6f, gap=.018f;
        var slab=root.Find("Studio floor structural slab");float width=slab.localScale.x;
        float minX=slab.position.x-width/2f, minZ=1f-depth/2f;
        int cols=Mathf.RoundToInt(width/pitch), rows=Mathf.RoundToInt(depth/pitch);
        float pitchX=width/cols, pitchZ=depth/rows;
        for(int row=0;row<rows;row++) for(int col=0;col<cols;col++)
            Box("Studio floor tile",new Vector3(minX+pitchX*(col+.5f),.008f,minZ+pitchZ*(row+.5f)),new Vector3(pitchX-gap,.016f,pitchZ-gap),(col+row)%2==0?light:cool,false);
    }

    static void Workstations()
    {
        int index=0;
        foreach(float x in new[]{11.0f,13.05f,16.85f,18.9f})
        {
            Box("Acoustic teal desk divider",new Vector3(x,1.36f,.45f),new Vector3(1.98f,.62f,.065f),index%2==0?teal:blue);
            foreach(int side in new[]{-1,1})
            {
                float z=.45f+side*.49f; float yaw=side<0?180:0;
                Box("Workstation oak desktop "+(++index),new Vector3(x,.79f,z),new Vector3(1.98f,.08f,.89f),oak);
                foreach(float dx in new[]{-.86f,.86f}) Box("Steel desk leg",new Vector3(x+dx,.38f,z),new Vector3(.055f,.76f,.7f),ink);
                Cabinet(x+.61f,z,.64f);
                Model(Computers+"SM_Prop_Computer_Monitor_02.obj","Workstation desktop monitor",new Vector3(x-.14f,.835f,z-side*.14f),yaw,.78f);
                Model(Computers+"SM_Prop_Computer_Keyboard_01.obj","Workstation keyboard",new Vector3(x-.14f,.835f,z+side*.20f),yaw,.47f);
                Model(Computers+"SM_Prop_Computer_Mouse_01.obj","Workstation mouse",new Vector3(x+.26f,.835f,z+side*.21f),yaw,.10f);
                Model(Computers+"SM_Prop_Computer_Tower_02.obj","Workstation desktop system unit",new Vector3(x-.64f,.035f,z),yaw,.60f);
                OfficeChair("Task chair",new Vector3(x-.12f,.02f,z+side*1.04f),new Vector3(x,.8f,z));
                Model(Air+"SM_Item_Mug_01.obj","Desk coffee mug",new Vector3(x-.72f,.835f,z+side*.16f),0,.14f);
                Model(Data+"SM_Prop_Book_Group_02.obj","Desk notebooks",new Vector3(x+.64f,.835f,z+side*.18f),90,.25f);
                if(side==1) Plant(new Vector3(x+.68f,.835f,z-.13f),.32f);
                for(int k=0;k<3;k++) Box("Divider sticky note",new Vector3(x-.66f+k*.20f,1.35f,.45f+side*.037f),new Vector3(.13f,.13f,.008f),k%2==0?yellow:orange,false);
            }
        }
        foreach(float x in new[]{12.0f,17.9f}) Rug(new Vector3(x,.024f,.38f),new Vector2(4.3f,3.8f),blue);
    }

    static void Meeting()
    {
        Rug(new Vector3(9.35f,.025f,5.35f),new Vector2(5.25f,3.8f),blue);
        // Entrance in the front right panel stays 1.2m wide.
        GlassWall(new Vector3(8.55f,1.7f,3.42f),new Vector3(4.55f,3.4f,.035f));
        GlassWall(new Vector3(12.13f,1.7f,5.48f),new Vector3(.035f,3.4f,4.1f));
        Box("Meeting frontage oak lintel",new Vector3(9.2f,3.55f,3.42f),new Vector3(5.9f,.3f,.24f),oak);
        foreach(float x in new[]{6.35f,8.6f,10.85f,12.13f}) Box("Meeting glass vertical frame",new Vector3(x,1.75f,3.42f),new Vector3(.075f,3.5f,.075f),ink);
        Box("Meeting table",new Vector3(9.3f,.79f,5.35f),new Vector3(3.4f,.10f,1.3f),oak);
        foreach(float x in new[]{8.1f,10.5f}) Box("Meeting table pedestal",new Vector3(x,.38f,5.35f),new Vector3(.16f,.76f,.85f),ink);
        foreach(float x in new[]{8.2f,9.3f,10.4f}) foreach(int side in new[]{-1,1})
            OfficeChair("Meeting chair",new Vector3(x,.03f,5.35f+side*1.15f),new Vector3(x,.8f,5.35f));
        Model(Air+"SM_Prop_Laptop_01.obj","Meeting laptop",new Vector3(9.3f,.85f,5.30f),0,.48f);
        Plant(new Vector3(10.45f,.85f,5.45f),.36f);
        Box("Meeting whiteboard frame",new Vector3(9.3f,2.15f,7.43f),new Vector3(3.6f,1.55f,.08f),ink);
        Box("Meeting whiteboard",new Vector3(9.3f,2.15f,7.37f),new Vector3(3.45f,1.40f,.025f),white,false);
        for(int i=0;i<7;i++) Box("Whiteboard planning cards",new Vector3(8.15f+(i%4)*.7f,2.15f-(i/4)*.35f,7.33f),new Vector3(.42f,.24f,.012f),i%2==0?yellow:teal,false);
        Plant(new Vector3(6.8f,.025f,6.85f),1.65f); Plant(new Vector3(11.4f,.025f,6.9f),1.5f);
    }

    static void Lounge()
    {
        Rug(new Vector3(8.55f,.026f,-3.40f),new Vector2(4.1f,3.35f),blue);
        Model(Air+"SM_Prop_Couch_08.obj","Studio lounge sofa",new Vector3(8.7f,.04f,-4.70f),0,2.9f,yellow);
        Model(Air+"SM_Prop_CoffeeTable_01.obj","Studio round coffee table",new Vector3(8.65f,.045f,-3.25f),0,1.3f);
        Model(Data+"SM_Prop_Book_Group_02.obj","Lounge books",new Vector3(8.55f,.57f,-3.25f),0,.35f);
        foreach(var item in new[]{new Vector3(7.25f,.28f,-2.7f),new Vector3(9.85f,.28f,-2.35f)})
            Box("Colourful upholstered pouf",item,new Vector3(.65f,.52f,.65f),item.x<8?teal:orange);
        Plant(new Vector3(6.75f,.02f,-4.8f),1.8f); Plant(new Vector3(10.4f,.02f,-4.85f),1.45f);
    }

    static void Refreshments()
    {
        Box("Coffee bar cabinet",new Vector3(14.45f,.48f,7.03f),new Vector3(3.3f,.96f,.75f),ink);
        Box("Coffee bar oak counter",new Vector3(14.45f,1.01f,7.03f),new Vector3(3.45f,.10f,.85f),oak);
        Model(Air+"SM_Prop_CoffeeMachine_02.obj","Studio espresso machine",new Vector3(13.65f,1.065f,7.0f),180,.65f);
        Model(Air+"SM_Prop_Coffee_Tray_01.obj","Studio cups and coffee tray",new Vector3(14.65f,1.065f,7.0f),0,.5f);
        Plant(new Vector3(15.65f,1.065f,7.0f),.46f);
        foreach(float x in new[]{13.7f,15.2f}) Model(Air+"SM_Prop_BarChair_02.obj","Coffee bar stool",new Vector3(x,.02f,5.95f),180,1.05f);
        Box("Collaboration oak table",new Vector3(17.85f,.77f,4.8f),new Vector3(3.2f,.12f,1.3f),white);
        foreach(float x in new[]{16.7f,19f}) Box("Collaboration table trestle",new Vector3(x,.37f,4.8f),new Vector3(.16f,.74f,1.0f),oak);
        Rug(new Vector3(17.85f,.025f,4.8f),new Vector2(4.5f,3.4f),teal);
        foreach(float x in new[]{16.7f,18.8f}) foreach(float z in new[]{3.65f,5.95f}) Box("Collaboration soft stool",new Vector3(x,.26f,z),new Vector3(.62f,.5f,.62f),(x+z)%2>1?yellow:blue);
        Plant(new Vector3(17.85f,.84f,4.8f),.46f);
        Model(Data+"SM_Prop_Book_Group_02.obj","Collaboration books",new Vector3(18.65f,.84f,4.8f),0,.35f);
    }

    static void Decorate()
    {
        foreach(float x in new[]{12.15f,16.65f,19.6f}) Shelf(new Vector3(x,0,-5.03f));
        Box("Printer cabinet",new Vector3(20.18f,.44f,-3.5f),new Vector3(1.15f,.88f,.78f),white);
        Model(Data+"SM_Prop_Computer_Printer_01.obj","Shared office printer",new Vector3(20.18f,.89f,-3.5f),180,.74f);
        foreach(var p in new[]{new Vector3(20.35f,0,-4.8f),new Vector3(20.3f,0,2.85f),new Vector3(20.3f,0,6.75f),new Vector3(15.25f,0,2.65f)}) Plant(p,1.65f);
        foreach(float z in new[]{-1.5f,1.0f,4.2f}) Plant(new Vector3(20.85f,1.04f,z),.44f);
    }

    static void OpenConnection()
    {
        string[] blocked={"Right entry door","Door timber leaf","Door raised panel","Door brass handle","Door brass plate"};
        foreach(var t in SceneManager.GetActiveScene().GetRootGameObjects().Where(g=>g!=root.gameObject).SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
            if(blocked.Contains(t.name)) t.gameObject.SetActive(false);
        // The original closed leaves are retained, so the connection can be restored.
        var walnut=AssetDatabase.LoadAssetAtPath<Material>("Assets/ExecutiveOffice/Materials/Walnut joinery.mat");
        Box("Open connecting oak door leaf",new Vector3(6.75f,1.29f,.13f),new Vector3(1.30f,2.56f,.075f),walnut);
        Box("Open door brass handle",new Vector3(7.25f,1.13f,.045f),new Vector3(.15f,.025f,.08f),yellow);
        Box("Level connecting threshold",new Vector3(6.16f,.003f,-.55f),new Vector3(.55f,.006f,1.34f),oak);
    }

    static Material Mat(string name,Color color,bool unlit=false)
    {
        string path=Folder+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path);}
        m.color=color; if(m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",.28f); EditorUtility.SetDirty(m); return m;
    }
    static GameObject Box(string name,Vector3 p,Vector3 size,Material mat,bool collide=true)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(root); g.transform.position=p; g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=mat; if(!collide) UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
    }
    static Bounds Bounds(GameObject g){var rs=g.GetComponentsInChildren<Renderer>(); var b=rs[0].bounds; foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    static GameObject Model(string path,string name,Vector3 bottom,float yaw,float maxSize,Material replacement=null)
    {
        var g=ExecutiveOfficeBuilder.PlaceDecoration(path,root,Vector3.zero,yaw);g.name=name;
        var b=Bounds(g);g.transform.localScale*=maxSize/Mathf.Max(b.size.x,b.size.y,b.size.z);b=Bounds(g);
        g.transform.position+=bottom-new Vector3(b.center.x,b.min.y,b.center.z);
        if(replacement!=null) foreach(var r in g.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>replacement).ToArray();
        if(maxSize>.7f) {b=Bounds(g); var c=g.AddComponent<BoxCollider>();c.center=g.transform.InverseTransformPoint(b.center);c.size=new Vector3(b.size.x/g.transform.lossyScale.x,b.size.y/g.transform.lossyScale.y,b.size.z/g.transform.lossyScale.z);}
        return g;
    }
    static void Plant(Vector3 p,float size){Model(Data+"SM_Prop_PotPlant_01.obj","Studio potted plant",p,0,size);}
    static void OfficeChair(string name,Vector3 bottom,Vector3 table)
    {
        var g=Model(Data+"SM_Prop_Manager_Chair_01.obj",name,bottom,0,1.18f);
        // Infer the backrest from the upper mesh so imported axis conventions do
        // not turn alternate chairs away from the desk.
        var b=Bounds(g); Vector3 upper=Vector3.zero; int count=0;
        foreach(var f in g.GetComponentsInChildren<MeshFilter>()) foreach(var vertex in f.sharedMesh.vertices)
        {
            var world=f.transform.TransformPoint(vertex);
            if(world.y>b.min.y+b.size.y*.72f){upper+=world;count++;}
        }
        var back=count>0?upper/count-b.center:Vector3.back; back.y=0;
        var outward=bottom-table; outward.y=0;
        if(back.sqrMagnitude>.0001f) g.transform.rotation=Quaternion.FromToRotation(back.normalized,outward.normalized)*g.transform.rotation;
        b=Bounds(g);g.transform.position+=bottom-new Vector3(b.center.x,b.min.y,b.center.z);
        // Rebuild the simplified collider after rotating the imported model.
        var collider=g.GetComponent<BoxCollider>();UnityEngine.Object.DestroyImmediate(collider);
        var mesh=g.GetComponentsInChildren<MeshFilter>();
        foreach(var f in mesh){var c=f.gameObject.AddComponent<BoxCollider>();c.center=f.sharedMesh.bounds.center;c.size=f.sharedMesh.bounds.size;}
    }
    static void Cabinet(float x,float z,float height)
    {
        Box("White rolling drawer pedestal",new Vector3(x,height/2+.03f,z),new Vector3(.45f,height,.62f),white);
        for(int i=0;i<3;i++) Box("Drawer graphite pull",new Vector3(x,.19f+i*.18f,z-.317f),new Vector3(.21f,.017f,.025f),ink,false);
    }
    static void Rug(Vector3 p,Vector2 size,Material mat)
    {
        Box("Zoned carpet border",p,new Vector3(size.x,.012f,size.y),ink,false);
        Box("Zoned carpet field",p+Vector3.up*.009f,new Vector3(size.x-.16f,.008f,size.y-.16f),mat,false);
    }
    static void GlassWall(Vector3 p,Vector3 size)
    {
        Box("Meeting clear glass partition",p,size,glass);
        Box("Meeting partition upper rail",new Vector3(p.x,3.43f,p.z),new Vector3(Mathf.Max(size.x,.07f),.065f,Mathf.Max(size.z,.07f)),ink);
        Box("Meeting partition lower rail",new Vector3(p.x,.06f,p.z),new Vector3(Mathf.Max(size.x,.07f),.09f,Mathf.Max(size.z,.07f)),ink);
    }
    static void Screen(Vector3 p,int side)
    {
        var sky=Mat("Monitor sky",new Color(.20f,.60f,.82f),true);
        Box("Monitor display blue desktop",p,new Vector3(.52f,.29f,.008f),sky,false);
        Box("Monitor application window",p+new Vector3(-.07f,.015f,side*.006f),new Vector3(.31f,.21f,.003f),white,false);
        Box("Monitor application header",p+new Vector3(-.07f,.105f,side*.008f),new Vector3(.31f,.028f,.002f),teal,false);
        for(int i=0;i<4;i++) Box("Monitor application content",p+new Vector3(-.08f,.060f-i*.035f,side*.008f),new Vector3(.22f,.012f,.002f),i%2==0?blue:teal,false);
    }
    static void Shelf(Vector3 p)
    {
        Box("Low studio storage back",p+new Vector3(0,.54f,0),new Vector3(2.0f,1.08f,.46f),white);
        for(int j=0;j<3;j++) for(int i=0;i<2;i++)
        {
            float x=p.x-.65f+j*.65f,y=.06f+i*.51f;
            Box("Storage cubby recess",new Vector3(x,y+.21f,p.z+.24f),new Vector3(.56f,.43f,.016f),ink,false);
            Model(Data+"SM_Prop_Book_Group_02.obj","Storage books",new Vector3(x,y+.015f,p.z+.30f),0,.45f);
        }
        Plant(p+new Vector3(.7f,1.09f,0),.46f);
    }
    static void Label(string text,Vector3 p,float size,Color color,float yaw=0)
    {
        var g=new GameObject("Studio wall lettering");g.transform.SetParent(root);g.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));
        var t=g.AddComponent<TextMesh>();t.text=text;t.characterSize=size;t.fontSize=64;t.color=color;t.anchor=TextAnchor.UpperLeft;
    }
    public static string Validate()
    {
        var g=GameObject.Find(Group); if(g==null)throw new Exception("Studio extension is missing.");
        int desks=g.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Workstation oak desktop"));
        int expected=g.transform.Find("Studio east wing +7.2m")!=null?12:8;
        if(desks!=expected)throw new Exception("Unexpected workstation count: "+desks);
        var hits=Physics.OverlapBox(new Vector3(6.1f,1.0f,-.55f),new Vector3(.4f,.85f,.42f));
        if(hits.Any(c=>c.enabled && c.gameObject.activeInHierarchy && c.GetComponentInParent<OfficeDoor>()==null))throw new Exception("Connecting doorway is blocked: "+string.Join(",",hits.Select(c=>c.name)));
        return "Studio office saved: eight computer workstations, glass meeting room, coffee bar, lounge, collaboration table, windows, storage and plants. Connecting doorway clear.";
    }
}
