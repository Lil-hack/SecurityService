using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TripoArmClearanceSetup
{
    [MenuItem("Tools/Office/Fix Tripo Arm Clearance")]
    public static string Apply()
    {
        const string path = "Assets/character/Tripo_Textured.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);
        Avatar avatar;
        try
        {
            avatar = prefab.GetComponent<Animator>().avatar;
            var correction = prefab.GetComponent<TripoArmClearance>();
            if (correction == null) correction = prefab.AddComponent<TripoArmClearance>();
            correction.minimumArmAngle = 35f;
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        int count = 0;
        foreach (var animator in SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Animator>(true)))
        {
            if (animator.avatar != avatar) continue;
            var correction = animator.GetComponent<TripoArmClearance>();
            if (correction == null) correction = animator.gameObject.AddComponent<TripoArmClearance>();
            correction.minimumArmAngle = 35f;
            animator.Rebind();
            animator.Update(.2f);
            correction.ApplyPose();
            EditorUtility.SetDirty(correction);
            count++;
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        return "Arm clearance applied to " + count + " Tripo characters and the textured prefab.";
    }
}
