using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ProgressiveThrowSetup
{
    [MenuItem("Tools/Office/Apply Progressive Throw")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play mode first.");
        foreach(var g in Object.FindObjectsByType<OfficePropGrabber>())
        {Undo.RecordObject(g,"Progressive short-to-long throw");g.throwSpeed=3.3f;g.maximumThrowSpeed=11;g.throwChargeSeconds=2f;EditorUtility.SetDirty(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);}
    }
}

