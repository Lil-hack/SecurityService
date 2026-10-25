using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static class OfficeStaffConversationSetup
{
    const string Folder="Assets/OfficeExtension/StaffDialogue";
    [Serializable]sealed class Recipe
    {
        public string id,displayName,personality,preferences,greeting,alreadyPaidGreeting,voicePath;
        public int difficulty;
        public float speechRate,pitchSemitones,rasp,tremble,volumeVariation,volume;
    }
    [Serializable]sealed class Manifest {public Recipe[] personas;}
    [MenuItem("Tools/Office/Add Staff Loan Dialogues")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play before configuring staff conversations.");
        string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();
        Directory.CreateDirectory(Folder+"/Portraits");AssetDatabase.Refresh();
        var recipes=JsonUtility.FromJson<Manifest>(File.ReadAllText("Tools/Gemma/Prompts/office_staff_defaults.json")).personas;
        var audio=JsonUtility.FromJson<Manifest>(File.ReadAllText("Assets/Resources/Audio/StaffLoans/manifest.json")).personas;
        var personas=new OfficeStaffPersona[recipes.Length];
        for(int i=0;i<recipes.Length;i++)
        {
            var recipe=recipes[i];string path=Folder+"/"+recipe.id+".asset";
            var persona=AssetDatabase.LoadAssetAtPath<OfficeStaffPersona>(path);
            if(persona==null){persona=ScriptableObject.CreateInstance<OfficeStaffPersona>();JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(recipe),persona);AssetDatabase.CreateAsset(persona,path);}
            var baked=audio.Single(p=>p.id==recipe.id);persona.preparedVoicePath=baked.voicePath;persona.preparedRate=baked.speechRate;persona.preparedPitch=baked.pitchSemitones;persona.preparedGreeting=baked.greeting;persona.preparedPaid=baked.alreadyPaidGreeting;
            persona.greetingClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/StaffLoans/"+recipe.id+"_greeting.wav");
            persona.alreadyPaidClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/StaffLoans/"+recipe.id+"_paid.wav");
            persona.thinkingClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/StaffLoans/"+recipe.id+"_thinking.wav");
            if(persona.greetingClip==null||persona.thinkingClip==null||persona.alreadyPaidClip==null)throw new FileNotFoundException("Generate staff voice clips with Tools/Piper/build_staff_loans.py first");
            persona.accent=i==0?new Color(.25f,.77f,.62f):i==1?new Color(.61f,.54f,.92f):i==2?new Color(.88f,.66f,.31f):new Color(.91f,.46f,.64f);
            CapturePortrait(persona);
            EditorUtility.SetDirty(persona);personas[i]=persona;
            string prefabPath="Assets/character/officeperson/"+recipe.id+"/Staff.prefab";var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
            try{Configure(prefab,persona);PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
        AssetDatabase.SaveAssets();
        foreach(string path in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/ExecutiveOffice.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path);int configured=0;
            var loaded=recipes.Select(r=>AssetDatabase.LoadAssetAtPath<OfficeStaffPersona>(Folder+"/"+r.id+".asset")).ToArray();
            foreach(var npc in Object.FindObjectsByType<OfficeStaffPhysics>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var persona=loaded.FirstOrDefault(p=>npc.name.StartsWith("NPC "+p.id+" "));
                if(persona==null)continue;Configure(npc.gameObject,persona);configured++;
            }
            if(configured!=4)throw new InvalidOperationException("Expected four staff NPCs in "+scene.name+", got "+configured);
            foreach(var board in Object.FindObjectsByType<OfficeTeamDashboard>(FindObjectsInactive.Include,FindObjectsSortMode.None))board.RefreshDisplay();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<OfficeStaffPersona>(Folder+"/man1.asset");
        Debug.Log("Four staff loan dialogues configured: Gena 2/$100, Alla 3/$200, Boris 4/$500, Zhanna 5/$1000. Four distinct Piper voices and instant prepared greetings; Jev primary, Gemma fallback and generated replies. Trust and payouts persist.");
    }
    static void Configure(GameObject actor,OfficeStaffPersona persona)
    {
        var dialogue=actor.GetComponent<OfficeStaffConversation>();if(dialogue==null)dialogue=actor.AddComponent<OfficeStaffConversation>();dialogue.persona=persona;
        if(actor.GetComponent<OfficeStaffSpeechMotion>()==null)actor.AddComponent<OfficeStaffSpeechMotion>();
        EditorUtility.SetDirty(dialogue);if(PrefabUtility.IsPartOfPrefabInstance(dialogue))PrefabUtility.RecordPrefabInstancePropertyModifications(dialogue);
    }
    static void CapturePortrait(OfficeStaffPersona persona)
    {
        var stage=new GameObject("Temporary staff dialogue portrait"){hideFlags=HideFlags.HideAndDontSave};stage.transform.position=new Vector3(0,1000,0);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/character/officeperson/"+persona.id+"/Staff.prefab");
        var actor=Object.Instantiate(source,stage.transform);actor.transform.localPosition=Vector3.zero;actor.transform.localRotation=Quaternion.identity;
        foreach(var t in actor.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
        var animator=actor.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(.1f);
        var head=animator.GetBoneTransform(HumanBodyBones.Head);var right=animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);var left=animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Vector3 direction=Vector3.Cross(right.position-left.position,Vector3.up).normalized;
        var skin=actor.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>!r.name.StartsWith("Toon"));
        Vector3 focus=head.position;focus.y=Mathf.Lerp(head.position.y,skin.bounds.max.y,.3f);
        var camera=new GameObject("Staff portrait camera").AddComponent<Camera>();camera.transform.SetParent(stage.transform,false);camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.085f,.14f,1);camera.orthographic=true;camera.orthographicSize=.39f;camera.nearClipPlane=.01f;camera.farClipPlane=10;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;camera.transform.position=focus+direction*2+Vector3.up*.04f;camera.transform.LookAt(focus);
        var light=new GameObject("Portrait light").AddComponent<Light>();light.transform.SetParent(stage.transform,false);light.type=LightType.Directional;light.cullingMask=1<<30;light.color=new Color(1,.9f,.8f);light.intensity=2;light.transform.rotation=Quaternion.LookRotation(-direction+Vector3.down*.6f);
        var rt=RenderTexture.GetTemporary(256,256,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;var texture=new Texture2D(256,256,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,256,256),0,0);texture.Apply();
            string path=Folder+"/Portraits/"+persona.id+".png";File.WriteAllBytes(path,texture.EncodeToPNG());AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.mipmapEnabled=false;importer.maxTextureSize=256;importer.SaveAndReimport();persona.portrait=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(texture);Object.DestroyImmediate(stage);}
    }
}
