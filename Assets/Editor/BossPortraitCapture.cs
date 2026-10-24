using System.IO;
using UnityEngine;
using UnityEditor;
public static class BossPortraitCapture
{
 [MenuItem("Tools/Office/Capture Boss Chat Portrait")]
 public static void Capture()
 {
  var boss=Object.FindAnyObjectByType<OfficeBossNpc>();var head=boss.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
  var renderers=boss.GetComponentsInChildren<Renderer>();var layers=new int[renderers.Length];for(int i=0;i<renderers.Length;i++){layers[i]=renderers[i].gameObject.layer;renderers[i].gameObject.layer=30;}
  var camera=new GameObject("Boss portrait capture").AddComponent<Camera>();camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.09f,.15f);camera.fieldOfView=32;
  camera.transform.position=head.position+boss.transform.forward*1.6f+Vector3.up*.14f;camera.transform.LookAt(head.position+Vector3.up*.14f);
  var rt=new RenderTexture(256,256,24);camera.targetTexture=rt;
  try{camera.Render();RenderTexture.active=rt;var texture=new Texture2D(256,256,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,256,256),0,0);texture.Apply();Directory.CreateDirectory("Assets/Resources/Characters");File.WriteAllBytes("Assets/Resources/Characters/BossPortrait.png",texture.EncodeToPNG());Object.DestroyImmediate(texture);}
  finally{for(int i=0;i<renderers.Length;i++)renderers[i].gameObject.layer=layers[i];camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(camera.gameObject);}
  AssetDatabase.Refresh();
 }
}
