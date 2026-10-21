using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ExecutiveWindowGlass
{
    const string Folder="Assets/ExecutiveOffice/Materials/";

    [MenuItem("Tools/Office/Add Executive Window Glass")]
    public static void Apply()
    {
        var scene=SceneManager.GetActiveScene();
        var architecture=scene.GetRootGameObjects().First(g=>g.name=="01 Architecture and walnut joinery").transform;
        var old=architecture.Find("Window glass and reflections");
        if(old!=null) Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Window glass and reflections").transform;
        root.SetParent(architecture,false);
        Directory.CreateDirectory(Folder);
        var glass=Transparent("Window clear blue glass",new Color(.68f,.84f,.94f,.105f),true);
        var sheen=Transparent("Window faint reflected highlights",new Color(.82f,.93f,1,.065f),false);
        var line=Transparent("Window fine reflected highlights",new Color(.94f,.98f,1,.085f),false);
        int index=0;
        foreach(float z in new[]{-1.8f,0,1.8f}) foreach(bool upper in new[]{false,true})
        {
            float height=upper?.70f:1.60f;
            var pane=GameObject.CreatePrimitive(PrimitiveType.Quad); pane.name="Glass pane "+(++index);
            pane.transform.SetParent(root,false); pane.transform.localPosition=new Vector3(-3.96f,upper?2.95f:1.70f,z);
            pane.transform.localRotation=Quaternion.Euler(0,-90,0); pane.transform.localScale=new Vector3(1.70f,height,1);
            Object.DestroyImmediate(pane.GetComponent<Collider>());
            var renderer=pane.GetComponent<MeshRenderer>(); renderer.sharedMaterial=glass;
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
            // Gentle tapered streaks hint at reflected room light without hiding the skyline.
            Highlight("Soft diagonal sheen",pane.transform,sheen,upper?.10f:.31f,.12f);
            Highlight("Fine diagonal glint",pane.transform,line,upper?.10f:.31f,.012f);
        }
        // A local cubemap reflects the existing room, including its lamps and furniture.
        root.gameObject.SetActive(false);
        var reflectionCamera=new GameObject("Temporary glass reflection camera").AddComponent<Camera>();
        reflectionCamera.transform.position=new Vector3(-3.6f,2.1f,0);
        reflectionCamera.nearClipPlane=.08f; reflectionCamera.farClipPlane=35;
        reflectionCamera.backgroundColor=new Color(.14f,.18f,.22f);
        reflectionCamera.clearFlags=CameraClearFlags.SolidColor;
        var cube=new Cubemap(256,TextureFormat.RGBAHalf,false);
        bool captured=reflectionCamera.RenderToCubemap(cube);
        Object.DestroyImmediate(reflectionCamera.gameObject);
        root.gameObject.SetActive(true);
        if(captured)
        {
            const string path="Assets/ExecutiveOffice/WindowRoomReflection.cubemap";
            var existing=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if(existing!=null) { EditorUtility.CopySerialized(cube,existing); Object.DestroyImmediate(cube); cube=existing; }
            else AssetDatabase.CreateAsset(cube,path);
            var probe=new GameObject("Window room reflection probe").AddComponent<ReflectionProbe>();
            probe.transform.SetParent(root,false); probe.transform.localPosition=new Vector3(-3.6f,2.1f,0);
            probe.mode=ReflectionProbeMode.Custom; probe.customBakedTexture=cube;
            probe.boxProjection=true; probe.size=new Vector3(14,7,14); probe.center=new Vector3(4,0,0);
            probe.intensity=.65f; probe.importance=10; probe.blendDistance=0;
        }
        else Object.DestroyImmediate(cube);
        AssetDatabase.SaveAssets();
        if(!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
        Debug.Log("Six transparent window panes and room reflections saved.");
    }

    static Material Transparent(string name,Color color,bool lit)
    {
        var mat=new Material(Shader.Find(lit?"Universal Render Pipeline/Lit":"Universal Render Pipeline/Unlit")){name=name};
        mat.SetFloat("_Surface",1); mat.SetFloat("_Blend",0); mat.SetFloat("_AlphaClip",0);
        mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite",0); mat.SetFloat("_Cull",(float)CullMode.Off);
        mat.SetColor("_BaseColor",color); mat.SetOverrideTag("RenderType","Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        if(lit)
        {
            mat.SetFloat("_Smoothness",.96f); mat.SetFloat("_Metallic",0);
            mat.SetFloat("_SpecularHighlights",1); mat.SetFloat("_EnvironmentReflections",1);
            mat.SetFloat("_ReceiveShadows",0); mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");
        }
        mat.renderQueue=lit?3000:3001;
        string path=Folder+name+".mat";
        var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old!=null) { EditorUtility.CopySerialized(mat,old); Object.DestroyImmediate(mat); return old; }
        AssetDatabase.CreateAsset(mat,path); return mat;
    }

    static void Highlight(string name,Transform pane,Material material,float slope,float thickness)
    {
        var go=new GameObject(name); go.transform.SetParent(pane,false);
        // Quad-local coordinates keep every streak inside its individual frame.
        var mesh=new Mesh(){name=name};
        mesh.vertices=new[]{new Vector3(-.49f,-slope/2-.06f,-.002f),new Vector3(.49f,slope/2-.06f,-.002f),new Vector3(.49f,slope/2-.06f+thickness,-.002f),new Vector3(-.49f,-slope/2-.06f+thickness,-.002f)};
        mesh.triangles=new[]{0,2,1,0,3,2}; mesh.RecalculateNormals();
        string path="Assets/ExecutiveOffice/"+name+"_"+(slope>.2f?"lower":"upper")+".asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old!=null) { EditorUtility.CopySerialized(mesh,old); Object.DestroyImmediate(mesh); mesh=old; }
        else AssetDatabase.CreateAsset(mesh,path);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
    }
}
