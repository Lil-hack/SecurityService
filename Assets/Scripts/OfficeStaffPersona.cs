using UnityEngine;

[CreateAssetMenu(menuName="Office/Staff dialogue persona")]
public sealed class OfficeStaffPersona : ScriptableObject
{
    public string id,displayName;
    [Range(1,4)]public int difficulty=1;
    [TextArea(3,8)]public string personality;
    [TextArea(3,8)]public string preferences;
    [TextArea(2,5)]public string greeting,alreadyPaidGreeting;
    [Header("Индивидуальный голос Piper")]
    public string voicePath;
    [Range(.65f,1.3f)]public float speechRate=1.2f;
    [Range(-12,7)]public float pitchSemitones;
    [Range(0,1)]public float rasp=.025f,tremble=.08f,volumeVariation=.08f,volume=1;
    public AudioClip greetingClip,thinkingClip,alreadyPaidClip;
    [HideInInspector]public string preparedVoicePath,preparedGreeting,preparedPaid;
    [HideInInspector]public float preparedRate,preparedPitch;
    public Texture2D portrait;
    public Color accent=new Color(.24f,.75f,.66f);
    public int RequiredPersuasions=>Mathf.Clamp(difficulty,1,4)+1;
    public int Reward=>new[]{100,200,500,1000}[Mathf.Clamp(difficulty,1,4)-1];
    public bool PreparedMatches(string text,string preparedText)=>voicePath==preparedVoicePath&&Mathf.Abs(speechRate-preparedRate)<.001f&&Mathf.Abs(pitchSemitones-preparedPitch)<.001f&&text==preparedText;
}
