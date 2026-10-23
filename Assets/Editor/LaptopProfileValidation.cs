using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class LaptopProfileValidation
{
 [MenuItem("Tools/Office/Validate Laptop Player Profile")]
 public static void Validate()
 {
  if(!Application.isPlaying)throw new Exception("Enter Play first.");
  const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
  var laptop=UnityEngine.Object.FindAnyObjectByType<LaptopDesktop>();var type=typeof(LaptopDesktop);
  Func<string,object> get=n=>type.GetField(n,flags).GetValue(laptop);
  Action<string> call=n=>type.GetMethod(n,flags).Invoke(laptop,null);
  var input=(InputField)get("playerNameInput");
  string[] keys={"Office.PlayerName","Office.CharacterPrefab"};var values=new string[2];var existed=new bool[2];
  for(int i=0;i<2;i++){existed[i]=PlayerPrefs.HasKey(keys[i]);values[i]=PlayerPrefs.GetString(keys[i],"");}
  try
  {
   var mode=type.GetField("mode",flags);mode.SetValue(laptop,Enum.Parse(mode.FieldType,"Desktop"));
   ((GameObject)get("desktopRoot")).SetActive(true);laptop.OpenApp("Выбрать персонажа");
   input.text="   ";call("ChooseCharacter");
   if(PlayerPrefs.GetString(keys[0],"")!=values[0])throw new Exception("Blank name was saved.");
   if(!((Text)get("nameValidation")).text.Contains("Введите имя"))throw new Exception("Missing validation message.");
   type.GetField("characterIndex",flags).SetValue(laptop,1);call("ShowPreview");input.text="  Алексей  ";call("ChooseCharacter");
   if(LaptopDesktop.PlayerName!="Алексей")throw new Exception("Name was not trimmed and saved.");
   string chosen=laptop.characters[1].name;
   if(PlayerPrefs.GetString(keys[1])!=chosen)throw new Exception("Character not saved.");
   type.GetField("characterIndex",flags).SetValue(laptop,0);call("RestorePlayerProfile");
   if((int)get("characterIndex")!=1)throw new Exception("Character not restored.");
   if(!((Text)get("characterDescription")).text.Contains("дресс-кода"))throw new Exception("Description missing.");
   Canvas.ForceUpdateCanvases();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
   ((Camera)get("characterCamera")).Render();
   var canvas=((GameObject)get("desktopRoot")).GetComponent<Canvas>();
   var camera=new GameObject("Profile UI preview").AddComponent<Camera>();camera.transform.position=new Vector3(10000,10000,10000);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=~0;
   var rt=new RenderTexture(1600,1000,24);camera.targetTexture=rt;
   var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;
   try
   {
    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
    var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();System.IO.File.WriteAllBytes("OfficePreview/LaptopPlayerProfile.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
   }
   finally{canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camera.gameObject);}
   Debug.Log("PASS laptop profile: blank names rejected, trimmed name and prefab saved, selection restored, descriptions present.");
  }
  finally{for(int i=0;i<2;i++){if(existed[i])PlayerPrefs.SetString(keys[i],values[i]);else PlayerPrefs.DeleteKey(keys[i]);}PlayerPrefs.Save();}
 }
}

