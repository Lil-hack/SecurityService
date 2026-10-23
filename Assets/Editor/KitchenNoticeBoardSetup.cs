using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KitchenNoticeBoardSetup
{
    const string Folder="Assets/OfficeExtension/Materials/";
    static Material Mat(string name,Color color)
    {
        string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",.18f);EditorUtility.SetDirty(m);return m;
    }
    static GameObject Box(Transform root,string name,Vector3 position,Vector3 size,Material mat)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static void Words(Transform root,Font font,string text,Vector3 position,float width,float height,Color color,bool bold=false)
    {
        var g=new GameObject("Notice - "+text.Split('\n')[0]);g.transform.SetParent(root,false);g.transform.localPosition=position;
        var tm=g.AddComponent<TextMesh>();tm.font=font;tm.fontSize=64;tm.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=color;tm.text=text;tm.lineSpacing=1.12f;
        font.RequestCharactersInTexture(text,tm.fontSize,tm.fontStyle);
        float max=0;foreach(var line in text.Split('\n')){float advance=0;foreach(char c in line){font.GetCharacterInfo(c,out var glyph,tm.fontSize,tm.fontStyle);advance+=glyph.advance;}max=Mathf.Max(max,advance);}
        float lines=text.Split('\n').Length;float lineHeight=font.lineHeight*tm.fontSize/(float)font.fontSize;
        tm.characterSize=Mathf.Min(width/Mathf.Max(1,max),height/Mathf.Max(1,lineHeight*lines*tm.lineSpacing))*10;
        g.AddComponent<KitchenWorldText>().Refresh();
    }
    [MenuItem("Tools/Office/Add Kitchen Notice Board")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new System.Exception("Stop Play first.");
        var kitchen=GameObject.Find("Kitchen room");var old=kitchen.transform.Find("Kitchen notice board");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Kitchen notice board").transform;root.SetParent(kitchen.transform);root.SetPositionAndRotation(new Vector3(23.15f,2.62f,7.72f),Quaternion.Euler(0,180,0));
        var wood=Mat("Notice walnut frame",new Color(.34f,.20f,.105f));var cork=Mat("Notice warm cork",new Color(.53f,.35f,.19f));
        var cream=Mat("Notice ivory paper",new Color(.96f,.92f,.80f));var mint=Mat("Notice mint paper",new Color(.69f,.84f,.73f));var yellow=Mat("Notice butter paper",new Color(.97f,.78f,.36f));
        var pin=Mat("Notice rust pins",new Color(.68f,.20f,.12f));var ink=new Color(.012f,.016f,.018f);var font=kitchen.GetComponentsInChildren<TextMesh>().First(t=>t.font!=null).font;
        Box(root,"Walnut notice frame",Vector3.zero,new Vector3(3.35f,1.90f,.08f),wood);Box(root,"Recessed cork backing",new Vector3(0,0,-.047f),new Vector3(3.19f,1.74f,.035f),cork);
        Words(root,font,"КУХОННЫЙ КОМИТЕТ",new Vector3(0,.66f,-.083f),2.9f,.22f,new Color(1,.94f,.77f),true);
        Words(root,font,"ПЕРЕРЫВ — ТОЖЕ ЧАСТЬ РАБОТЫ",new Vector3(0,.44f,-.083f),2.8f,.105f,new Color(.98f,.89f,.69f));
        string[] texts={"ПЛАН НА ДЕНЬ\n\n1. Кофе\n2. Ещё кофе\n3. Видимость работы", "ВАЖНО!\n\nЧужой обед —\nне тестовый стенд.\nРуками не проверять.", "ДЕДЛАЙН ГОРИТ?\n\nНе паникуй.\nЭто просто чайник.\nВыдохни и налей чай."};
        var papers=new[]{cream,mint,yellow};
        for(int i=0;i<3;i++)
        {
            var card=new GameObject("Pinned kitchen note "+(i+1)).transform;card.SetParent(root,false);card.localPosition=new Vector3((i-1)*1.015f,-.12f,-.083f);card.localRotation=Quaternion.Euler(0,0,i==0?-2:i==2?2:0);
            Box(card,"Paper shadow",new Vector3(.015f,-.015f,.006f),new Vector3(.91f,.93f,.014f),wood);Box(card,"Paper",Vector3.zero,new Vector3(.91f,.93f,.012f),papers[i]);
            Words(card,font,texts[i],new Vector3(0,-.025f,-.012f),.80f,.76f,ink,true);
            var tack=GameObject.CreatePrimitive(PrimitiveType.Sphere);tack.name="Copper pin";tack.transform.SetParent(card,false);tack.transform.localPosition=new Vector3(0,.40f,-.025f);tack.transform.localScale=new Vector3(.055f,.055f,.025f);tack.GetComponent<Renderer>().sharedMaterial=pin;Object.DestroyImmediate(tack.GetComponent<Collider>());
        }
        Words(root,font,"Кружку помыл — KPI закрыл.",new Vector3(0,-.73f,-.083f),2.5f,.13f,new Color(1,.94f,.77f));
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(kitchen.scene);EditorSceneManager.SaveScene(kitchen.scene);Capture();
    }
    [MenuItem("Tools/Office/Capture Kitchen Notice Board")]
    public static void Capture()
    {
        Directory.CreateDirectory("OfficePreview");var camera=new GameObject("Temporary notice preview").AddComponent<Camera>();camera.nearClipPlane=.05f;camera.fieldOfView=58;
        camera.transform.position=new Vector3(25.5f,2.9f,12.5f);camera.transform.LookAt(new Vector3(23.8f,2.4f,7.72f));
        var rt=new RenderTexture(1200,900,24);camera.targetTexture=rt;
        try{camera.Render();RenderTexture.active=rt;var image=new Texture2D(1200,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,900),0,0);image.Apply();File.WriteAllBytes("OfficePreview/Kitchen_NoticeBoard.png",image.EncodeToPNG());Object.DestroyImmediate(image);}
        finally{camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(camera.gameObject);}
    }
}

