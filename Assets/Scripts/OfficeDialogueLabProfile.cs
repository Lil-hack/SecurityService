using System;
using UnityEngine;

[CreateAssetMenu(menuName="Office/Dialogue test profile")]
public sealed class OfficeDialogueLabProfile : ScriptableObject
{
    public string displayName,sourceScene,sourceObject;
    public bool isBoss;
    public OfficeStaffPersona staff;
    [HideInInspector]public OfficeStaffPersona sourceStaff;
    public Texture2D portrait;
    public string modelPath="Models/gemma-4-E2B-it.litertlm",pythonPath=".local/gemma-runtime/Scripts/python.exe";
    public BossSettings boss=new BossSettings();
    public VoiceSettings voice=new VoiceSettings();
    [Serializable] public sealed class BossSettings
    {
        [TextArea(3,8)]public string BossRolePrompt,ReplyInstructions,AcceptanceInstructions,RejectionInstructions;
        [TextArea(3,10)]public string JevQuestion,JevInstructions,JevAcceptCriteria,JevRejectCriteria,GemmaEvaluationRules;
        [TextArea(2,5)]public string Greeting,HiredGreeting,AcceptedFallbackReply,RejectedFallbackReply;
    }
    [Serializable] public sealed class VoiceSettings
    {
        public string voicePath;
        public float SpeechRate=1,PitchSemitones,Tremble,Rasp,VolumeVariation,PauseSeconds=.04f,Volume=1;
        public bool TelephoneEnabled;
    }
    public string Label=>isBoss?displayName:staff!=null?staff.displayName:displayName;
    public string Greeting=>isBoss?boss.Greeting:staff.greeting;
}
