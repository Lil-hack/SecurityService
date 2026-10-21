using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeThirdPersonSetup
{
    [MenuItem("Tools/Office/Add Third Person Player")]
    public static void Apply()
    {
        var scene=SceneManager.GetActiveScene();
        var old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Office third-person player");
        if(old!=null) Object.DestroyImmediate(old);
        foreach(var camera in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)))
        {
            if(camera.name=="Third-person gameplay camera") { Object.DestroyImmediate(camera.gameObject); continue; }
            camera.enabled=false; camera.tag="Untagged";
            var listener=camera.GetComponent<AudioListener>(); if(listener!=null) listener.enabled=false;
        }
        var player=new GameObject("Office third-person player"); player.tag="Player"; player.layer=2;
        player.transform.position=new Vector3(3.45f,.035f,-2.65f);
        var capsule=player.AddComponent<CharacterController>();
        capsule.height=1.8f; capsule.radius=.27f; capsule.center=new Vector3(0,.9f,0);
        capsule.skinWidth=.025f; capsule.stepOffset=.20f; capsule.slopeLimit=48; capsule.minMoveDistance=0;
        var controller=player.AddComponent<OfficeThirdPersonController>();
        var visual=new GameObject("Articulated office visitor").transform; visual.SetParent(player.transform,false);
        controller.visualRoot=visual;
        var jacket=Material("Visitor navy jacket",new Color(.055f,.15f,.25f));
        var trousers=Material("Visitor grey trousers",new Color(.12f,.15f,.19f));
        var shoes=Material("Visitor dark shoes",new Color(.035f,.028f,.025f));
        var skin=Material("Visitor skin",new Color(.72f,.46f,.29f));
        var hair=Material("Visitor hair",new Color(.07f,.045f,.025f));
        var shirt=Material("Visitor shirt",new Color(.88f,.88f,.82f));
        Shape("Jacket",PrimitiveType.Capsule,visual,new Vector3(0,1.17f,0),new Vector3(.46f,.33f,.30f),jacket);
        Shape("Shirt collar",PrimitiveType.Cube,visual,new Vector3(0,1.42f,.10f),new Vector3(.13f,.10f,.11f),shirt);
        Shape("Head",PrimitiveType.Sphere,visual,new Vector3(0,1.63f,0),new Vector3(.30f,.34f,.29f),skin);
        Shape("Hair",PrimitiveType.Sphere,visual,new Vector3(0,1.73f,-.015f),new Vector3(.31f,.19f,.30f),hair);
        foreach(float x in new[]{-.058f,.058f}) Shape("Eye",PrimitiveType.Sphere,visual,new Vector3(x,1.65f,.132f),Vector3.one*.025f,hair);
        controller.leftLeg=Limb("Left leg",visual,new Vector3(-.13f,.88f,0),false,trousers,shoes,skin);
        controller.rightLeg=Limb("Right leg",visual,new Vector3(.13f,.88f,0),false,trousers,shoes,skin);
        controller.leftArm=Limb("Left arm",visual,new Vector3(-.29f,1.4f,0),true,jacket,shoes,skin);
        controller.rightArm=Limb("Right arm",visual,new Vector3(.29f,1.4f,0),true,jacket,shoes,skin);
        foreach(var t in player.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=2;
        var cameraObject=new GameObject("Third-person gameplay camera");
        var follow=cameraObject.AddComponent<Camera>(); follow.tag="MainCamera"; follow.fieldOfView=62;
        follow.nearClipPlane=.05f; follow.farClipPlane=70; follow.allowHDR=true;
        follow.backgroundColor=new Color(.15f,.18f,.22f); cameraObject.AddComponent<AudioListener>();
        controller.followCamera=follow; controller.UpdateCamera();
        // The reference scene has an open front for photography; close it only for movement.
        var boundary=GameObject.Find("Office gameplay front boundary");
        if(boundary==null) boundary=new GameObject("Office gameplay front boundary");
        boundary.transform.position=new Vector3(1,1.25f,-5.60f);
        var wall=boundary.GetComponent<BoxCollider>(); if(wall==null) wall=boundary.AddComponent<BoxCollider>();
        wall.size=new Vector3(10.2f,2.5f,.15f);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
        if(!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
        Debug.Log("Office third-person controller installed: WASD, Shift, Space, mouse, wheel, Escape.");
    }

    static Transform Limb(string name,Transform parent,Vector3 pivot,bool arm,Material clothes,Material shoes,Material skin)
    {
        var root=new GameObject(name).transform; root.SetParent(parent,false); root.localPosition=pivot;
        if(arm)
        {
            Shape("Sleeve",PrimitiveType.Capsule,root,new Vector3(0,-.21f,0),new Vector3(.14f,.245f,.14f),clothes);
            Shape("Hand",PrimitiveType.Sphere,root,new Vector3(0,-.46f,0),new Vector3(.12f,.15f,.12f),skin);
        }
        else
        {
            Shape("Trouser leg",PrimitiveType.Capsule,root,new Vector3(0,-.36f,0),new Vector3(.19f,.36f,.20f),clothes);
            Shape("Shoe",PrimitiveType.Cube,root,new Vector3(0,-.81f,.045f),new Vector3(.20f,.14f,.31f),shoes);
        }
        return root;
    }

    static void Shape(string name,PrimitiveType shape,Transform parent,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(shape); go.name=name; go.transform.SetParent(parent,false);
        go.transform.localPosition=position; go.transform.localScale=scale;
        go.GetComponent<Renderer>().sharedMaterial=material;
        Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    static Material Material(string name,Color color)
    {
        const string folder="Assets/ExecutiveOffice/Materials/"; Directory.CreateDirectory(folder);
        var mat=AssetDatabase.LoadAssetAtPath<Material>(folder+name+".mat");
        if(mat==null) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,folder+name+".mat"); }
        mat.color=color; mat.SetFloat("_Smoothness",.22f); return mat;
    }
}
