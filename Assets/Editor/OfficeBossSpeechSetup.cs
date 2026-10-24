using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class OfficeBossSpeechSetup
{
 const string Path="Assets/character/boss/BossSpeechFace.asset";
 [MenuItem("Tools/Office/Add Boss Speech Animation")]
 public static void Apply()
 {
  if(Application.isPlaying)throw new Exception("Stop Play first.");
  var boss=UnityEngine.Object.FindAnyObjectByType<OfficeBossNpc>();var r=boss.GetComponentInChildren<SkinnedMeshRenderer>();
  if(true)
  {
   string sourcePath=AssetDatabase.GetAssetPath(r.sharedMesh);var importer=AssetImporter.GetAtPath(sourcePath) as ModelImporter;
   bool readable=importer!=null&&importer.isReadable;
   if(importer!=null&&!readable){importer.isReadable=true;importer.SaveAndReimport();}
   r=boss.GetComponentInChildren<SkinnedMeshRenderer>();var source=r.sharedMesh;var mesh=UnityEngine.Object.Instantiate(source);mesh.name="Boss face with speech morph";
   mesh.ClearBlendShapes();var posed=mesh.vertices;var delta=new Vector3[mesh.vertexCount];var zero=new Vector3[mesh.vertexCount];var weights=mesh.boneWeights;var bind=mesh.bindposes;
   var head=boss.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);int count=0;
   for(int i=0;i<delta.Length;i++)
   {
    var w=weights[i];
    var m0=r.bones[w.boneIndex0].localToWorldMatrix*bind[w.boneIndex0];
    var m1=r.bones[w.boneIndex1].localToWorldMatrix*bind[w.boneIndex1];
    var m2=r.bones[w.boneIndex2].localToWorldMatrix*bind[w.boneIndex2];
    var m3=r.bones[w.boneIndex3].localToWorldMatrix*bind[w.boneIndex3];
    Matrix4x4 skin=new Matrix4x4();for(int k=0;k<16;k++)skin[k]=m0[k]*w.weight0+m1[k]*w.weight1+m2[k]*w.weight2+m3[k]*w.weight3;
    var world=skin.MultiplyPoint3x4(posed[i]);var relative=world-head.position;
    float x=Vector3.Dot(relative,boss.transform.right),y=Vector3.Dot(relative,boss.transform.up),z=Vector3.Dot(relative,boss.transform.forward);
    float width=Mathf.Exp(-Mathf.Pow(x/.19f,4));float front=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.06f,.16f,z));
    // Keep the nose and eyes fixed; only the lower lip/chin drop.
    float lower=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.09f,.025f,y))*Mathf.Exp(-Mathf.Pow((y+.005f)/.15f,2));
    float upper=Mathf.Exp(-Mathf.Pow((y-.09f)/.016f,2));
    float lip=width*front;var movement=boss.transform.up*((-.085f*lower+.012f*upper)*lip)+boss.transform.forward*(.022f*lower*lip);
    if(movement.sqrMagnitude<.00000001f)continue;
    delta[i]=skin.inverse.MultiplyVector(movement);count++;
   }
   mesh.AddBlendShapeFrame("BossSpeechOpen",100,delta,zero,zero);
   var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Path);
   if(existing==null){AssetDatabase.CreateAsset(mesh,Path);}else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
   r.sharedMesh=mesh;EditorUtility.SetDirty(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);
   if(importer!=null&&!readable){importer.isReadable=false;importer.SaveAndReimport();}
   Debug.Log("Boss mouth morph vertices: "+count);
  }
  var headBone=boss.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
  var oldMouth=headBone.Find("Speech mouth interior");if(oldMouth!=null)UnityEngine.Object.DestroyImmediate(oldMouth.gameObject);
  var motion=boss.GetComponent<OfficeBossSpeechMotion>();if(motion==null)motion=Undo.AddComponent<OfficeBossSpeechMotion>(boss.gameObject);motion.face=r;motion.mouthStrength=100;EditorUtility.SetDirty(motion);
  AssetDatabase.DeleteAsset("Assets/character/boss/BossMouthOpening.asset");
  AssetDatabase.DeleteAsset("Assets/character/boss/BossMouthInterior.mat");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(boss.gameObject.scene);EditorSceneManager.SaveScene(boss.gameObject.scene);
  r.SetBlendShapeWeight(r.sharedMesh.GetBlendShapeIndex("BossSpeechOpen"),100);BossFaceAudit.Inspect();r.SetBlendShapeWeight(r.sharedMesh.GetBlendShapeIndex("BossSpeechOpen"),0);
  Debug.Log("Boss mouth/head animation linked to real Piper audio.");
 }
}


