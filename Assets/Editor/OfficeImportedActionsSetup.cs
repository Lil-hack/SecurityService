using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OfficeImportedActionsSetup
{
    const string Folder="Assets/character/officeperson/FBX/";
    [MenuItem("Tools/Office/Use Imported Get Up Animations")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first.");
        AnimationClip Clip(string name)=>AssetDatabase.LoadAllAssetsAtPath(Folder+name+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
        var front=Clip("A_StandUp_Front");var back=Clip("A_StandUp_Back");
        var player=UnityEngine.Object.FindAnyObjectByType<OfficeThirdPersonController>();
        foreach(var controller in new[]{player.humanoidAnimator.runtimeAnimatorController as AnimatorController,AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/character/officeperson/StaffIdle.controller")})
        {
            var sm=controller.layers[0].stateMachine;
            foreach(var pair in new[]{("GetUpFront",front),("GetUpBack",back)})
            {var s=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==pair.Item1)??sm.AddState(pair.Item1);s.motion=pair.Item2;s.speed=1;}
            EditorUtility.SetDirty(controller);
        }
        foreach(var npc in UnityEngine.Object.FindObjectsByType<OfficeStaffPhysics>()){npc.getUpFront=front;npc.getUpBack=back;EditorUtility.SetDirty(npc);}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);
        Debug.Log("Imported get-up animations configured; original punches and jump preserved.");
    }
}
