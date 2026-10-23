using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;

public static class OfficeDogLifeSetup
{
    const string Prefab="Assets/character/officeperson/dog/Office_Dog.prefab";
    const string Folder="Assets/character/officeperson/dog/";
    [MenuItem("Tools/Office/Validate Dog Head Animation")]
    public static void Validate()
    {
        var dog=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        try
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Dog_HeadIdle.anim");
            var head=dog.GetComponentsInChildren<Transform>().First(t=>t.name=="bone_4");
            var paw=dog.GetComponentsInChildren<Transform>().First(t=>t.name=="tripo::0_Left_Limb_2");
            clip.SampleAnimation(dog,0);var start=head.localRotation;var pawPosition=paw.position;
            clip.SampleAnimation(dog,3);if(Quaternion.Angle(start,head.localRotation)<5)throw new System.Exception("Head does not animate.");
            if(Vector3.Distance(paw.position,pawPosition)>.0001f)throw new System.Exception("Head motion moved paw.");
            foreach(float t in new[]{0f,3f,6f})
            {
                clip.SampleAnimation(dog,t);
                OfficeDogPrefabSetup.Capture(dog,TripoHumanoidAnimationSetup.SkinnedBoundsOf(dog),"OfficePreview/Dog_Head_"+t+".png");
            }
            clip.SampleAnimation(dog,14);if(Quaternion.Angle(start,head.localRotation)>.1f)throw new System.Exception("Head loop is discontinuous.");
            Debug.Log("PASS dog head turns and tilts; paws fixed; seamless loop; previews captured.");
        }
        finally{Object.DestroyImmediate(dog);}
    }
    [MenuItem("Tools/Office/Animate Dog Head")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new System.Exception("Stop Play first.");
        var dog=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        try
        {
            var bones=dog.GetComponentsInChildren<Transform>();var head=bones.First(t=>t.name=="bone_4");var tip=bones.First(t=>t.name=="bone_5");
            var clip=new AnimationClip{name="Dog - curious head idle",frameRate=30};
            var yaw=new AnimationCurve(new Keyframe(0,0),new Keyframe(1.5f,-9),new Keyframe(3,-9),new Keyframe(4.6f,12),new Keyframe(6.3f,12),new Keyframe(7.4f,0),new Keyframe(8.4f,-4),new Keyframe(8.7f,4),new Keyframe(9,-4),new Keyframe(9.3f,0),new Keyframe(10.8f,7),new Keyframe(12,7),new Keyframe(14,0));
            var tilt=new AnimationCurve(new Keyframe(0,0),new Keyframe(2,-7),new Keyframe(4,-2),new Keyframe(6,6),new Keyframe(8.5f,0),new Keyframe(10,-5),new Keyframe(12,-2),new Keyframe(14,0));
            foreach(var curve in new[]{yaw,tilt})for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);}
            foreach(var bone in new[]{head,tip})
            {
                var rest=bone.localRotation;var curves=Enumerable.Range(0,4).Select(_=>new AnimationCurve()).ToArray();Quaternion previous=rest;
                var up=bone.parent.InverseTransformDirection(dog.transform.up);var right=bone.parent.InverseTransformDirection(dog.transform.forward);var forward=bone.parent.InverseTransformDirection(-dog.transform.right);
                for(int frame=0;frame<=420;frame++)
                {
                    float t=frame/30f;float nod=Mathf.Sin(t*Mathf.PI*4/14)*2.2f;
                    var q=bone==head?Quaternion.AngleAxis(yaw.Evaluate(t),up)*Quaternion.AngleAxis(nod,right)*Quaternion.AngleAxis(tilt.Evaluate(t),forward)*rest:Quaternion.AngleAxis(nod*.3f,right)*rest;
                    if(Quaternion.Dot(q,previous)<0)q=new Quaternion(-q.x,-q.y,-q.z,-q.w);previous=q;
                    for(int n=0;n<4;n++)curves[n].AddKey(t,q[n]);
                }
                string path=AnimationUtility.CalculateTransformPath(bone,dog.transform);
                for(int n=0;n<4;n++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[n]),curves[n]);
            }
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.loopBlend=true;settings.startTime=0;settings.stopTime=14;AnimationUtility.SetAnimationClipSettings(clip,settings);clip.EnsureQuaternionContinuity();
            var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Dog_HeadIdle.anim");
            if(existing==null)AssetDatabase.CreateAsset(clip,Folder+"Dog_HeadIdle.anim");else{EditorUtility.CopySerialized(clip,existing);Object.DestroyImmediate(clip);clip=existing;}
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"Dog_Life.controller");if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"Dog_Life.controller");
            var sm=controller.layers[0].stateMachine;var state=sm.states.Length>0?sm.states[0].state:sm.AddState("Curious idle");state.motion=clip;sm.defaultState=state;EditorUtility.SetDirty(controller);
            dog.GetComponent<Animator>().runtimeAnimatorController=controller;PrefabUtility.SaveAsPrefabAsset(dog,Prefab);AssetDatabase.SaveAssets();
            var sceneDogs=SceneManagerDogs().Where(g=>g!=dog).ToArray();foreach(var sceneDog in sceneDogs)
            {
                var animator=sceneDog.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.enabled=true;EditorUtility.SetDirty(animator);PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                EditorSceneManager.MarkSceneDirty(sceneDog.scene);
            }
            Debug.Log("Dog head idle saved; scene dogs updated: "+sceneDogs.Length);
        }
        finally{Object.DestroyImmediate(dog);}
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
    static GameObject[] SceneManagerDogs()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Animator>(true)).Where(a=>a.name=="Office_Dog" && a.GetComponentInChildren<SkinnedMeshRenderer>()!=null).Select(a=>a.gameObject).ToArray();
    [MenuItem("Tools/Office/Inspect Dog Bones")]
    public static void Inspect()
    {
        var dog=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        try
        {
            var skin=dog.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();skin.BakeMesh(mesh);var vertices=mesh.vertices;var weights=skin.sharedMesh.boneWeights;
            var report=new System.Text.StringBuilder();
            for(int i=0;i<skin.bones.Length;i++)
            {
                Vector3 center=Vector3.zero;float sum=0;
                for(int v=0;v<vertices.Length;v++)
                {
                    var w=weights[v];float weight=(w.boneIndex0==i?w.weight0:0)+(w.boneIndex1==i?w.weight1:0)+(w.boneIndex2==i?w.weight2:0)+(w.boneIndex3==i?w.weight3:0);
                    center+=skin.transform.TransformPoint(vertices[v])*weight;sum+=weight;
                }
                report.AppendLine(skin.bones[i].name+" pivot="+skin.bones[i].position+" skinCentre="+(sum>0?center/sum:Vector3.zero)+" weight="+sum);
            }
            Object.DestroyImmediate(mesh);Directory.CreateDirectory("OfficePreview");File.WriteAllText("OfficePreview/DogBones.txt",report.ToString());
        }
        finally{Object.DestroyImmediate(dog);}
    }
}

