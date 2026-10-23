using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KitchenSoftFurnishings
{
    [MenuItem("Tools/Office/Refine Kitchen Cushions And Chair")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play mode first.");
        var kitchen=GameObject.Find("Kitchen room");Undo.RegisterFullObjectHierarchyUndo(kitchen,"Refine kitchen seating");
        foreach(var t in kitchen.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Bench accent cushion" || t.name=="Kitchen tailored cushions").ToArray()) Object.DestroyImmediate(t.gameObject);
        var root=new GameObject("Kitchen tailored cushions").transform;root.SetParent(kitchen.transform,false);
        var yellow=Mat("Kitchen sunflower");var linen=Mat("Kitchen linen");var teal=Mat("Kitchen teal");
        for(int i=0;i<2;i++)
        {
            var pillow=new GameObject(i==0?"Round tufted mustard cushion":"Sage bolster with linen piping").transform;
            pillow.SetParent(root,false);pillow.position=new Vector3(27.54f,.78f,12+i*1.05f);
            pillow.rotation=Quaternion.Euler(i==0?8:-8,0,-14);
            var body=i==0?yellow:teal;var trim=i==0?teal:linen;
            Sphere(pillow,"Soft upholstered body",Vector3.zero,new Vector3(.20f,.43f,i==0?.43f:.52f),body);
            const int n=48;
            for(int j=0;j<n;j++)
            {
                float a=j*Mathf.PI*2/n,b=(j+1)*Mathf.PI*2/n;
                Rod(pillow,"Contrasting stitched welt",new Vector3(-.035f,Mathf.Cos(a)*.211f,Mathf.Sin(a)*(i==0?.211f:.256f)),new Vector3(-.035f,Mathf.Cos(b)*.211f,Mathf.Sin(b)*(i==0?.211f:.256f)),.005f,trim);
            }
            Sphere(pillow,"Covered tuft button",new Vector3(-.102f,0,0),new Vector3(.018f,.045f,.045f),trim);
            if(i==1)foreach(float z in new[]{-.15f,.15f})
                for(int j=0;j<32;j++)
                {
                    float a=j*Mathf.PI*2/32,b=(j+1)*Mathf.PI*2/32;
                    Rod(pillow,"Bolster embroidered band",new Vector3(Mathf.Cos(a)*.081f,Mathf.Sin(a)*.171f,z),new Vector3(Mathf.Cos(b)*.081f,Mathf.Sin(b)*.171f,z),.007f,linen);
                }
        }
        var green=kitchen.GetComponentsInChildren<Transform>().Where(t=>t.name=="Kitchen dining chair").OrderBy(t=>t.position.x).First();
        green.rotation=Quaternion.Euler(0,90,0);
        Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(kitchen.scene);EditorSceneManager.SaveScene(kitchen.scene);
        KitchenRoomDetailing.Capture();
        System.IO.File.WriteAllText("OfficePreview/KitchenSeating_Result.txt","Saved two soft tailored cushions; west green chair yaw 90, facing table.");
    }
    static Material Mat(string n)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/ExecutiveOffice/Materials/"+n+".mat");
    static GameObject Sphere(Transform p,string n,Vector3 pos,Vector3 scale,Material m)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
    static void Rod(Transform p,string n,Vector3 a,Vector3 b,float r,Material m)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=(a+b)*.5f;g.transform.localScale=new Vector3(r*2,(b-a).magnitude*.5f,r*2);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);g.GetComponent<Renderer>().sharedMaterial=m;Object.DestroyImmediate(g.GetComponent<Collider>());}
}
