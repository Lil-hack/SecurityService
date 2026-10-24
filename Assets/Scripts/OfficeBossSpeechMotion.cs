using UnityEngine;
[DefaultExecutionOrder(350)]
public sealed class OfficeBossSpeechMotion : MonoBehaviour
{
    public SkinnedMeshRenderer face;
    public string mouthShape="BossSpeechOpen";
    public float mouthStrength=100;
    public float nodDegrees=26,turnDegrees=9,tiltDegrees=6;
    float headWeight;Vector3 headAngles;
    public float MouthAmount {get;private set;}
    Transform head;OfficeBossInterview interview;GemmaSpeech speech;int shape=-1;
    Quaternion applied=Quaternion.identity;bool appliedHead;
    void Awake()
    {
        head=GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);interview=GetComponent<OfficeBossInterview>();
        if(face!=null)shape=face.sharedMesh.GetBlendShapeIndex(mouthShape);
    }
    void Update(){UndoHead();}
    void UndoHead(){if(appliedHead&&head!=null)head.localRotation=head.localRotation*Quaternion.Inverse(applied);appliedHead=false;}
    void LateUpdate()
    {
        if(speech==null)speech=GetComponent<GemmaSpeech>();
        float target=interview!=null&&interview.Active&&speech!=null&&speech.Playing?speech.PlaybackLevel:0;
        if(target>.015f)target=Mathf.Clamp01(Mathf.Sqrt(target)*1.4f)*(.45f+.55f*Mathf.Abs(Mathf.Sin(Time.unscaledTime*17)));
        MouthAmount=Mathf.Lerp(MouthAmount,target,1-Mathf.Exp(-Time.unscaledDeltaTime*25));
        if(MouthAmount<.001f)MouthAmount=0;
        if(face!=null&&shape>=0)face.SetBlendShapeWeight(shape,MouthAmount*mouthStrength);
        bool speaking=interview!=null&&interview.Active&&speech!=null&&speech.Playing;
        float expression=speaking?.55f+.45f*speech.PlaybackLevel:0;
        headWeight=Mathf.Lerp(headWeight,expression,1-Mathf.Exp(-Time.unscaledDeltaTime*(speaking?7:10)));
        float time=Time.unscaledTime;
        // Unequal frequencies and a slowly changing bias avoid a repeated bobble.
        float nod=Mathf.Sin(time*7.2f)*.72f+Mathf.Sin(time*3.15f+.7f)*.28f;
        float turn=Mathf.Sin(time*1.55f+.9f)*.65f+(Mathf.PerlinNoise(time*.35f,2.7f)-.5f)*.7f;
        float tilt=Mathf.Sin(time*2.25f+1.4f)*.65f+Mathf.Sin(time*.85f)*.35f;
        Vector3 desired=new Vector3(nod*nodDegrees,turn*turnDegrees,tilt*tiltDegrees)*headWeight;
        headAngles=Vector3.Lerp(headAngles,desired,1-Mathf.Exp(-Time.unscaledDeltaTime*10));
        if(head!=null&&headAngles.sqrMagnitude>.0001f)
        {
            Quaternion before=head.localRotation;
            Quaternion worldOffset=transform.rotation*Quaternion.Euler(headAngles)*Quaternion.Inverse(transform.rotation);
            head.rotation=worldOffset*head.rotation;
            applied=Quaternion.Inverse(before)*head.localRotation;appliedHead=true;
        }
    }
    void OnDisable(){UndoHead();if(face!=null&&shape>=0)face.SetBlendShapeWeight(shape,0);MouthAmount=0;headWeight=0;headAngles=Vector3.zero;}
}




