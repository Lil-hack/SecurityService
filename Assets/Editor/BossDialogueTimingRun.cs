using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
public static class BossDialogueTimingRun
{
 [Serializable] public class Event {public double seconds;public int turn;public string kind,detail;}
 [Serializable] public class Report {public string started;public bool success;public string failure;public List<Event> events=new List<Event>();}
 static Report report;static OfficeBossInterview chat;static OfficeThirdPersonController player;static GemmaSpeech speech;
 static Vector3 home;static Quaternion rotation;static bool hadHired,oldHired;static int savedHired,phase,turn;static double started,eligibleSince;static float originalVolume;
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static readonly string[] Messages={"Я ничего не умею. Просто хочу получать зарплату.","Я умею работать с таблицами и документами. Автоматизирую отчёты и проверку ошибок, чтобы коллеги тратили меньше времени на рутину и получали точные данные."};
 [MenuItem("Tools/Office/Run Timed Boss Conversation")]
 public static void Run()
 {
  if(!Application.isPlaying)throw new Exception("Enter Play first.");if(report!=null)throw new Exception("Timing run already active.");
  var debug=UnityEngine.Object.FindAnyObjectByType<EditorPlayControls>();if(debug!=null)UnityEngine.Object.DestroyImmediate(debug.gameObject);
  chat=UnityEngine.Object.FindAnyObjectByType<OfficeBossInterview>();player=GameObject.FindGameObjectWithTag("Player").GetComponent<OfficeThirdPersonController>();
  home=player.transform.position;rotation=player.transform.rotation;originalVolume=AudioListener.volume;
  hadHired=PlayerPrefs.HasKey("Office.BossHired");savedHired=PlayerPrefs.GetInt("Office.BossHired");oldHired=chat.Hired;
  typeof(OfficeBossInterview).GetField("<Hired>k__BackingField",Flags).SetValue(chat,false);player.SetControlEnabled(true);
  bool found=false;foreach(var offset in new[]{new Vector3(0,0,1.5f),new Vector3(1.5f,0,0),new Vector3(-1.5f,0,0),new Vector3(0,0,-1.5f),new Vector3(.8f,0,.8f)})
  {Move(chat.transform.position+chat.transform.TransformDirection(offset));if(chat.CanTalk()){found=true;break;}}
  if(!found){Move(home);typeof(OfficeBossInterview).GetField("<Hired>k__BackingField",Flags).SetValue(chat,oldHired);throw new Exception("No reachable interaction position.");}
  speech=chat.GetComponent<GemmaSpeech>();if(speech==null)speech=chat.gameObject.AddComponent<GemmaSpeech>();
  phase=turn=0;eligibleSince=0;started=EditorApplication.timeSinceStartup;report=new Report{started=DateTime.Now.ToString("O")};
  chat.TimingEvent+=Record;speech.TimingEvent+=Record;EditorApplication.update+=Tick;chat.BeginInterview();Record("voice_preloaded",speech.Ready.ToString());Record("test_begin", "Gemma process starts at interaction; Piper preloaded at scene start. 1 second scripted thinking pause.");
 }
 static void Move(Vector3 p){var c=player.GetComponent<CharacterController>();c.enabled=false;player.transform.position=p;c.enabled=true;Physics.SyncTransforms();}
 static void Record(string kind,string detail){if(report!=null)report.events.Add(new Event{seconds=EditorApplication.timeSinceStartup-started,turn=turn,kind=kind,detail=detail});}
 static void Tick()
 {
  if(report==null)return;
  try
  {
   if(!Application.isPlaying)throw new Exception("Play mode interrupted.");
   if(EditorApplication.timeSinceStartup-started>240)throw new Exception("Timed out after 240 seconds.");
   if(!chat.Active)
   {
    if(chat.Hired&&phase==2){report.success=true;Record("control_restored",player.ControlEnabled.ToString());Finish();return;}
    throw new Exception("Dialogue ended unexpectedly.");
   }
   bool ready=(bool)typeof(OfficeBossInterview).GetField("ready",Flags).GetValue(chat);bool busy=(bool)typeof(OfficeBossInterview).GetField("busy",Flags).GetValue(chat);
   if(phase<2&&ready&&!busy&&!speech.HasPendingSpeech)
   {
    if(eligibleSince==0){eligibleSince=EditorApplication.timeSinceStartup;Record("input_available", "");}
    if(EditorApplication.timeSinceStartup-eligibleSince<1)return;
    turn=phase+1;var input=(TextField)typeof(OfficeBossInterview).GetField("input",Flags).GetValue(chat);input.value=Messages[phase];
    chat.Submit();phase++;eligibleSince=0;
   }
  }
  catch(Exception e){report.failure=e.ToString();Record("failure",e.Message);Finish();}
 }
 static void Finish()
 {
  EditorApplication.update-=Tick;chat.TimingEvent-=Record;speech.TimingEvent-=Record;
  Directory.CreateDirectory("OfficePreview");File.WriteAllText("OfficePreview/BossDialogueTiming.json",JsonUtility.ToJson(report,true));
  Debug.Log("Boss timing run "+(report.success?"PASS":"FAILED")+"; duration="+(EditorApplication.timeSinceStartup-started).ToString("F2")+" s. See OfficePreview/BossDialogueTiming.json");
  chat.EndInterview();Move(home);player.transform.rotation=rotation;AudioListener.volume=originalVolume;
  typeof(OfficeBossInterview).GetField("<Hired>k__BackingField",Flags).SetValue(chat,oldHired);
  if(hadHired)PlayerPrefs.SetInt("Office.BossHired",savedHired);else PlayerPrefs.DeleteKey("Office.BossHired");PlayerPrefs.Save();report=null;
 }
}
