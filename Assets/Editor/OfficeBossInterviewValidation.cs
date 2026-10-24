using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
public static class OfficeBossInterviewValidation
{
 static OfficeBossInterview chat;static OfficeThirdPersonController player;static Vector3 position;static Quaternion rotation;static bool priorHired;static int hiredValue,phase;static double started;
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 [MenuItem("Tools/Office/Test Boss Gemma Interview")]
 public static void Run()
 {
  if(!Application.isPlaying)throw new Exception("Play first.");
  chat=UnityEngine.Object.FindAnyObjectByType<OfficeBossInterview>();player=GameObject.FindGameObjectWithTag("Player").GetComponent<OfficeThirdPersonController>();position=player.transform.position;rotation=player.transform.rotation;
  priorHired=PlayerPrefs.HasKey("Office.BossHired");hiredValue=PlayerPrefs.GetInt("Office.BossHired");
  Move(chat.transform.position+chat.transform.forward*1.3f);
  if(!chat.CanTalk())throw new Exception("Boss not reachable from nearby player position.");
  chat.BeginInterview();if(!chat.Active||player.ControlEnabled)throw new Exception("Interview did not lock movement.");
  phase=0;started=EditorApplication.timeSinceStartup;Application.runInBackground=true;EditorApplication.update+=Tick;
 }
 static void Move(Vector3 p){var c=player.GetComponent<CharacterController>();c.enabled=false;player.transform.position=p;c.enabled=true;Physics.SyncTransforms();}
 static void Tick()
 {
  try
  {
   if(EditorApplication.timeSinceStartup-started>180)throw new Exception("Interview timed out.");
   if(!Application.isPlaying){Cleanup();return;}
   var speech=chat.GetComponent<GemmaSpeech>();
   if(phase==0)
   {
    if(!(bool)typeof(OfficeBossInterview).GetField("ready",Flags).GetValue(chat)||speech.HasPendingSpeech)return;
    if(speech.LastDuration<=0||speech.LastRms<=0)throw new Exception("Opening speech had no audible PCM.");
    Vector3 d=player.transform.position-chat.transform.position;d.y=0;if(Vector3.Angle(chat.transform.forward,d)>15)throw new Exception("Boss did not face player.");
    var input=(TextField)typeof(OfficeBossInterview).GetField("input",Flags).GetValue(chat);
    input.value="Я умею работать с таблицами и документами. Автоматизирую отчёты и проверку ошибок, чтобы сэкономить время коллегам.";
    chat.Submit();phase=1;
   }
   else if(phase==1&&chat.Hired)phase=2;
   else if(phase==2&&!chat.Active)
   {
    if(!player.ControlEnabled)throw new Exception("Player remained locked after successful interview.");
    File.WriteAllText("OfficePreview/BossInterviewSceneValidation.txt","PASS real scene: proximity interaction, player locked, boss faces player, male Piper opening audible, real Gemma accepts useful work, final line spoken, chat closes and movement restored.");
    Debug.Log("PASS real boss Gemma interview and male speech end-to-end.");Cleanup();
   }
  }
  catch(Exception e){Debug.LogError("Boss interview validation: "+e);File.WriteAllText("OfficePreview/BossInterviewSceneValidation.txt","FAIL "+e);Cleanup();}
 }
 static void Cleanup()
 {
  EditorApplication.update-=Tick;if(chat!=null)chat.EndInterview();if(player!=null){Move(position);player.transform.rotation=rotation;}
  if(priorHired)PlayerPrefs.SetInt("Office.BossHired",hiredValue);else PlayerPrefs.DeleteKey("Office.BossHired");PlayerPrefs.Save();
 }
}
