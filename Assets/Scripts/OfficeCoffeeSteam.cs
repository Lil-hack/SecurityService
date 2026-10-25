using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public sealed class OfficeCoffeeSteam : MonoBehaviour
{
    [Min(3)] public float visibleDistance=16;
    public Transform cup;
    ParticleSystem steam;
    float nextCheck;
    void Awake()=>steam=GetComponent<ParticleSystem>();
    void Update()
    {
        if(Time.time<nextCheck)return;nextCheck=Time.time+.25f;
        var camera=Camera.main;
        bool upright=cup==null||Vector3.Dot(cup.up,Vector3.up)>.65f;
        bool visible=upright&&(camera==null||(camera.transform.position-transform.position).sqrMagnitude<visibleDistance*visibleDistance);
        var emission=steam.emission;emission.enabled=visible;
        if(visible&&!steam.isPlaying)steam.Play();
    }
}
