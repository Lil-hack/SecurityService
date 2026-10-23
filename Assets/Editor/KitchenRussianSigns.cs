using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KitchenRussianSigns
{
    [MenuItem("Tools/Office/Russian Kitchen Jokes")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode first.");
        var kitchen=GameObject.Find("Kitchen room");
        var texts=kitchen.GetComponentsInChildren<TextMesh>();
        string[] boards={"Kitchen motto board","Kitchen fridge note","Kitchen break poster"};
        string[] jokes={"КОФЕ — НАШ ТИМЛИД\nБЕЗ НЕГО\nНИКТО НЕ РАБОТАЕТ","ЧУЖОЙ ОБЕД\nНЕ БАГ.\nНЕ ТРОГАЙ.","НЕ МЕШАЙ!\nЯ НЕ ЕМ,\nЯ ЗАГРУЖАЮ\nОБНОВЛЕНИЯ."};
        for(int i=0;i<boards.Length;i++)
        {
            var board=kitchen.transform.Find(boards[i]);var text=texts.OrderBy(t=>Vector3.Distance(t.transform.position,board.position)).First();
            Undo.RecordObject(text,"Russian kitchen joke");text.text=jokes[i];text.fontStyle=FontStyle.Bold;text.font.RequestCharactersInTexture(jokes[i],text.fontSize,text.fontStyle);
            // Font metrics give a stable fit even before TextMesh regenerates its renderer bounds.
            float width=0,height=0;foreach(var line in jokes[i].Split('\n'))
            {float advance=0;foreach(char c in line){if(!text.font.GetCharacterInfo(c,out var glyph,text.fontSize,text.fontStyle))throw new Exception("Missing glyph: "+c);advance+=glyph.advance;}width=Mathf.Max(width,advance);height+=text.font.lineHeight*(text.fontSize/(float)text.font.fontSize);}
            float maxWidth=i==2?board.localScale.z:board.localScale.x,maxHeight=board.localScale.y;
            text.characterSize=Mathf.Min(maxWidth*.82f/Mathf.Max(1,width),maxHeight*.72f/Mathf.Max(1,height))*10;
            text.GetComponent<KitchenWorldText>().Refresh();EditorUtility.SetDirty(text);
        }
        EditorSceneManager.MarkSceneDirty(kitchen.scene);EditorSceneManager.SaveScene(kitchen.scene);
        KitchenWorldTextSetup.Apply();KitchenRoomDetailing.Capture();
        File.WriteAllText("OfficePreview/KitchenRussianSigns.txt","Saved 3 Russian jokes; Cyrillic glyphs checked and font metrics fitted to boards.");
    }
}
