using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeDialogueLabSetup
{
    public const string ScenePath="Assets/Scenes/OfficeDialogueLab.unity";
    const string SourceScene="Assets/Scenes/ExecutiveOffice.unity",Folder="Assets/DialogueLab/Profiles";
    [MenuItem("Tools/Office/Dialogue Lab/Open test scene")]
    public static void Open(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Остановите Play Mode.");EditorSceneManager.SaveOpenScenes();EditorSceneManager.OpenScene(ScenePath);}
    [MenuItem("Tools/Office/Dialogue Lab/Create from game scene")]
    public static string Create()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Остановите Play Mode.");
        EditorSceneManager.SaveOpenScenes();
        if(SceneManager.GetActiveScene().path!=SourceScene)EditorSceneManager.OpenScene(SourceScene);
        var bosses=UnityEngine.Object.FindObjectsByType<OfficeBossInterview>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var people=UnityEngine.Object.FindObjectsByType<OfficeStaffConversation>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.persona!=null).OrderBy(p=>p.persona.id).ToArray();
        if(bosses.Length!=1||people.Length==0)throw new InvalidOperationException("Не найдены игровые диалоги.");
        EnsureFolder(Folder);var list=new System.Collections.Generic.List<OfficeDialogueLabProfile>();
        string path=Folder+"/Boss.asset";
        var profile=AssetDatabase.LoadAssetAtPath<OfficeDialogueLabProfile>(path);
        if(profile==null)
        {
            var source=bosses[0];profile=ScriptableObject.CreateInstance<OfficeDialogueLabProfile>();
            profile.isBoss=true;profile.displayName="Босс · директор";profile.sourceScene=SourceScene;profile.sourceObject=source.name;
            profile.modelPath=source.modelPath;profile.pythonPath=source.pythonPath;
            CopyBossSettings(source,profile);
            profile.portrait=Resources.Load<Texture2D>("Characters/BossPortrait");AssetDatabase.CreateAsset(profile,path);
        }
        list.Add(profile);
        foreach(var source in people)
        {
            path=Folder+"/"+source.persona.id+".asset";profile=AssetDatabase.LoadAssetAtPath<OfficeDialogueLabProfile>(path);
            if(profile==null)
            {
                var persona=UnityEngine.Object.Instantiate(source.persona);persona.name=source.persona.id+"_test";
                AssetDatabase.CreateAsset(persona,Folder+"/"+source.persona.id+"_persona.asset");
                profile=ScriptableObject.CreateInstance<OfficeDialogueLabProfile>();profile.displayName=source.persona.displayName;
                profile.sourceScene=SourceScene;profile.sourceObject=source.name;profile.staff=persona;profile.sourceStaff=source.persona;profile.portrait=source.persona.portrait;
                profile.modelPath=source.modelPath;profile.pythonPath=source.pythonPath;AssetDatabase.CreateAsset(profile,path);
            }
            list.Add(profile);
        }
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("2D UI Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.085f,.13f);camera.transform.position=new Vector3(0,0,-10);camera.gameObject.AddComponent<AudioListener>();
        new GameObject("Main Light").AddComponent<Light>().type=LightType.Directional;
        var lab=new GameObject("Office Dialogue Lab · Boss and all NPCs").AddComponent<OfficeDialogueLab>();lab.profiles=list.ToArray();
        EditorSceneManager.SaveScene(scene,ScenePath);Selection.activeGameObject=lab.gameObject;
        return "Created 2D dialogue lab: "+list.Count+" profiles copied from game scene.";
    }
    static void EnsureFolder(string folder)
    {
        string parent=System.IO.Path.GetDirectoryName(folder).Replace('\\','/');
        if(!AssetDatabase.IsValidFolder(parent))EnsureFolder(parent);
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(folder));
    }
    public static void CopyBossSettings(OfficeBossInterview source,OfficeDialogueLabProfile target)
    {
        foreach(var field in typeof(OfficeDialogueLabProfile.BossSettings).GetFields())
            field.SetValue(target.boss,typeof(OfficeBossInterview).GetField(field.Name).GetValue(source));
        foreach(var field in typeof(OfficeDialogueLabProfile.VoiceSettings).GetFields())
            field.SetValue(target.voice,typeof(OfficeBossInterview).GetField(field.Name).GetValue(source));
    }
    [MenuItem("Tools/Office/Dialogue Lab/Apply saved profiles to game")]
    public static void ApplySavedToGame()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Сохраните тестовые профили и остановите Play Mode перед переносом в игру.");
        var scene=SceneManager.GetSceneByPath(SourceScene);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(SourceScene,OpenSceneMode.Additive);
        try
        {
            foreach(string guid in AssetDatabase.FindAssets("t:OfficeDialogueLabProfile",new[]{Folder}))
            {
                var p=AssetDatabase.LoadAssetAtPath<OfficeDialogueLabProfile>(AssetDatabase.GUIDToAssetPath(guid));
                if(p.isBoss)
                {
                    var target=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OfficeBossInterview>(true)).Single();
                    Undo.RecordObject(target,"Apply dialogue lab boss settings");JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(p.boss),target);JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(p.voice),target);
                    target.modelPath=p.modelPath;target.pythonPath=p.pythonPath;EditorUtility.SetDirty(target);EditorSceneManager.MarkSceneDirty(scene);
                }
                else if(p.sourceStaff!=null)
                {
                    Undo.RecordObject(p.sourceStaff,"Apply dialogue lab NPC settings");string name=p.sourceStaff.name;EditorUtility.CopySerialized(p.staff,p.sourceStaff);p.sourceStaff.name=name;
                    EditorUtility.SetDirty(p.sourceStaff);AssetDatabase.SaveAssetIfDirty(p.sourceStaff);
                    foreach(var target in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OfficeStaffConversation>(true)).Where(c=>c.persona==p.sourceStaff))
                    {Undo.RecordObject(target,"Apply dialogue lab runtime paths");target.modelPath=p.modelPath;target.pythonPath=p.pythonPath;EditorUtility.SetDirty(target);EditorSceneManager.MarkSceneDirty(scene);}
                }
            }
            EditorSceneManager.SaveScene(scene);Debug.Log("Saved dialogue lab profiles applied to game. Game progress was not reset.");
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
