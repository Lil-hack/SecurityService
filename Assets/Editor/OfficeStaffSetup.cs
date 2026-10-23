using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OfficeStaffSetup
{
    const string Folder="Assets/character/officeperson/";
    [MenuItem("Tools/Office/Enable Staff Physics")]
    public static void EnablePhysics()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first.");
        var root=GameObject.Find("Office staff NPCs");
        foreach(Transform actor in root.transform)if(actor.GetComponent<OfficeStaffPhysics>()==null)actor.gameObject.AddComponent<OfficeStaffPhysics>();
        foreach(var folder in new[]{"girl1","girl2","man1","man2"})
        {
            string path=Folder+folder+"/Staff.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
            try{if(prefab.GetComponent<OfficeStaffPhysics>()==null)prefab.AddComponent<OfficeStaffPhysics>();PrefabUtility.SaveAsPrefabAsset(prefab,path);}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
        EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);AssetDatabase.SaveAssets();
    }
    [MenuItem("Tools/Office/Test Staff Knockdown")]
    public static void TestKnockdown()
    {
        if(!Application.isPlaying)throw new Exception("Enter Play first.");
        var npcs=UnityEngine.Object.FindObjectsByType<OfficeStaffPhysics>(FindObjectsSortMode.None);
        if(npcs.Length!=4)throw new Exception("Expected four staff NPCs.");
        foreach(var npc in npcs)if(!npc.Punch(npc.transform.position+Vector3.up,npc.transform.forward))throw new Exception("Knockdown rejected.");
        Application.runInBackground=true; float start=Time.time;
        void Check()
        {
            if(!Application.isPlaying){EditorApplication.update-=Check;return;}
            if(Time.time-start<8)return;
            EditorApplication.update-=Check;
            foreach(var npc in npcs)if(npc.IsDown || !npc.GetComponent<CapsuleCollider>().enabled || !npc.GetComponentInChildren<Animator>().enabled)throw new Exception("NPC did not recover: "+npc.name);
            Debug.Log("PASS four staff ragdolls knocked back and recovered to animated standing.");
        }
        EditorApplication.update+=Check;
    }
    [MenuItem("Tools/Office/Place Office Staff")]
    public static void Place()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first.");
        var old=GameObject.Find("Office staff NPCs");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
        var root=new GameObject("Office staff NPCs");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"StaffIdle.controller");
        if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"StaffIdle.controller");
        var sm=controller.layers[0].stateMachine;
        var state=sm.states.Length>0?sm.states[0].state:sm.AddState("Idle");
        state.motion=AssetDatabase.LoadAllAssetsAtPath("Assets/character/boss/Animations/UnityStandard/m@Idle.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));sm.defaultState=state;
        var names=new[]{"girl1","man1","girl2","man2"};
        var spots=new[]{new Vector3(14.3f,.03f,3.1f),new Vector3(15.6f,.03f,3.5f),new Vector3(22.6f,.03f,13.5f),new Vector3(24.9f,.03f,12.65f)};
        var yaws=new[]{73f,253f,0f,230f};var heights=new[]{1.67f,1.80f,1.70f,1.78f};
        for(int i=0;i<4;i++)
        {
            string folder=Folder+names[i]+"/";string path=Directory.GetFiles(folder,"*.fbx").Single().Replace('\\','/');
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var actor=new GameObject("NPC "+names[i]+(i<2?" - Office":" - Kitchen"));actor.transform.SetParent(root.transform);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(model);visual.transform.SetParent(actor.transform,false);
            var animator=visual.GetComponent<Animator>();
            if(animator.avatar==null || !animator.avatar.isHuman || !animator.avatar.isValid)throw new Exception(names[i]+" invalid Avatar");
            string texturePath=Directory.GetFiles(folder+"Textures","*.jpg").Single().Replace('\\','/');
            var mat=AssetDatabase.LoadAssetAtPath<Material>(folder+"Staff_URP.mat");
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,folder+"Staff_URP.mat");}
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.25f);mat.SetFloat("_Metallic",0);EditorUtility.SetDirty(mat);
            foreach(var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>()){r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();r.updateWhenOffscreen=true;}
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();animator.Play(state.name,0,i*.23f);animator.Update(0);
            var b=TripoHumanoidAnimationSetup.SkinnedBoundsOf(visual);visual.transform.localScale*=heights[i]/b.size.y;animator.Update(0);
            b=TripoHumanoidAnimationSetup.SkinnedBoundsOf(visual);visual.transform.localPosition-=new Vector3(b.center.x,b.min.y,b.center.z);
            var capsule=actor.AddComponent<CapsuleCollider>();capsule.radius=.26f;capsule.height=heights[i];capsule.center=Vector3.up*heights[i]*.5f;
            actor.AddComponent<OfficeStaffPhysics>();
            PrefabUtility.SaveAsPrefabAsset(actor,folder+"Staff.prefab");
            actor.transform.SetPositionAndRotation(spots[i],Quaternion.Euler(0,yaws[i],0));
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);
        Capture();
    }
    [MenuItem("Tools/Office/Capture Office Staff")]
    public static void Capture()
    {
        Directory.CreateDirectory("OfficePreview");
        var camera=new GameObject("Temporary staff preview").AddComponent<Camera>();camera.nearClipPlane=.05f;camera.fieldOfView=55;
        var rt=new RenderTexture(1100,800,24);camera.targetTexture=rt;
        try
        {
            for(int i=0;i<2;i++)
            {
                camera.transform.position=i==0?new Vector3(14.8f,3.1f,7.3f):new Vector3(26.4f,2.9f,10.5f);
                camera.transform.LookAt(i==0?new Vector3(14.9f,1,3.3f):new Vector3(23.6f,1,13.5f));
                camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(1100,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1100,800),0,0);image.Apply();
                File.WriteAllBytes("OfficePreview/Staff_"+(i==0?"Office":"Kitchen")+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            }
        }
        finally{camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camera.gameObject);}
        Debug.Log("Four staff NPCs prepared and placed; previews captured.");
    }
    [MenuItem("Tools/Office/Prepare Office Staff")]
    public static void Prepare()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first.");
        foreach(var path in Directory.GetFiles(Folder,"*.fbx",SearchOption.AllDirectories))
        {
            var p=path.Replace('\\','/');var importer=(ModelImporter)AssetImporter.GetAtPath(p);
            importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importCameras=false;importer.importLights=false;
            string textures=Path.GetDirectoryName(p).Replace('\\','/')+"/Textures";Directory.CreateDirectory(textures);
            importer.ExtractTextures(textures);importer.SaveAndReimport();
        }
        AssetDatabase.Refresh();
        Directory.CreateDirectory("OfficePreview");
        File.WriteAllText("OfficePreview/StaffImport.txt",string.Join("\n",Directory.GetFiles(Folder,"*.fbx",SearchOption.AllDirectories).Select(path=>{
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\','/'));var a=model.GetComponent<Animator>();
            return path+" Human="+(a!=null && a.avatar!=null && a.avatar.isValid && a.avatar.isHuman)+" Materials="+string.Join(",",model.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.name+":"+(m.mainTexture!=null?m.mainTexture.name:"NO TEXTURE")));
        })));
        Debug.Log("Staff import complete");
    }
}


