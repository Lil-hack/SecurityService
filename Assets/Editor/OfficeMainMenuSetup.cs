using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static class OfficeMainMenuSetup
{
    public const string ScenePath="Assets/Scenes/MainMenu.unity";
    const string GamePath="Assets/Scenes/ExecutiveOffice.unity";
    [MenuItem("Tools/Office/Create Main Menu Scene")]
    public static void Create()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        if(File.Exists(ScenePath))throw new InvalidOperationException("MainMenu already exists; open it instead of replacing it.");
        var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(current.isDirty)EditorSceneManager.SaveScene(current);
        if(!AssetDatabase.CopyAsset(GamePath,ScenePath))throw new Exception("Could not copy office scene.");
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var player=Object.FindAnyObjectByType<OfficeThirdPersonController>();
        var laptop=Object.FindAnyObjectByType<LaptopDesktop>();
        var prefabs=laptop.characters;
        var hero=new GameObject("Menu hero - Skuf");
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[0]);visual.transform.SetParent(hero.transform,false);
        OfficeThirdPersonController.PlaceHumanoid(visual,hero.transform,0);
        hero.transform.SetPositionAndRotation(new Vector3(12.4f,.04f,-2.3f),Quaternion.Euler(0,-35,0));
        player.gameObject.SetActive(false);
        // Keep the office cast, lighting and props, but don't start chat/voice workers behind the menu.
        foreach(var script in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(script==null)continue;
            string type=script.GetType().Name;
            if(new[]{"OfficeBossInterview","OfficeBossSpeechMotion","GemmaSpeech","GemmaTestChat","LaptopDesktop","OfficeCameraOrbit"}.Contains(type))Object.DestroyImmediate(script);
        }
        foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None)){camera.enabled=false;camera.tag="Untagged";}
        foreach(var listener in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include,FindObjectsSortMode.None))listener.enabled=false;
        var cameraObject=new GameObject("Main menu camera");var view=cameraObject.AddComponent<Camera>();view.tag="MainCamera";view.fieldOfView=58;view.nearClipPlane=.05f;view.farClipPlane=100;
        view.transform.position=new Vector3(9f,2.15f,-4.65f);view.transform.LookAt(new Vector3(14.4f,1.35f,.2f));
        view.gameObject.AddComponent<AudioListener>();view.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        var menu=new GameObject("Main menu").AddComponent<OfficeMainMenu>();menu.backgroundCamera=view;menu.heroAnimator=visual.GetComponent<Animator>();
        var people=Object.FindObjectsByType<OfficeStaffPhysics>(FindObjectsSortMode.None);
        Place(people,"NPC girl1 - Office",new Vector3(18.9f,.03f,2.8f),180);
        Place(people,"NPC girl2 - Kitchen",new Vector3(12.6f,.03f,2.3f),130);
        var walker=Place(people,"NPC man1 - Office",new Vector3(14.2f,.03f,-1.8f),90);
        if(walker!=null){var patrol=walker.GetComponent<OfficeStaffPatrol>();if(patrol!=null)patrol.waypoints=new[]{new Vector3(14.2f,.03f,-1.8f),new Vector3(17f,.03f,-1.8f)};}
        var typist=Place(people,"NPC man2 - Kitchen",new Vector3(16.7f,.03f,-1.08f),0);
        if(typist!=null)
        {
            var typing=typist.GetComponent<OfficeStaffTyping>();
            if(typing!=null)
            {
                var all=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
                typing.keyboard=all.Where(t=>t.name=="Workstation keyboard").OrderBy(t=>Vector3.Distance(t.position,typist.transform.position)).FirstOrDefault();
                typing.chair=all.Where(t=>t.name=="Task chair").OrderBy(t=>Vector3.Distance(t.position,typist.transform.position)).FirstOrDefault();
            }
        }
        Polish();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Selection.activeGameObject=menu.gameObject;
        Debug.Log("MainMenu created: live office cast, settings, verified/resumable Gemma download; first build scene.");
    }
    static OfficeStaffPhysics Place(OfficeStaffPhysics[] people,string name,Vector3 position,float yaw)
    {var actor=people.FirstOrDefault(p=>p.name==name);if(actor!=null)actor.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));return actor;}
    [MenuItem("Tools/Office/Polish Main Menu Scene")]
    public static void Polish()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath);
        var menu=Object.FindAnyObjectByType<OfficeMainMenu>();menu.heroAnimator.transform.parent.rotation=Quaternion.Euler(0,145,0);
        foreach(var prop in Object.FindObjectsByType<OfficePushableProp>())
        {prop.keepInitialPoseUntilGrab=true;prop.Body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;prop.Body.isKinematic=true;EditorUtility.SetDirty(prop);EditorUtility.SetDirty(prop.Body);}
        var typing=Object.FindAnyObjectByType<OfficeStaffTyping>();
        if(typing!=null)
        {
            var all=Object.FindObjectsByType<Transform>();
            typing.keyboard=all.Where(t=>t.name=="Workstation keyboard").OrderBy(t=>Vector3.Distance(t.position,typing.transform.position)).First();
            typing.chair=all.Where(t=>t.name=="Task chair").OrderBy(t=>Vector3.Distance(t.GetComponentInChildren<Renderer>().bounds.center,typing.keyboard.position)).First();
            var renderers=typing.chair.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            typing.chair.position+=new Vector3(typing.transform.position.x,.03f,typing.transform.position.z)-new Vector3(b.center.x,b.min.y,b.center.z);
        }
        CaptureCards(new[]{"skuf/Skuf","altgirl/Altgirl","tubik/Tubik","shtrih/Shtrih"}.Select(p=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/character/"+p+"_Textured.prefab")).ToArray());
        var scenes=new[]{new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene(GamePath,true)};
        EditorBuildSettings.globalScenes=scenes.Concat(EditorBuildSettings.globalScenes.Where(s=>s.path!=ScenePath&&s.path!=GamePath)).ToArray();
        var profile=AssetDatabase.LoadAssetAtPath<BuildProfile>(WindowsBuildProfileSetup.ProfilePath);
        if(profile!=null){profile.scenes=scenes;EditorUtility.SetDirty(profile);}
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);EditorSceneManager.SaveScene(menu.gameObject.scene);
        AssetDatabase.SaveAssets();
    }
    static void CaptureCards(GameObject[] prefabs)
    {
        var stage=new GameObject("Temporary menu portraits");stage.transform.position=new Vector3(0,1000,0);
        var view=new GameObject("Portrait camera").AddComponent<Camera>();view.transform.SetParent(stage.transform,false);view.cullingMask=1<<30;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.025f,.075f,.12f);view.orthographic=true;
        view.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var key=new GameObject("Portrait key").AddComponent<Light>();key.transform.SetParent(stage.transform,false);key.type=LightType.Directional;key.intensity=2;key.color=new Color(1,.86f,.68f);key.cullingMask=1<<30;key.transform.rotation=Quaternion.Euler(35,-30,0);
        var fill=new GameObject("Portrait fill").AddComponent<Light>();fill.transform.SetParent(stage.transform,false);fill.type=LightType.Directional;fill.intensity=.8f;fill.color=new Color(.45f,.75f,1);fill.cullingMask=1<<30;fill.transform.rotation=Quaternion.Euler(20,150,0);
        var cast=new GameObject[4];
        try
        {
            for(int i=0;i<4;i++)
            {
                var holder=new GameObject("Portrait actor");holder.transform.SetParent(stage.transform,false);
                cast[i]=Object.Instantiate(prefabs[i],holder.transform);OfficeThirdPersonController.PlaceHumanoid(cast[i],holder.transform,30);
                var animator=cast[i].GetComponent<Animator>();if(animator!=null){animator.SetBool("Grounded",true);animator.SetFloat("Speed",0);animator.Play("Locomotion",0,i*.2f);animator.Update(.1f);}
                holder.transform.localPosition=new Vector3((i-1.5f)*1.1f,0,0);holder.transform.localRotation=Quaternion.Euler(0,90,0);holder.SetActive(i==0);
            }
            cast[0].transform.parent.localPosition=Vector3.zero;
            view.transform.localPosition=new Vector3(0,1.2f,-5);view.transform.LookAt(stage.transform.position+Vector3.up*1.05f);view.orthographicSize=1.1f;SavePortrait(view,"MenuSolo");
            for(int i=0;i<4;i++){cast[i].transform.parent.gameObject.SetActive(true);cast[i].transform.parent.localPosition=new Vector3((i-1.5f)*.86f,0,i%2*.3f);}
            view.orthographicSize=1.25f;SavePortrait(view,"MenuTeam");
        }
        finally{Object.DestroyImmediate(stage);}
    }
    static void SavePortrait(Camera camera,string name)
    {
        string path="Assets/Resources/Characters/"+name+".png";
        var rt=RenderTexture.GetTemporary(768,480,24);var previous=RenderTexture.active;var tex=new Texture2D(768,480,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,768,480),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
        finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
        AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.SaveAndReimport();
    }
}
