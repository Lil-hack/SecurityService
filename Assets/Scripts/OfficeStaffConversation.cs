using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor=UnityEngine.Cursor;

[DisallowMultipleComponent,RequireComponent(typeof(OfficeStaffPhysics))]
public sealed class OfficeStaffConversation : MonoBehaviour
{
    public OfficeStaffPersona persona;
    public float interactionDistance=2.8f;
    public string modelPath="Models/gemma-4-E2B-it.litertlm",pythonPath=".local/gemma-runtime/Scripts/python.exe";
    public static OfficeStaffConversation Current {get;private set;}
    public bool Active=>Current==this;
    public bool Ready=>ready;
    public bool Busy=>busy;
    public string LastReply {get;private set;}
    public string EvaluationBackend {get;private set;}
    public string LastOutcome=>lastOutcome;
    public OfficeLoanProgress.State Progress {get;private set;}
    public event Action<string,string> TimingEvent;
    [Serializable]sealed class PersonaData {public string displayName,personality,preferences;public int difficulty;}
    [Serializable]sealed class GameState {public float trust;public bool loan_requested,payment_granted;public int amount;public string outcome;}
    [Serializable]sealed class Command {public string command,text;public PersonaData persona;public string[] accepted_messages;public OfficeLoanProgress.Line[] history;public GameState game_state;}
    [Serializable]sealed class Reply {public string kind,text,outcome,backend;public bool valid,novel,loan_request;}
    OfficeThirdPersonController player;
    OfficeStaffPhysics physics;
    OfficeStaffPatrol patrol;
    OfficeStaffTyping typing;
    Animator animator,playerAnimator;
    GemmaSpeech speech;
    Process worker;
    readonly ConcurrentQueue<Action> pending=new ConcurrentQueue<Action>();
    UIDocument document;PanelSettings panel;
    VisualElement overlay;ScrollView history;TextField input;Button send,retry;Label status,mission,payment,streamingBody;ProgressBar trust;
    bool ready,busy,closing,replyFinished,paymentTurn,streamOpen,oldControl,oldPatrol,oldTyping,oldBackground,oldKinematic;
    float npcAnimationSpeed,playerAnimationSpeed,started,closingStarted;
    int generation;
    string lastMessage,pendingText,lastOutcome="neutral",diagnostics="";
    Quaternion originalRotation;
    CursorLockMode oldLock;bool oldCursor;
    OfficeLoanProgress.Line[] turnHistory;
    string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,".."));

    void Start()
    {
        physics=GetComponent<OfficeStaffPhysics>();patrol=GetComponent<OfficeStaffPatrol>();typing=GetComponent<OfficeStaffTyping>();animator=GetComponentInChildren<Animator>();
        var actor=GameObject.FindGameObjectWithTag("Player");if(actor!=null)player=actor.GetComponent<OfficeThirdPersonController>();
        BuildUI();
    }
    bool Nearby()
    {
        if(persona==null||player==null||!player.ControlEnabled||Current!=null||LaptopDesktop.SessionActive||LaptopDesktop.BlocksWorldUse||physics.IsDown)return false;
        Vector3 delta=transform.position-player.transform.position;delta.y=0;return delta.magnitude<=interactionDistance;
    }
    public bool CanTalk()
    {
        if(!Nearby())return false;
        float distance=(transform.position-player.transform.position).sqrMagnitude;
        foreach(var other in FindObjectsByType<OfficeStaffConversation>(FindObjectsSortMode.None))
            if(other!=this&&other.Nearby()&&(other.transform.position-player.transform.position).sqrMagnitude<distance)return false;
        Vector3 from=player.transform.position+Vector3.up*1.5f,to=transform.position+Vector3.up*1.5f;
        foreach(var hit in Physics.RaycastAll(from,(to-from).normalized,Vector3.Distance(from,to),~0,QueryTriggerInteraction.Ignore))
            if(!hit.transform.IsChildOf(transform)&&!hit.transform.IsChildOf(player.transform)&&hit.distance<Vector3.Distance(from,to)-.22f)return false;
        return true;
    }
    void Update()
    {
        while(pending.TryDequeue(out var action))action();
        if(!Active){if(Application.isFocused&&Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame&&CanTalk())BeginDialogue();return;}
        if(physics.IsDown||player==null){EndDialogue();return;}
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){EndDialogue();return;}
        var delta=player.transform.position-transform.position;delta.y=0;
        if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),Time.unscaledDeltaTime*5);
        if(busy&&Time.realtimeSinceStartup-started>180)Fail("Ответ задерживается. Попробуйте ещё раз.");
        if(closing&&replyFinished&&(speech==null||!speech.HasPendingSpeech||VoiceFailed())&&Time.realtimeSinceStartup-closingStarted>2)EndDialogue();
        Controls();
    }
    void OnGUI()
    {
        if(CanTalk()&&GUI.Button(new Rect(Screen.width*.5f-180,Screen.height-120,360,46),"E — поговорить: "+persona.displayName))BeginDialogue();
    }
    public bool BeginDialogue()
    {
        if(!CanTalk())return false;
        Current=this;Progress=OfficeLoanProgress.Get(persona);originalRotation=transform.rotation;
        oldPatrol=patrol!=null&&patrol.enabled;oldTyping=typing!=null&&typing.enabled;
        if(patrol!=null)patrol.enabled=false;if(typing!=null)typing.enabled=false;
        npcAnimationSpeed=animator.speed;
        if(!oldTyping)
        {
            foreach(var parameter in animator.parameters)if(parameter.name=="PatrolSpeed")animator.SetFloat("PatrolSpeed",0);
            if(animator.HasState(0,Animator.StringToHash("Idle")))animator.Play("Idle",0,0);
            animator.Update(.1f);
        }
        animator.speed=0;
        var body=GetComponent<Rigidbody>();oldKinematic=body==null||body.isKinematic;if(body!=null)body.isKinematic=true;
        oldControl=player.ControlEnabled;player.SetControlEnabled(false);
        var combat=player.GetComponent<OfficePunchCombat>();if(combat!=null)combat.Cancel();
        playerAnimator=player.humanoidAnimator;if(playerAnimator!=null)
        {
            playerAnimationSpeed=playerAnimator.speed;playerAnimator.SetFloat("Speed",0);playerAnimator.SetFloat("VerticalSpeed",0);playerAnimator.SetBool("Grounded",true);
            playerAnimator.Play("Locomotion",0,0);playerAnimator.Update(0);playerAnimator.speed=0;
        }
        oldLock=Cursor.lockState;oldCursor=Cursor.visible;oldBackground=Application.runInBackground;Application.runInBackground=true;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        closing=replyFinished=paymentTurn=streamOpen=false;pendingText=null;streamingBody=null;LastReply="";lastMessage="";
        overlay.style.display=DisplayStyle.Flex;history.Clear();input.value="";input.Blur();
        PrepareVoice();speech.PlaybackStarted+=Reveal;speech.StatusChanged+=VoiceStatus;
        bool paid=OfficeLoanProgress.Paid(persona);string greeting=paid?persona.alreadyPaidGreeting:persona.greeting;
        AddMessage(false,greeting);LastReply=greeting;
        var clip=paid?persona.alreadyPaidClip:persona.greetingClip;
        if(clip!=null&&persona.PreparedMatches(greeting,paid?persona.preparedPaid:persona.preparedGreeting))speech.PlayPrepared(clip,greeting);else if(!VoiceFailed())speech.Speak(greeting);
        if(paid){closing=replyFinished=true;closingStarted=Time.realtimeSinceStartup;status.text="Этот заём уже получен. Помните о долге.";}
        else{OfficeLoanProgress.Remember(Progress,"character",greeting);StartWorker();}
        RefreshTrust();TimingEvent?.Invoke("dialogue_begin",persona.id);return true;
    }
    void PrepareVoice()
    {
        speech=GetComponent<GemmaSpeech>();if(speech==null)speech=gameObject.AddComponent<GemmaSpeech>();speech.enabled=true;
        speech.TelephoneEnabled=false;speech.SpeechRate=persona.speechRate;speech.PitchSemitones=persona.pitchSemitones;speech.Rasp=persona.rasp;speech.Tremble=persona.tremble;speech.VolumeVariation=persona.volumeVariation;speech.PauseSeconds=.04f;speech.Volume=persona.volume;
        speech.Initialize(Path.Combine(Root,pythonPath),Root,persona.voicePath);
    }
    VisualElement Avatar(bool small=false)
    {
        var avatar=new VisualElement();avatar.AddToClassList("boss-avatar");if(small)avatar.AddToClassList("boss-avatar-small");
        if(persona.portrait!=null){var image=new Image{image=persona.portrait,scaleMode=ScaleMode.ScaleAndCrop,pickingMode=PickingMode.Ignore};image.style.width=Length.Percent(100);image.style.height=Length.Percent(100);avatar.Add(image);}return avatar;
    }
    void BuildUI()
    {
        if(persona==null)return;
        panel=ScriptableObject.CreateInstance<PanelSettings>();panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(1600,900);panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("GemmaTheme");panel.sortingOrder=510;
        document=gameObject.AddComponent<UIDocument>();document.panelSettings=panel;var root=document.rootVisualElement;root.pickingMode=PickingMode.Ignore;root.style.flexGrow=1;
        root.styleSheets.Add(Resources.Load<StyleSheet>("OfficeBossChat"));root.styleSheets.Add(Resources.Load<StyleSheet>("OfficeStaffChat"));
        overlay=new VisualElement{name="staff-dialog-"+persona.id};overlay.AddToClassList("boss-dialog");overlay.AddToClassList("staff-dialog");root.Add(overlay);
        overlay.style.borderTopColor=overlay.style.borderRightColor=overlay.style.borderBottomColor=overlay.style.borderLeftColor=persona.accent;
        var header=new VisualElement();header.AddToClassList("boss-header");overlay.Add(header);header.Add(Avatar());
        var heading=new VisualElement();heading.style.flexGrow=1;header.Add(heading);var title=new Label(persona.displayName);title.AddToClassList("boss-title");heading.Add(title);
        var tier=new Label("Уровень "+persona.difficulty+" · заём "+OfficeTeamDashboard.Money(persona.Reward));tier.AddToClassList("boss-presence");heading.Add(tier);
        var close=new Button(EndDialogue){text="×",tooltip="Завершить разговор · Esc"};close.AddToClassList("boss-more");close.AddToClassList("boss-close");header.Add(close);
        var objective=new Label("Расположите коллегу и попросите деньги в долг.");objective.AddToClassList("staff-objective");overlay.Add(objective);
        trust=new ProgressBar{lowValue=0,highValue=100};trust.AddToClassList("staff-trust");overlay.Add(trust);
        mission=new Label();mission.AddToClassList("staff-mission");overlay.Add(mission);
        payment=new Label();payment.AddToClassList("staff-payment");overlay.Add(payment);
        status=new Label();status.AddToClassList("boss-status");overlay.Add(status);
        history=new ScrollView(ScrollViewMode.Vertical);history.AddToClassList("boss-history");history.horizontalScrollerVisibility=ScrollerVisibility.Hidden;overlay.Add(history);
        retry=new Button(StartWorker){text="Попробовать снова"};retry.AddToClassList("boss-retry");overlay.Add(retry);
        var compose=new VisualElement();compose.AddToClassList("boss-compose");overlay.Add(compose);
        input=new TextField{multiline=true,maxLength=1000};input.AddToClassList("boss-input");compose.Add(input);
        var placeholder=new Label("Введите сообщение…"){pickingMode=PickingMode.Ignore};placeholder.AddToClassList("boss-placeholder");input.Add(placeholder);
        bool focus=false;input.RegisterValueChangedCallback(e=>placeholder.style.display=!focus&&string.IsNullOrEmpty(e.newValue)?DisplayStyle.Flex:DisplayStyle.None);
        input.RegisterCallback<FocusInEvent>(_=>{focus=true;placeholder.style.display=DisplayStyle.None;});input.RegisterCallback<FocusOutEvent>(_=>{focus=false;placeholder.style.display=string.IsNullOrEmpty(input.value)?DisplayStyle.Flex:DisplayStyle.None;});
        input.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Return&&!e.shiftKey){Submit();e.PreventDefault();e.StopPropagation();}},TrickleDown.TrickleDown);
        send=new Button(Submit){text="➤",tooltip="Отправить · Enter"};send.AddToClassList("boss-send");compose.Add(send);overlay.style.display=DisplayStyle.None;
    }
    Label AddMessage(bool user,string text)
    {
        var row=new VisualElement();row.AddToClassList("boss-row");if(user)row.AddToClassList("boss-row-player");else row.Add(Avatar(true));
        var bubble=new VisualElement();bubble.AddToClassList("boss-bubble");if(user)bubble.AddToClassList("boss-bubble-player");row.Add(bubble);
        var meta=new VisualElement();meta.AddToClassList("boss-message-meta");bubble.Add(meta);
        var name=new Label(user?(string.IsNullOrWhiteSpace(LaptopDesktop.PlayerName)?"Вы":LaptopDesktop.PlayerName):persona.displayName){enableRichText=false};name.AddToClassList("boss-message-name");meta.Add(name);
        var time=new Label(DateTime.Now.ToString("HH:mm"));time.AddToClassList("boss-time");meta.Add(time);
        var body=new Label(text){enableRichText=false};body.AddToClassList("boss-message-text");bubble.Add(body);history.Add(row);history.schedule.Execute(()=>history.ScrollTo(row));return body;
    }
    void RefreshTrust()
    {
        trust.value=Progress.Trust;trust.title="Доверие · "+Progress.Trust.ToString("0.#")+"%";
        mission.text="Разных подходов: "+Mathf.Min(Progress.accepted.Count,persona.RequiredPersuasions)+" / "+persona.RequiredPersuasions+" · цель 100%";
        bool paid=OfficeLoanProgress.Paid(persona);payment.style.display=paid?DisplayStyle.Flex:DisplayStyle.None;payment.text="Заём получен: "+OfficeTeamDashboard.Money(persona.Reward)+" · не забудьте вернуть!";
        if(!paid&&Progress.trustUnits>=120&&!Progress.loanRequested)status.text="Доверие полное. Теперь попросите деньги в долг.";
    }
    public bool SubmitText(string text)
    {
        if(!Active||!ready||busy||closing||speech.HasPendingSpeech&&!VoiceFailed()||string.IsNullOrWhiteSpace(text))return false;
        lastMessage=text.Trim();if(lastMessage.Length>1000)lastMessage=lastMessage.Substring(0,1000);
        input.value="";AddMessage(true,lastMessage);turnHistory=Progress.history.ToArray();OfficeLoanProgress.Remember(Progress,"user",lastMessage);
        busy=true;replyFinished=false;paymentTurn=false;streamOpen=false;streamingBody=null;pendingText=null;started=Time.realtimeSinceStartup;
        status.text=persona.displayName+" обдумывает ваши слова…";
        if(persona.thinkingClip!=null&&persona.PreparedMatches(persona.greeting,persona.preparedGreeting))speech.PlayPrepared(persona.thinkingClip,"Хм, дайте подумать…");
        Write("social_judge");TimingEvent?.Invoke("player_submit",lastMessage);Controls();return true;
    }
    public void Submit()=>SubmitText(input.value);
    void StartWorker()
    {
        if(!Active||closing)return;StopWorker();busy=true;started=Time.realtimeSinceStartup;status.text="Послушайте приветствие коллеги.";
        int version=++generation;
        try
        {
            string python=Path.Combine(Root,pythonPath),model=GemmaModelFiles.Resolve(modelPath,Root);
            if(!File.Exists(python)||!File.Exists(model))throw new FileNotFoundException("Local Gemma runtime is unavailable");
            worker=new Process{StartInfo=new ProcessStartInfo{FileName=python,Arguments="-u \""+Path.Combine(Root,"Tools/Gemma/worker.py")+"\" --model \""+model+"\" --office-social",WorkingDirectory=Root,UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8},EnableRaisingEvents=true};
            worker.StartInfo.EnvironmentVariables["PYTHONUTF8"]="1";
            worker.OutputDataReceived+=(_,e)=>{if(!string.IsNullOrEmpty(e.Data))pending.Enqueue(()=>{if(version==generation)Receive(e.Data);});};
            worker.ErrorDataReceived+=(_,e)=>{if(!string.IsNullOrEmpty(e.Data))pending.Enqueue(()=>{if(version==generation){diagnostics+=e.Data+"\n";if(diagnostics.Length>1500)diagnostics=diagnostics.Substring(diagnostics.Length-1500);}});};
            worker.Exited+=(_,__)=>pending.Enqueue(()=>{if(version==generation&&Active)Fail("Не удалось получить ответ. Попробуйте ещё раз.");});
            worker.Start();worker.BeginOutputReadLine();worker.BeginErrorReadLine();
        }
        catch(Exception e){UnityEngine.Debug.LogWarning("Staff dialogue setup: "+e.Message);Fail("Разговор пока недоступен. Попробуйте ещё раз.");}
        Controls();
    }
    void Write(string command)
    {
        try
        {
            var data=new Command{command=command,text=lastMessage,persona=new PersonaData{displayName=persona.displayName,personality=persona.personality,preferences=persona.preferences,difficulty=persona.difficulty},accepted_messages=Progress.accepted.ToArray(),history=turnHistory??Progress.history.ToArray(),game_state=new GameState{trust=Progress.Trust,loan_requested=Progress.loanRequested,payment_granted=paymentTurn,amount=persona.Reward,outcome=lastOutcome}};
            byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(data)+"\n");worker.StandardInput.BaseStream.Write(bytes,0,bytes.Length);worker.StandardInput.BaseStream.Flush();
        }
        catch(Exception e){UnityEngine.Debug.LogWarning("Staff dialogue send: "+e.Message);Fail("Сообщение не отправлено. Попробуйте ещё раз.");}
    }
    void Receive(string json)
    {
        Reply reply;try{reply=JsonUtility.FromJson<Reply>(json);}catch{return;}if(reply==null||!Active)return;
        TimingEvent?.Invoke(reply.kind,json);
        if(reply.kind=="ready"){ready=true;busy=false;status.text="Введите сообщение · Enter — отправить";}
        else if(reply.kind=="social_evaluation")
        {
            EvaluationBackend=reply.backend;
            lastOutcome=reply.valid?reply.outcome:"unavailable";
            paymentTurn=OfficeLoanProgress.Apply(persona,Progress,lastMessage,reply.valid,reply.outcome,reply.novel,reply.loan_request);
            if(paymentTurn){closing=true;closingStarted=Time.realtimeSinceStartup;}
            RefreshTrust();Write("social_reply");
        }
        else if(reply.kind=="social_sentence")
        {
            pendingText=string.IsNullOrEmpty(pendingText)?reply.text:pendingText+" "+reply.text;
            if(VoiceFailed()){Reveal();speech.Cancel();}
            else{if(!streamOpen){speech.BeginStream(true);streamOpen=true;}speech.AppendText(reply.text+" ");}
            status.text=persona.displayName+" отвечает…";
        }
        else if(reply.kind=="social_done")
        {
            LastReply=reply.text;busy=false;replyFinished=true;if(streamOpen)speech.EndStream();
            if(string.IsNullOrWhiteSpace(reply.text)&&!streamOpen){pendingText="Не расслышал. Попробуйте сказать иначе.";Reveal();}
            if(!speech.HasPendingSpeech)Reveal();OfficeLoanProgress.Remember(Progress,"character",reply.text);
            status.text=closing?"Заём получен. Разговор завершится после ответа.":lastOutcome=="repeated"?"Попробуйте новый комплимент или другой аргумент.":lastOutcome=="negative"?"Доверие снизилось. Попробуйте восстановить отношения.":lastOutcome=="unavailable"?"Не удалось понять реплику. Доверие не изменилось.":"Введите сообщение · Enter — отправить";RefreshTrust();
        }
        else if(reply.kind=="fatal")Fail("Не удалось получить ответ. Попробуйте ещё раз.");
        Controls();
    }
    bool VoiceFailed()=>speech!=null&&(speech.Status.StartsWith("Ошибка")||speech.Status.StartsWith("Нет голоса")||speech.Status.StartsWith("Не найден")||speech.Status.StartsWith("Piper завершён"));
    void VoiceStatus(string _){if(Active&&VoiceFailed()){Reveal();speech.Cancel();}}
    void Reveal()
    {
        if(!Active||string.IsNullOrWhiteSpace(pendingText))return;
        if(streamingBody==null)streamingBody=AddMessage(false,pendingText);else streamingBody.text+=" "+pendingText;pendingText=null;
        history.schedule.Execute(()=>history.ScrollTo(streamingBody));
    }
    void Fail(string message)
    {
        StopWorker();busy=false;ready=false;status.text=message;
        if(streamOpen&&speech!=null)speech.EndStream();streamOpen=false;
        if(paymentTurn)
        {
            pendingText="Ладно, держи "+persona.Reward+" долларов в долг. Только верни — у меня кошелёк с памятью!";Reveal();replyFinished=closing=true;closingStarted=Time.realtimeSinceStartup;
        }
        else if(input!=null&&!string.IsNullOrEmpty(lastMessage))input.value=lastMessage;
        Controls();
    }
    void Controls()
    {
        if(send==null)return;
        send.SetEnabled(Active&&ready&&!busy&&!closing&&(speech==null||!speech.HasPendingSpeech||VoiceFailed()));input.SetEnabled(!closing);
        retry.style.display=Active&&!ready&&!busy&&!closing?DisplayStyle.Flex:DisplayStyle.None;
    }
    public void EndDialogue()
    {
        if(!Active)return;Current=null;StopWorker();
        if(speech!=null){speech.PlaybackStarted-=Reveal;speech.StatusChanged-=VoiceStatus;speech.Cancel();speech.enabled=false;}
        if(overlay!=null)overlay.style.display=DisplayStyle.None;
        transform.rotation=originalRotation;if(animator!=null)animator.speed=npcAnimationSpeed;
        if(patrol!=null)patrol.enabled=oldPatrol;if(typing!=null)typing.enabled=oldTyping;
        var body=GetComponent<Rigidbody>();if(body!=null)body.isKinematic=oldKinematic;
        if(playerAnimator!=null)playerAnimator.speed=playerAnimationSpeed;if(player!=null)player.SetControlEnabled(oldControl);
        Cursor.lockState=oldLock;Cursor.visible=oldCursor;Application.runInBackground=oldBackground;
        TimingEvent?.Invoke("dialogue_end",persona.id);
    }
    void StopWorker(){generation++;ready=busy=false;if(worker==null)return;try{if(!worker.HasExited)worker.Kill();}catch{}worker.Dispose();worker=null;}
    void OnDisable(){EndDialogue();StopWorker();}
    void OnDestroy(){if(panel!=null)Destroy(panel);}
}
