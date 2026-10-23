using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeStaffTypingSetup
{
    static Transform[] All()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
    static Bounds Bounds(Transform t)
    {
        var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;
    }
    [MenuItem("Tools/Office/Seat Man2 At Computer")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first.");
        var npc=UnityEngine.Object.FindObjectsByType<OfficeStaffPhysics>().First(n=>n.name.Contains("man2"));
        var animator=npc.GetComponentInChildren<Animator>();
        // This scene's model was moved within the original kitchen actor wrapper.
        var visualLocation=new Vector3(24.9f,.03f,12.65f)+Quaternion.Euler(0,230,0)*animator.transform.localPosition;
        var existingTyping=npc.GetComponent<OfficeStaffTyping>();
        var all=All();var keyboard=animator.transform.localPosition.sqrMagnitude<.5f && existingTyping!=null && existingTyping.keyboard!=null?existingTyping.keyboard:all.Where(t=>t.name=="Workstation keyboard").OrderBy(t=>Vector3.Distance(t.position,visualLocation)).First();
        var chair=all.Where(t=>t.name=="Task chair").OrderBy(t=>Vector3.Distance(Bounds(t).center,keyboard.position)).First();
        var cb=Bounds(chair);var bottom=new Vector3(cb.center.x,cb.min.y,cb.center.z);
        var forward=keyboard.position-bottom;forward.y=0;forward.Normalize();
        var target=new Vector3(keyboard.position.x,bottom.y,keyboard.position.z)-forward*.70f;
        chair.position+=target-bottom;
        var old=chair.GetComponent<OfficePushableProp>();if(old!=null){old.keepInitialPoseUntilGrab=true;old.Body.isKinematic=true;EditorUtility.SetDirty(old);EditorUtility.SetDirty(old.Body);}
        npc.transform.SetPositionAndRotation(new Vector3(target.x,.03f,target.z),Quaternion.LookRotation(forward));
        animator.transform.localPosition=Vector3.zero;animator.Rebind();animator.Update(0);
        var modelBounds=TripoHumanoidAnimationSetup.SkinnedBoundsOf(animator.gameObject);
        var localCenter=npc.transform.InverseTransformPoint(modelBounds.center);var localBottom=npc.transform.InverseTransformPoint(new Vector3(modelBounds.center.x,modelBounds.min.y,modelBounds.center.z));
        animator.transform.localPosition-=new Vector3(localCenter.x,localBottom.y,localCenter.z);
        PrefabUtility.RecordPrefabInstancePropertyModifications(animator.transform);EditorUtility.SetDirty(animator.transform);
        var typing=npc.GetComponent<OfficeStaffTyping>();if(typing==null)typing=npc.gameObject.AddComponent<OfficeStaffTyping>();typing.keyboard=keyboard;typing.chair=chair;typing.seatHeight=.49f;
        EditorUtility.SetDirty(typing);EditorUtility.SetDirty(npc.transform);EditorUtility.SetDirty(chair);
        EditorSceneManager.MarkSceneDirty(npc.gameObject.scene);EditorSceneManager.SaveScene(npc.gameObject.scene);
        Directory.CreateDirectory("OfficePreview");File.WriteAllText("OfficePreview/StaffTyping.txt",npc.name+" at "+target+" keyboard "+keyboard.position+" chair "+cb.size);
        Debug.Log("Man2 seated at nearest workstation; typing configured.");
    }
    [MenuItem("Tools/Office/Capture Typing NPC")]
    public static void Capture()
    {
        var typing=UnityEngine.Object.FindAnyObjectByType<OfficeStaffTyping>();var t=typing.transform;
        var animator=typing.GetComponentInChildren<Animator>();
        File.AppendAllText("OfficePreview/StaffTyping.txt","\nHand positions: "+animator.GetBoneTransform(HumanBodyBones.LeftHand).position+" / "+animator.GetBoneTransform(HumanBodyBones.RightHand).position+" keyboard="+typing.keyboard.position+" hips="+animator.GetBoneTransform(HumanBodyBones.Hips).position);
        var camera=new GameObject("Temporary typing preview").AddComponent<Camera>();camera.nearClipPlane=.05f;camera.fieldOfView=48;
        camera.transform.position=t.position+t.right*1.9f+t.forward*1.4f+Vector3.up*1.7f;camera.transform.LookAt(t.position+Vector3.up*.9f);
        var rt=new RenderTexture(1000,850,24);camera.targetTexture=rt;
        try{camera.Render();RenderTexture.active=rt;var image=new Texture2D(1000,850,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,850),0,0);image.Apply();File.WriteAllBytes("OfficePreview/StaffTyping.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
        finally{camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camera.gameObject);}
    }
}
