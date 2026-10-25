using System;
using System.Collections.Generic;
using UnityEngine;

public static class OfficeLoanProgress
{
    public const string SaveKey="Office.StaffLoans.v1";
    [Serializable]public sealed class Line {public string role,text;}
    [Serializable]public sealed class State
    {
        public string id;
        public int trustUnits=60;
        public bool loanRequested;
        public List<string> accepted=new List<string>();
        public List<Line> history=new List<Line>();
        public float Trust=>trustUnits*100f/120;
    }
    [Serializable]sealed class Ledger {public List<State> npcs=new List<State>();}
    public static string RewardId(OfficeStaffPersona persona)=>"office.loan."+persona.id;
    public static bool Paid(OfficeStaffPersona persona)=>OfficeTeamProgress.HasReward(RewardId(persona));
    static Ledger Read()
    {
        try{var value=JsonUtility.FromJson<Ledger>(PlayerPrefs.GetString(SaveKey,""));if(value!=null&&value.npcs!=null)return value;}catch(ArgumentException){}
        return new Ledger();
    }
    public static State Get(OfficeStaffPersona persona)
    {
        var state=Read().npcs.Find(p=>p!=null&&p.id==persona.id)??new State{id=persona.id};
        state.trustUnits=Mathf.Clamp(state.trustUnits,0,120);
        if(state.accepted==null)state.accepted=new List<string>();if(state.history==null)state.history=new List<Line>();
        if(Paid(persona))state.trustUnits=120;return state;
    }
    public static void Save(State state)
    {
        var ledger=Read();ledger.npcs.RemoveAll(p=>p==null||p.id==state.id);ledger.npcs.Add(state);
        PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(ledger));PlayerPrefs.Save();
    }
    static string Normal(string value)=>System.Text.RegularExpressions.Regex.Replace((value??"").ToLowerInvariant(),@"[^\p{L}\p{N}]+"," ").Trim();
    public static bool Apply(OfficeStaffPersona persona,State state,string text,bool valid,string outcome,bool novel,bool loanRequest)
    {
        if(!valid||Paid(persona))return false;
        if(outcome!="positive"&&outcome!="negative"&&outcome!="neutral"&&outcome!="repeated")return false;
        bool paid=ApplyPreview(persona,state,text,valid,outcome,novel,loanRequest)&&
            OfficeTeamProgress.TryRecordReward(RewardId(persona),persona.Reward);
        Save(state);return paid;
    }
    // Same trust rules without reading saves, paying rewards or writing PlayerPrefs.
    public static bool ApplyPreview(OfficeStaffPersona persona,State state,string text,bool valid,string outcome,bool novel,bool loanRequest)
    {
        if(!valid)return false;
        if(outcome!="positive"&&outcome!="negative"&&outcome!="neutral"&&outcome!="repeated")return false;
        if(outcome=="negative")state.trustUnits=Mathf.Max(0,state.trustUnits-12);
        else
        {
            if(loanRequest)state.loanRequested=true;
            bool repeated=state.accepted.Exists(p=>Normal(p)==Normal(text));
            if(outcome=="positive"&&novel&&!repeated)
            {state.trustUnits=Mathf.Min(120,state.trustUnits+60/persona.RequiredPersuasions);state.accepted.Add(text);}
        }
        return state.trustUnits>=120&&state.accepted.Count>=persona.RequiredPersuasions&&state.loanRequested;
    }
    public static void Remember(State state,string role,string text)
    {
        state.history.Add(new Line{role=role,text=text});if(state.history.Count>12)state.history.RemoveRange(0,state.history.Count-12);Save(state);
    }
}
