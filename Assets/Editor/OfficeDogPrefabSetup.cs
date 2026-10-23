using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class OfficeDogPrefabSetup
{
    const string Folder="Assets/character/officeperson/dog/";
    const string Model=Folder+"tripo_rigging_671adc53-cf71-4c6f-b018-1e0965674dde_meshopt.glb";
    [MenuItem("Tools/Office/Prepare Rigged Dog Prefab")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first.");
        if(!File.Exists(Model)) { OfficeDogStandalone.Validate();Debug.Log("Standalone dog prefab already prepared; GLB is no longer required.");return; }
        AssetDatabase.ImportAsset(Model,ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(Folder+"Textures/Dog_Albedo.jpg");
        var ti=(TextureImporter)AssetImporter.GetAtPath(Folder+"Textures/Dog_Albedo.jpg");ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.maxTextureSize=2048;ti.SaveAndReimport();
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Model);if(source==null)throw new Exception("GLB model did not import.");
        var root=new GameObject("Office Dog");var dog=(GameObject)PrefabUtility.InstantiatePrefab(source);dog.transform.SetParent(root.transform,false);
        try
        {
            dog.name="Dog Rig";
            var skins=dog.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if(skins.Length==0 || skins.Any(s=>s.bones.Length==0 || s.bones.Any(b=>b==null)))throw new Exception("Skin/bone bindings missing.");
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Dog_URP.mat");
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,Folder+"Dog_URP.mat");}
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Textures/Dog_Albedo.jpg"));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.22f);mat.SetFloat("_Metallic",0);EditorUtility.SetDirty(mat);
            foreach(var skin in skins){skin.sharedMaterials=skin.sharedMaterials.Select(_=>mat).ToArray();skin.updateWhenOffscreen=true;}
            var bounds=TripoHumanoidAnimationSetup.SkinnedBoundsOf(root);dog.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            var avatar=AvatarBuilder.BuildGenericAvatar(root,"");avatar.name="Dog Generic Rig";
            if(!avatar.isValid)throw new Exception("Generic Avatar invalid.");
            string avatarPath=Folder+"Dog_GenericAvatar.asset";var existing=AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath);
            if(existing==null)AssetDatabase.CreateAsset(avatar,avatarPath);else{EditorUtility.CopySerialized(avatar,existing);UnityEngine.Object.DestroyImmediate(avatar);avatar=existing;}
            var animator=root.AddComponent<Animator>();animator.avatar=avatar;animator.applyRootMotion=false;
            bounds=TripoHumanoidAnimationSetup.SkinnedBoundsOf(root);var box=root.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size;
            // Verify skin responds to a bone, then restore the bind pose.
            var skinTest=skins[0];var bone=skinTest.bones.First(b=>b.childCount>0);var rotation=bone.localRotation;
            var before=new Mesh();var after=new Mesh();skinTest.BakeMesh(before);bone.localRotation*=Quaternion.Euler(0,20,0);skinTest.BakeMesh(after);bone.localRotation=rotation;
            var a=before.vertices;var b=after.vertices;float movement=0;for(int i=0;i<a.Length;i++)movement=Mathf.Max(movement,Vector3.Distance(a[i],b[i]));
            UnityEngine.Object.DestroyImmediate(before);UnityEngine.Object.DestroyImmediate(after);if(movement<.0001f)throw new Exception("Skin does not deform with its rig.");
            PrefabUtility.SaveAsPrefabAsset(root,Folder+"Office_Dog.prefab");AssetDatabase.SaveAssets();
            Directory.CreateDirectory("OfficePreview");File.WriteAllText("OfficePreview/DogRig.txt","PASS Generic Avatar, "+skins.Sum(s=>s.bones.Length)+" bound bones, textured URP skin, deformation verified ("+movement+"), collider and ground-level pivot. No animation clips in source GLB.");
            Capture(root,bounds);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        Debug.Log("Rigged textured dog prefab saved: "+Folder+"Office_Dog.prefab");
    }
    public static void Capture(GameObject root,Bounds bounds,string output="OfficePreview/Dog_Prefab.png")
    {
        foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
        var camera=new GameObject("Dog preview").AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.19f,.23f);camera.nearClipPlane=.01f;
        camera.transform.position=bounds.center+new Vector3(-1,.45f,-1)*bounds.size.magnitude;camera.transform.LookAt(bounds.center);
        var light=new GameObject("Dog key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(30,-40,0);
        var rt=new RenderTexture(900,750,24);camera.targetTexture=rt;
        try{camera.Render();RenderTexture.active=rt;var image=new Texture2D(900,750,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,750),0,0);image.Apply();File.WriteAllBytes(output,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
        finally{camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(light.gameObject);}
    }
}

