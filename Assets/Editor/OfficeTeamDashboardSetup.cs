using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class OfficeTeamDashboardSetup
{
    const string Folder="Assets/ExecutiveOffice/Dashboard";
    const string RootName="Office team dashboard";
    static Font font;
    static Sprite rounded;
    static Material ui,housing;
    static readonly Color White=new Color(.91f,.96f,.96f),Muted=new Color(.52f,.65f,.71f),Mint=new Color(.32f,.87f,.76f),Gold=new Color(.97f,.79f,.43f);

    [MenuItem("Tools/Office/Add Team Dashboard")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play before adding the wall dashboard.");
        string original=SceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();
        Directory.CreateDirectory(Folder+"/Portraits");AssetDatabase.Refresh();
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rounded=RoundedSprite();ui=Mat("World dashboard UI",Shader.Find("Office/World Dashboard UI"),Color.white);
        housing=Mat("Dashboard satin graphite",Shader.Find("Universal Render Pipeline/Lit"),new Color(.035f,.075f,.095f));housing.SetFloat("_Smoothness",.38f);housing.SetFloat("_Metallic",.18f);EditorUtility.SetDirty(housing);
        CapturePortraits();
        var portraits=Enumerable.Range(0,4).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/Portraits/Player"+i+".png")).ToArray();
        foreach(string path in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/ExecutiveOffice.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path);var old=GameObject.Find(RootName);if(old!=null)Object.DestroyImmediate(old);
            Build(portraits);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        Selection.activeGameObject=GameObject.Find(RootName);
        Debug.Log("Office team dashboard saved on the north teal wall in ExecutiveOffice and MainMenu. Live profile, hiring status, persisted earnings/jobs and four leaderboard slots; no fabricated revenue or coop participants.");
    }
    static Material Mat(string name,Shader shader,Color color)
    {
        string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
        if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);else if(material.HasProperty("_Color"))material.SetColor("_Color",color);
        EditorUtility.SetDirty(material);return material;
    }
    static Sprite RoundedSprite()
    {
        const int size=96;string path=Folder+"/Rounded card.png";var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            var q=new Vector2(Mathf.Abs(x+.5f-size*.5f)-(size*.5f-20),Mathf.Abs(y+.5f-size*.5f)-(size*.5f-20));
            float distance=new Vector2(Mathf.Max(0,q.x),Mathf.Max(0,q.y)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-20;
            texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(.5f-distance)));
        }
        texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder=new Vector4(24,24,24,24);importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;
    }
    static Image Card(Transform parent,string name,float x,float y,float width,float height,Color color,bool round=true)
    {
        var image=Rect(parent,name,x,y,width,height).gameObject.AddComponent<Image>();image.color=color;image.material=ui;image.raycastTarget=false;
        if(round){image.sprite=rounded;image.type=Image.Type.Sliced;}return image;
    }
    static Text Words(Transform parent,string name,string text,float x,float y,float width,float height,int size,Color color,bool bold=false,TextAnchor alignment=TextAnchor.MiddleLeft)
    {
        var label=Rect(parent,name,x,y,width,height).gameObject.AddComponent<Text>();label.font=font;label.fontSize=size;label.color=color;label.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;
        label.alignment=alignment;label.text=text;label.material=ui;label.supportRichText=false;label.raycastTarget=false;
        label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;return label;
    }
    static Mesh HousingMesh()
    {
        string path=Folder+"/Rounded dashboard housing.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old!=null)return old;
        var method=typeof(RoundedOfficeTables).GetMethod("Build",BindingFlags.Static|BindingFlags.NonPublic);
        var mesh=(Mesh)method.Invoke(null,new object[]{new Vector3(5.76f,.12f,2.16f)});
        mesh.name="Soft bevel dashboard housing";mesh.vertices=mesh.vertices.Select(v=>new Vector3(v.x,-v.z,v.y)).ToArray();mesh.normals=mesh.normals.Select(v=>new Vector3(v.x,-v.z,v.y)).ToArray();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static void Build(Sprite[] portraits)
    {
        var root=new GameObject(RootName);root.transform.position=new Vector3(18.65f,2.65f,7.40f);
        var frame=new GameObject("Rounded graphite display housing");frame.transform.SetParent(root.transform,false);frame.transform.localScale=new Vector3(5.76f,2.16f,.12f);
        frame.AddComponent<MeshFilter>().sharedMesh=HousingMesh();var renderer=frame.AddComponent<MeshRenderer>();renderer.sharedMaterial=housing;renderer.shadowCastingMode=ShadowCastingMode.On;
        var screen=new GameObject("World-space statistics screen",typeof(RectTransform),typeof(Canvas));screen.transform.SetParent(root.transform,false);
        var canvas=screen.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=0;
        var rt=screen.GetComponent<RectTransform>();rt.sizeDelta=new Vector2(1680,600);rt.localPosition=new Vector3(0,0,-.068f);rt.localScale=Vector3.one/300;
        var dashboard=root.AddComponent<OfficeTeamDashboard>();dashboard.portraits=portraits;
        Card(rt,"Deep navy screen",0,0,1680,600,new Color(.032f,.067f,.105f));
        for(int i=0;i<3;i++)Card(rt,"Pulse logo bar",36+i*14,58-i*10,8,18+i*10,Mint);
        Words(rt,"Dashboard title","ПУЛЬС ОФИСА",96,21,830,49,42,White,true);
        Words(rt,"Dashboard subtitle","Большие планы. Маленькие перерывы.",97,72,900,26,20,Muted);
        Card(rt,"Working day pill",1290,33,350,42,new Color(.065f,.19f,.20f));Card(rt,"Live dot",1306,49,10,10,Mint);
        Words(rt,"Working day label","РАБОЧИЙ ДЕНЬ ИДЁТ",1329,36,294,35,18,Mint,true);
        Card(rt,"Header rule",36,115,1608,1,new Color(.16f,.30f,.36f),false);
        var a=Card(rt,"Team revenue card",36,137,520,133,new Color(.065f,.17f,.18f)).transform;
        Words(a,"Revenue caption","ОБЩАЯ ВЫРУЧКА",22,13,460,24,20,Mint,true);
        dashboard.totalRevenue=Words(a,"Total revenue","0 ₽",22,40,470,55,52,White,true);Fit(dashboard.totalRevenue,24,52);
        Words(a,"Revenue note","Сумма заработка всей команды",23,103,470,22,18,Muted);
        var b=Card(rt,"Completed jobs card",576,137,520,133,new Color(.078f,.125f,.19f)).transform;
        Words(b,"Jobs caption","ВЫПОЛНЕНО ЗАДАНИЙ",22,13,470,24,20,new Color(.51f,.69f,.92f),true);
        dashboard.completedJobs=Words(b,"Completed jobs","0",22,40,470,55,52,White,true);Fit(dashboard.completedJobs,24,52);
        Words(b,"Jobs note","Каждое дело идёт в общий результат",23,103,474,22,18,Muted);
        var c=Card(rt,"Team leader card",1116,137,528,133,new Color(.19f,.145f,.085f)).transform;
        Words(c,"Leader caption","ЛИДЕР КОМАНДЫ",22,13,480,24,20,Gold,true);
        dashboard.leaderName=Words(c,"Leader name","Пока без лидера",22,46,480,44,32,White,true);Fit(dashboard.leaderName,22,32);
        dashboard.leaderHint=Words(c,"Leader detail","Первое дело — первое место",23,103,480,22,18,Gold);

        var profile=Card(rt,"Your employee card",36,291,520,256,new Color(.064f,.113f,.155f)).transform;
        Words(profile,"Profile caption","ВАША КАРТОЧКА",21,12,470,25,18,Muted,true);
        Card(profile,"Portrait surround",20,49,108,108,new Color(.11f,.30f,.32f));
        dashboard.portrait=Card(profile,"Character portrait",24,53,100,100,Color.white,false);
        dashboard.portraitFallback=Words(profile,"Portrait fallback","ВЫ",24,53,100,100,30,Mint,true,TextAnchor.MiddleCenter);
        dashboard.profileName=Words(profile,"Your name","Новый сотрудник",148,47,350,70,29,White,true);Fit(dashboard.profileName,22,29);
        dashboard.characterName=Words(profile,"Your character","Персонаж не выбран",149,123,350,30,23,Mint);
        Card(profile,"Employment tag",20,170,478,30,new Color(.09f,.21f,.23f));
        dashboard.profileStatus=Words(profile,"Employment status","ВПЕРЕДИ СОБЕСЕДОВАНИЕ",30,170,456,30,16,Mint,true);
        Words(profile,"Your lifetime caption","ВАША ВЫРУЧКА",22,209,245,17,15,Muted);
        Words(profile,"Today caption","ЗА СЕГОДНЯ",290,209,205,17,15,Muted);
        dashboard.personalRevenue=Words(profile,"Your earnings","0 ₽",22,227,245,25,24,White,true);Fit(dashboard.personalRevenue,16,24);
        dashboard.todayRevenue=Words(profile,"Today earnings","0 ₽",290,227,205,25,24,White,true);Fit(dashboard.todayRevenue,16,24);

        var ranks=Card(rt,"Team leaderboard",576,291,1068,256,new Color(.045f,.087f,.134f)).transform;
        Words(ranks,"Leaderboard caption","ТАБЛИЦА ЛИДЕРОВ",20,10,600,32,24,White,true);
        dashboard.teamCount=Words(ranks,"Player count","1 / 4 УЧАСТНИКА",730,13,316,28,17,Mint,true,TextAnchor.MiddleRight);
        Words(ranks,"Place heading","№",22,48,54,19,15,Muted);
        Words(ranks,"Player heading","СОТРУДНИК",87,48,480,19,15,Muted);
        Words(ranks,"Jobs heading","ЗАДАНИЯ",658,48,145,19,15,Muted,false,TextAnchor.MiddleRight);
        Words(ranks,"Revenue heading","ВЫРУЧКА",850,48,190,19,15,Muted,false,TextAnchor.MiddleRight);
        dashboard.rowNames=new Text[4];dashboard.rowRoles=new Text[4];dashboard.rowJobs=new Text[4];dashboard.rowRevenue=new Text[4];dashboard.rowRanks=new Text[4];dashboard.rowBackgrounds=new Image[4];
        for(int i=0;i<4;i++)
        {
            var row=Card(ranks,"Leaderboard row "+(i+1),12,75+i*42,1044,37,new Color(.07f,.13f,.17f));dashboard.rowBackgrounds[i]=row;
            dashboard.rowRanks[i]=Words(row.transform,"Position","0"+(i+1),10,0,54,37,20,i==0?Gold:Muted,true);
            dashboard.rowNames[i]=Words(row.transform,"Name","Место для коллеги",75,0,430,37,23,White,true);Fit(dashboard.rowNames[i],18,23);
            dashboard.rowRoles[i]=Words(row.transform,"Role","Ожидание коопа",510,0,180,37,16,Muted);
            dashboard.rowJobs[i]=Words(row.transform,"Jobs","—",700,0,91,37,23,White,false,TextAnchor.MiddleRight);
            dashboard.rowRevenue[i]=Words(row.transform,"Revenue","—",825,0,205,37,23,Gold,true,TextAnchor.MiddleRight);Fit(dashboard.rowRevenue[i],15,23);
        }
        Words(rt,"Dashboard footer","Кофе в выручку не входит. Мы проверяли.",38,565,1120,24,18,Muted);
        dashboard.clock=Words(rt,"Updated time","ОБНОВЛЕНО --:--",1280,565,360,24,17,Muted,false,TextAnchor.MiddleRight);
        dashboard.RefreshDisplay();EditorUtility.SetDirty(dashboard);
    }
    static void Fit(Text label,int min,int max){label.resizeTextForBestFit=true;label.resizeTextMinSize=min;label.resizeTextMaxSize=max;}
    static void CapturePortraits()
    {
        var stage=new GameObject("Temporary dashboard portraits"){hideFlags=HideFlags.HideAndDontSave};stage.transform.position=new Vector3(0,1000,0);
        var camera=new GameObject("Dashboard portrait camera").AddComponent<Camera>();camera.transform.SetParent(stage.transform,false);camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.064f,.113f,.155f,0);camera.orthographic=true;camera.orthographicSize=.43f;camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var key=new GameObject("Portrait key").AddComponent<Light>();key.transform.SetParent(stage.transform,false);key.type=LightType.Directional;key.intensity=2;key.color=new Color(1,.87f,.72f);key.cullingMask=1<<30;key.transform.rotation=Quaternion.Euler(35,-30,0);
        var fill=new GameObject("Portrait fill").AddComponent<Light>();fill.transform.SetParent(stage.transform,false);fill.type=LightType.Directional;fill.intensity=.8f;fill.color=new Color(.5f,.8f,1);fill.cullingMask=1<<30;fill.transform.rotation=Quaternion.Euler(20,150,0);
        string[] paths={"skuf/Skuf","altgirl/Altgirl","tubik/Tubik","shtrih/Shtrih"};
        try
        {
            for(int i=0;i<paths.Length;i++)
            {
                var holder=new GameObject("Portrait actor").transform;holder.SetParent(stage.transform,false);
                var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/character/"+paths[i]+"_Textured.prefab"),holder);
                OfficeThirdPersonController.PlaceHumanoid(actor,holder,30);holder.localRotation=Quaternion.Euler(0,90,0);
                var animator=actor.GetComponent<Animator>();animator.SetBool("Grounded",true);animator.SetFloat("Speed",0);animator.Play("Locomotion",0,.1f);animator.Update(.1f);
                var head=animator.GetBoneTransform(HumanBodyBones.Head);var skin=actor.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>!r.name.StartsWith("Toon"));
                var focus=head.position;focus.y=Mathf.Lerp(focus.y,skin.bounds.max.y,.34f);camera.transform.position=focus+new Vector3(0,.03f,-2);camera.transform.LookAt(focus);
                var rt=RenderTexture.GetTemporary(256,256,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;var texture=new Texture2D(256,256,TextureFormat.RGBA32,false);
                try
                {
                    camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,256,256),0,0);texture.Apply();
                    var pixels=texture.GetPixels();for(int p=0;p<pixels.Length;p++){float x=p%256-127.5f,y=p/256-127.5f;pixels[p].a*=Mathf.Clamp01((126-Mathf.Sqrt(x*x+y*y))/2);}
                    texture.SetPixels(pixels);texture.Apply();string path=Folder+"/Portraits/Player"+i+".png";File.WriteAllBytes(path,texture.EncodeToPNG());AssetDatabase.ImportAsset(path);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=true;importer.alphaIsTransparency=true;importer.maxTextureSize=256;importer.SaveAndReimport();
                }
                finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(texture);Object.DestroyImmediate(holder.gameObject);}
            }
        }
        finally{Object.DestroyImmediate(stage);}
    }
}
