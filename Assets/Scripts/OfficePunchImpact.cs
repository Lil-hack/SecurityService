using UnityEngine;
using UnityEngine.Rendering;

public sealed class OfficePunchImpact : MonoBehaviour
{
    Material material;
    public static void Spawn(Vector3 point,Vector3 normal)
    {
        var go=new GameObject("Punch dust puff");
        go.transform.SetPositionAndRotation(point+normal*.025f,Quaternion.LookRotation(normal));
        go.AddComponent<OfficePunchImpact>();
    }
    void Awake()
    {
        material=new Material(Resources.Load<Shader>("PunchImpact")){hideFlags=HideFlags.HideAndDontSave};
        var ps=gameObject.AddComponent<ParticleSystem>();
        ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;
        main.loop=false;main.duration=.55f;main.startLifetime=new ParticleSystem.MinMaxCurve(.28f,.48f);
        main.startSpeed=new ParticleSystem.MinMaxCurve(.12f,.45f);
        main.startSize=new ParticleSystem.MinMaxCurve(.075f,.13f);
        main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
        main.startColor=new Color(.82f,.79f,.72f,.65f);main.gravityModifier=-.025f;
        main.maxParticles=12;main.simulationSpace=ParticleSystemSimulationSpace.World;
        var emission=ps.emission;emission.rateOverTime=0;
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=.035f;
        var size=ps.sizeOverLifetime;size.enabled=true;
        size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.5f),new Keyframe(.3f,1.2f),new Keyframe(1,1.9f)));
        var fade=ps.colorOverLifetime;fade.enabled=true;
        var gradient=new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.1f),new GradientAlphaKey(.45f,.5f),new GradientAlphaKey(0,1)});
        fade.color=gradient;
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
        renderer.renderMode=ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        ps.Play();ps.Emit(7);Destroy(gameObject,.65f);
    }
    void OnDestroy(){if(material!=null)Destroy(material);}
}
