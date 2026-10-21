using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExecutiveOfficeEnclosure
{
    const string Group="04 Enclosed office and guest lounge";
    const string Airship="Assets/models/RaccoonGame/Content/Airship/Props/";
    const string Data="Assets/models/meshes/data/SM_Prop/";
    const string Paintings="Assets/models/RaccoonGame/Content/AssetPacks/LowPolyWorld/Meshes/Interior/";
    static Transform root;
    static Material walnut,blue,cream,gold,glow;

    [MenuItem("Tools/Office/Enclose And Furnish Entire Office")]
    public static string Apply()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Stop Play mode before editing the room.");
        var scene=SceneManager.GetActiveScene();
        var old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==Group);
        if(old!=null) UnityEngine.Object.DestroyImmediate(old);
        root=new GameObject(Group).transform;
        walnut=Mat("Walnut joinery"); blue=Mat("Blue grey wall"); cream=Mat("Ivory linen"); gold=Mat("Brushed brass"); glow=Mat("Warm diffuser");
        Box("Front solid blue wall",new Vector3(1,1.975f,-5.6f),new Vector3(10.2f,3.95f,.24f),blue);
        Box("Front walnut skirting",new Vector3(1,.13f,-5.42f),new Vector3(10,.26f,.12f),walnut);
        Box("Front walnut dado rail",new Vector3(1,1.04f,-5.41f),new Vector3(10,.09f,.13f),walnut);
        for(int i=0;i<8;i++)
        {
            float x=-3.35f+i*1.24f;
            Box("Front recessed timber panel",new Vector3(x,.62f,-5.455f),new Vector3(1.10f,.68f,.06f),walnut);
            Box("Front inset panel",new Vector3(x,.62f,-5.405f),new Vector3(.95f,.52f,.025f),blue);
        }
        Box("Front cornice",new Vector3(1,3.82f,-5.40f),new Vector3(10,.22f,.20f),walnut);
        Box("Front cove light diffuser",new Vector3(1,3.90f,-5.25f),new Vector3(9.7f,.035f,.055f),glow);
        // Join the wall tops to the ceiling, including the old 5 cm reveal.
        Box("Front ceiling closure",new Vector3(1,3.94f,-5.6f),new Vector3(10.2f,.16f,.24f),walnut);
        Box("Back ceiling closure",new Vector3(1,3.94f,3.6f),new Vector3(10.2f,.16f,.24f),walnut);
        Box("Left ceiling closure",new Vector3(-4.1f,3.94f,-1),new Vector3(.24f,.16f,9.4f),walnut);
        Box("Right ceiling closure",new Vector3(6.1f,3.94f,-1),new Vector3(.24f,.16f,9.4f),walnut);
        var boundary=GameObject.Find("Office gameplay front boundary"); if(boundary!=null) boundary.SetActive(false);
        var windowCollision=new GameObject("Continuous window collision").AddComponent<BoxCollider>();
        windowCollision.transform.SetParent(root,false); windowCollision.center=new Vector3(-4.075f,2.08f,0); windowCollision.size=new Vector3(.12f,2.55f,5.5f);

        // Guest lounge is behind the visitor chairs, leaving the central aisle open.
        Box("Guest lounge rug border",new Vector3(-1.90f,.009f,-3.97f),new Vector3(3.55f,.014f,2.55f),Mat("Navy leather"));
        Box("Guest lounge rug field",new Vector3(-1.90f,.018f,-3.97f),new Vector3(3.30f,.008f,2.30f),cream);
        var sofa=Model(Airship+"SM_Prop_Couch_08.obj","Guest lounge sofa",new Vector3(-1.9f,.025f,-4.76f),0,2.7f);
        var table=Model(Airship+"SM_Prop_CoffeeTable_01.obj","Guest coffee table",new Vector3(-1.9f,.026f,-3.52f),0,1.3f);
        var tb=BoundsOf(table);
        Model(Data+"SM_Prop_Book_Group_02.obj","Guest table books",new Vector3(-2.10f,tb.max.y+.004f,-3.52f),90,.37f);
        Model(Airship+"SM_Prop_Coffee_Tray_01.obj","Guest coffee tray",new Vector3(-1.60f,tb.max.y+.005f,-3.52f),0,.34f);
        Model(Airship+"SM_Prop_FloorLamp_02.obj","Guest reading floor lamp",new Vector3(-3.55f,.015f,-4.65f),0,1.75f);
        Point("Guest reading lamp light",new Vector3(-3.50f,1.45f,-4.55f),.7f,3);
        Model(Paintings+"SM_Int_WallPainting02_1_c.obj","Lounge framed artwork left",new Vector3(-2.65f,1.83f,-5.41f),0,1.05f);
        Model(Paintings+"SM_Int_WallPainting02_2_c.obj","Lounge framed artwork right",new Vector3(-1.14f,1.83f,-5.41f),0,1.05f);

        // Walnut console on the new wall, separate from the entrance circulation.
        Box("Guest console cabinet",new Vector3(2,.43f,-5.03f),new Vector3(2.55f,.76f,.57f),walnut);
        Box("Guest console top",new Vector3(2,.85f,-5.03f),new Vector3(2.68f,.08f,.64f),walnut);
        foreach(float x in new[]{1.18f,2,2.82f})
        {
            Box("Console cabinet door inset",new Vector3(x,.44f,-4.735f),new Vector3(.73f,.60f,.024f),blue);
            Box("Console cabinet brass pull",new Vector3(x,.60f,-4.708f),new Vector3(.18f,.018f,.025f),gold);
        }
        Model(Data+"SM_Prop_Book_Group_02.obj","Console book collection",new Vector3(1.17f,.895f,-5.00f),0,.49f);
        Model(Data+"SM_Prop_PotPlant_01.obj","Console plant",new Vector3(2.87f,.895f,-5.01f),0,.50f);
        Model(Paintings+"SM_Int_WallPainting02_3_c.obj","Console framed artwork",new Vector3(2,1.55f,-5.41f),0,1.55f);
        Sconce(new Vector3(.45f,2.65f,-5.31f)); Sconce(new Vector3(3.58f,2.65f,-5.31f));

        // Refreshments occupy the empty front part of the right wall; the door stays clear.
        Box("Coffee station cabinet",new Vector3(5.55f,.44f,-3.69f),new Vector3(.66f,.82f,1.80f),walnut);
        Box("Coffee station countertop",new Vector3(5.55f,.90f,-3.69f),new Vector3(.76f,.10f,1.92f),walnut);
        foreach(float z in new[]{-4.13f,-3.25f})
        {
            Box("Coffee cabinet door inset",new Vector3(5.207f,.46f,z),new Vector3(.025f,.64f,.77f),blue);
            Box("Coffee cabinet brass handle",new Vector3(5.18f,.66f,z),new Vector3(.025f,.018f,.15f),gold);
        }
        Model(Airship+"SM_Prop_CoffeeMachine_02.obj","Guest coffee machine",new Vector3(5.52f,.956f,-3.96f),270,.50f);
        Model(Airship+"SM_Prop_Coffee_Tray_01.obj","Guest service tray",new Vector3(5.51f,.956f,-3.24f),270,.44f);
        Model(Airship+"SM_Item_Mug_01.obj","Guest mug",new Vector3(5.42f,.97f,-3.24f),0,.15f);
        Model(Data+"SM_Prop_WaterCooler_01.obj","Entry water dispenser",new Vector3(5.52f,.015f,-2.29f),270,1.30f);
        Model("Assets/models/RaccoonGame/Content/AssetPacks/LowPolyWorld/Meshes/Interior/SM_Int_WallClock01.obj","Coffee corner wall clock",new Vector3(5.93f,2.20f,-3.70f),270,.62f);
        Model(Data+"SM_Prop_PotPlant_01.obj","Front corner floor plant",new Vector3(4.35f,.015f,-4.83f),0,1.55f);
        Model(Data+"SM_Prop_PotPlant_01.obj","Rear right corner floor plant",new Vector3(5.25f,.015f,2.67f),0,1.68f);
        Model(Paintings+"SM_Int_WallPainting01_2_c.obj","Right rear wall artwork",new Vector3(5.90f,1.72f,1.35f),270,1.20f);
        Point("Guest lounge warm fill",new Vector3(.3f,2.85f,-3.9f),.8f,5);
        AddModelColliders();
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Office fully enclosed; guest lounge, console gallery, coffee corner, plants and appropriate colliders saved.";
    }

    static Material Mat(string name) { return AssetDatabase.LoadAssetAtPath<Material>("Assets/ExecutiveOffice/Materials/"+name+".mat"); }
    static GameObject Box(string name,Vector3 pos,Vector3 size,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(root,false);
        go.transform.localPosition=pos; go.transform.localScale=size; go.GetComponent<Renderer>().sharedMaterial=material; return go;
    }
    static GameObject Model(string path,string name,Vector3 pos,float yaw,float maximumSize)
    {
        var go=ExecutiveOfficeBuilder.PlaceDecoration(path,root,Vector3.zero,yaw); go.name=name;
        if(name.Contains("artwork")) go.transform.rotation=Quaternion.Euler(90,yaw,0);
        var b=BoundsOf(go); go.transform.localScale*=maximumSize/Mathf.Max(b.size.x,b.size.y,b.size.z);
        b=BoundsOf(go); go.transform.position+=pos-new Vector3(b.center.x,b.min.y,b.center.z); return go;
    }
    static Bounds BoundsOf(GameObject go) { var rs=go.GetComponentsInChildren<Renderer>(); var b=rs[0].bounds; foreach(var r in rs)b.Encapsulate(r.bounds); return b; }
    static void Point(string name,Vector3 pos,float intensity,float range)
    {
        var light=new GameObject(name).AddComponent<Light>(); light.transform.SetParent(root,false); light.transform.position=pos;
        light.type=LightType.Point; light.color=new Color(1,.76f,.48f); light.intensity=intensity; light.range=range;
    }
    static void Sconce(Vector3 pos)
    {
        Box("Wall sconce brass backplate",pos,new Vector3(.16f,.32f,.06f),gold);
        Box("Wall sconce ivory shade",pos+new Vector3(0,.06f,.08f),new Vector3(.22f,.30f,.18f),glow);
        Point("Console warm wall light",pos+new Vector3(0,.12f,.22f),.6f,3);
    }
    static void AddModelColliders()
    {
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            if(filter.GetComponent<Collider>()!=null || filter.sharedMesh==null) continue;
            var collider=filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh=filter.sharedMesh;
        }
    }
}
