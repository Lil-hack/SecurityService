using UnityEngine;

[DefaultExecutionOrder(360)]
public sealed class OfficeStaffSpeechMotion : MonoBehaviour
{
    Transform head,jaw;Quaternion headOffset,jawOffset;bool appliedHead,appliedJaw;
    OfficeStaffConversation dialogue;OfficeStaffPhysics physics;GemmaSpeech speech;float weight;
    void Start(){var animator=GetComponentInChildren<Animator>();if(animator==null)return;head=animator.GetBoneTransform(HumanBodyBones.Head);jaw=animator.GetBoneTransform(HumanBodyBones.Jaw);dialogue=GetComponent<OfficeStaffConversation>();physics=GetComponent<OfficeStaffPhysics>();}
    void Update()=>Undo();
    void Undo()
    {
        if(physics!=null&&physics.IsDown){appliedHead=appliedJaw=false;return;}
        if(appliedHead&&head!=null)head.localRotation=head.localRotation*Quaternion.Inverse(headOffset);
        if(appliedJaw&&jaw!=null)jaw.localRotation=jaw.localRotation*Quaternion.Inverse(jawOffset);appliedHead=appliedJaw=false;
    }
    void LateUpdate()
    {
        if(speech==null)speech=GetComponent<GemmaSpeech>();
        bool talking=dialogue!=null&&dialogue.Active&&speech!=null&&speech.Playing&&physics!=null&&!physics.IsDown;
        weight=Mathf.Lerp(weight,talking?.45f+.55f*speech.PlaybackLevel:0,1-Mathf.Exp(-Time.unscaledDeltaTime*10));
        if(head!=null&&weight>.001f){headOffset=Quaternion.Euler(Mathf.Sin(Time.unscaledTime*5.5f)*9*weight,Mathf.Sin(Time.unscaledTime*1.9f)*6*weight,Mathf.Sin(Time.unscaledTime*2.4f)*3*weight);head.localRotation*=headOffset;appliedHead=true;}
        if(jaw!=null&&talking){jawOffset=Quaternion.Euler(Mathf.Clamp01(speech.PlaybackLevel*3)*14,0,0);jaw.localRotation*=jawOffset;appliedJaw=true;}
    }
    void OnDisable()=>Undo();
}
