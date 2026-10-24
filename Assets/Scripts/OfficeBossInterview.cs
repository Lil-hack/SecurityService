using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

[RequireComponent(typeof(OfficeBossNpc))]
public sealed class OfficeBossInterview : MonoBehaviour
{
    public float interactionDistance=3.2f;
    public string modelPath="Models/gemma-4-E2B-it.litertlm";
    public string pythonPath=".local/gemma-runtime/Scripts/python.exe";
    public string voicePath="Models/Piper/Mantella-RU-Piper-1.0/SKSE/Plugins/MantellaSoftware/piper/models/skyrim/low/malecommoner.onnx";
    public event System.Action<string,string> TimingEvent;
    public bool Active {get;private set;}
    public bool Hired {get;private set;}
    [Serializable] sealed class Command {public string command,text;}
    [Serializable] sealed class Reply {public string kind,text;public bool accepted,valid;}
    OfficeBossNpc boss;OfficeThirdPersonController player;GemmaSpeech speech;
    UIDocument document;PanelSettings panel;VisualElement overlay;ScrollView history;TextField input;Label status;Button send,retry;
    Texture2D chatPortrait;
    bool streamingReply;Label streamingBody,thinkingLine;
    AudioClip[] thinkingClips;int lastThinkingIndex=-1;
    static readonly string[] ThinkingPhrases={
        "Так, дайте подумать. Хорошие решения любят небольшую паузу.",
        "Секундочку, сейчас разложу ваши слова по полочкам в голове.",
        "Хм, любопытно. Дайте свериться с моим внутренним кадровиком.",
        "Так, мысль поймал. Теперь посмотрим, куда она нас приведёт.",
        "Одну минутку. Даже у начальника мысли ходят не строем.",
        "Дайте сообразить. Тут главное не спешить с умным видом.",
        "Хм, сейчас подумаю. Вопрос простой, а ответ любит паузу.",
        "Секунду, сопоставляю факты. Кофе в этом деле не всесилен.",
        "Так, занятный поворот. Дайте мне немного переварить сказанное.",
        "Сейчас прикину варианты. Голова работает, просто без индикатора."};
    Animator pausedPlayerAnimator;float previousAnimatorSpeed=1;
    Process worker;readonly ConcurrentQueue<Action> pending=new ConcurrentQueue<Action>();
    bool ready,busy,oldRoutine,oldControl,closing;int generation;float requestStarted,speechStarted;string opening,diagnostics="",pendingBossLine;
    Quaternion bossRotation;CursorLockMode oldLock;bool oldVisible,oldBackground;
    string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,".."));
    void Awake(){boss=GetComponent<OfficeBossNpc>();Hired=PlayerPrefs.GetInt("Office.BossHired",0)==1;}
    void Start(){var p=GameObject.FindGameObjectWithTag("Player");if(p!=null)player=p.GetComponent<OfficeThirdPersonController>();BuildUI();PrepareVoice();thinkingClips=Resources.LoadAll<AudioClip>("Audio/BossThinking");Array.Sort(thinkingClips,(a,b)=>string.CompareOrdinal(a.name,b.name));}
    public bool CanTalk()
    {
        if(Active||player==null||!player.ControlEnabled||LaptopDesktop.SessionActive||LaptopDesktop.BlocksWorldUse)return false;
        Vector3 delta=transform.position-player.transform.position;delta.y=0;
        if(delta.magnitude>interactionDistance)return false;
        Vector3 from=player.transform.position+Vector3.up*1.5f,to=transform.position+Vector3.up*1.5f;
        foreach(var hit in Physics.RaycastAll(from,(to-from).normalized,Vector3.Distance(from,to),~0,QueryTriggerInteraction.Ignore))
            if(!hit.transform.IsChildOf(player.transform)&&!hit.transform.IsChildOf(transform)&&hit.distance<Vector3.Distance(from,to)-.25f)return false;
        return true;
    }
    void Update()
    {
        while(pending.TryDequeue(out var action))action();
        if(!Active)
        {if(Application.isFocused&&Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame&&CanTalk())BeginInterview();return;}
        // Keep input focus and world use separate, even when other scripts inspect the cursor.
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){EndInterview();return;}
        Vector3 direction=player.transform.position-transform.position;direction.y=0;
        if(direction.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),Time.unscaledDeltaTime*5);
        if(busy&&Time.realtimeSinceStartup-requestStarted>180){StopWorker();status.text="Ответ задерживается. Попробуйте ещё раз или завершите разговор.";busy=false;UpdateControls();}
        if(closing&&!speech.HasPendingSpeech&&Time.realtimeSinceStartup-speechStarted>2)EndInterview();
        UpdateControls();
    }
    void OnGUI()
    {
        if(!CanTalk())return;
        var rect=new Rect(Screen.width*.5f-155,Screen.height-120,310,46);
        if(GUI.Button(rect,Hired?"E — поговорить с директором":"E — собеседование с боссом"))BeginInterview();
    }
    public void BeginInterview()
    {
        if(!CanTalk())return;
        TimingEvent?.Invoke("dialogue_begin", "");
        Active=true;closing=false;oldRoutine=boss.automaticRoutine;boss.automaticRoutine=false;
        bossRotation=transform.rotation;oldControl=player.ControlEnabled;player.SetControlEnabled(false);PausePlayerAnimation();
        oldLock=Cursor.lockState;oldVisible=Cursor.visible;oldBackground=Application.runInBackground;Application.runInBackground=true;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        boss.PlayAction(OfficeBossNpc.Action.Talk,120);
        overlay.style.display=DisplayStyle.Flex;history.Clear();input.value="";
        opening=Hired?"Вы уже в команде! Добро пожаловать, коллега.":"Здравствуйте! Расскажите о себе: почему я должен принять вас на работу?";

        PrepareVoice();
        speech.PlaybackStarted-=RevealBossLine;speech.PlaybackStarted+=RevealBossLine;
        speech.StatusChanged-=OnVoiceStatus;speech.StatusChanged+=OnVoiceStatus;
        SayBoss(opening);
        if(Hired){closing=true;speechStarted=Time.realtimeSinceStartup;status.text="Вы приняты · Esc — закрыть";}else StartWorker();
        input.Blur();UpdateControls();
    }
    void PrepareVoice()
    {
        speech=GetComponent<GemmaSpeech>();if(speech==null)speech=gameObject.AddComponent<GemmaSpeech>();speech.enabled=true;
        speech.TelephoneEnabled=false;speech.PitchSemitones=3;speech.Rasp=.06f;speech.Tremble=.3f;speech.VolumeVariation=.12f;speech.SpeechRate=1.3f;speech.PauseSeconds=.045f;speech.Volume=1;
        speech.Initialize(Path.Combine(Root,pythonPath),Root,voicePath);
    }
    VisualElement ChatAvatar(bool small=false)
    {
        var avatar=new VisualElement();avatar.AddToClassList("boss-avatar");if(small)avatar.AddToClassList("boss-avatar-small");
        if(chatPortrait!=null){var image=new Image{image=chatPortrait,scaleMode=ScaleMode.ScaleAndCrop};image.style.width=Length.Percent(100);image.style.height=Length.Percent(100);image.pickingMode=PickingMode.Ignore;avatar.Add(image);}
        avatar.pickingMode=PickingMode.Ignore;return avatar;
    }
    void PausePlayerAnimation()
    {
        var combat=player.GetComponent<OfficePunchCombat>();if(combat!=null)combat.Cancel();
        pausedPlayerAnimator=player.humanoidAnimator;if(pausedPlayerAnimator==null)return;
        previousAnimatorSpeed=pausedPlayerAnimator.speed;
        pausedPlayerAnimator.SetFloat("Speed",0);pausedPlayerAnimator.SetFloat("VerticalSpeed",0);pausedPlayerAnimator.SetBool("Grounded",true);
        pausedPlayerAnimator.Play("Locomotion",0,0);pausedPlayerAnimator.Update(0);
        pausedPlayerAnimator.speed=0;
    }
    void BuildUI()
    {
        panel=ScriptableObject.CreateInstance<PanelSettings>();panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(1600,900);panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("GemmaTheme");panel.sortingOrder=500;
        document=GetComponent<UIDocument>();if(document==null)document=gameObject.AddComponent<UIDocument>();document.panelSettings=panel;
        var root=document.rootVisualElement;root.style.flexGrow=1;root.pickingMode=PickingMode.Ignore;root.styleSheets.Add(Resources.Load<StyleSheet>("OfficeBossChat"));
        chatPortrait=Resources.Load<Texture2D>("Characters/BossPortrait");
        overlay=new VisualElement{name="boss-dialog"};overlay.AddToClassList("boss-dialog");root.Add(overlay);
        var header=new VisualElement();header.AddToClassList("boss-header");overlay.Add(header);header.Add(ChatAvatar());
        var heading=new VisualElement();heading.style.flexGrow=1;heading.style.minWidth=0;header.Add(heading);
        var title=new Label("Директор");title.AddToClassList("boss-title");heading.Add(title);
        var online=new VisualElement();online.AddToClassList("boss-online");var dot=new VisualElement();dot.AddToClassList("boss-dot");online.Add(dot);
        var presence=new Label("В сети");presence.AddToClassList("boss-presence");online.Add(presence);heading.Add(online);
        var close=new Button(EndInterview){name="boss-chat-close",text="×",tooltip="Завершить разговор · Esc"};close.AddToClassList("boss-more");close.AddToClassList("boss-close");header.Add(close);
        status=new Label("Директор готов к разговору");status.AddToClassList("boss-status");overlay.Add(status);
        history=new ScrollView(ScrollViewMode.Vertical){name="boss-chat-history"};history.AddToClassList("boss-history");history.horizontalScrollerVisibility=ScrollerVisibility.Hidden;overlay.Add(history);
        retry=new Button(StartWorker){text="Попробовать снова"};retry.AddToClassList("boss-retry");overlay.Add(retry);
        var compose=new VisualElement();compose.AddToClassList("boss-compose");overlay.Add(compose);
        input=new TextField{name="boss-chat-input",multiline=true,maxLength=1200};input.AddToClassList("boss-input");compose.Add(input);
        bool typingInput=false;
        var placeholder=new Label("Введите сообщение…"){pickingMode=PickingMode.Ignore};placeholder.AddToClassList("boss-placeholder");input.Add(placeholder);
        input.RegisterValueChangedCallback(e=>placeholder.style.display=!typingInput&&string.IsNullOrEmpty(e.newValue)?DisplayStyle.Flex:DisplayStyle.None);
        input.RegisterCallback<FocusInEvent>(_=>{typingInput=true;placeholder.style.display=DisplayStyle.None;});
        input.RegisterCallback<FocusOutEvent>(_=>{typingInput=false;placeholder.style.display=string.IsNullOrEmpty(input.value)?DisplayStyle.Flex:DisplayStyle.None;});
        input.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Return&&!e.shiftKey){Submit();e.StopPropagation();e.PreventDefault();}},TrickleDown.TrickleDown);
        send=new Button(Submit){text="➤",tooltip="Отправить · Enter"};send.AddToClassList("boss-send");compose.Add(send);
        overlay.style.display=DisplayStyle.None;
    }
    void SayBoss(string text)
    {
        TimingEvent?.Invoke("tts_requested",text);
        pendingBossLine=text;
        if(VoiceFailed(speech.Status)){RevealBossLine();return;}
        speech.Speak(text);
    }
    static bool VoiceFailed(string value)=>value.StartsWith("Ошибка")||value.StartsWith("Нет голоса")||value.StartsWith("Не найден")||value.StartsWith("Piper завершён");
    void RevealBossLine()
    {
        if(!Active||string.IsNullOrEmpty(pendingBossLine))return;
        if(thinkingLine!=null){thinkingLine.parent.parent.RemoveFromHierarchy();thinkingLine=null;}
        TimingEvent?.Invoke("text_visible",pendingBossLine);
        if(streamingReply)
        {
            if(streamingBody==null)streamingBody=AddMessage("Директор",pendingBossLine);
            else {streamingBody.text+=" "+pendingBossLine;history.schedule.Execute(()=>history.ScrollTo(streamingBody));}
        }
        else AddMessage("Директор",pendingBossLine);
        pendingBossLine=null;
    }
    void OnVoiceStatus(string value)
    {
        if(Active&&VoiceFailed(value)){RevealBossLine();speech.Cancel();}
    }
    Label AddMessage(string who,string text)
    {
        bool director=who=="Директор";
        var row=new VisualElement();row.AddToClassList("boss-row");if(!director)row.AddToClassList("boss-row-player");else row.Add(ChatAvatar(true));
        var bubble=new VisualElement();bubble.AddToClassList("boss-bubble");if(!director)bubble.AddToClassList("boss-bubble-player");row.Add(bubble);
        var meta=new VisualElement();meta.AddToClassList("boss-message-meta");bubble.Add(meta);
        var name=new Label(who){enableRichText=false};name.AddToClassList("boss-message-name");if(!director)name.AddToClassList("boss-player-name");meta.Add(name);
        var time=new Label(DateTime.Now.ToString("HH:mm"));time.AddToClassList("boss-time");if(!director)time.AddToClassList("boss-player-time");meta.Add(time);
        var body=new Label(text){enableRichText=false};body.AddToClassList("boss-message-text");bubble.Add(body);
        if(!director){var checks=new Label("✓✓");checks.AddToClassList("boss-checks");bubble.Add(checks);}
        history.Add(row);history.schedule.Execute(()=>history.ScrollTo(row));return body;
    }
    void PlayThinkingPhrase()
    {
        if(thinkingClips==null||thinkingClips.Length==0)return;
        int count=Math.Min(thinkingClips.Length,ThinkingPhrases.Length);
        int index=UnityEngine.Random.Range(0,count);
        if(count>1&&index==lastThinkingIndex)index=(index+1+UnityEngine.Random.Range(0,count-1))%count;
        lastThinkingIndex=index;
        thinkingLine=AddMessage("Директор",ThinkingPhrases[index]);thinkingLine.style.opacity=.75f;
        TimingEvent?.Invoke("thinking_phrase",ThinkingPhrases[index]);
        speech.PlayPrepared(thinkingClips[index],ThinkingPhrases[index]);
    }
    public void Submit()
    {
        if(!Active||!ready||busy||closing||speech.HasPendingSpeech||string.IsNullOrWhiteSpace(input.value))return;
        string text=input.value.Trim();TimingEvent?.Invoke("player_submit",text);input.value="";AddMessage(string.IsNullOrWhiteSpace(LaptopDesktop.PlayerName)?"Вы":LaptopDesktop.PlayerName,text);
        streamingReply=false;streamingBody=null;pendingBossLine=null;
        busy=true;requestStarted=Time.realtimeSinceStartup;status.text="Директор обдумывает ваш ответ…";PlayThinkingPhrase();Write("interview",text);UpdateControls();
    }
    void StartWorker()
    {
        if(!Active||closing)return;StopWorker();diagnostics="";ready=false;busy=true;requestStarted=Time.realtimeSinceStartup;
        status.text="Послушайте приветствие директора.";
        int current=++generation;
        try
        {
            string python=Path.Combine(Root,pythonPath),model=Path.Combine(Root,modelPath);
            if(!File.Exists(python)||!File.Exists(model))throw new FileNotFoundException("Не найдены локальная среда Python или модель Gemma в Models.");
            worker=new Process{StartInfo=new ProcessStartInfo{FileName=python,Arguments="-u \""+Path.Combine(Root,"Tools/Gemma/worker.py")+"\" --model \""+model+"\" --system-prompt \""+Path.Combine(Root,"Tools/Gemma/Prompts/office_boss.txt")+"\"",WorkingDirectory=Root,UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8},EnableRaisingEvents=true};
            worker.StartInfo.EnvironmentVariables["PYTHONUTF8"]="1";
            worker.OutputDataReceived+=(_,e)=>{if(!string.IsNullOrEmpty(e.Data))pending.Enqueue(()=>{if(current==generation)Receive(e.Data);});};
            worker.ErrorDataReceived+=(_,e)=>{if(!string.IsNullOrEmpty(e.Data))pending.Enqueue(()=>{if(current==generation){diagnostics+=e.Data+"\n";if(diagnostics.Length>2000)diagnostics=diagnostics.Substring(diagnostics.Length-2000);}});};
            worker.Exited+=(_,__)=>pending.Enqueue(()=>{if(current==generation&&Active){ready=busy=false;status.text="Не удалось получить ответ. Попробуйте ещё раз или нажмите Esc.";UnityEngine.Debug.LogWarning("Boss Gemma: "+diagnostics);}});
            worker.Start();worker.BeginOutputReadLine();worker.BeginErrorReadLine();
        }
        catch(Exception e){StopWorker();UnityEngine.Debug.LogWarning("Boss setup: "+e.Message);status.text="Разговор пока недоступен. Попробуйте ещё раз.";}
        UpdateControls();
    }
    void Receive(string json)
    {
        Reply reply;try{reply=JsonUtility.FromJson<Reply>(json);}catch{return;}
        if(reply==null||!Active)return;
        if(reply.kind=="ready"||reply.kind=="interview_result"||reply.kind=="interview_sentence")TimingEvent?.Invoke(reply.kind,json);
        if(reply.kind=="ready"){ready=true;busy=false;status.text="Введите сообщение · Enter — отправить";}
        else if(reply.kind=="interview_sentence")
        {
            if(!busy||string.IsNullOrWhiteSpace(reply.text))return;
            if(!streamingReply){streamingReply=true;speech.BeginStream(true);}
            pendingBossLine=string.IsNullOrEmpty(pendingBossLine)?reply.text:pendingBossLine+" "+reply.text;
            TimingEvent?.Invoke("tts_requested",reply.text);
            speech.AppendText(reply.text+" ");status.text="Директор отвечает…";
        }
        else if(reply.kind=="interview_result")
        {
            busy=false;
            string answer=reply.text;
            if(string.IsNullOrWhiteSpace(answer))answer=reply.valid&&reply.accepted?"Вы приняты! Добро пожаловать в команду.":"Расскажите конкретнее о своих навыках и пользе для компании.";
            if(streamingReply)
            {
                speech.EndStream();
                if(!speech.HasPendingSpeech)RevealBossLine();
            }
            else if(thinkingLine!=null)
            {
                streamingReply=true;pendingBossLine=answer;speech.BeginStream(true);speech.AppendText(answer);speech.EndStream();
            }
            else SayBoss(answer);
            boss.PlayAction(reply.accepted&&reply.valid?OfficeBossNpc.Action.Nod:OfficeBossNpc.Action.Talk,10);
            if(reply.valid&&reply.accepted){Hired=true;closing=true;speechStarted=Time.realtimeSinceStartup;PlayerPrefs.SetInt("Office.BossHired",1);PlayerPrefs.Save();status.text="Вы приняты! Разговор завершится после ответа директора.";}
            else {status.text="Уточните свою причину и попробуйте ещё раз.";input.Focus();}
        }
        else if(reply.kind=="fatal"){speech.Cancel();RevealBossLine();streamingReply=false;StopWorker();UnityEngine.Debug.LogWarning("Boss worker: "+reply.text);status.text="Не удалось получить ответ. Попробуйте ещё раз.";}
        UpdateControls();
    }
    void Write(string command,string text)
    {
        try{byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Command{command=command,text=text})+"\n");worker.StandardInput.BaseStream.Write(bytes,0,bytes.Length);worker.StandardInput.BaseStream.Flush();}
        catch(Exception e){StopWorker();UnityEngine.Debug.LogWarning("Boss send: "+e.Message);status.text="Сообщение не отправлено. Попробуйте ещё раз.";}
    }
    void UpdateControls()
    {
        if(send==null)return;send.SetEnabled(Active&&ready&&!busy&&!closing&&(speech==null||!speech.HasPendingSpeech));input.SetEnabled(!closing);
        retry.style.display=Active&&!ready&&!busy&&!closing?DisplayStyle.Flex:DisplayStyle.None;
    }
    public void EndInterview()
    {
        if(!Active)return;TimingEvent?.Invoke("dialogue_end", "");Active=false;closing=false;StopWorker();if(speech!=null){speech.PlaybackStarted-=RevealBossLine;speech.StatusChanged-=OnVoiceStatus;speech.Cancel();}pendingBossLine=null;streamingReply=false;streamingBody=null;thinkingLine=null;
        if(overlay!=null)overlay.style.display=DisplayStyle.None;
        boss.automaticRoutine=oldRoutine;transform.rotation=bossRotation;if(oldRoutine)boss.PlayAction(OfficeBossNpc.Action.Idle,1);
        if(pausedPlayerAnimator!=null){pausedPlayerAnimator.speed=previousAnimatorSpeed;pausedPlayerAnimator=null;}
        if(player!=null)player.SetControlEnabled(oldControl);Cursor.lockState=oldLock;Cursor.visible=oldVisible;Application.runInBackground=oldBackground;
    }
    void StopWorker()
    {
        generation++;ready=busy=false;if(worker==null)return;
        try{if(!worker.HasExited)worker.Kill();}catch{}worker.Dispose();worker=null;
    }
    void OnDisable(){EndInterview();StopWorker();if(speech!=null)speech.enabled=false;}
    void OnDestroy(){if(panel!=null)Destroy(panel);}
}

