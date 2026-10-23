using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KitchenDoorDirection
{
    [MenuItem("Tools/Office/Reverse Kitchen Door")]
    public static void Apply()
    {
        if(Application.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
        var target=new Vector3(26.4f,0,7.6f);
        var doors=Object.FindObjectsByType<OfficeDoor>(FindObjectsSortMode.None).OrderBy(d=>Vector2.Distance(new Vector2(d.transform.position.x,d.transform.position.z),new Vector2(target.x,target.z))).ToArray();
        if(doors.Length==0 || Vector3.Distance(doors[0].transform.position,target)>3) throw new System.InvalidOperationException("Kitchen door not found near kitchen entrance.");
        var door=doors[0];Undo.RecordObject(door,"Reverse kitchen door opening");
        door.openAngle=180f;
        EditorUtility.SetDirty(door);EditorSceneManager.MarkSceneDirty(door.gameObject.scene);EditorSceneManager.SaveScene(door.gameObject.scene);
        System.IO.File.WriteAllText("OfficePreview/KitchenDoorDirection.txt",door.name+" at "+door.transform.position+": closed 90, open "+door.openAngle);
    }
}
