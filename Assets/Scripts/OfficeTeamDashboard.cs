using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public sealed class OfficeTeamDashboard : MonoBehaviour
{
    public Text totalRevenue,completedJobs,leaderName,leaderHint,profileName,characterName,profileStatus,personalRevenue,todayRevenue,teamCount,clock;
    public Image portrait;
    public Text portraitFallback;
    public Sprite[] portraits;
    public Text[] rowNames,rowRoles,rowJobs,rowRevenue,rowRanks;
    public Image[] rowBackgrounds;
    float nextRefresh;
    static readonly CultureInfo Russian=CultureInfo.GetCultureInfo("ru-RU");
    void OnEnable(){OfficeTeamProgress.Changed+=RefreshDisplay;RefreshDisplay();}
    void OnDisable(){OfficeTeamProgress.Changed-=RefreshDisplay;}
    void Update(){if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.5f;RefreshDisplay();}}
    public static string Money(long value)=>"$"+value.ToString("N0",Russian);
    static string Name(string name,int length=28)
    {
        name=string.IsNullOrWhiteSpace(name)?"Сотрудник":name.Replace('\n',' ').Replace('\r',' ');
        return name.Length>length?name.Substring(0,length-1)+"…":name;
    }
    static void Put(Text label,string value){if(label!=null)label.text=value;}
    public void RefreshDisplay()
    {
        var members=OfficeTeamProgress.GetTeamMembers();var local=members.Find(p=>p.isLocal);
        long revenue=0;long jobs=0;foreach(var member in members){revenue=OfficeTeamProgress.Sum(revenue,member.revenue);jobs+=member.completedJobs;}
        Put(totalRevenue,Money(revenue));Put(completedJobs,jobs.ToString("N0",Russian));
        bool hasLeader=members.Count>0&&(members[0].revenue>0||members[0].completedJobs>0);
        Put(leaderName,hasLeader?Name(members[0].name,24):"Пока без лидера");
        if(hasLeader)
        {
            int ties=members.FindAll(p=>p.revenue==members[0].revenue&&p.completedJobs==members[0].completedJobs).Count;
            Put(leaderHint,ties>1?"Первое место делят: "+ties:Money(members[0].revenue)+" · лучший результат");
        }
        else Put(leaderHint,"Первое дело — первое место");
        if(local!=null)
        {
            Put(profileName,Name(local.name,40));Put(characterName,OfficeTeamProgress.CharacterName(local.character));
            Put(profileStatus,PlayerPrefs.GetInt("Office.BossHired",0)==1?"В КОМАНДЕ · МОЖНО РАБОТАТЬ":"ВПЕРЕДИ СОБЕСЕДОВАНИЕ");
            Put(personalRevenue,Money(local.revenue));Put(todayRevenue,Money(local.todayRevenue));
            int index=OfficeTeamProgress.CharacterIndex(local.character);
            bool show=portraits!=null&&index>=0&&index<portraits.Length&&portraits[index]!=null;
            if(portrait!=null){portrait.enabled=show;if(show)portrait.sprite=portraits[index];}
            if(portraitFallback!=null)portraitFallback.enabled=!show;
        }
        Put(teamCount,members.Count<=4?members.Count+" / 4 УЧАСТНИКА":members.Count+" УЧАСТНИКОВ · ТОП 4");
        for(int i=0;rowNames!=null&&i<rowNames.Length;i++)
        {
            bool present=i<members.Count;var member=present?members[i]:null;
            Put(rowRanks[i],(i+1).ToString("00"));Put(rowNames[i],present?Name(member.name):"Место для коллеги");
            Put(rowRoles[i],present?(member.isLocal?"ВЫ":OfficeTeamProgress.CharacterName(member.character)):"Ожидание коопа");
            Put(rowJobs[i],present?member.completedJobs.ToString("N0",Russian):"—");Put(rowRevenue[i],present?Money(member.revenue):"—");
            if(rowBackgrounds[i]!=null)rowBackgrounds[i].color=present?(member.isLocal?new Color(.08f,.23f,.24f,1):new Color(.09f,.15f,.20f,1)):new Color(.067f,.10f,.145f,.65f);
            rowNames[i].color=present?new Color(.9f,.95f,.94f):new Color(.37f,.46f,.52f);
            rowRevenue[i].color=present?new Color(.94f,.79f,.47f):new Color(.34f,.43f,.49f);
        }
        Put(clock,"ОБНОВЛЕНО "+DateTime.Now.ToString("HH:mm"));
    }
}
