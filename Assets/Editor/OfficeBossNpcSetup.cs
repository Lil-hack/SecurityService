using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeBossNpcSetup
{
    const string Folder="Assets/character/Animations/BossActions/";
    const string ControllerPath="Assets/character/Animations/BossNpc.controller";
    static readonly string[] Channels={"typing","writing","reading","drinking","watch","tie","talking","nodding","shrugging","waving","surprise","anger","pointing"};
    static readonly string[] Active={"typing","writing","reading","drinking","watch","tie","","","","","talking","nodding","shrugging","waving","surprise","anger","pointing",""};
    static readonly float[] Lengths={6,8,8,7,4,4,2.5f,2.5f,1,1.5f,5,3,3,3,4,4,4,4};

    [MenuItem("Tools/Office/Animate Boss NPC")]
    public static string Apply()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before configuring the boss.");
        var boss=All().FirstOrDefault(t=>string.Equals(t.name,"boss",StringComparison.OrdinalIgnoreCase));
        if(boss==null) throw new InvalidOperationException("No character named boss in the active scene.");
        var animator=boss.GetComponent<Animator>();
        if(animator==null || animator.avatar==null || !animator.avatar.isValid || !animator.avatar.isHuman) throw new InvalidOperationException("Boss needs a valid Humanoid Avatar.");
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var idle=Clip("m@Idle.fbx"); var walk=Clip("m@WalkForwards.fbx");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if(controller==null) controller=AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        var sm=controller.layers[0].stateMachine;
        foreach(var old in sm.states) sm.RemoveState(old.state);
        foreach(OfficeBossNpc.Action action in Enum.GetValues(typeof(OfficeBossNpc.Action)))
        {
            int index=(int)action; float length=Lengths[index];
            var source=action==OfficeBossNpc.Action.Walk?walk:idle;
            var generated=MakeClip(source,action,length,Active[index]);
            string path=Folder+action+".anim";
            var asset=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(asset==null) { AssetDatabase.CreateAsset(generated,path); asset=generated; }
            else { EditorUtility.CopySerialized(generated,asset); UnityEngine.Object.DestroyImmediate(generated); }
            var state=sm.AddState(action.ToString(),new Vector3((index%5)*210,(index/5)*70));
            state.motion=asset; if(action==OfficeBossNpc.Action.Walk) state.speed=.65f;
            if(action==OfficeBossNpc.Action.Idle) sm.defaultState=state;
        }
        var layers=controller.layers; layers[0].iKPass=true; controller.layers=layers;
        EditorUtility.SetDirty(controller);

        var npc=boss.GetComponent<OfficeBossNpc>();
        if(npc==null) npc=boss.gameObject.AddComponent<OfficeBossNpc>();
        // Restore any temporary prop poses before reconfiguring an existing NPC.
        npc.enabled=false;
        var oldMarkers=GameObject.Find("Boss activity anchors");
        if(oldMarkers!=null) UnityEngine.Object.DestroyImmediate(oldMarkers);
        var markers=new GameObject("Boss activity anchors").transform;
        npc.seatAnchor=Anchor(markers,"Boss seat",new Vector3(0,.005f,1.55f),180);
        npc.walkAnchor=Anchor(markers,"Boss window-side standing spot",new Vector3(2.6f,.005f,1.6f),200);
        npc.keyboardTarget=Anchor(markers,"Keyboard hand contact",new Vector3(0,1.20f,1.17f));
        npc.paperTarget=Anchor(markers,"Document hand contact",new Vector3(-.46f,1.19f,1.18f));
        npc.doorTarget=All().First(t=>t.name=="Right entry door");
        npc.player=GameObject.FindGameObjectWithTag("Player")?.transform;
        npc.chair=All().First(t=>t.name=="SM_Prop_OfficeChair_01");
        npc.chair.position=new Vector3(.05f,0,1.97f);
        npc.mug=All().First(t=>t.name=="SM_Item_Mug_01");
        SeatObject(npc.mug,new Vector3(.45f,1.15f,1.23f));
        // The imported laptop had an offset pivot and faced the visitors. Keep it centred,
        // turn its screen towards the boss and bring the keyboard within his arm reach.
        var laptop=All().First(t=>t.name=="SM_Prop_Laptop_01");
        laptop.rotation=Quaternion.identity;
        SeatObject(laptop,new Vector3(0,1.15f,.90f));
        var props=GameObject.Find("Boss interaction props"); if(props!=null) UnityEngine.Object.DestroyImmediate(props);
        var propsRoot=new GameObject("Boss interaction props").transform;
        var paperMat=Material("Boss document ivory",new Color(.90f,.87f,.77f));
        var ink=Material("Boss document ink",new Color(.13f,.16f,.21f));
        npc.document=Box("Boss working document",propsRoot,new Vector3(-.46f,1.165f,1.18f),new Vector3(.30f,.004f,.20f),paperMat).transform;
        for(int i=0;i<5;i++)
        {
            var line=Box("Printed document line",npc.document,Vector3.zero,Vector3.one,ink).transform;
            line.localPosition=new Vector3(0,.6f,-.33f+i*.14f); line.localScale=new Vector3(i==0?.72f:.62f,.12f,.022f);
        }
        var pencil=All().First(t=>t.name=="SM_Item_Pencil_01");
        npc.pen=UnityEngine.Object.Instantiate(pencil,propsRoot); npc.pen.name="Boss held pencil";
        foreach(var collider in npc.pen.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
        npc.pen.gameObject.SetActive(false);
        foreach(var collider in npc.mug.GetComponentsInChildren<Collider>()) collider.enabled=false;
        boss.SetPositionAndRotation(npc.seatAnchor.position,npc.seatAnchor.rotation);
        animator.runtimeAnimatorController=controller; animator.applyRootMotion=false;
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate; animator.enabled=true;
        if(boss.GetComponent<TripoArmClearance>()==null) boss.gameObject.AddComponent<TripoArmClearance>();
        npc.enabled=true; npc.automaticRoutine=true;
        animator.Rebind(); npc.ResetRoutine(false); animator.Update(.2f);
        boss.GetComponent<TripoArmClearance>().ApplyPose(); npc.ApplyInteractionPose();
        ConfigureStandingStart(false);
        EditorUtility.SetDirty(npc); EditorUtility.SetDirty(animator);
        PrefabUtility.RecordPrefabInstancePropertyModifications(boss); PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        File.WriteAllText(Folder+"README.md","Boss actions: 18 authored clips using Unity Standard Assets Humanoid idle/walk as their base.\n"+
            "The clips also animate interaction weights on OfficeBossNpc; late pose IK matches this broad character to the desk and props.\n"+
            "The original Unity clip source and license are in ../UnityStandard/SOURCE.md and LICENSE.md.\n"+
            "Work routine: type, write, read, coffee, watch, tie, stand, walk, turn, return, sit.\n"+
            "Approach the desk: boss looks up, waves, talks, nods, shrugs, then resumes work.\n"+
            "Press E nearby to cycle surprise, anger, pointing to the door and conversation gestures.\n");
        AssetDatabase.SaveAssets(); var scene=SceneManager.GetActiveScene(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "18 boss clips, work routine, seated pose IK, prop interactions, greeting and E reactions saved.";
    }

    [MenuItem("Tools/Office/Boss Starts Beside Chair")]
    public static void ConfigureStandingStart() { ConfigureStandingStart(true); }

    static void ConfigureStandingStart(bool save)
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before configuring the boss.");
        var npc=All().First(t=>string.Equals(t.name,"boss",StringComparison.OrdinalIgnoreCase)).GetComponent<OfficeBossNpc>();
        if(npc.standingAnchor==null)
            npc.standingAnchor=Anchor(npc.seatAnchor.parent,"Boss initial standing spot",new Vector3(3.331904f,.005f,1.547031f),197.927f);
        npc.startStanding=true; npc.initialStandingSeconds=2;
        var animator=npc.GetComponent<Animator>();
        var controller=animator.runtimeAnimatorController as AnimatorController;
        if(controller!=null)
        {
            var sm=controller.layers[0].stateMachine;
            sm.defaultState=sm.states.First(s=>s.state.name=="Idle").state;
            EditorUtility.SetDirty(controller);
        }
        npc.ResetRoutine(); // Restore prop homes before evaluating the standing preview.
        npc.chair.position=new Vector3(.05f,0,1.97f);
        animator.Rebind(); npc.ResetRoutine(false,true); animator.Update(0);
        npc.GetComponent<TripoArmClearance>()?.ApplyPose(); npc.ApplyInteractionPose();
        npc.chair.position=new Vector3(.05f,0,1.97f); // Preview must not bake the runtime chair shift into its home.
        EditorUtility.SetDirty(npc); EditorUtility.SetDirty(npc.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(npc.transform);
        if(save)
        {
            AssetDatabase.SaveAssets(); var scene=SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
    }

    static AnimationClip Clip(string name) { return AssetDatabase.LoadAllAssetsAtPath("Assets/character/Animations/UnityStandard/"+name).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__")); }
    static AnimationClip MakeClip(AnimationClip source,OfficeBossNpc.Action action,float length,string active)
    {
        var clip=new AnimationClip { name="Boss "+action,frameRate=30 };
        var bindings=AnimationUtility.GetCurveBindings(source);
        var curves=bindings.Select(b=>
        {
            var curve=AnimationUtility.GetEditorCurve(source,b);
            float scale=length/source.length;
            var keys=curve.keys;
            for(int k=0;k<keys.Length;k++) { keys[k].time*=scale; keys[k].inTangent/=scale; keys[k].outTangent/=scale; }
            return new AnimationCurve(keys);
        }).ToArray();
        AnimationUtility.SetEditorCurves(clip,bindings,curves);
        foreach(string channel in Channels)
        {
            var curve=AnimationCurve.Constant(0,length,0);
            if(channel==active)
                curve=action==OfficeBossNpc.Action.Typing?AnimationCurve.Constant(0,length,1):
                    new AnimationCurve(new Keyframe(0,0),new Keyframe(.18f*length,1),new Keyframe(.75f*length,1),new Keyframe(length,0));
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(OfficeBossNpc),channel),curve);
        }
        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(OfficeBossNpc),"poseClock"),AnimationCurve.Linear(0,0,length,length));
        var sit=AnimationCurve.Constant(0,length,1);
        if(action==OfficeBossNpc.Action.StandUp) sit=AnimationCurve.EaseInOut(0,1,length,0);
        if(action==OfficeBossNpc.Action.SitDown) sit=AnimationCurve.EaseInOut(0,0,length,1);
        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(OfficeBossNpc),"seatedPose"),sit);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime=action==OfficeBossNpc.Action.Typing || action==OfficeBossNpc.Action.Walk || action==OfficeBossNpc.Action.Idle;
        settings.loopBlend=settings.loopTime; settings.startTime=0; settings.stopTime=length;
        AnimationUtility.SetAnimationClipSettings(clip,settings); clip.EnsureQuaternionContinuity();
        return clip;
    }
    static Transform[] All() { return SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray(); }
    static Transform Anchor(Transform parent,string name,Vector3 position,float yaw=0) { var t=new GameObject(name).transform; t.SetParent(parent); t.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0)); return t; }
    static void SeatObject(Transform t,Vector3 bottomCentre)
    {
        var rs=t.GetComponentsInChildren<Renderer>(); var b=rs[0].bounds; foreach(var r in rs) b.Encapsulate(r.bounds);
        t.position+=bottomCentre-new Vector3(b.center.x,b.min.y,b.center.z);
    }
    static Material Material(string name,Color color)
    {
        string path=Folder+name+".mat"; var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
        mat.color=color; return mat;
    }
    static GameObject Box(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent); g.transform.position=position; g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=material; UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
    }
}
