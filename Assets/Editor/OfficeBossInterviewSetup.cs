using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class OfficeBossInterviewSetup
{
 [MenuItem("Tools/Office/Enable Boss Gemma Interview")]
 public static void Apply()
 {
  if(Application.isPlaying)throw new Exception("Stop Play first.");
  var boss=UnityEngine.Object.FindAnyObjectByType<OfficeBossNpc>();if(boss==null)throw new Exception("Office boss not found.");
  if(boss.GetComponent<OfficeBossInterview>()==null)Undo.AddComponent<OfficeBossInterview>(boss.gameObject);
  EditorSceneManager.MarkSceneDirty(boss.gameObject.scene);EditorSceneManager.SaveScene(boss.gameObject.scene);
  Debug.Log("Gemma interview enabled on "+boss.name);
 }
}
