using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class OfficeDialogueLabValidation
{
    static Task running;
    public static string Status {get;private set;}="Not run";
    const string Report="OfficePreview/DialogueLabValidation.txt";
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static string Snapshot()=>PlayerPrefs.GetString(OfficeLoanProgress.SaveKey,"<missing>")+"\n"+PlayerPrefs.GetString(OfficeTeamProgress.SaveKey,"<missing>")+"\n"+PlayerPrefs.GetInt("Office.BossHired",-1);
    [MenuItem("Tools/Office/Dialogue Lab/Test all five dialogues in Play Mode")]
    public static void RunFromMenu()=>Begin();
    public static string Begin()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Open OfficeDialogueLab and enter Play Mode.");
        if(running!=null&&!running.IsCompleted)return Status;
        running=Run();return "Dialogue lab validation started";
    }
    static async Task Run()
    {
        Directory.CreateDirectory("OfficePreview");var report=new List<string>();string before=Snapshot();
        var lab=UnityEngine.Object.FindAnyObjectByType<OfficeDialogueLab>();
        try
        {
            Require(lab!=null&&lab.profiles.Length==5,"Expected boss and four NPCs.");
            // Exercise the shared loan state transitions without writes or rewards.
            var p=UnityEngine.Object.Instantiate(Array.Find(lab.profiles,x=>!x.isBoss).staff);p.difficulty=1;
            var s=new OfficeLoanProgress.State{id=p.id};
            Require(!OfficeLoanProgress.ApplyPreview(p,s,"Внимательный",true,"positive",true,false)&&s.trustUnits==90,"First compliment state");
            Require(!OfficeLoanProgress.ApplyPreview(p,s,"Надёжный",true,"positive",true,false)&&s.trustUnits==120,"Second compliment requires explicit loan request");
            Require(!OfficeLoanProgress.ApplyPreview(p,s,"Внимательный",true,"positive",true,false)&&s.accepted.Count==2,"Duplicate must not add a reason");
            Require(OfficeLoanProgress.ApplyPreview(p,s,"Одолжи денег",true,"neutral",false,true),"Full trust plus explicit loan request");
            Require(!OfficeLoanProgress.ApplyPreview(p,s,"Ошибка",false,"positive",true,false)&&s.accepted.Count==2,"Invalid evaluation must not mutate state");
            OfficeLoanProgress.ApplyPreview(p,s,"Грубость",true,"negative",false,false);Require(s.trustUnits==108,"Negative reaction lowers trust");
            UnityEngine.Object.Destroy(p);Require(before==Snapshot(),"Preview state touched gameplay saves");
            report.Add("PASS shared trust rules: novelty, repeat, loan request, invalid result, negative reaction; no save writes.");
            lab.voiceEnabled=false;
            for(int i=0;i<lab.profiles.Length;i++)
            {
                lab.Select(i);var profile=lab.Current;
                Require(profile!=lab.profiles[i],"Runtime profile must be a copy");
                if(profile.isBoss)Require(!string.IsNullOrWhiteSpace(profile.boss.BossRolePrompt)&&!string.IsNullOrWhiteSpace(profile.boss.Greeting)&&!string.IsNullOrWhiteSpace(profile.voice.voicePath),"Boss role, greeting and voice must be copied from game");
                if(!profile.isBoss)Require(profile.staff!=lab.profiles[i].staff&&profile.staff!=profile.sourceStaff,"NPC persona must be copied");
                Status="Loading "+profile.Label;Write(report);
                var timer=System.Diagnostics.Stopwatch.StartNew();await lab.StartTestAsync();double load=timer.Elapsed.TotalSeconds;
                Status="Generating "+profile.Label;Write(report);timer.Restart();
                string message=profile.isBoss?"Я умею работать с таблицами и автоматизировать отчёты, чтобы команда тратила меньше времени на рутину.":"Привет! Как проходит твой рабочий день?";
                string reply=await lab.SendAsync(message);
                Require(!string.IsNullOrWhiteSpace(reply)&&System.Text.RegularExpressions.Regex.IsMatch(reply,"[А-Яа-яЁё]"),"Expected Russian reply");
                Require(before==Snapshot(),"Live dialogue touched gameplay saves");
                report.Add($"PASS {profile.Label}: load={load:0.00}s, response={timer.Elapsed.TotalSeconds:0.00}s, backend={lab.EvaluationBackend}, outcome={lab.LastOutcome}\n{reply}");
                lab.StopTest();Write(report);
            }
            // Verify the explicit save action persists a test profile and leaves source persona intact.
            lab.Select(1);var saved=lab.profiles[1];string original=lab.Current.staff.personality;
            string sourceJson=EditorJsonUtility.ToJson(saved.sourceStaff);
            lab.Current.staff.personality=original+"\nПроверка сохранения тестового профиля.";lab.SaveSelected();
            Require(saved.staff.personality==lab.Current.staff.personality,"Test profile save did not persist");
            lab.Current.staff.personality=original;lab.SaveSelected();
            Require(EditorJsonUtility.ToJson(saved.sourceStaff)==sourceJson,"Saving test profile modified gameplay persona");
            report.Add("PASS test profile editing and persistence; source NPC unchanged.");
            Require(before==Snapshot(),"Gameplay save changed after tests");
            lab.Select(0);Status="PASS all five dialogues; gameplay saves unchanged";Write(report);Debug.Log(Status);
        }
        catch(Exception e){lab?.StopTest();Status="FAILED: "+e;Write(report);Debug.LogException(e);}
    }
    static void Write(List<string> report)=>File.WriteAllText(Report,Status+"\n\n"+string.Join("\n\n",report),Encoding.UTF8);
}
