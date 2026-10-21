using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TripoHumanoidAnimationSetup
{
    const string Folder="Assets/character/Animations/UnityStandard/";
    const string ControllerPath="Assets/character/Animations/TripoLocomotion.controller";
    const string PrefabPath="Assets/character/Tripo_Textured.prefab";

    [MenuItem("Tools/Office/Connect Tripo Humanoid Animations")]
    public static string Apply()
    {
        AssetDatabase.Refresh();
        var idle=Configure("m@Idle.fbx",true);
        var walk=Configure("m@WalkForwards.fbx",true);
        var run=Configure("m@RunForwards.fbx",true);
        var jump=Configure("m@StandingJump_Frame01.fbx",false);
        var fall=Configure("m@Falling.fbx",true);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if(controller==null) controller=AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.parameters=new[]{
            new AnimatorControllerParameter{name="Speed",type=AnimatorControllerParameterType.Float},
            new AnimatorControllerParameter{name="Grounded",type=AnimatorControllerParameterType.Bool,defaultBool=true},
            new AnimatorControllerParameter{name="VerticalSpeed",type=AnimatorControllerParameterType.Float}};
        var machine=controller.layers[0].stateMachine;
        var locomotion=State(machine,"Locomotion"); var jumping=State(machine,"Jump"); var falling=State(machine,"Fall");
        foreach(var state in new[]{locomotion,jumping,falling}) foreach(var t in state.transitions) state.RemoveTransition(t);
        var blend=locomotion.motion as BlendTree;
        if(blend==null) { blend=new BlendTree{name="Idle Walk Run",blendType=BlendTreeType.Simple1D}; AssetDatabase.AddObjectToAsset(blend,controller); }
        blend.blendParameter="Speed"; blend.useAutomaticThresholds=false;
        blend.children=new[]{
            new ChildMotion{motion=idle,threshold=0,timeScale=1},
            new ChildMotion{motion=walk,threshold=2.2f,timeScale=1.25f},
            new ChildMotion{motion=run,threshold=4.2f,timeScale=1.1f}};
        locomotion.motion=blend; locomotion.iKOnFeet=true;
        jumping.motion=jump; falling.motion=fall;
        var startJump=Transition(locomotion,jumping);
        startJump.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");
        startJump.AddCondition(AnimatorConditionMode.Greater,.1f,"VerticalSpeed");
        var startFall=Transition(locomotion,falling);
        startFall.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");
        startFall.AddCondition(AnimatorConditionMode.Less,.1f,"VerticalSpeed");
        Transition(jumping,falling).AddCondition(AnimatorConditionMode.Less,-.1f,"VerticalSpeed");
        Transition(jumping,locomotion).AddCondition(AnimatorConditionMode.If,0,"Grounded");
        Transition(falling,locomotion).AddCondition(AnimatorConditionMode.If,0,"Grounded");
        machine.defaultState=locomotion; EditorUtility.SetDirty(controller);

        // Keep the textured model reusable outside the office, with its Avatar and controller.
        var prefabRoot=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var animator=prefabRoot.GetComponent<Animator>();
            if(animator==null || animator.avatar==null || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new InvalidOperationException("Tripo needs a valid Humanoid Avatar.");
            animator.runtimeAnimatorController=controller; animator.applyRootMotion=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            if(prefabRoot.GetComponent<TripoArmClearance>()==null) prefabRoot.AddComponent<TripoArmClearance>();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot,PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }

        var player=GameObject.Find("Office third-person player");
        if(player==null) { OfficeThirdPersonSetup.Apply(); player=GameObject.Find("Office third-person player"); }
        var motor=player.GetComponent<OfficeThirdPersonController>();
        if(motor.visualRoot!=null) UnityEngine.Object.DestroyImmediate(motor.visualRoot.gameObject);
        var visual=new GameObject("Tripo animated visual").transform; visual.SetParent(player.transform,false);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        model.transform.SetParent(visual,false);
        // Humanoid retargeting aligns this rig to +Z even though its imported bind pose faces -X.
        model.transform.localRotation=Quaternion.identity;
        var renderers=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach(var r in renderers) r.updateWhenOffscreen=true;
        var actor=model.GetComponent<Animator>(); actor.applyRootMotion=false;
        actor.Rebind(); actor.SetBool("Grounded",true); actor.SetFloat("Speed",0);
        actor.Play("Locomotion",0,0); actor.Update(.2f);
        // Use the deformed mesh, not the FBX's generous T-pose renderer bounds.
        var bounds=SkinnedBoundsOf(model);
        model.transform.localScale*=1.72f/bounds.size.y;
        actor.Update(0); bounds=SkinnedBoundsOf(model);
        var centre=visual.InverseTransformPoint(bounds.center);
        var bottom=visual.InverseTransformPoint(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
        model.transform.localPosition-=new Vector3(centre.x,bottom.y,centre.z);
        foreach(var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=2;
        motor.visualRoot=visual; motor.humanoidAnimator=actor;
        motor.leftLeg=null; motor.rightLeg=null; motor.leftArm=null; motor.rightArm=null;
        motor.RefreshVisual(); actor.SetBool("Grounded",true); actor.SetFloat("Speed",0); actor.Update(0);
        // Existing placed copies also receive an idle pose instead of remaining in a T-pose.
        foreach(var other in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Animator>(true)))
        {
            if(other==actor || other.avatar!=actor.avatar || other.GetComponent<OfficeBossNpc>()!=null) continue;
            other.runtimeAnimatorController=controller; other.applyRootMotion=false;
            other.Rebind(); other.SetBool("Grounded",true); other.SetFloat("Speed",0);
            other.Play("Locomotion",0,0); other.Update(.2f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(other);
        }
        motor.UpdateCamera();
        actor.GetComponent<TripoArmClearance>().ApplyPose();
        AssetDatabase.SaveAssets();
        var scene=SceneManager.GetActiveScene(); EditorSceneManager.MarkSceneDirty(scene);
        if(!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
        return "Tripo player connected: Idle, Walk, Run, Jump, Fall. Avatar valid; root motion disabled; existing character movement and camera retained.";
    }

    static AnimationClip Configure(string file,bool loop)
    {
        string path=Folder+file;
        var importer=AssetImporter.GetAtPath(path) as ModelImporter;
        if(importer==null) throw new FileNotFoundException(path);
        var clips=importer.clipAnimations;
        if(clips.Length==0) clips=importer.defaultClipAnimations;
        foreach(var clip in clips)
        {
            clip.loopTime=loop; clip.loopPose=loop;
            clip.lockRootRotation=true; clip.lockRootHeightY=true;
            // Extract horizontal root motion and let applyRootMotion=false discard it.
            // Baking it into the pose leaves the running mesh ahead of its collider.
            clip.lockRootPositionXZ=false;
            clip.keepOriginalOrientation=true; clip.keepOriginalPositionY=true; clip.keepOriginalPositionXZ=false;
            clip.events=new AnimationEvent[0];
        }
        importer.clipAnimations=clips; importer.SaveAndReimport();
        var result=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .First(c=>!c.name.StartsWith("__preview__") && !c.name.EndsWith("_Mirror"));
        if(!result.humanMotion || result.length<=0) throw new InvalidOperationException("Invalid Humanoid clip: "+file);
        return result;
    }

    static AnimatorState State(AnimatorStateMachine machine,string name)
    {
        var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name);
        return state!=null?state:machine.AddState(name);
    }

    static AnimatorStateTransition Transition(AnimatorState from,AnimatorState to)
    {
        var transition=from.AddTransition(to); transition.hasExitTime=false;
        transition.duration=.14f; transition.hasFixedDuration=true; return transition;
    }

    public static Bounds SkinnedBoundsOf(GameObject root)
    {
        var bounds=new Bounds(); bool first=true;
        foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh=new Mesh(); renderer.BakeMesh(mesh,true);
            foreach(var vertex in mesh.vertices)
            {
                var point=renderer.transform.TransformPoint(vertex);
                if(first) { bounds=new Bounds(point,Vector3.zero); first=false; } else bounds.Encapsulate(point);
            }
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        return bounds;
    }
}
