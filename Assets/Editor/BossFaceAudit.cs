using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class BossFaceAudit
{
 [MenuItem("Tools/Office/Preview Open Boss Mouth")]
 public static void OpenMouth()
 {
  var motion=Object.FindAnyObjectByType<OfficeBossSpeechMotion>();var r=motion.face;int index=r.sharedMesh.GetBlendShapeIndex(motion.mouthShape);
  var closed=new Mesh();var open=new Mesh();r.SetBlendShapeWeight(index,0);r.BakeMesh(closed,false);r.SetBlendShapeWeight(index,100);r.BakeMesh(open,false);
  float max=0;int moved=0;var v0=closed.vertices;var v1=open.vertices;for(int i=0;i<v0.Length;i++){float d=(r.transform.rotation*(v1[i]-v0[i])).magnitude;if(d>.001f)moved++;max=Mathf.Max(max,d);}
  Debug.Log("Actual baked face deformation: "+moved+" vertices; max "+max+" m");
  // Render the evaluated mesh directly so Editor skinning caches cannot hide the preview.
  var preview=new GameObject("Baked face preview");preview.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);preview.transform.localScale=Vector3.one;
  preview.AddComponent<MeshFilter>().sharedMesh=open;preview.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;
  bool enabled=r.enabled;r.enabled=false;Inspect();r.enabled=enabled;r.SetBlendShapeWeight(index,0);
  Object.DestroyImmediate(preview);Object.DestroyImmediate(open);Object.DestroyImmediate(closed);
 }
 [MenuItem("Tools/Office/Inspect Boss Face")]
 public static void Inspect()
 {
  var boss=Object.FindAnyObjectByType<OfficeBossNpc>();var a=boss.GetComponent<Animator>();var lines=new System.Collections.Generic.List<string>();
  lines.Add("Boss "+boss.name+"; jaw="+a.GetBoneTransform(HumanBodyBones.Jaw));
  foreach(var t in boss.GetComponentsInChildren<Transform>(true))if(t.name.ToLower().Contains("head")||t.name.ToLower().Contains("jaw")||t.name.ToLower().Contains("mouth"))lines.Add("Bone "+t.name+" "+t.position);
  foreach(var r in boss.GetComponentsInChildren<SkinnedMeshRenderer>(true)){lines.Add("Path "+AssetDatabase.GetAssetPath(r.sharedMesh));lines.Add("Mesh "+r.name+" vertices="+r.sharedMesh.vertexCount+" readable="+r.sharedMesh.isReadable);for(int i=0;i<r.sharedMesh.blendShapeCount;i++)lines.Add("Blend "+i+" "+r.sharedMesh.GetBlendShapeName(i));}
  var head=a.GetBoneTransform(HumanBodyBones.Head);var cam=new GameObject("Face preview").AddComponent<Camera>();cam.fieldOfView=32;cam.transform.position=head.position+boss.transform.forward*1.5f+Vector3.up*.1f;cam.transform.LookAt(head.position+Vector3.up*.1f);var skin=boss.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();skin.BakeMesh(baked,false);lines.Add("BAKE "+baked.bounds+" transform "+skin.transform.position+" scale "+skin.transform.lossyScale+" renderer "+skin.bounds);var samples=baked.vertices.Select((v,i)=>new {p=skin.transform.TransformPoint(v),i=i}).Select(v=>new {v.p,v.i,s=cam.WorldToViewportPoint(v.p)*900}).OrderBy(v=>Mathf.Abs(v.s.x-450)+Mathf.Abs(v.s.y-450)).Take(8);foreach(var v in samples){var d=v.p-head.position;lines.Add("Mouth sample "+v.i+" relative="+new Vector3(Vector3.Dot(d,boss.transform.right),Vector3.Dot(d,boss.transform.up),Vector3.Dot(d,boss.transform.forward)));}Object.DestroyImmediate(baked);
var rt=new RenderTexture(900,900,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(900,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,900,900),0,0);tex.Apply();File.WriteAllBytes("OfficePreview/BossFace.png",tex.EncodeToPNG());Object.DestroyImmediate(tex);cam.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(cam.gameObject);
File.WriteAllLines("OfficePreview/BossFaceAudit.txt",lines);Debug.Log(string.Join("\n",lines));
 }
}






