using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StudioSouthDoorSetup
{
    [MenuItem("Tools/Office/Add South Studio Door")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode first.");
        var root=GameObject.Find("05 Open plan studio office").transform;
        if(root.Find("South studio doorway")!=null){ConfigureExit(root.Find("South studio doorway"));return;}
        var group=new GameObject("South studio doorway").transform;group.SetParent(root,false);
        const float x=16.65f,left=15.9f,right=17.4f;
        foreach(var t in root.Cast<Transform>().ToArray())
        {
            if((t.name=="Studio storage cabinet" || t.name=="Storage books" || t.name=="Studio potted plant") && Mathf.Abs(t.position.x-x)<1.15f && t.position.z< -4.5f)t.gameObject.SetActive(false);
        }
        var wall=root.Find("Studio south wall");var wallMat=wall.GetComponent<Renderer>().sharedMaterial;
        float min=wall.position.x-wall.localScale.x/2,max=wall.position.x+wall.localScale.x/2;
        Box(group,"Wall left of door",new Vector3((min+left)/2,2.1f,-5.6f),new Vector3(left-min,4.2f,.2f),wallMat);
        Box(group,"Wall right of door",new Vector3((max+right)/2,2.1f,-5.6f),new Vector3(max-right,4.2f,.2f),wallMat);
        Box(group,"Door lintel wall",new Vector3(x,3.5f,-5.6f),new Vector3(1.5f,1.4f,.2f),wallMat);wall.gameObject.SetActive(false);
        foreach(var t in root.Cast<Transform>().Where(t=>t.name=="Studio black skirting" && t.position.z< -5))t.gameObject.SetActive(false);
        var oak=AssetDatabase.LoadAssetAtPath<Material>("Assets/ExecutiveOffice/Materials/Walnut joinery.mat");
        foreach(float side in new[]{left,right})Box(group,"Walnut door jamb",new Vector3(side,1.38f,-5.57f),new Vector3(.09f,2.76f,.27f),oak);
        Box(group,"Walnut top casing",new Vector3(x,2.77f,-5.57f),new Vector3(1.6f,.12f,.27f),oak);
        var source=UnityEngine.Object.FindObjectsByType<OfficeDoor>().OrderBy(d=>Vector3.Distance(d.transform.position,new Vector3(27.09f,0,7.6f))).First();
        var door=UnityEngine.Object.Instantiate(source,group);door.name="South studio interactive door";door.transform.position=new Vector3(17.35f,0,-5.6f);door.openAngle=0;door.startOpen=false;door.SetOpen(false,true);
        // An enclosed landing makes the new doorway lead to usable space.
        var floor=AssetDatabase.LoadAssetAtPath<Material>("Assets/ExecutiveOffice/Materials/Kitchen oak floor.mat");
        Box(group,"Landing floor",new Vector3(x,-.05f,-7.1f),new Vector3(2.5f,.1f,3),floor);
        Box(group,"Landing back wall",new Vector3(x,2.1f,-8.6f),new Vector3(2.7f,4.2f,.2f),wallMat);
        foreach(float side in new[]{x-1.3f,x+1.3f})Box(group,"Landing side wall",new Vector3(side,2.1f,-7.1f),new Vector3(.2f,4.2f,3),wallMat);
        Box(group,"Landing ceiling",new Vector3(x,4.16f,-7.1f),new Vector3(2.7f,.15f,3.2f),wallMat);
        var light=new GameObject("Landing warm light").AddComponent<Light>();light.transform.SetParent(group);light.transform.position=new Vector3(x,3,-7);light.type=LightType.Point;light.intensity=4;light.range=3;light.color=new Color(1,.86f,.67f);
        Physics.SyncTransforms();
        var hits=Physics.OverlapBox(new Vector3(x,1.3f,-5.6f),new Vector3(.59f,1.2f,.3f));
        if(hits.Any(c=>c.enabled && c.gameObject.activeInHierarchy && c.GetComponentInParent<OfficeDoor>()==null))throw new Exception("Door opening obstructed: "+string.Join(",",hits.Select(c=>c.name)));
        ConfigureExit(group);
        System.IO.File.WriteAllText("OfficePreview/SouthStudioDoor.txt","PASS: cabinet, books and plant removed at x=16.65; opening clear; interactive door and enclosed landing saved.");
    }
    static void ConfigureExit(Transform group)
    {
        var door=group.GetComponentInChildren<OfficeDoor>();
        Undo.RegisterFullObjectHierarchyUndo(door.gameObject,"Correct closed exit door geometry");
        door.startOpen=false;door.SetOpen(false,true);EditorUtility.SetDirty(door);
        Physics.SyncTransforms();
        // The copied door carries its source room's leaf orientation. Align the leaf itself,
        // not just the hinge's open/closed flag, to the new south-wall opening.
        var direction=door.leaf.bounds.center-door.transform.position;direction.y=0;
        float correction=Vector3.SignedAngle(direction,Vector3.left,Vector3.up);
        foreach(Transform child in door.transform)child.RotateAround(door.transform.position,Vector3.up,correction);
        Physics.SyncTransforms();
        if(door.leaf.bounds.size.z>.35f || Mathf.Abs(door.leaf.bounds.center.z+5.6f)>.15f)
            throw new Exception("Door leaf is not aligned with the closed doorway.");
        var old=group.Find("Green EXIT sign");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var sign=new GameObject("Green EXIT sign").transform;sign.SetParent(group,false);sign.position=new Vector3(16.65f,3.05f,-5.4f);
        const string path="Assets/OfficeExtension/Materials/Exit sign green.mat";
        var green=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(green==null){green=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(green,path);}
        green.color=new Color(.015f,.37f,.12f,.28f);
        green.SetFloat("_Surface",1);green.SetFloat("_Blend",0);
        green.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        green.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        green.SetFloat("_ZWrite",0);green.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        green.SetOverrideTag("RenderType","Transparent");green.renderQueue=3000;
        green.EnableKeyword("_EMISSION");green.SetColor("_EmissionColor",new Color(.025f,.65f,.12f)*.6f);EditorUtility.SetDirty(green);
        Box(sign,"Illuminated green sign backing",sign.position,new Vector3(1.05f,.31f,.05f),green);
        var letters=new GameObject("EXIT world text");letters.transform.SetParent(sign,false);letters.transform.position=sign.position+Vector3.forward*.032f;letters.transform.rotation=Quaternion.Euler(0,180,0);
        var text=letters.AddComponent<TextMesh>();text.text="EXIT";text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=96;text.characterSize=.04f;text.fontStyle=FontStyle.Bold;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.78f,1,.81f);
        letters.AddComponent<KitchenWorldText>().Refresh();
        var spill=new GameObject("EXIT green light spill").AddComponent<Light>();spill.transform.SetParent(sign,false);spill.transform.position=sign.position+Vector3.forward*.15f;spill.type=LightType.Point;spill.color=new Color(.15f,1,.32f);spill.intensity=.18f;spill.range=.8f;spill.shadows=LightShadows.None;
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(group.gameObject.scene);EditorSceneManager.SaveScene(group.gameObject.scene);
        System.IO.File.WriteAllText("OfficePreview/ExitSign.txt","Saved: exit door starts CLOSED; green EXIT sign uses depth-tested world text above the doorway.");
    }
    [MenuItem("Tools/Office/Verify Exit Closed In Play Mode")]
    public static void VerifyClosed()
    {
        if(!Application.isPlaying)throw new Exception("Run in Play Mode.");
        var door=GameObject.Find("South studio interactive door").GetComponent<OfficeDoor>();
        Physics.SyncTransforms();var b=door.leaf.bounds;
        if(door.IsOpen || door.startOpen || b.size.x<1.2f || b.size.z>.35f || Mathf.Abs(b.center.z+5.6f)>.15f)throw new Exception("Exit is not physically closed after startup.");
        System.IO.File.AppendAllText("OfficePreview/ExitSign.txt","\nPASS Play Mode: door closed across doorway, leaf bounds "+b);
    }
    static void Box(Transform root,string name,Vector3 p,Vector3 s,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root);g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=m;}
}
