using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class OfficeStaffGetUpSetup
{
    [MenuItem("Tools/Office/Test Thrown Prop And Local Get Up")]
    public static void Test()
    {
        if(!Application.isPlaying)throw new System.Exception("Enter Play first.");
        Application.runInBackground=true;
        var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/character/officeperson/man1/Staff.prefab");
        var npc=Object.Instantiate(original).GetComponent<OfficeStaffPhysics>();
        npc.transform.SetPositionAndRotation(new Vector3(1000,.03f,1000),Quaternion.identity);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Temporary throw test floor";floor.transform.position=new Vector3(1000,-.1f,1000);floor.transform.localScale=new Vector3(30,.2f,30);
        var ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);ball.transform.localScale=Vector3.one*.2f;ball.transform.position=npc.transform.position+new Vector3(0,1,2);
        var rb=ball.AddComponent<Rigidbody>();rb.mass=.7f;
        var prop=ball.AddComponent<OfficePushableProp>();var holder=Object.FindFirstObjectByType<OfficePropGrabber>();
        if(!prop.Grab(holder))throw new System.Exception("Cannot prepare thrown test prop.");
        prop.ReleaseFromPlayer(holder,new Vector3(0,0,-8),Vector3.zero);
        float start=Time.time;bool struck=false;
        void Check()
        {
            if(!Application.isPlaying){EditorApplication.update-=Check;return;}
            struck|=npc.IsDown;
            if(Time.time-start<9)return;
            EditorApplication.update-=Check;
            try
            {
                if(!struck)throw new System.Exception("Thrown prop did not knock down NPC.");
                if(npc.IsDown || !npc.GetComponent<CapsuleCollider>().enabled)throw new System.Exception("Get up did not finish.");
                if(Vector3.Distance(npc.transform.position,npc.LastFallPosition)>.05f)throw new System.Exception("NPC moved away from fall location.");
                if(Vector3.Distance(npc.transform.position,new Vector3(1000,.03f,1000))<.4f)throw new System.Exception("Test did not displace NPC.");
                Debug.Log("PASS actual thrown prop collision, knockback, authored get-up and standing at fall position: "+npc.transform.position);
            }
            finally{Object.Destroy(npc.gameObject);Object.Destroy(ball);Object.Destroy(floor);}
        }
        EditorApplication.update+=Check;
    }
    [MenuItem("Tools/Office/Create Staff Get Up Animations")]
    public static void Apply()
    {
        const string folder="Assets/character/officeperson/";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"StaffIdle.controller");
        var source=AssetDatabase.LoadAllAssetsAtPath("Assets/character/boss/Animations/UnityStandard/m@Idle.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
        foreach(bool back in new[]{false,true})
        {
            string name=back?"GetUpBack":"GetUpFront";
            var clip=new AnimationClip{name=name,frameRate=30};
            foreach(var b in AnimationUtility.GetCurveBindings(source))
                AnimationUtility.SetEditorCurve(clip,b,AnimationCurve.Constant(0,2,AnimationUtility.GetEditorCurve(source,b).Evaluate(0)));
            void Pose(string muscle,float start,float kneel,float end=0)
            {
                var curve=new AnimationCurve(new Keyframe(0,start),new Keyframe(.65f,kneel),new Keyframe(1.85f,end),new Keyframe(2,end));
                for(int i=0;i<curve.length;i++) {AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);}
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),muscle),curve);
            }
            Pose("Spine Front-Back",back?.1f:.7f,.45f);Pose("Chest Front-Back",back?.3f:.5f,.2f);
            Pose("Left Upper Leg Front-Back",.8f,.65f);Pose("Right Upper Leg Front-Back",.85f,.4f);
            Pose("Left Lower Leg Stretch",-.95f,-.85f);Pose("Right Lower Leg Stretch",-.95f,-.65f);
            Pose("Left Arm Down-Up",-.3f,-.65f,-.8f);Pose("Right Arm Down-Up",-.3f,-.65f,-.8f);
            Pose("Left Arm Front-Back",back?-.4f:.7f,.55f);Pose("Right Arm Front-Back",back?-.4f:.7f,.55f);
            Pose("Left Forearm Stretch",back?-.8f:-.3f,-.65f);Pose("Right Forearm Stretch",back?-.8f:-.3f,-.65f);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.startTime=0;settings.stopTime=2;AnimationUtility.SetAnimationClipSettings(clip,settings);
            string path=folder+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(existing==null)AssetDatabase.CreateAsset(clip,path);else {EditorUtility.CopySerialized(clip,existing);Object.DestroyImmediate(clip);clip=existing;}
            var sm=controller.layers[0].stateMachine;var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??sm.AddState(name);state.motion=clip;
        }
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        File.WriteAllText(folder+"GetUp_README.md","Two authored Humanoid get-up clips: front support and back sit-up, kneeling then standing. Based on the existing licensed Unity Standard idle pose. OfficeStaffPhysics blends the physical pose into the clip and controls pelvis height; no root translation to spawn.\n");
        Debug.Log("Authored front/back get-up animations saved.");
    }
}

