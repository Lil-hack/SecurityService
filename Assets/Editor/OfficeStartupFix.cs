using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OfficeStartupFix
{
    [MenuItem("Tools/Office/Fix Kitchen Startup And Brighten Director")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode first.");
        var kitchen=GameObject.Find("Kitchen room");Undo.RegisterFullObjectHierarchyUndo(kitchen,"Stabilize kitchen startup");
        foreach(var p in kitchen.GetComponentsInChildren<OfficePushableProp>(true))
        {p.keepInitialPoseUntilGrab=true;p.Body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;p.Body.isKinematic=true;EditorUtility.SetDirty(p);EditorUtility.SetDirty(p.Body);}
        var plant=kitchen.transform.Find("Kitchen window plant");if(plant!=null)plant.gameObject.SetActive(false);
        foreach(var l in UnityEngine.Object.FindObjectsByType<Light>())
        {
            if(l.name=="Soft front fill"){l.intensity=5.5f;l.range=8;l.color=new Color(.93f,.94f,1);}
            if(l.name=="Window soft daylight"){l.intensity=5.5f;l.range=8;}
            if(l.name=="Pendant warm light")l.intensity=8;
            if(l.name=="Executive desk pool")l.intensity=15;
            EditorUtility.SetDirty(l);
        }
        var root=GameObject.Find("Atmosphere · evening studio");
        var fill=root.transform.Find("Director soft room fill");
        if(fill==null){fill=new GameObject("Director soft room fill").transform;fill.SetParent(root.transform,false);fill.gameObject.AddComponent<Light>();}
        fill.position=new Vector3(2.5f,2.8f,-1.8f);var light=fill.GetComponent<Light>();light.type=LightType.Point;light.intensity=4;light.range=6.5f;light.color=new Color(1,.91f,.78f);light.shadows=LightShadows.None;
        Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(kitchen.scene);EditorSceneManager.SaveScene(kitchen.scene);
        OfficeAtmosphereSetup.CaptureAfter();
        File.WriteAllText("OfficePreview/KitchenStartupFix.txt","Saved: kitchen props pinned until first grab, door-side plant disabled, director lighting brightened.");
    }
    [MenuItem("Tools/Office/Validate Kitchen Startup Stability")]
    public static async void Validate()
    {
        try
        {
            if(!Application.isPlaying)throw new Exception("Run in Play Mode.");
            await Task.Delay(6000);
            if(!Application.isPlaying)throw new Exception("Play Mode ended during stability check.");
            var props=GameObject.Find("Kitchen room").GetComponentsInChildren<OfficePushableProp>();
            foreach(var p in props)
                if(!p.InitialPosePinned || !p.CanPickUp || Vector3.Distance(p.InitialPosition,p.transform.position)>.001f || Quaternion.Angle(p.InitialRotation,p.transform.rotation)>.1f)
                    throw new Exception("Startup pose changed: "+p.name);
            var holder=UnityEngine.Object.FindAnyObjectByType<OfficePropGrabber>();var mug=props.First(p=>p.name=="Kitchen mug");
            if(!mug.Grab(holder))throw new Exception("Pinned mug cannot be grabbed.");
            mug.ReleaseFromPlayer(holder,Vector3.right,Vector3.zero);
            if(mug.Body.isKinematic || !mug.Body.useGravity || mug.InitialPosePinned)throw new Exception("Released mug did not become dynamic.");
            File.AppendAllText("OfficePreview/KitchenStartupFix.txt","\nPASS: "+props.Length+" props stayed in their initial pose for 6 seconds; pinned mug grabbed and released with dynamic physics.");
        }
        catch(Exception e){File.AppendAllText("OfficePreview/KitchenStartupFix.txt","\nFAIL: "+e);Debug.LogException(e);}
    }
}
