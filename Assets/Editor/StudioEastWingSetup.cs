using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StudioEastWingSetup
{
    const string Group="05 Open plan studio office";
    const string Marker="Studio east wing +7.2m";
    static Transform root;
    [MenuItem("Tools/Office/Extend Studio To The Right")]
    public static string Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
        root=GameObject.Find(Group).transform;
        if(root.Find(Marker)!=null)return Validate();
        var original=root.Cast<Transform>().ToArray();
        foreach(var t in original)
        {
            if(new[]{"Studio floor structural slab","Studio ceiling","Studio south wall","Studio north teal wall","Studio black skirting"}.Contains(t.name))
            {t.position+=Vector3.right*3.6f;var s=t.localScale;s.x+=7.2f;t.localScale=s;}
            if(new[]{"Window lower wall","Window upper transom","City panorama outside windows","Black window mullion","Window glass continuous","Continuous oak window sill"}.Contains(t.name))
                t.position+=Vector3.right*7.2f;
            if(t.name=="Printer cabinet" || t.name=="Shared office printer" || (t.name=="Studio potted plant" && t.position.x>20 && (t.position.y<.05f || t.position.x>20.7f)))
                t.position+=Vector3.right*7.2f;
        }
        var light=Material("Studio porcelain tile");var cool=Material("Studio porcelain tile cool");
        for(int row=0;row<22;row++)for(int col=0;col<12;col++)
            Box("Studio floor tile",new Vector3(21.2f+(col+.5f)*.6f,.008f,-5.6f+(row+.5f)*.6f),new Vector3(.582f,.016f,.582f),(row+col)%2==0?light:cool,false);

        string[] deskParts={"Acoustic teal desk divider","Steel desk leg","White rolling drawer pedestal","Drawer graphite pull","Desk coffee mug","Desk notebooks","Divider sticky note","Task chair","Studio potted plant"};
        foreach(var t in original.Where(t=>(t.name.StartsWith("Workstation ") || deskParts.Contains(t.name)) && t.position.x>15.7f && t.position.x<20 && t.position.z>-2.4f && t.position.z<2.6f))
        {
            var copy=Clone(t,t.position+Vector3.right*6);
            if(copy.name.StartsWith("Workstation oak desktop"))copy.name="Workstation oak desktop east "+(t.position.z<.45f?"south":"north")+" "+t.position.x;
        }
        foreach(var t in original.Where(t=>(t.name=="Zoned carpet border" || t.name=="Zoned carpet field") && Mathf.Abs(t.position.x-17.9f)<.1f && t.position.z<1))
            Clone(t,t.position+Vector3.right*6);

        foreach(var t in original.Where(t=>(t.name.StartsWith("Suspended linear light") || t.name=="Light suspension cable" || t.name=="Exposed ventilation duct") && t.position.x>17 && t.position.x<20))
            Clone(t,t.position+Vector3.right*7.2f);
        foreach(float z in new[]{-2.5f,5})
        {
            var l=new GameObject("Studio east ceiling light").AddComponent<Light>();l.transform.SetParent(root);l.transform.position=new Vector3(24.8f,3.35f,z);
            l.type=LightType.Point;l.intensity=1.8f;l.range=8;l.color=new Color(1,.9f,.73f);l.shadows=LightShadows.None;
        }
        var oak=Material("Honey oak");var steel=Material("Graphite steel");
        Box("Collaboration oak table",new Vector3(23.95f,.79f,-3.65f),new Vector3(2.7f,.10f,1.1f),oak);
        foreach(float x in new[]{22.85f,25.05f})Box("Collaboration table trestle",new Vector3(x,.37f,-3.65f),new Vector3(.12f,.74f,.85f),steel);
        foreach(int side in new[]{-1,1})
        {
            var chair=original.First(t=>t.name=="Task chair" && (side<0?t.position.z<.45f:t.position.z>.45f));
            Clone(chair,new Vector3(23.95f,.02f,-3.65f+side*1.05f));
        }
        var book=original.First(t=>t.name=="Collaboration books");Clone(book,new Vector3(24.65f,.845f,-3.65f));
        var plant=original.First(t=>t.name=="Studio potted plant" && t.position.y>.8f && t.position.y<1 && t.position.x<20);
        Clone(plant,new Vector3(23.15f,.845f,-3.65f));
        Clone(original.First(t=>t.name=="Desk coffee mug"),new Vector3(23.8f,.845f,-3.4f));

        var sofa=original.First(t=>t.name=="Studio lounge sofa");var sofaCopy=Clone(sofa,new Vector3(23.2f,.04f,6.48f));
        sofaCopy.rotation=Quaternion.Euler(0,180,0)*sofaCopy.rotation;
        var table=original.First(t=>t.name=="Studio round coffee table");Clone(table,new Vector3(23.2f,.045f,4.85f));
        Clone(original.First(t=>t.name=="Lounge books"),new Vector3(23.2f,.57f,4.85f));
        var pouf=original.First(t=>t.name=="Colourful upholstered pouf");Clone(pouf,new Vector3(25.1f,.28f,4.85f));
        Box("Zoned carpet border",new Vector3(23.25f,.026f,5.25f),new Vector3(4.6f,.012f,3.55f),steel,false);
        Box("Zoned carpet field",new Vector3(23.25f,.035f,5.25f),new Vector3(4.44f,.008f,3.39f),Material("Ocean blue"),false);

        var cabinet=original.First(t=>t.name=="Studio storage cabinet");var origin=cabinet.position;var target=new Vector3(26.1f,0,7.1f);
        foreach(var t in original.Where(t=>t==cabinet || ((t.name=="Storage books" || t.name=="Studio potted plant") && Mathf.Abs(t.position.x-origin.x)<1.05f && Mathf.Abs(t.position.z-origin.z)<.5f)))
        {
            var copy=Clone(t,target+Quaternion.Euler(0,180,0)*(t.position-origin));copy.rotation=Quaternion.Euler(0,180,0)*t.rotation;
        }
        var floorPlant=original.First(t=>t.name=="Studio potted plant" && t.position.y<.05f);
        Clone(floorPlant,new Vector3(21.2f,.02f,6.75f));Clone(floorPlant,new Vector3(26.7f,.02f,-4.85f));
        foreach(float x in new[]{22.5f,25.4f})
        {
            var g=ExecutiveOfficeBuilder.PlaceDecoration("Assets/models/RaccoonGame/Content/AssetPacks/LowPolyWorld/Meshes/Interior/SM_Int_WallPainting02_3_c.obj",root,Vector3.zero);
            g.name="East wing framed artwork";g.transform.rotation=Quaternion.Euler(90,0,0);
            var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            g.transform.localScale*=1.7f/Mathf.Max(b.size.x,b.size.y,b.size.z);b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            g.transform.position+=new Vector3(x,2.2f,-5.43f)-b.center;
        }
        new GameObject(Marker).transform.SetParent(root,false);
        Physics.SyncTransforms();StudioPropPhysicsSetup.Apply();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveScene(root.gameObject.scene);
        return Validate();
    }
    static Transform Clone(Transform source,Vector3 position)
    {
        var copy=UnityEngine.Object.Instantiate(source,root);copy.name=source.name;copy.position=position;
        var body=copy.GetComponent<Rigidbody>();if(body!=null){body.position=position;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
        return copy;
    }
    static Material Material(string name){return AssetDatabase.LoadAssetAtPath<Material>("Assets/OfficeExtension/Materials/"+name+".mat");}
    static void Box(string name,Vector3 p,Vector3 size,Material material,bool collision=true)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root);g.transform.position=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
        if(!collision)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.BatchingStatic);
    }
    public static string Validate()
    {
        var r=GameObject.Find(Group).transform;int desks=r.Cast<Transform>().Count(t=>t.name.StartsWith("Workstation oak desktop"));
        if(desks!=12)throw new Exception("Expected twelve computer desks after extension, found "+desks);
        if(r.Find("Window lower wall").position.x<28.3f)throw new Exception("Outer window wall was not moved.");
        if(!Physics.Raycast(new Vector3(27,2,-2),Vector3.down,out var hit,3) || hit.collider.name!="Studio floor structural slab")throw new Exception("East wing floor collision missing.");
        return "PASS: office extended 7.2m to the right; 12 computer workstations, project table, second lounge, storage, art and plants. "+StudioPropPhysicsSetup.Validate();
    }
}
