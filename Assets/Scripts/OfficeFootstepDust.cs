using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(CharacterController))]
public sealed class OfficeFootstepDust : MonoBehaviour
{
    ParticleSystem dust;Material material;CharacterController motor;
    Vector3 previous;float distance;bool left;
    void Start()
    {
        motor=GetComponent<CharacterController>();previous=transform.position;
        var go=new GameObject("Footstep dust trail");go.transform.SetParent(transform,false);
        dust=go.AddComponent<ParticleSystem>();dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=dust.main;main.loop=true;main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,.65f);
        main.startSize=new ParticleSystem.MinMaxCurve(.06f,.12f);main.startSpeed=0;main.maxParticles=100;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=-.015f;
        var emission=dust.emission;emission.rateOverTime=0;var shape=dust.shape;shape.enabled=false;
        var size=dust.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,1.8f));
        var fade=dust.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)});fade.color=gradient;
        material=new Material(Resources.Load<Shader>("PunchImpact"));var r=dust.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        dust.Play();
    }
    void LateUpdate()
    {
        if(dust==null)return;
        var delta=transform.position-previous;previous=transform.position;
        var horizontal=Vector3.ProjectOnPlane(delta,Vector3.up);float speed=horizontal.magnitude/Mathf.Max(Time.deltaTime,.001f);
        if(!motor.isGrounded || speed<.3f || horizontal.magnitude>1){distance=0;return;}
        distance+=horizontal.magnitude;bool running=speed>3;float stride=running?.95f:.65f;
        if(distance<stride)return;distance%=stride;left=!left;
        var controller=GetComponent<OfficeThirdPersonController>();var right=controller.visualRoot!=null?controller.visualRoot.right:transform.right;
        var point=transform.position+right*(left?-.13f:.13f)+Vector3.up*.045f;
        for(int i=0;i<(running?6:3);i++)
        {
            var e=new ParticleSystem.EmitParams();e.position=point+Random.insideUnitSphere*.035f;
            e.velocity=-horizontal.normalized*Random.Range(.04f,running?.28f:.12f)+Vector3.up*Random.Range(.04f,.12f);
            e.startSize=Random.Range(.06f,running?.15f:.10f);e.startColor=new Color(.73f,.70f,.64f,running?.27f:.18f);e.rotation=Random.Range(0,360);
            dust.Emit(e,1);
        }
    }
    void OnDestroy(){if(material!=null)Destroy(material);}
}
