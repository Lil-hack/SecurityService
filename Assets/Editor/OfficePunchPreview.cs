using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class OfficePunchPreview
{
    [MenuItem("Tools/Office/Capture Punch Sequence")]
    public static void Capture()
    {
        var combat=Object.FindAnyObjectByType<OfficePunchCombat>();var controller=combat.GetComponent<OfficeThirdPersonController>();var animator=controller.humanoidAnimator;
        var parts=controller.visualRoot.GetComponentsInChildren<Transform>();var layers=parts.Select(t=>t.gameObject.layer).ToArray();
        var skins=controller.visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>();var matrices=skins.Select(s=>s.forceMatrixRecalculationPerRender).ToArray();
        var go=new GameObject("Punch pose camera");var camera=go.AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.16f,.21f);camera.fieldOfView=42;
        var center=controller.transform.position+Vector3.up*1.1f;camera.transform.position=center+controller.visualRoot.forward*2.8f+controller.visualRoot.right*1.5f+Vector3.up*.4f;camera.transform.LookAt(center);
        var lightObject=new GameObject("Punch pose key");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<31;light.intensity=1.8f;lightObject.transform.rotation=Quaternion.Euler(25,135,0);
        var rt=new RenderTexture(512,512,24);var old=RenderTexture.active;var atlas=new Texture2D(1536,1024,TextureFormat.RGB24,false);
        try
        {
            foreach(var p in parts)p.gameObject.layer=31;foreach(var s in skins)s.forceMatrixRecalculationPerRender=true;
            camera.targetTexture=rt;
            for(int row=0;row<2;row++)
            {
                combat.Cancel();combat.TryPunch();int layer=animator.GetLayerIndex("Punches");
                for(int col=0;col<3;col++)
                {
                    float phase=new[]{.18f,.46f,.62f}[col];animator.SetLayerWeight(layer,1);animator.Play(combat.LastPunchWasLeft?"Punches.Jab":"Punches.Cross",layer,phase);animator.Update(0);combat.ApplyPunchStyle(phase);
                    camera.Render();RenderTexture.active=rt;atlas.ReadPixels(new Rect(0,0,512,512),col*512,(1-row)*512);
                }
            }
            atlas.Apply();File.WriteAllBytes("OfficePreview/Punch_Sequence.png",atlas.EncodeToPNG());
        }
        finally
        {
            combat.Cancel();animator.Update(0);for(int i=0;i<parts.Length;i++)parts[i].gameObject.layer=layers[i];for(int i=0;i<skins.Length;i++)skins[i].forceMatrixRecalculationPerRender=matrices[i];
            camera.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(atlas);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);Object.DestroyImmediate(lightObject);
        }
    }
}
