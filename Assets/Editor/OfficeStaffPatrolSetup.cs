using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeStaffPatrolSetup
{
    [MenuItem("Tools/Office/Make Man1 Walk Office")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first.");
        var npc=UnityEngine.Object.FindObjectsByType<OfficeStaffPhysics>().First(n=>n.name=="NPC man1 - Office");
        var controller=npc.GetComponentInChildren<Animator>().runtimeAnimatorController as AnimatorController;
        var walk=AssetDatabase.LoadAllAssetsAtPath("Assets/character/boss/Animations/UnityStandard/m@WalkForwards.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
        if(!controller.parameters.Any(p=>p.name=="PatrolSpeed"))controller.AddParameter("PatrolSpeed",AnimatorControllerParameterType.Float);
        var sm=controller.layers[0].stateMachine;var idle=sm.states.First(s=>s.state.name=="Idle").state;
        var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Walk")??sm.AddState("Walk");state.motion=walk;state.speed=.7f;
        if(!idle.transitions.Any(t=>t.destinationState==state)){var t=idle.AddTransition(state);t.hasExitTime=false;t.duration=.18f;t.AddCondition(AnimatorConditionMode.Greater,.1f,"PatrolSpeed");}
        if(!state.transitions.Any(t=>t.destinationState==idle)){var t=state.AddTransition(idle);t.hasExitTime=false;t.duration=.18f;t.AddCondition(AnimatorConditionMode.Less,.1f,"PatrolSpeed");}
        var patrol=npc.GetComponent<OfficeStaffPatrol>();if(patrol==null)patrol=npc.gameObject.AddComponent<OfficeStaffPatrol>();
        float y=npc.transform.position.y;float z=npc.transform.position.z;
        patrol.waypoints=new[]{new Vector3(9.5f,y,z),new Vector3(25.5f,y,z)};patrol.speed=.9f;patrol.pauseSeconds=1.7f;
        EditorUtility.SetDirty(controller);EditorUtility.SetDirty(patrol);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(npc.gameObject.scene);EditorSceneManager.SaveScene(npc.gameObject.scene);
        Debug.Log("Man1 patrol configured along office aisle, with walking animation, pauses and obstacle checks.");
    }
    [MenuItem("Tools/Office/Test Man1 Patrol")]
    public static void Test()
    {
        if(!Application.isPlaying)throw new Exception("Enter Play first.");Application.runInBackground=true;
        var patrol=UnityEngine.Object.FindAnyObjectByType<OfficeStaffPatrol>();var start=patrol.transform.position;float began=Time.time;
        void Check()
        {
            if(!Application.isPlaying){EditorApplication.update-=Check;return;}
            if(Time.time-began<5)return;EditorApplication.update-=Check;
            float distance=Vector3.Distance(start,patrol.transform.position);
            if(distance<1)throw new Exception("Patrol did not move: "+distance);
            var animator=patrol.GetComponentInChildren<Animator>();if(animator.GetFloat("PatrolSpeed")<.1f)throw new Exception("Walking animation is inactive.");
            Debug.Log("PASS Man1 patrol moved "+distance+" metres with walk animation.");
        }
        EditorApplication.update+=Check;
    }
}
