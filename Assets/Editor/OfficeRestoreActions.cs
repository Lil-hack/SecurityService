using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class OfficeRestoreActions
{
    [MenuItem("Tools/Office/Restore Original Punches And Jump")]
    public static void Apply()
    {
        OfficePunchSetup.Apply();
        var jump=AssetDatabase.LoadAllAssetsAtPath("Assets/character/boss/Animations/UnityStandard/m@StandingJump_Frame01.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
        var player=Object.FindAnyObjectByType<OfficeThirdPersonController>();var combat=player.GetComponent<OfficePunchCombat>();
        foreach(var controller in new[]{player.humanoidAnimator.runtimeAnimatorController as AnimatorController,AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/character/officeperson/StaffIdle.controller")})
        {
            foreach(var s in controller.layers[0].stateMachine.states.Select(s=>s.state))
            {
                if(s.name=="Jump"){s.motion=jump;s.speed=1;}
                if(s.name=="Jab"){s.motion=combat.jab;s.speed=OfficePunchCombat.PlaybackSpeed;}
                if(s.name=="Cross"){s.motion=combat.cross;s.speed=OfficePunchCombat.PlaybackSpeed;}
            }
            EditorUtility.SetDirty(controller);
        }
        AssetDatabase.SaveAssets();Debug.Log("Restored original jump and alternating punches; imported get-up clips preserved.");
    }
}
