using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OfficeDoorPlaques
{
    [MenuItem("Tools/Office/Add White Door Plaques")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode first.");
        var doors=UnityEngine.Object.FindObjectsByType<OfficeDoor>();
        var kitchen=doors.OrderBy(d=>Vector3.Distance(d.transform.position,new Vector3(27.09f,0,7.6f))).First();
        var boss=doors.First(d=>d.name=="Interactive connecting door");
        var city=doors.First(d=>d.name=="South studio interactive door");
        const string path="Assets/OfficeExtension/Materials/White door plaque.mat";
        var white=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(white==null){white=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(white,path);}
        white.color=new Color(.94f,.93f,.87f);EditorUtility.SetDirty(white);
        foreach(var door in new[]{kitchen,boss,city})
        {
            Undo.RegisterFullObjectHierarchyUndo(door.gameObject,"Add door labels");
            foreach(var t in door.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("White door plaque ·")).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
            door.SetOpen(false,true);
        }
        Physics.SyncTransforms();
        Add(kitchen,"Кухня",Vector3.back,white);
        Add(boss,"Офис",Vector3.left,white);Add(boss,"Босс",Vector3.right,white);
        Add(city,"В город",Vector3.forward,white);
        foreach(var door in new[]{kitchen,boss,city})
        {
            // Labels belong to the hinge and follow both opening and closing.
            foreach(var text in door.GetComponentsInChildren<TextMesh>())
                if(text.transform.parent.name.StartsWith("White door plaque ·") && text.GetComponent<KitchenWorldText>()==null)throw new Exception("Door text missing depth test.");
            EditorSceneManager.MarkSceneDirty(door.gameObject.scene);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(city.gameObject.scene);
        File.WriteAllText("OfficePreview/DoorPlaques.txt","Saved: kitchen exterior Кухня; boss door from director side Офис, studio side Босс; city exit В город. Four depth-tested plaques parented to moving hinges.");
    }
    static void Add(OfficeDoor door,string caption,Vector3 normal,Material white)
    {
        var center=door.leaf.bounds.center;center.y=1.96f;
        var plaque=new GameObject("White door plaque · "+caption).transform;
        plaque.SetParent(door.transform,true);plaque.SetPositionAndRotation(center+normal*.105f,Quaternion.LookRotation(-normal,Vector3.up));
        var backing=GameObject.CreatePrimitive(PrimitiveType.Cube);backing.name="Ivory plaque backing";backing.transform.SetParent(plaque,false);backing.transform.localScale=new Vector3(.72f,.25f,.016f);backing.GetComponent<Renderer>().sharedMaterial=white;UnityEngine.Object.DestroyImmediate(backing.GetComponent<Collider>());
        var letters=new GameObject("Door label · "+caption);letters.transform.SetParent(plaque,false);letters.transform.localPosition=new Vector3(0,0,-.012f);
        var text=letters.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=caption;text.fontSize=64;text.characterSize=.021f;text.fontStyle=FontStyle.Bold;text.color=new Color(.055f,.09f,.12f);text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
        text.font.RequestCharactersInTexture(caption,64,FontStyle.Bold);letters.AddComponent<KitchenWorldText>().Refresh();
        foreach(var t in plaque.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
    }
}
