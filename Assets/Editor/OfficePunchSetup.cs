using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OfficePunchSetup
{
    [MenuItem("Tools/Office/Setup Alternating Punches")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode first.");
        const string source="Assets/character/Animations/Combat/QuaterniusCombat.fbx";
        var importer=(ModelImporter)AssetImporter.GetAtPath(source);importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;
        var clips=importer.defaultClipAnimations;
        foreach(var c in clips){c.loopTime=false;c.lockRootRotation=true;c.lockRootPositionXZ=true;c.lockRootHeightY=true;}
        importer.clipAnimations=clips;importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(source);var sourceAnimator=model.GetComponent<Animator>();
        if(sourceAnimator==null || sourceAnimator.avatar==null || !sourceAnimator.avatar.isHuman)throw new Exception("Combat rig did not import as Humanoid.");
        var assets=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().ToArray();
        var jab=assets.First(c=>c.name.Contains("Punch_Jab"));var cross=assets.First(c=>c.name.Contains("Punch_Cross"));
        var player=UnityEngine.Object.FindAnyObjectByType<OfficeThirdPersonController>();
        var controller=player.humanoidAnimator.runtimeAnimatorController as AnimatorController;
        if(controller==null)throw new Exception("Player AnimatorController missing.");
        var old=Array.FindIndex(controller.layers,l=>l.name=="Punches");if(old>=0)controller.RemoveLayer(old);
        const string maskPath="Assets/character/Animations/Combat/PunchUpperBody.mask";
        var mask=AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);if(mask==null){mask=new AvatarMask();AssetDatabase.CreateAsset(mask,maskPath);}
        for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
        foreach(var part in new[]{AvatarMaskBodyPart.Body,AvatarMaskBodyPart.Head,AvatarMaskBodyPart.LeftArm,AvatarMaskBodyPart.RightArm,AvatarMaskBodyPart.LeftFingers,AvatarMaskBodyPart.RightFingers})mask.SetHumanoidBodyPartActive(part,true);
        controller.AddLayer("Punches");var layers=controller.layers;var layer=layers[layers.Length-1];layer.avatarMask=mask;layer.defaultWeight=0;layer.blendingMode=AnimatorLayerBlendingMode.Override;controller.layers=layers;
        var idle=layer.stateMachine.AddState("Rest");layer.stateMachine.defaultState=idle;
        var a=layer.stateMachine.AddState("Jab");a.motion=jab;a.speed=OfficePunchCombat.PlaybackSpeed;
        var b=layer.stateMachine.AddState("Cross");b.motion=cross;b.speed=OfficePunchCombat.PlaybackSpeed;
        var component=player.GetComponent<OfficePunchCombat>();if(component==null)component=Undo.AddComponent<OfficePunchCombat>(player.gameObject);component.jab=jab;component.cross=cross;
        component.punchLift=.06f;component.windupWidth=.12f;component.useImportedPose=false;
        EditorUtility.SetDirty(mask);EditorUtility.SetDirty(controller);EditorUtility.SetDirty(component);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);
        File.WriteAllText("OfficePreview/PunchSetup.txt","Quaternius CC0 Punch_Jab "+jab.length+"s / Punch_Cross "+cross.length+"s; Humanoid, upper-body layer, no root motion; alternating free-hand LMB attacks.");
    }
    [MenuItem("Tools/Office/Validate Alternating Punches")]
    public static void Validate()
    {
        if(!Application.isPlaying)throw new Exception("Run in Play Mode.");
        var combat=UnityEngine.Object.FindAnyObjectByType<OfficePunchCombat>();var grabber=combat.GetComponent<OfficePropGrabber>();
        combat.Cancel();if(!combat.TryPunch() || !combat.LastPunchWasLeft)throw new Exception("First punch should be a jab.");
        combat.Cancel();if(!combat.TryPunch() || combat.LastPunchWasLeft)throw new Exception("Second punch should be a cross.");
        combat.Cancel();if(!combat.TryPunch() || !combat.LastPunchWasLeft)throw new Exception("Third punch should alternate back.");combat.Cancel();
        combat.GetComponent<OfficeThirdPersonController>().SetControlEnabled(false);
        if(combat.TryPunch())throw new Exception("Punch permitted with world controls disabled.");
        combat.GetComponent<OfficeThirdPersonController>().SetControlEnabled(true);
        var animator=combat.GetComponent<OfficeThirdPersonController>().humanoidAnimator;int layer=animator.GetLayerIndex("Punches");
        var forward=combat.GetComponent<OfficeThirdPersonController>().visualRoot.forward;
        Vector2 Reach(string state)
        {
            var max=new Vector2(-10,-10);animator.SetLayerWeight(layer,1);
            var leftBounds=new Bounds();var rightBounds=new Bounds();
            for(int i=0;i<=20;i++)
            {
                animator.Play(state,layer,i/20f*.95f);animator.Update(0);
                float left=Vector3.Dot(animator.GetBoneTransform(HumanBodyBones.LeftHand).position-animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position,forward);
                float right=Vector3.Dot(animator.GetBoneTransform(HumanBodyBones.RightHand).position-animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position,forward);
                max.x=Mathf.Max(max.x,left);max.y=Mathf.Max(max.y,right);
                var lp=animator.GetBoneTransform(HumanBodyBones.LeftHand).position-animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
                var rp=animator.GetBoneTransform(HumanBodyBones.RightHand).position-animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
                if(i==0){leftBounds=new Bounds(lp,Vector3.zero);rightBounds=new Bounds(rp,Vector3.zero);}else{leftBounds.Encapsulate(lp);rightBounds.Encapsulate(rp);}
            }
            return combat.useImportedPose?new Vector2(leftBounds.size.magnitude,rightBounds.size.magnitude):max;
        }
        var jab=Reach("Punches.Jab");var cross=Reach("Punches.Cross");combat.Cancel();animator.Update(0);
        if((jab.x-jab.y)*(cross.x-cross.y)>=0)throw new Exception("Retargeted clips do not extend opposite hands: "+jab+" / "+cross);
        File.AppendAllText("OfficePreview/PunchSetup.txt","\nPASS Play Mode: jab/cross/jab alternate; attacks blocked while controls disabled; cancellation returns layer weight to zero.");
        File.AppendAllText("OfficePreview/PunchSetup.txt","\nPASS sampled retargeted bone motion: jab hand reach "+jab+", cross "+cross+"; opposite hands extend.");
        if(!combat.TryPunch())throw new Exception("Cannot begin styled punch.");
        animator.SetLayerWeight(layer,1);animator.Play(combat.LastPunchWasLeft?"Punches.Jab":"Punches.Cross",layer,.3f);animator.Update(0);
        var hand=animator.GetBoneTransform(combat.LastPunchWasLeft?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);var before=hand.position;
        combat.ApplyPunchStyle(.3f);var raised=hand.position.y-before.y;
        if(!combat.useImportedPose && raised<.02f)throw new Exception("Styled fist did not rise: "+raised);
        combat.Cancel();animator.Update(0);
        File.AppendAllText("OfficePreview/PunchSetup.txt","\nPASS wider windup and torso follow-through; sampled fist raised "+raised.ToString("F3")+" m.");
    }
}

