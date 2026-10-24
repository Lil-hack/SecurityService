using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Cursor = UnityEngine.Cursor;
public static class BossChatPreview
{
 [MenuItem("Tools/Office/Preview Boss Messenger")]
 public static void Preview()
 {
  if(!Application.isPlaying)throw new Exception("Enter Play first.");
  var debug=UnityEngine.Object.FindAnyObjectByType<EditorPlayControls>();if(debug!=null)UnityEngine.Object.DestroyImmediate(debug.gameObject);
  var chat=UnityEngine.Object.FindAnyObjectByType<OfficeBossInterview>();const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
  var type=typeof(OfficeBossInterview);var root=(VisualElement)type.GetField("overlay",flags).GetValue(chat);root.style.display=DisplayStyle.Flex;
  var history=(ScrollView)type.GetField("history",flags).GetValue(chat);history.Clear();var method=type.GetMethod("AddMessage",flags);
  method.Invoke(chat,new object[]{"Директор","Понимаю, что начать бывает непросто. Можете привести пример, как вы видите свою роль в организации рабочего процесса?"});
  method.Invoke(chat,new object[]{"Вы","Я ничего не умею."});
  method.Invoke(chat,new object[]{"Директор","Важно не только то, что вы умеете, но и то, как готовы учиться и подходить к задачам. Здесь ценятся инициатива и желание разобраться в процессах."});
  ((Label)type.GetField("status",flags).GetValue(chat)).text="Напишите сообщение директору";
  ((Button)type.GetField("retry",flags).GetValue(chat)).style.display=DisplayStyle.None;
  var field=(TextField)type.GetField("input",flags).GetValue(chat);field.value="";field.Blur();
  var player=GameObject.FindGameObjectWithTag("Player").GetComponent<OfficeThirdPersonController>();player.SetControlEnabled(false);
  Cursor.lockState=CursorLockMode.None;Cursor.visible=true;Application.runInBackground=true;
  for(int i=0;i<3;i++)method.Invoke(chat,new object[]{"Директор","Расскажите подробнее, как вы готовы помогать коллегам и учиться новому в нашей команде."});
  root.schedule.Execute(()=>{history.scrollOffset=Vector2.zero;ScreenCapture.CaptureScreenshot("OfficePreview/BossMessenger.png");}).StartingIn(1000);
  Debug.Log("Boss messenger preview ready.");
 }
}



