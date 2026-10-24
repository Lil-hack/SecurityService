using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class BossSpeechMotionValidation
{
 static OfficeBossInterview chat;static OfficeThirdPersonController player;static OfficeBossSpeechMotion motion;static Vector3 home;static bool hired;static float peak;static double started,quiet;static Quaternion headStart;static float headChange;static bool captured;
 [MenuItem("Tools/Office/Test Boss Speech Motion")]
 public static void Run()
 {
  if(!Application.isPlaying)throw new Exception("Play first.");
  var panel=UnityEngine.Object.FindAnyObjectByType<EditorPlayControls>();if(panel!=null)UnityEngine.Object.DestroyImmediate(panel.gameObject);
  chat=UnityEngine.Object.FindAnyObjectByType<OfficeBossInterview>();motion=chat.GetComponent<OfficeBossSpeechMotion>();player=GameObject.FindGameObjectWithTag("Player").GetComponent<OfficeThirdPersonController>();
  hired=chat.Hired;typeof(OfficeBossInterview).GetField("<Hired>k__BackingField",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(chat,false);
  player.SetControlEnabled(true);home=player.transform.position;Move(chat.transform.position+chat.transform.forward*1.3f);player.humanoidAnimator.SetFloat("Speed",4);chat.BeginInterview();if(!chat.Active)throw new Exception("Cannot start test conversation.");
  if(player.humanoidAnimator.speed!=0||player.humanoidAnimator.GetFloat("Speed")!=0)throw new Exception("Player animation did not stop in idle.");
  peak=headChange=0;captured=false;started=EditorApplication.timeSinceStartup;quiet=0;headStart=chat.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head).localRotation;EditorApplication.update+=Tick;
 }
 static void Move(Vector3 p){var c=player.GetComponent<CharacterController>();c.enabled=false;player.transform.position=p;c.enabled=true;Physics.SyncTransforms();}
 static void Tick()
 {
  try
  {
   if(!Application.isPlaying){Cleanup();return;}
   if(EditorApplication.timeSinceStartup-started>60)throw new Exception("Speech motion timeout.");
   peak=Mathf.Max(peak,motion.MouthAmount);headChange=Mathf.Max(headChange,Quaternion.Angle(headStart,chat.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head).localRotation));
   if(!captured&&motion.MouthAmount>.55f){captured=true;BossFaceAudit.Inspect();}
   var speech=chat.GetComponent<GemmaSpeech>();
   if(peak>.05f&&!speech.HasPendingSpeech)
   {
    if(quiet==0)quiet=EditorApplication.timeSinceStartup;
    if(EditorApplication.timeSinceStartup-quiet<.7)return;
    if(motion.MouthAmount>.01f)throw new Exception("Mouth stayed open after speech.");
    if(headChange<.1f)throw new Exception("Head did not animate.");
    string result="PASS real Piper voice drives boss mouth; peak="+peak+"; head movement="+headChange+" degrees; mouth closes in silence.";
    File.WriteAllText("OfficePreview/BossSpeechMotionValidation.txt",result);chat.EndInterview();if(player.humanoidAnimator.speed<=0)throw new Exception("Player animator was not resumed.");
    Debug.Log(result+" Player idle frozen and restored after dialogue.");Cleanup();
   }
  }
  catch(Exception e){Debug.LogError(e);Cleanup();}
 }
 static void Cleanup(){EditorApplication.update-=Tick;if(chat!=null){chat.EndInterview();typeof(OfficeBossInterview).GetField("<Hired>k__BackingField",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(chat,hired);}if(player!=null)Move(home);}
}

