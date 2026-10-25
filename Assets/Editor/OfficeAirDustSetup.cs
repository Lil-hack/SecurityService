using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class OfficeAirDustSetup
{
    const string Group="Airborne dust · sunlight";
    const string MaterialPath="Assets/ExecutiveOffice/LivingDetails/Materials/Airborne dust.mat";

    [MenuItem("Tools/Office/Apply Airborne Dust")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play before setting up airborne dust.");
        string original=SceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();
        Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));AssetDatabase.Refresh();
        var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if(material==null){material=new Material(Shader.Find("Office/Airborne Dust"));AssetDatabase.CreateAsset(material,MaterialPath);}
        material.SetFloat("_NearFade",.65f);material.SetFloat("_FarFade",20);EditorUtility.SetDirty(material);
        foreach(string path in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/ExecutiveOffice.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path);
            // Retire only the previous window motes; preserve footsteps, coffee steam and lighting.
            foreach(var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.name=="Executive window dust"||p.name=="Studio window dust"||p.name=="Kitchen window dust").ToArray())Object.DestroyImmediate(ps.gameObject);
            var old=GameObject.Find(Group);if(old!=null)Object.DestroyImmediate(old);
            var root=new GameObject(Group).transform;
            var warm=new Color(1,.89f,.69f,.62f);var neutral=new Color(.79f,.86f,.94f,.32f);
            Zone(root,"Director · sunlit motes",new Vector3(-.7f,1.95f,-.65f),new Vector3(5.4f,2.65f,5.3f),64,2.5f,warm,material,317);
            Zone(root,"Studio south · window motes",new Vector3(25.25f,1.95f,-2.1f),new Vector3(4.4f,2.65f,5.6f),80,3f,warm,material,613);
            Zone(root,"Studio north · window motes",new Vector3(25.25f,1.95f,4.25f),new Vector3(4.4f,2.65f,5.1f),80,3f,warm,material,947);
            Zone(root,"Kitchen · warm floating dust",new Vector3(25.0f,1.85f,11.75f),new Vector3(4.7f,2.5f,5.3f),64,2.5f,warm,material,1231);
            Zone(root,"Studio centre · sparse ambient dust",new Vector3(15.2f,1.85f,1.1f),new Vector3(11.5f,2.4f,10.3f),45,1.7f,neutral,material,1607);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        Selection.activeGameObject=GameObject.Find(Group);
        Debug.Log("Airborne dust saved in MainMenu and ExecutiveOffice: five prewarmed, capped zones per scene (333 particles maximum), slow drift, warm window motes, sparse cool fill; round alpha-blended specks fade near the camera and behind opaque geometry.");
    }
    static void Zone(Transform parent,string name,Vector3 position,Vector3 size,int limit,float rate,Color tint,Material material,uint seed)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;
        var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=seed;
        var main=ps.main;main.duration=30;main.loop=true;main.prewarm=true;main.playOnAwake=true;
        main.startLifetime=new ParticleSystem.MinMaxCurve(18,30);main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.006f,.015f);main.startColor=tint;
        main.maxParticles=limit;main.simulationSpace=ParticleSystemSimulationSpace.World;main.cullingMode=ParticleSystemCullingMode.PauseAndCatchup;
        var emission=ps.emission;emission.rateOverTime=rate;
        var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=size;
        var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
        velocity.x=new ParticleSystem.MinMaxCurve(-.010f,.010f);velocity.y=new ParticleSystem.MinMaxCurve(-.006f,.004f);velocity.z=new ParticleSystem.MinMaxCurve(-.008f,.008f);
        var noise=ps.noise;noise.enabled=true;noise.strength=.016f;noise.frequency=.24f;noise.scrollSpeed=.065f;noise.quality=ParticleSystemNoiseQuality.Low;noise.damping=true;
        var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.12f),new GradientAlphaKey(1,.4f),new GradientAlphaKey(.8f,.8f),new GradientAlphaKey(0,1)});fade.color=gradient;
        var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;
        renderer.minParticleSize=.00065f;renderer.maxParticleSize=.0032f;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
    }
}
