using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor=UnityEngine.Cursor;

public sealed class OfficeDialogueLab : MonoBehaviour
{
    public OfficeDialogueLabProfile[] profiles;
    public bool voiceEnabled=true;
    public bool completedScenario;
    public int SelectedIndex {get;private set;}
    public bool Busy {get;private set;}
    public bool Ready=>worker!=null && worker.Ready;
    public bool Hired {get;private set;}
    public bool Paid {get;private set;}
    public string LastReply {get;private set;}="";
    public string EvaluationBackend {get;private set;}="—";
    public string LastOutcome {get;private set;}="—";
    public OfficeLoanProgress.State Progress {get;private set;}
    public OfficeDialogueLabProfile Current=>copies[SelectedIndex];
    OfficeDialogueLabProfile[] copies;
    OfficeDialogueLabWorker worker;GemmaSpeech speech;
    UIDocument document;PanelSettings panel;
    ScrollView chat,settings;VisualElement roster;
    TextField input;Label heading,status,metrics,voiceStatus,settingsHint;Button start,stop,send,save;
    Label answerBubble;string streamed="",lastMessage;OfficeLoanProgress.Line[] turnHistory;
    bool streamOpen,previousBackground;int version;
    string settingsTab="Роль";
    static readonly string[] Tabs={"Роль","Голос","Оценка","Запуск"};
    string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,".."));
    [Serializable]sealed class PromptData
    {public string role,reply,evaluation,accepted,rejected,accepted_fallback,rejected_fallback,jev_question,jev_instructions,jev_true,jev_false;}
    [Serializable]sealed class PersonaData {public string displayName,personality,preferences;public int difficulty;}
    [Serializable]sealed class GameState {public float trust;public bool loan_requested,payment_granted;public int amount;public string outcome;}
    [Serializable]sealed class Command
    {
        public string command,text;public PromptData prompts;public PersonaData persona;
        public string[] accepted_messages;public OfficeLoanProgress.Line[] history;public GameState game_state;
    }
    void Awake()
    {
        previousBackground=Application.runInBackground;Application.runInBackground=true;
        copies=new OfficeDialogueLabProfile[profiles.Length];
        for(int i=0;i<profiles.Length;i++){copies[i]=Instantiate(profiles[i]);if(profiles[i].staff!=null)copies[i].staff=Instantiate(profiles[i].staff);}
        worker=gameObject.AddComponent<OfficeDialogueLabWorker>();worker.PacketReceived+=Receive;
    }
    void Start(){BuildUI();Select(0);}
    void Update()
    {
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        if(voiceStatus!=null)voiceStatus.text=voiceEnabled?(speech==null?"Голос будет запущен вместе с тестом":speech.Status.Replace("Нина Петровна",Current.Label)):"Озвучка выключена";
        Controls();
    }
    void BuildUI()
    {
        panel=ScriptableObject.CreateInstance<PanelSettings>();panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;
        panel.referenceResolution=new Vector2Int(1600,900);panel.screenMatchMode=PanelScreenMatchMode.Expand;
        panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("GemmaTheme");panel.sortingOrder=1000;
        document=gameObject.AddComponent<UIDocument>();document.panelSettings=panel;
        var root=document.rootVisualElement;root.styleSheets.Add(Resources.Load<StyleSheet>("OfficeDialogueLab"));root.AddToClassList("lab");
        var title=new Label("Лаборатория диалогов");title.AddToClassList("lab-title");root.Add(title);
        root.Add(new Label("Босс и коллеги из кабинета · настройки и прогресс тестов хранятся отдельно от игры"));
        var columns=new VisualElement();columns.AddToClassList("columns");root.Add(columns);
        roster=new ScrollView();roster.AddToClassList("roster");columns.Add(roster);
        var center=new VisualElement();center.AddToClassList("conversation");columns.Add(center);
        heading=new Label();heading.AddToClassList("section-title");center.Add(heading);
        metrics=new Label();metrics.AddToClassList("metrics");center.Add(metrics);
        var actions=new VisualElement();actions.AddToClassList("row");center.Add(actions);
        start=new Button(()=>RunUI(StartTestAsync)){text="Новый тест"};actions.Add(start);
        stop=new Button(StopTest){text="Остановить"};actions.Add(stop);
        var mute=new Toggle("Озвучка"){value=voiceEnabled};mute.RegisterValueChangedCallback(e=>{voiceEnabled=e.newValue;if(!voiceEnabled&&speech!=null)speech.Cancel();});actions.Add(mute);
        chat=new ScrollView();chat.AddToClassList("chat");center.Add(chat);
        status=new Label();status.AddToClassList("status");center.Add(status);
        voiceStatus=new Label();voiceStatus.AddToClassList("hint");center.Add(voiceStatus);
        input=new TextField{multiline=true};input.AddToClassList("message-input");center.Add(input);
        input.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Return&&e.ctrlKey){Submit();e.StopPropagation();}});
        send=new Button(Submit){text="Отправить · Ctrl + Enter"};send.AddToClassList("primary");center.Add(send);
        var right=new VisualElement();right.AddToClassList("inspector");columns.Add(right);
        right.Add(new Label("Настройки персонажа"){name="settings-title"});
        var tabs=new VisualElement();tabs.AddToClassList("row");right.Add(tabs);
        foreach(string tab in Tabs){string t=tab;tabs.Add(new Button(()=>{settingsTab=t;BuildSettings();}){text=t});}
        settings=new ScrollView();settings.AddToClassList("settings-scroll");right.Add(settings);
        save=new Button(SaveSelected){text="Сохранить тестовый профиль"};right.Add(save);
        settingsHint=new Label("Изменения роли применяются к следующему сообщению. Пути запуска — после нового теста.");settingsHint.AddToClassList("hint");right.Add(settingsHint);
        for(int i=0;i<copies.Length;i++)
        {
            int index=i;var p=copies[i];var button=new Button(()=>Select(index));button.AddToClassList("person");
            if(p.portrait!=null)button.Add(new Image{image=p.portrait,scaleMode=ScaleMode.ScaleAndCrop});
            var labels=new VisualElement();labels.AddToClassList("person-labels");button.Add(labels);
            labels.Add(new Label(p.Label));labels.Add(new Label(p.isBoss?"Собеседование":"Доверие и заём"));roster.Add(button);
        }
    }
    public void Select(int index)
    {
        if(index<0||index>=copies.Length)throw new ArgumentOutOfRangeException(nameof(index));
        StopTest();SelectedIndex=index;completedScenario=false;ResetState();
        if(chat!=null){chat.Clear();input.value="";heading.text=Current.Label;AddMessage(Current.Label,Current.Greeting);status.text="Нажмите «Новый тест», чтобы загрузить модель.";BuildSettings();}
        RefreshMetrics();
        for(int i=0;roster!=null&&i<roster.childCount;i++)roster[i].EnableInClassList("selected",i==index);
    }
    void ResetState(){Hired=Paid=false;LastReply="";EvaluationBackend=LastOutcome="—";Progress=new OfficeLoanProgress.State{id=Current.isBoss?"boss":Current.staff.id};Remember("character",Current.Greeting);}
    void Remember(string role,string text){Progress.history.Add(new OfficeLoanProgress.Line{role=role,text=text});if(Progress.history.Count>12)Progress.history.RemoveAt(0);}
    public async Task StartTestAsync()
    {
        StopTest();int active=version;ResetState();Hired=Current.isBoss&&completedScenario;Paid=!Current.isBoss&&completedScenario;
        string opening=Hired?Current.boss.HiredGreeting:Paid?Current.staff.alreadyPaidGreeting:Current.Greeting;
        Progress.history.Clear();Remember("character",opening);chat.Clear();AddMessage(Current.Label,opening);Busy=true;status.text="Загрузка Gemma и диалога…";
        try{if(!completedScenario)await worker.StartAsync(Current);if(active!=version)return;ApplyVoice();if(voiceEnabled)speech.Speak(opening);status.text=completedScenario?"Проверка реплики завершённого сценария. Для переписки выключите режим во вкладке «Запуск».":"Готово. Напишите сообщение.";}
        catch(Exception e){if(active==version){status.text="Ошибка: "+e.Message;throw;}}
        finally{if(active==version){Busy=false;RefreshMetrics();Controls();}}
    }
    public void StopTest()
    {
        version++;Busy=false;if(worker!=null)worker.Stop();ReleaseVoice();
        if(status!=null)status.text="Тест остановлен. Можно выбрать персонажа или начать заново.";
    }
    void ReleaseVoice(){streamOpen=false;if(speech!=null){speech.Cancel();Destroy(speech);speech=null;}}
    void ApplyVoice()
    {
        if(!voiceEnabled)return;
        if(speech==null)speech=gameObject.AddComponent<GemmaSpeech>();
        var v=Current.voice;var p=Current.staff;
        string path=Current.isBoss?v.voicePath:p.voicePath;
        speech.SpeechRate=Current.isBoss?v.SpeechRate:p.speechRate;speech.PitchSemitones=Current.isBoss?v.PitchSemitones:p.pitchSemitones;
        speech.Rasp=Current.isBoss?v.Rasp:p.rasp;speech.Tremble=Current.isBoss?v.Tremble:p.tremble;
        speech.VolumeVariation=Current.isBoss?v.VolumeVariation:p.volumeVariation;speech.Volume=Current.isBoss?v.Volume:p.volume;
        speech.PauseSeconds=Current.isBoss?v.PauseSeconds:.04f;speech.TelephoneEnabled=Current.isBoss&&v.TelephoneEnabled;
        string python=Path.IsPathRooted(Current.pythonPath)?Current.pythonPath:Path.Combine(Root,Current.pythonPath);
        if(speech.VoicePath!=null&&speech.VoicePath!=path)speech.SelectVoice(path);else speech.Initialize(python,Root,path);
    }
    async void RunUI(Func<Task> action){try{await action();}catch(OperationCanceledException){}catch(Exception e){Debug.LogWarning("Dialogue lab: "+e.Message);}}
    void Submit(){if(!Busy&&Ready&&!string.IsNullOrWhiteSpace(input.value))RunUI(async()=>{string text=input.value;input.value="";await SendAsync(text);});}
    public async Task<string> SendAsync(string text)
    {
        if(Busy||!Ready||Hired||Paid)throw new InvalidOperationException("Начните новый тест или дождитесь ответа.");
        lastMessage=(text??"").Trim();if(lastMessage.Length==0)throw new ArgumentException("Введите сообщение.");
        if(lastMessage.Length>1000)lastMessage=lastMessage.Substring(0,1000);
        int active=version;Busy=true;streamed="";streamOpen=false;answerBubble=null;LastReply="";
        turnHistory=Progress.history.ToArray();Remember("user",lastMessage);AddMessage("Вы",lastMessage);ApplyVoice();
        var timer=System.Diagnostics.Stopwatch.StartNew();status.text="Оценка реплики и ответ персонажа…";
        try
        {
            OfficeDialogueLabWorker.Reply reply;
            if(Current.isBoss)
            {
                var b=Current.boss;
                reply=await worker.RequestAsync(new Command{command="interview",text=lastMessage,prompts=new PromptData{
                    role=b.BossRolePrompt,reply=b.ReplyInstructions,evaluation=b.GemmaEvaluationRules,accepted=b.AcceptanceInstructions,rejected=b.RejectionInstructions,
                    accepted_fallback=b.AcceptedFallbackReply,rejected_fallback=b.RejectedFallbackReply,
                    jev_question=b.JevQuestion,jev_instructions=b.JevInstructions,jev_true=b.JevAcceptCriteria,jev_false=b.JevRejectCriteria}},"interview_result");
                if(active!=version)return "";
                Hired=reply.valid&&reply.accepted;EvaluationBackend=reply.backend??"unavailable";LastOutcome=reply.valid?(Hired?"Принят":"Нужно уточнить"):"Оценка недоступна";
            }
            else
            {
                var judged=await worker.RequestAsync(StaffCommand("social_judge",false),"social_evaluation");if(active!=version)return "";
                EvaluationBackend=judged.backend??"unavailable";LastOutcome=judged.valid?judged.outcome:"unavailable";
                Paid=OfficeLoanProgress.ApplyPreview(Current.staff,Progress,lastMessage,judged.valid,judged.outcome,judged.novel,judged.loan_request);
                RefreshMetrics();reply=await worker.RequestAsync(StaffCommand("social_reply",Paid),"social_done");
            }
            if(active!=version)return "";
            LastReply=reply.text;if(string.IsNullOrWhiteSpace(LastReply))LastReply=Current.isBoss?(Hired?Current.boss.AcceptedFallbackReply:Current.boss.RejectedFallbackReply):"Не расслышал. Попробуйте сказать иначе.";
            if(answerBubble==null)answerBubble=AddMessage(Current.Label,LastReply);else answerBubble.text=Current.Label+"\n"+LastReply;
            if(voiceEnabled){if(streamOpen)speech.EndStream();else speech.Speak(LastReply);}streamOpen=false;Remember("character",LastReply);
            status.text=$"Ответ за {timer.Elapsed.TotalSeconds:0.0} с · "+(Hired?"Вы приняты. Начните новый тест.":Paid?"Тестовый заём получен. Начните новый тест.":"Можно отправить следующую реплику.");
            return LastReply;
        }
        catch(Exception e){if(active==version){status.text="Ошибка: "+e.Message;if(speech!=null)speech.Cancel();}throw;}
        finally{if(active==version){Busy=false;RefreshMetrics();Controls();}}
    }
    Command StaffCommand(string command,bool paid)
    {
        var p=Current.staff;return new Command{command=command,text=lastMessage,
            persona=new PersonaData{displayName=p.displayName,personality=p.personality,preferences=p.preferences,difficulty=p.difficulty},
            accepted_messages=Progress.accepted.ToArray(),history=turnHistory,
            game_state=new GameState{trust=Progress.Trust,loan_requested=Progress.loanRequested,payment_granted=paid,amount=p.Reward,outcome=LastOutcome}};
    }
    void Receive(OfficeDialogueLabWorker.Reply packet)
    {
        if(!Busy||(packet.kind!="interview_sentence"&&packet.kind!="social_sentence"))return;
        streamed+=(streamed.Length>0?" ":"")+packet.text;
        if(answerBubble==null)answerBubble=AddMessage(Current.Label,streamed);else answerBubble.text=Current.Label+"\n"+streamed;
        if(voiceEnabled&&speech!=null){if(!streamOpen){speech.BeginStream(true);streamOpen=true;}speech.AppendText(packet.text+" ");}
        chat.ScrollTo(answerBubble);
    }
    Label AddMessage(string who,string text)
    {
        var label=new Label(who+"\n"+text){enableRichText=false};label.AddToClassList("bubble");if(who=="Вы")label.AddToClassList("player-bubble");
        chat.Add(label);chat.schedule.Execute(()=>{if(label.parent==chat.contentContainer)chat.ScrollTo(label);});return label;
    }
    void RefreshMetrics()
    {
        if(metrics==null||Progress==null)return;
        metrics.text=Current.isBoss?$"Оценщик: {EvaluationBackend} · {LastOutcome}":$"Доверие: {Progress.Trust:0.#}% · подходы {Progress.accepted.Count}/{Current.staff.RequiredPersuasions} · заём ${Current.staff.Reward}\nОценщик: {EvaluationBackend} · {LastOutcome}";
    }
    void Controls()
    {
        if(send==null)return;send.SetEnabled(Ready&&!Busy&&!Hired&&!Paid);input.SetEnabled(!Busy&&!Hired&&!Paid);
        start.SetEnabled(!Busy);stop.SetEnabled(Busy||Ready||(speech!=null&&speech.HasPendingSpeech));save.SetEnabled(!Busy);settings.SetEnabled(!Busy);
    }
    void BuildSettings()
    {
        settings.Clear();settings.scrollOffset=Vector2.zero;var p=Current;
        if(settingsTab=="Роль")
        {
            if(p.isBoss){StringField(p,nameof(p.displayName),"Имя в тесте");StringField(p.boss,"BossRolePrompt","Роль босса",true);StringField(p.boss,"ReplyInstructions","Стиль ответов",true);StringField(p.boss,"Greeting","Приветствие",true);StringField(p.boss,"HiredGreeting","Для принятого сотрудника",true);StringField(p.boss,"AcceptanceInstructions","Ответ при приёме",true);StringField(p.boss,"RejectionInstructions","Ответ при отказе",true);StringField(p.boss,"AcceptedFallbackReply","Запасная реплика при приёме",true);StringField(p.boss,"RejectedFallbackReply","Запасная реплика при отказе",true);}
            else{StringField(p.staff,"displayName","Имя");StringField(p.staff,"personality","Роль и характер",true);StringField(p.staff,"preferences","Что ценит / убеждение",true);StringField(p.staff,"greeting","Приветствие",true);StringField(p.staff,"alreadyPaidGreeting","После выдачи займа",true);var difficulty=new SliderInt("Сложность",1,4){value=p.staff.difficulty,showInputField=true};difficulty.RegisterValueChangedCallback(e=>{p.staff.difficulty=e.newValue;RefreshMetrics();});settings.Add(difficulty);}
        }
        else if(settingsTab=="Оценка")
        {
            if(p.isBoss){StringField(p.boss,"JevQuestion","Вопрос Jev",true);StringField(p.boss,"JevInstructions","Инструкция Jev",true);StringField(p.boss,"JevAcceptCriteria","Критерий приёма",true);StringField(p.boss,"JevRejectCriteria","Критерий отказа",true);StringField(p.boss,"GemmaEvaluationRules","Резервная оценка Gemma",true);}
            else settings.Add(new Label("Jev оценивает эффект, новизну и просьбу о займе с учётом роли и предпочтений. Gemma используется при недоступном или неопределённом результате. Доверие и выплату считает игровая логика.\n\nНастройте характер, предпочтения и сложность во вкладке «Роль»."));
        }
        else if(settingsTab=="Голос")
        {
            object v=p.isBoss?(object)p.voice:p.staff;StringField(v,"voicePath","Файл голоса .onnx");
            FloatField(v,p.isBoss?"SpeechRate":"speechRate","Темп речи",.65f,1.3f);FloatField(v,p.isBoss?"PitchSemitones":"pitchSemitones","Высота голоса",-12,7);
            FloatField(v,p.isBoss?"Volume":"volume","Громкость",0,1);FloatField(v,p.isBoss?"Rasp":"rasp","Хрипотца",0,1);FloatField(v,p.isBoss?"Tremble":"tremble","Дрожание",0,1);FloatField(v,p.isBoss?"VolumeVariation":"volumeVariation","Колебание громкости",0,1);
            if(p.isBoss){FloatField(v,"PauseSeconds","Паузы",0,.5f);var phone=new Toggle("Телефонный фильтр"){value=p.voice.TelephoneEnabled};phone.RegisterValueChangedCallback(e=>p.voice.TelephoneEnabled=e.newValue);settings.Add(phone);}
            settings.Add(new Button(()=>{ApplyVoice();if(voiceEnabled)speech.Speak(Current.Greeting);}){text="Прослушать приветствие"});
            settings.Add(new Button(()=>{if(speech!=null)speech.Cancel();}){text="Остановить озвучку"});
        }
        else{var completed=new Toggle(p.isBoss?"Игрок уже принят на работу":"Заём уже получен"){value=completedScenario};completed.RegisterValueChangedCallback(e=>completedScenario=e.newValue);settings.Add(completed);StringField(p,"modelPath","Модель Gemma .litertlm");StringField(p,"pythonPath","Python LiteRT-LM");settings.Add(new Label("Источник: "+p.sourceScene+"\n"+p.sourceObject+"\n\nНовый тест сбрасывает только тестовую историю, решение и доверие выбранного персонажа. Игровые деньги и сохранения не изменяются."));}
    }
    void StringField(object target,string field,string label,bool multiline=false)
    {
        var member=target.GetType().GetField(field);var control=new TextField(label){value=(string)member.GetValue(target)??"",multiline=multiline};
        if(multiline)control.AddToClassList("long-field");control.RegisterValueChangedCallback(e=>member.SetValue(target,e.newValue));settings.Add(control);
    }
    void FloatField(object target,string field,string label,float low,float high)
    {
        var member=target.GetType().GetField(field);var control=new Slider(label,low,high){value=(float)member.GetValue(target),showInputField=true};
        control.RegisterValueChangedCallback(e=>member.SetValue(target,Mathf.Clamp(e.newValue,low,high)));settings.Add(control);
    }
    public void SaveSelected()
    {
#if UNITY_EDITOR
        var source=profiles[SelectedIndex];var copy=Current;string originalName=source.name;
        var staffCopy=copy.staff;
        if(staffCopy!=null){string n=source.staff.name;UnityEditor.EditorUtility.CopySerialized(staffCopy,source.staff);source.staff.name=n;UnityEditor.EditorUtility.SetDirty(source.staff);UnityEditor.AssetDatabase.SaveAssetIfDirty(source.staff);}
        copy.staff=source.staff;UnityEditor.EditorUtility.CopySerialized(copy,source);source.name=originalName;copy.staff=staffCopy;
        UnityEditor.EditorUtility.SetDirty(source);UnityEditor.AssetDatabase.SaveAssetIfDirty(source);
        settingsHint.text="Сохранено в "+UnityEditor.AssetDatabase.GetAssetPath(source)+". Игровой профиль не изменён.";
#else
        settingsHint.text="Постоянное сохранение тестовых профилей доступно в Unity Editor.";
#endif
    }
    void OnDestroy()
    {
        version++;if(worker!=null)worker.Stop();ReleaseVoice();Application.runInBackground=previousBackground;
        if(copies!=null)foreach(var p in copies){if(p.staff!=null)Destroy(p.staff);Destroy(p);}
        if(panel!=null)Destroy(panel);
    }
}
