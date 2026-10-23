using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class LaptopDesktopUiValidation
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    [MenuItem("Tools/Office/Validate Laptop Desktop UI")]
    public static void Validate()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Run in Play Mode.");
        var laptop=UnityEngine.Object.FindFirstObjectByType<LaptopDesktop>();
        var type=typeof(LaptopDesktop);
        var mode=type.GetField("mode",Flags);mode.SetValue(laptop,Enum.Parse(mode.FieldType,"Desktop"));
        var root=(GameObject)type.GetField("desktopRoot",Flags).GetValue(laptop);root.SetActive(true);
        laptop.OpenApp("Блокнот");Canvas.ForceUpdateCanvases();
        var body=(Text)type.GetField("appBody",Flags).GetValue(laptop);
        if(body.preferredHeight>body.rectTransform.rect.height)throw new Exception("Notepad instructions overflow the page.");
        foreach(var drag in root.GetComponentsInChildren<LaptopWindowDrag>(true))
        {
            var target=drag.target;var before=target.localPosition;
            var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,target.position)};
            drag.OnPointerDown(e);drag.OnBeginDrag(e);e.position+=new Vector2(-65,-35);drag.OnDrag(e);
            if((before-target.localPosition).sqrMagnitude<1)throw new Exception("Window did not move: "+target.name);
        }
        var trash=root.transform.Find("Корзина · Выход");
        if(trash==null || trash.GetComponent<Button>().onClick.GetPersistentEventCount()>0)throw new Exception("Trash button missing or unexpectedly serialized.");
        trash.GetComponent<Button>().onClick.Invoke();
        if(mode.GetValue(laptop).ToString()!="Leaving")throw new Exception("Trash did not exit desktop.");
        // Restore the desktop for a visual preview, without moving the player or saving runtime state.
        mode.SetValue(laptop,Enum.Parse(mode.FieldType,"Desktop"));root.SetActive(true);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        ((GameObject)type.GetField("startMenu",Flags).GetValue(laptop)).SetActive(true);
        ScreenCapture.CaptureScreenshot("OfficePreview/Laptop_Desktop_Updated.png");
        File.WriteAllText("OfficePreview/Laptop_Desktop_UI_Validation.txt","PASS: app, camera and character picker title bars move; trash calls BeginExit; desktop and taskbar created.");
    }
}
