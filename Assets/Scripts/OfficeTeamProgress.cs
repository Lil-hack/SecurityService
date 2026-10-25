using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class OfficeRevenueDay
{
    public string date;
    public long revenue;
}

[Serializable]
public sealed class OfficeTeamMember
{
    public string id,name,character;
    public long revenue,todayRevenue;
    public int completedJobs;
    public bool isLocal;
}

// Code-owned income ledger. Loan payouts use atomic idempotent reward entries.
public static class OfficeTeamProgress
{
    public const string SaveKey="Office.Statistics.Local.v1",IdKey="Office.Statistics.PlayerId";
    [Serializable]sealed class SaveData
    {
        public int version=1;
        public long revenue;
        public int completedJobs;
        public List<OfficeRevenueDay> days=new List<OfficeRevenueDay>();
        public List<string> rewards=new List<string>();
    }
    static readonly Dictionary<string,OfficeTeamMember> remote=new Dictionary<string,OfficeTeamMember>(StringComparer.Ordinal);
    static string previousId;
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntime(){remote.Clear();previousId=null;Changed=null;}
    public static string LocalPlayerId
    {
        get
        {
            string id=PlayerPrefs.GetString(IdKey,"");
            if(string.IsNullOrEmpty(id))
            {
                if(!Application.isPlaying)return "local-preview";
                id=Guid.NewGuid().ToString("N");PlayerPrefs.SetString(IdKey,id);PlayerPrefs.Save();
            }
            if(previousId!=null&&previousId!=id)remote.Clear();previousId=id;return id;
        }
    }
    static string Today=>DateTime.Now.ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
    static SaveData Read()
    {
        SaveData data=null;string json=PlayerPrefs.GetString(SaveKey,"");
        if(!string.IsNullOrEmpty(json))try{data=JsonUtility.FromJson<SaveData>(json);}catch(ArgumentException){}
        if(data==null||data.version!=1)data=new SaveData();
        data.revenue=Math.Max(0,data.revenue);data.completedJobs=Math.Max(0,data.completedJobs);
        if(data.days==null)data.days=new List<OfficeRevenueDay>();if(data.rewards==null)data.rewards=new List<string>();return data;
    }
    public static long Sum(long a,long b)=>a>long.MaxValue-b?long.MaxValue:a+b;
    public static OfficeTeamMember GetLocalMember()
    {
        var data=Read();long today=0;foreach(var day in data.days)if(day!=null&&day.date==Today)today=Sum(today,Math.Max(0,day.revenue));
        string name=LaptopDesktop.PlayerName;
        return new OfficeTeamMember{id=LocalPlayerId,name=string.IsNullOrWhiteSpace(name)?"Новый сотрудник":name,character=PlayerPrefs.GetString("Office.CharacterPrefab",""),revenue=data.revenue,todayRevenue=today,completedJobs=data.completedJobs,isLocal=true};
    }
    public static List<OfficeTeamMember> GetTeamMembers()
    {
        var local=GetLocalMember();var members=new List<OfficeTeamMember>{local};
        foreach(var player in remote.Values)if(player.id!=local.id)members.Add(Clone(player));
        return members.OrderByDescending(p=>p.revenue).ThenByDescending(p=>p.completedJobs).ThenBy(p=>p.id,StringComparer.Ordinal).ToList();
    }
    public static void RecordRevenue(long amount)=>Record(amount,false);
    public static void RecordCompletedJob(long reward)=>Record(reward,true);
    public static bool HasReward(string id)=>Read().rewards.Contains(id);
    public static bool TryRecordReward(string id,long amount,bool completedJob=true)
    {
        if(string.IsNullOrWhiteSpace(id))throw new ArgumentException("Reward ID is required",nameof(id));
        if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
        var data=Read();if(data.rewards.Contains(id))return false;
        data.rewards.Add(id);ApplyIncome(data,amount,completedJob);return true;
    }
    static void Record(long amount,bool job)
    {
        if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
        if(amount==0&&!job)return;
        ApplyIncome(Read(),amount,job);
    }
    static void ApplyIncome(SaveData data,long amount,bool job)
    {
        data.revenue=Sum(data.revenue,amount);
        if(job&&data.completedJobs<int.MaxValue)data.completedJobs++;
        var today=data.days.Find(d=>d!=null&&d.date==Today);
        if(today==null){today=new OfficeRevenueDay{date=Today};data.days.Add(today);}today.revenue=Sum(Math.Max(0,today.revenue),amount);
        data.days=data.days.Where(d=>d!=null).OrderByDescending(d=>d.date,StringComparer.Ordinal).Take(30).ToList();
        PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));PlayerPrefs.Save();Changed?.Invoke();
    }
    // Supply connected remote participants, excluding the local player. Networking
    // remains outside this class; remote earnings are not stored as local progress.
    public static void SetRemotePlayers(IEnumerable<OfficeTeamMember> players)
    {
        string local=LocalPlayerId;remote.Clear();
        if(players!=null)foreach(var p in players)
        {
            if(p==null||string.IsNullOrWhiteSpace(p.id)||p.id==local||p.revenue<0||p.todayRevenue<0||p.completedJobs<0)continue;
            remote[p.id]=Clone(p);
        }
        Changed?.Invoke();
    }
    static OfficeTeamMember Clone(OfficeTeamMember p)=>new OfficeTeamMember{id=p.id,name=p.name,character=p.character,revenue=p.revenue,todayRevenue=p.todayRevenue,completedJobs=p.completedJobs};
    public static int CharacterIndex(string character)
    {
        string value=(character??"").ToLowerInvariant();
        if(value.Contains("skuf"))return 0;if(value.Contains("altgirl"))return 1;if(value.Contains("tubik"))return 2;if(value.Contains("shtrih"))return 3;return -1;
    }
    public static string CharacterName(string character)
    {
        int i=CharacterIndex(character);return i<0?"Персонаж не выбран":new[]{"Скуф","Альтушка","Тюбик","Штрих"}[i];
    }
}
