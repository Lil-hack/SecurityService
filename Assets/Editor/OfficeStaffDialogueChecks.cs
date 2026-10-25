using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class OfficeStaffDialogueChecks
{
    public static string ValidateRules()
    {
        bool hadStats=PlayerPrefs.HasKey(OfficeTeamProgress.SaveKey),hadLoans=PlayerPrefs.HasKey(OfficeLoanProgress.SaveKey);
        string stats=PlayerPrefs.GetString(OfficeTeamProgress.SaveKey,""),loans=PlayerPrefs.GetString(OfficeLoanProgress.SaveKey,"");
        var report=new List<string>();
        try
        {
            PlayerPrefs.DeleteKey(OfficeTeamProgress.SaveKey);PlayerPrefs.DeleteKey(OfficeLoanProgress.SaveKey);
            foreach(string id in new[]{"man1","girl1","man2","girl2"})
            {
                var source=AssetDatabase.LoadAssetAtPath<OfficeStaffPersona>("Assets/OfficeExtension/StaffDialogue/"+id+".asset");
                var persona=Object.Instantiate(source);persona.id="qa."+id;
                try
                {
                    var state=OfficeLoanProgress.Get(persona);Require(state.Trust==50,"Initial trust");
                    for(int i=0;i<persona.RequiredPersuasions;i++)
                    {
                        bool won=OfficeLoanProgress.Apply(persona,state,"Разный аргумент "+i,true,"positive",true,true);
                        Require(won==(i==persona.RequiredPersuasions-1),"Required count "+id);
                    }
                    Require(state.Trust==100&&OfficeLoanProgress.Paid(persona),"Win/persistence "+id);
                    long before=OfficeTeamProgress.GetLocalMember().revenue;
                    Require(!OfficeLoanProgress.Apply(persona,state,"Ещё раз",true,"positive",true,true),"Double pay blocked");
                    Require(before==OfficeTeamProgress.GetLocalMember().revenue,"Income unchanged on replay");
                    Require(OfficeLoanProgress.Get(persona).Trust==100,"Restored trust");
                    persona.id+=".negative";state=OfficeLoanProgress.Get(persona);
                    OfficeLoanProgress.Apply(persona,state,"Грубость",true,"negative",false,false);Require(state.Trust==40,"Negative trust");
                    OfficeLoanProgress.Apply(persona,state,"Позитив",false,"positive",true,true);Require(state.Trust==40,"Unavailable judge");
                    OfficeLoanProgress.Apply(persona,state,"Комплимент",true,"positive",true,false);int current=state.trustUnits;
                    OfficeLoanProgress.Apply(persona,state,"Комплимент!",true,"positive",true,false);Require(state.trustUnits==current,"Exact repetition guard");
                    OfficeLoanProgress.Apply(persona,state,"Тот же смысл иначе",true,"repeated",false,false);Require(state.trustUnits==current,"Semantic repetition");
                    for(int i=0;i<20;i++)OfficeLoanProgress.Apply(persona,state,"Плохо",true,"negative",false,false);Require(state.Trust==0,"Trust floor");
                    persona.id+=".request";state=OfficeLoanProgress.Get(persona);
                    for(int i=0;i<persona.RequiredPersuasions;i++)Require(!OfficeLoanProgress.Apply(persona,state,"Личное качество "+i,true,"positive",true,false),"No loan without request");
                    Require(state.Trust==100&&!OfficeLoanProgress.Paid(persona),"Wait for explicit request");
                    Require(OfficeLoanProgress.Apply(persona,state,"Одолжи, пожалуйста",true,"neutral",false,true),"Loan after trust");
                    report.Add("PASS "+id+": "+persona.RequiredPersuasions+" varied positives / $"+persona.Reward+", loss, repetition, save, explicit request, idempotent reward");
                }
                finally{Object.DestroyImmediate(persona);}
            }
        }
        finally
        {
            if(hadStats)PlayerPrefs.SetString(OfficeTeamProgress.SaveKey,stats);else PlayerPrefs.DeleteKey(OfficeTeamProgress.SaveKey);
            if(hadLoans)PlayerPrefs.SetString(OfficeLoanProgress.SaveKey,loans);else PlayerPrefs.DeleteKey(OfficeLoanProgress.SaveKey);PlayerPrefs.Save();
            foreach(var board in Object.FindObjectsByType<OfficeTeamDashboard>(FindObjectsInactive.Include,FindObjectsSortMode.None))board.RefreshDisplay();
        }
        report.Add("Original player saves restored.");return string.Join("\n",report);
    }
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
