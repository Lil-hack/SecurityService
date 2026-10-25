using UnityEngine;

public sealed class OfficeFanMotion : MonoBehaviour
{
    public Transform rotor,head;
    public float degreesPerSecond=285;
    [Range(0,30)]public float oscillationAngle=14;
    [Min(2)]public float oscillationPeriod=11;
    Quaternion restHead,restRotor;
    float start;
    void OnEnable(){start=Time.time;if(head!=null)restHead=head.localRotation;if(rotor!=null)restRotor=rotor.localRotation;}
    void Update()
    {
        float t=Time.time-start;
        if(rotor!=null)rotor.localRotation=restRotor*Quaternion.AngleAxis(t*degreesPerSecond,Vector3.forward);
        if(head!=null)head.localRotation=restHead*Quaternion.AngleAxis(Mathf.Sin(t*Mathf.PI*2/oscillationPeriod)*oscillationAngle,Vector3.up);
    }
    void OnDisable(){if(head!=null)head.localRotation=restHead;if(rotor!=null)rotor.localRotation=restRotor;}
}
