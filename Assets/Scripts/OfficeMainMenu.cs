using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor=UnityEngine.Cursor;

[RequireComponent(typeof(GemmaModelInstaller))]
public sealed class OfficeMainMenu : MonoBehaviour
{
    public Camera backgroundCamera;
    public Animator heroAnimator;
    public string gameScene="ExecutiveOffice";
    public string menuTitle="SCAMPLAY";
    public bool ModelReady=>installer!=null&&installer.Ready;
    GemmaModelInstaller installer;PanelSettings panel;UIDocument document;
    VisualElement root,modeScreen,settingsScreen,rulesScreen,downloadPanel;
    Label modelLabel,modelDetail,soloHint,loadingText;ProgressBar modelProgress;Button soloButton,retryButton,pauseButton;
    Vector3 cameraHome;Quaternion cameraRotation;bool loading,previousBackground;
    readonly List<VisualElement> modals=new List<VisualElement>();
    void Start()
    {
        previousBackground=Application.runInBackground;Application.runInBackground=true;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        cameraHome=backgroundCamera.transform.position;cameraRotation=backgroundCamera.transform.rotation;
        if(heroAnimator!=null){heroAnimator.applyRootMotion=false;heroAnimator.SetFloat("Speed",0);heroAnimator.SetBool("Grounded",true);heroAnimator.SetFloat("VerticalSpeed",0);heroAnimator.Play("Locomotion",0,0);}
        BuildUI();installer=GetComponent<GemmaModelInstaller>();installer.Changed+=UpdateDownload;installer.Begin();UpdateDownload();
    }
    void Update()
    {
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        if(backgroundCamera!=null)
        {
            backgroundCamera.transform.position=cameraHome+new Vector3(Mathf.Sin(Time.unscaledTime*.13f)*.12f,Mathf.Sin(Time.unscaledTime*.19f)*.035f,0);
            backgroundCamera.transform.rotation=cameraRotation*Quaternion.Euler(0,Mathf.Sin(Time.unscaledTime*.12f)*.65f,0);
        }
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)CloseModals();
    }
    Label Text(VisualElement parent,string text,string css)
    {var label=new Label(text);label.AddToClassList(css);parent.Add(label);return label;}
    Button Button(VisualElement parent,string text,string css,Action click)
    {var button=new Button(click){text=text};foreach(var name in css.Split(' '))button.AddToClassList(name);parent.Add(button);return button;}
    VisualElement Modal(string title,out VisualElement card)
    {
        var backdrop=new VisualElement();backdrop.AddToClassList("menu-backdrop");root.Add(backdrop);backdrop.style.display=DisplayStyle.None;modals.Add(backdrop);
        card=new VisualElement();card.AddToClassList("menu-modal");backdrop.Add(card);
        var header=new VisualElement();header.AddToClassList("modal-header");card.Add(header);var t=Text(header,title,"modal-title");t.style.flexGrow=1;Button(header,"×","modal-close",CloseModals);
        return backdrop;
    }
    void Show(VisualElement target){CloseModals();target.style.display=DisplayStyle.Flex;target.BringToFront();}
    public void CloseModals(){foreach(var m in modals)m.style.display=DisplayStyle.None;PlayerPrefs.Save();}
    void BuildUI()
    {
        panel=ScriptableObject.CreateInstance<PanelSettings>();panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(1600,900);panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("GemmaTheme");panel.sortingOrder=1000;
        document=gameObject.AddComponent<UIDocument>();document.panelSettings=panel;root=document.rootVisualElement;root.style.flexGrow=1;root.styleSheets.Add(Resources.Load<StyleSheet>("OfficeMainMenu"));root.pickingMode=PickingMode.Ignore;
        var rail=new VisualElement();rail.AddToClassList("menu-rail");root.Add(rail);
        Text(rail,"ОФИСНЫЕ ИСТОРИИ","menu-eyebrow");Text(rail,menuTitle,"menu-brand");
        Text(rail,"Добро пожаловать в офис.\nКофе уже остыл, карьера ещё впереди.","menu-tagline");
        var navigation=new VisualElement();navigation.AddToClassList("menu-navigation");rail.Add(navigation);
        Button(navigation,"Начать игру  →","menu-nav menu-primary",()=>Show(modeScreen));
        Button(navigation,"Как играть","menu-nav",()=>Show(rulesScreen));
        Button(navigation,"Настройки","menu-nav",()=>Show(settingsScreen));
        Button(navigation,"Выход","menu-nav",Quit);
        var railSpacer=new VisualElement();railSpacer.style.flexGrow=1;rail.Add(railSpacer);
        Text(rail,"ОДНА СМЕНА. ТЫСЯЧА ПОВОДОВ ДЛЯ ХАОСА.","menu-footnote");
        Text(root,"SCAMPLAY  /  "+Application.version,"menu-version");
        var caption=new VisualElement();caption.AddToClassList("office-caption");root.Add(caption);Text(caption,"ОБЫЧНЫЙ ДЕНЬ В ОФИСЕ","caption-eyebrow");Text(caption,"Ваше рабочее место\nуже ждёт.","caption-title");
        downloadPanel=new VisualElement();downloadPanel.AddToClassList("download-panel");root.Add(downloadPanel);
        Text(downloadPanel,"ПОДГОТОВКА К ПЕРВОЙ СМЕНЕ","download-eyebrow");modelLabel=Text(downloadPanel,"Проверяем файлы игры…","download-title");
        modelProgress=new ProgressBar{lowValue=0,highValue=100,value=0,title="0%"};modelProgress.AddToClassList("model-progress");downloadPanel.Add(modelProgress);
        modelDetail=Text(downloadPanel,"","download-detail");var actions=new VisualElement();actions.AddToClassList("download-actions");downloadPanel.Add(actions);
        pauseButton=Button(actions,"Приостановить","menu-small",()=>installer.Pause());retryButton=Button(actions,"Продолжить","menu-small",()=>installer.Retry());
        modeScreen=Modal("Как начнём смену?",out var modes);modes.AddToClassList("mode-modal");
        var cards=new VisualElement();cards.AddToClassList("mode-cards");modes.Add(cards);
        var coop=new VisualElement();coop.AddToClassList("mode-card");coop.AddToClassList("mode-unavailable");cards.Add(coop);
        Text(coop,"КООПЕРАТИВ","mode-kicker");Text(coop,"С коллегами","mode-title");Text(coop,"Вместе пережить понедельник.\nЭтот режим ещё в разработке.","mode-description");
        var coopImage=new VisualElement();coopImage.AddToClassList("mode-art");coopImage.style.backgroundImage=new StyleBackground(Resources.Load<Texture2D>("Characters/MenuTeam"));coop.Add(coopImage);
        var coopButton=Button(coop,"Скоро","mode-start",()=>{});coopButton.SetEnabled(false);
        var solo=new VisualElement();solo.AddToClassList("mode-card");cards.Add(solo);
        Text(solo,"ОДИНОЧНАЯ ИГРА","mode-kicker");Text(solo,"Моя смена","mode-title");Text(solo,"Знакомьтесь, импровизируйте\nи попробуйте получить работу.","mode-description");
        var soloImage=new VisualElement();soloImage.AddToClassList("mode-art");soloImage.style.backgroundImage=new StyleBackground(Resources.Load<Texture2D>("Characters/MenuSolo"));solo.Add(soloImage);
        soloHint=Text(solo,"Дождитесь проверки модели","solo-hint");soloButton=Button(solo,"Начать смену  →","mode-start",StartSingle);soloButton.SetEnabled(false);
        loadingText=Text(modes,"","download-detail");
        settingsScreen=Modal("Настройки",out var settings);
        Slider(settings,"Общая громкость",OfficeGameSettings.Master,0,1,v=>OfficeGameSettings.SetMaster(v),v=>Mathf.RoundToInt(v*100)+"%");
        Slider(settings,"Громкость речи",OfficeGameSettings.Voice,0,1,v=>OfficeGameSettings.SetVoice(v),v=>Mathf.RoundToInt(v*100)+"%");
        Slider(settings,"Чувствительность мыши",OfficeGameSettings.Sensitivity,.03f,.30f,v=>OfficeGameSettings.SetSensitivity(v),v=>v.ToString("F2"));
        var full=new Toggle("Полный экран"){value=Screen.fullScreen};full.AddToClassList("menu-toggle");full.RegisterValueChangedCallback(e=>{Screen.fullScreen=e.newValue;PlayerPrefs.SetInt("Settings.Fullscreen",e.newValue?1:0);});settings.Add(full);
        var names=new List<string>(QualitySettings.names);var quality=new DropdownField("Качество графики",names,Mathf.Clamp(QualitySettings.GetQualityLevel(),0,names.Count-1));quality.AddToClassList("menu-quality");quality.RegisterValueChangedCallback(e=>{int index=names.IndexOf(e.newValue);if(index>=0){QualitySettings.SetQualityLevel(index,true);PlayerPrefs.SetInt("Settings.Quality",index);}});settings.Add(quality);
        Text(settings,"Настройки сохраняются автоматически.\nEsc — вернуться в меню.","settings-note");
        Button(settings,"Готово","mode-start",CloseModals);
        rulesScreen=Modal("Как пережить первый день",out var rules);
        Text(rules,"WASD — движение   •   Shift — бег\nПробел — прыжок   •   Мышь — обзор\nF — взять / отпустить предмет\nE — взаимодействовать\nЛКМ — удар, удержание с предметом — бросок\n\nНачните с ноутбука: выберите персонажа и имя.\nПодойдите к директору и убедите его принять вас на работу.\nВ чате: Enter — отправить, Shift+Enter — новая строка.\nEsc — закрыть разговор или окно.","rules-text");
        Button(rules,"Всё понятно","mode-start",CloseModals);
    }
    void Slider(VisualElement parent,string title,float value,float low,float high,Action<float> changed,Func<float,string> format)
    {
        var heading=new VisualElement();heading.AddToClassList("setting-heading");parent.Add(heading);var name=Text(heading,title,"setting-name");name.style.flexGrow=1;var amount=Text(heading,format(value),"setting-value");
        var slider=new Slider(low,high){value=value};slider.AddToClassList("menu-slider");slider.RegisterValueChangedCallback(e=>{amount.text=format(e.newValue);changed(e.newValue);});parent.Add(slider);
    }
    void UpdateDownload()
    {
        if(installer==null)return;
        modelLabel.text=installer.Message;modelProgress.value=installer.Progress*100;modelProgress.title=Mathf.FloorToInt(installer.Progress*100)+"%";
        bool download=installer.State==GemmaModelInstaller.Stage.Downloading;
        if(download)
        {
            string speed=installer.BytesPerSecond>0?FormatBytes((long)installer.BytesPerSecond)+"/с":"Подключаемся…";
            modelDetail.text=FormatBytes(installer.BytesReceived)+" / "+FormatBytes(installer.TotalBytes)+"  ·  "+speed;
        }
        else modelDetail.text=installer.Ready?"Одиночная игра доступна":"Для диалогов с персонажами нужен файл модели.\nЗагрузка выполняется один раз.";
        pauseButton.style.display=installer.Running?DisplayStyle.Flex:DisplayStyle.None;
        retryButton.style.display=installer.State==GemmaModelInstaller.Stage.Failed||installer.State==GemmaModelInstaller.Stage.Paused?DisplayStyle.Flex:DisplayStyle.None;
        soloButton.SetEnabled(installer.Ready&&!loading);
        soloHint.text=installer.Ready?"Всё готово к началу игры":installer.State==GemmaModelInstaller.Stage.Downloading?"Загрузка модели: "+Mathf.FloorToInt(installer.Progress*100)+"%":installer.State==GemmaModelInstaller.Stage.Checking?"Проверка файлов: "+Mathf.FloorToInt(installer.Progress*100)+"%":"Завершите подготовку модели на панели справа";
        if(installer.Ready)downloadPanel.AddToClassList("download-ready");else downloadPanel.RemoveFromClassList("download-ready");
    }
    static string FormatBytes(long value)=>value>=1073741824?(value/1073741824f).ToString("F2")+" ГБ":value>=1048576?(value/1048576f).ToString("F1")+" МБ":(value/1024f).ToString("F0")+" КБ";
    public void StartSingle()
    {
        if(!ModelReady||loading)return;
        if(!File.Exists(GemmaModelFiles.ExistingPath)||new FileInfo(GemmaModelFiles.ExistingPath).Length!=GemmaModelFiles.ExpectedBytes){installer.Begin();return;}
        loading=true;soloButton.SetEnabled(false);PlayerPrefs.Save();StartCoroutine(LoadGame());
    }
    IEnumerator LoadGame()
    {
        loadingText.text="Открываем офис…";var operation=SceneManager.LoadSceneAsync(gameScene);while(!operation.isDone){loadingText.text="Открываем офис… "+Mathf.RoundToInt(Mathf.Clamp01(operation.progress/.9f)*100)+"%";yield return null;}
    }
    void Quit()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }
    void OnDestroy(){if(installer!=null)installer.Changed-=UpdateDownload;Application.runInBackground=previousBackground;PlayerPrefs.Save();if(panel!=null)Destroy(panel);}
}
