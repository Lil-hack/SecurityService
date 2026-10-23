using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[DefaultExecutionOrder(-50),DisallowMultipleComponent]
public sealed class LaptopDesktop : MonoBehaviour
{
    public static bool BlocksWorldUse { get; private set; }
    public static bool SessionActive { get; private set; }

    public float useDistance=1.6f, blendSeconds=.8f;
    public Texture2D wallpaper;
    public GameObject[] characters;
    const string ScreenToken="Screen";
    const int CharacterApp=2;
    const int PreviewLayer=6;
    static readonly string[] AppNames={"Мой компьютер","Блокнот","Выбрать персонажа"};
    static readonly string[] AppEmoji={"LaptopEmoji/computer","LaptopEmoji/notepad","LaptopEmoji/character"};
    static readonly string[] AppNotes={"Этот компьютер.",
        "<size=25><b>ПАМЯТКА БУДУЩЕМУ КОЛЛЕГЕ</b></size>\n<color=#8C7355>Отдел кадров · инструкция по появлению в штате</color>\n\n"+
        "<b>01  Выберите персонажа</b>\nОткройте «Выбрать персонажа» и найдите своего героя.\nЖелательно того, кто выглядит готовым к понедельнику.\nНажмите «Выбрать»: красивое лицо — уже полрезюме.\n\n"+
        "<b>02  Введите своё имя для резюме</b>\nПора представиться! Укажите своё имя при заполнении\nрезюме. «Повелитель дедлайнов» — звучно, но кадровик\nвсё-таки ждёт имя, а не название финального босса.\n\n"+
        "<b>03  Проверьте результат</b>\nПерсонаж выбран, имя введено? Отлично, вы великолепны.\nОсталось убедить работодателя. Кофе мы уже поставили.\n\n<color=#8C7355>Служебная заметка: опыт спасения мира можно указать\nв разделе «Работа в стрессовых ситуациях».</color>",""};
    static readonly Color[] AppColors={new Color(.95f,.55f,.15f),new Color(.95f,.85f,.25f),new Color(.25f,.55f,.95f)};
    static readonly float[] PreviewSpeed={0f,2.2f,4.2f,0f};
    static readonly float[] PreviewVertical={0f,0f,0f,4f};
    static readonly bool[] PreviewGrounded={true,true,true,false};
    static readonly string[] PreviewState={"Locomotion","Locomotion","Locomotion","Jump"};
    static readonly string[] PreviewCaption={"Стоит","Идёт","Бежит","Прыгает"};
    const float PreviewFocus=.86f;

    enum Mode { Idle, Entering, Desktop, Leaving }
    Mode mode;
    bool claimsUse;
    Transform player;
    OfficeThirdPersonController controller;
    Camera view;
    Renderer screenRenderer;
    Camera roomCamera;
    RenderTexture roomTexture;
    GameObject desktopRoot, appWindow, cameraWindow, taskbar, startMenu, openAppSlot, characterPicker, previewInstance;
    Text appTitle, appBody, openAppLabel, clockLabel, dateLabel, characterName, characterPose, chooseLabel;
    const string NameKey="Office.PlayerName", CharacterKey="Office.CharacterPrefab";
    public static string PlayerName => PlayerPrefs.GetString(NameKey, "");
    static readonly string[] CharacterNames={"Скуф","Альтушка","Тюбик","Штрих"};
    static readonly string[] CharacterDescriptions={
        "• Неопрятный\n• Неактивный образ жизни\n• Консервативные взгляды",
        "• Плейлист мрачнее понедельника\n• Самовыражение важнее дресс-кода\n• Кофе чёрный, юмор ещё чернее",
        "• Эмоционально недоступный\n• Неуверенный\n• Манипулятор",
        "• Опасный\n• Связан с криминалом\n• Контролирующий"};
    static readonly string[] FunnyNames={
        "Василий Кофейный","Геннадий Дедлайн","Аркадий Перекур","Борис Без Паники","Жора Обеденный",
        "Паша Печенькин","Степан Диванович","Лёха Табличкин","Коля Кнопкин","Витя Пятница",
        "Семён Совещалкин","Федя Отчёткин","Игорь Не Сейчас","Толя На Созвоне","Валера Премия",
        "Миша Котлеткин","Гриша Клавишкин","Рома Без Звука","Денис Перезагрузкин","Олег Всё Успеем",
        "Саня Пельмень","Петя Паникович","Вова Кулерный","Тимофей Понедельник","Максим Авось",
        "Юра Обновляйкин","Леонид Печать","Захар Запаркин","Глеб На Минутку","Антон Премудрый",
        "Ася Чайная","Маша Дедлайн","Даша Печенька","Катя Пятиминутка","Лена Переменка",
        "Соня Будильник","Зоя Без Суеты","Нина Кофеиновна","Вера Всё Сойдётся","Люся Премия",
        "Таня На Удалёнке","Оля Шпаргалкина","Рита Перезагрузка","Полина Планёркина","Лиза Кнопочка",
        "Алёна После Обеда","Ксюша Круассан","Женя Тихий Час","Слава Антистресс","Саша Почти Готово"};
    InputField playerNameInput;
    Text characterDescription, nameValidation;
    Image chooseImage;
    Image[] pageDots;
    Sprite cardSprite, circleSprite;
    RenderTexture characterTexture;
    Camera characterCamera;
    Transform previewPivot, previewRig;
    Animator previewAnimator;
    int characterIndex;
    bool previewDrag;
    Vector3 previewAnchorOffset;
    Vector3 homePosition, startPosition, endPosition, standPoint;
    Quaternion homeRotation, startRotation, endRotation, faceRotation;
    bool turnBody, planted;
    CharacterController motor;
    float blend;
    Font font;

    void Awake()
    {
        foreach(var renderer in GetComponentsInChildren<Renderer>(true))
            if(renderer.name.Contains(ScreenToken)) { screenRenderer=renderer; break; }
        if(screenRenderer==null) screenRenderer=GetComponentInChildren<Renderer>();
    }

    void Start()
    {
        var playerObject=GameObject.FindGameObjectWithTag("Player");
        if(playerObject!=null)
        {
            player=playerObject.transform;
            controller=playerObject.GetComponent<OfficeThirdPersonController>();
            if(controller!=null) view=controller.followCamera;
        }
        if(view==null) view=Camera.main;
        if(view!=null) view.cullingMask&=~(1<<PreviewLayer);
        RestorePlayerProfile();
        BuildRoomCamera();
        BuildDesktop();
        if(desktopRoot!=null) desktopRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if(desktopRoot!=null) Destroy(desktopRoot);
        if(roomTexture!=null) roomTexture.Release();
        if(characterTexture!=null) characterTexture.Release();
        if(previewPivot!=null) Destroy(previewPivot.gameObject);
        if(previewRig!=null) Destroy(previewRig.gameObject);
        if(characterCamera!=null) Destroy(characterCamera.gameObject);
        if(SessionActive) SessionActive=false;
        BlocksWorldUse=false;
        ReleasePlayer();
    }

    void Update()
    {
        claimsUse=mode==Mode.Idle && InRange() && Cursor.lockState==CursorLockMode.Locked;
        BlocksWorldUse=claimsUse || mode!=Mode.Idle;
        SessionActive=mode!=Mode.Idle;
        if(mode==Mode.Desktop) RefreshClock();
        if(mode!=Mode.Idle) HoldPlayer();
        if(!Application.isFocused || Keyboard.current==null || view==null) return;
        if(claimsUse && Keyboard.current.eKey.wasPressedThisFrame) Begin();
        else if(mode!=Mode.Idle && Keyboard.current.escapeKey.wasPressedThisFrame) BeginExit();
    }

    void LateUpdate()
    {
        TurnTowardLaptop();
        if(mode==Mode.Entering || mode==Mode.Leaving) Animate();
        DrivePreview();
    }

    public bool InRange()
    {
        if(player==null || screenRenderer==null) return false;
        return Vector3.Distance(player.position+Vector3.up*1.1f,screenRenderer.bounds.center)<=useDistance;
    }

    public void Begin()
    {
        if(mode!=Mode.Idle || view==null || screenRenderer==null) return;
        ScreenFrame(out endPosition, out endRotation);
        homePosition=startPosition=view.transform.position;
        homeRotation=startRotation=view.transform.rotation;
        PlantPlayer();
        blend=0;
        mode=Mode.Entering;
        SessionActive=true;
        BlocksWorldUse=true;
        if(controller!=null) controller.SetControlEnabled(false);
        if(startMenu!=null) startMenu.SetActive(false);
        if(desktopRoot!=null) desktopRoot.SetActive(false);
    }

    public void BeginExit()
    {
        if(mode==Mode.Idle || mode==Mode.Leaving) return;
        if(mode==Mode.Desktop)
        {
            endPosition=view.transform.position;
            endRotation=view.transform.rotation;
            startPosition=homePosition;
            startRotation=homeRotation;
            blend=1;
        }
        if(startMenu!=null) startMenu.SetActive(false);
        if(desktopRoot!=null) desktopRoot.SetActive(false);
        Cursor.lockState=CursorLockMode.Locked;
        Cursor.visible=false;
        mode=Mode.Leaving;
    }

    public void OpenApp(string appName)
    {
        for(int i=0;i<AppNames.Length;i++) if(AppNames[i]==appName) { ShowApp(i); return; }
    }

    void Animate()
    {
        float step=Time.deltaTime/Mathf.Max(.05f,blendSeconds);
        blend=mode==Mode.Entering?Mathf.Min(1,blend+step):Mathf.Max(0,blend-step);
        float t=Mathf.SmoothStep(0,1,blend);
        view.transform.SetPositionAndRotation(Vector3.Lerp(startPosition,endPosition,t),Quaternion.Slerp(startRotation,endRotation,t));
        if(mode==Mode.Entering && blend>=1)
        {
            mode=Mode.Desktop;
            if(desktopRoot!=null) desktopRoot.SetActive(true);
            if(cameraWindow!=null) cameraWindow.transform.SetAsLastSibling();
            if(taskbar!=null) taskbar.transform.SetAsLastSibling();
            Cursor.lockState=CursorLockMode.None;
            Cursor.visible=true;
        }
        else if(mode==Mode.Leaving && blend<=0)
        {
            mode=Mode.Idle;
            SessionActive=false;
            turnBody=false;
            ReleasePlayer();
            if(controller!=null) controller.SetControlEnabled(true);
        }
    }

    void PlantPlayer()
    {
        if(player==null || screenRenderer==null) return;
        var center=screenRenderer.bounds.center;
        var outward=Outward();
        standPoint=center+outward*1.05f;
        standPoint.y=player.position.y;
        motor=player.GetComponent<CharacterController>();
        if(motor!=null) motor.enabled=false;
        player.position=standPoint;
        planted=true;
        var toScreen=center-standPoint;
        toScreen.y=0;
        if(toScreen.sqrMagnitude<.0001f) toScreen=-outward;
        faceRotation=Quaternion.LookRotation(toScreen);
        turnBody=true;
        var body=controller!=null && controller.visualRoot!=null ? controller.visualRoot : player;
        body.rotation=faceRotation;
        if(controller!=null)
        {
            controller.SetYaw(faceRotation.eulerAngles.y);
            controller.GetOrbitPose(out homePosition, out homeRotation);
        }
        IdlePose();
    }

    void HoldPlayer()
    {
        if(!planted || player==null) return;
        if(motor!=null && motor.enabled) motor.enabled=false;
        if((player.position-standPoint).sqrMagnitude>.000001f) player.position=standPoint;
        IdlePose();
    }

    void ReleasePlayer()
    {
        planted=false;
        if(motor!=null) motor.enabled=true;
    }

    void IdlePose()
    {
        var animator=controller!=null ? controller.humanoidAnimator : null;
        if(animator==null) return;
        animator.SetFloat("Speed",0f);
        animator.SetFloat("VerticalSpeed",0f);
        animator.SetBool("Grounded",true);
    }

    void TurnTowardLaptop()
    {
        if(!turnBody) return;
        var body=controller!=null && controller.visualRoot!=null ? controller.visualRoot : player;
        if(body==null) return;
        body.rotation=Quaternion.Slerp(body.rotation,faceRotation,1f-Mathf.Exp(-12f*Time.deltaTime));
    }

    void ScreenFrame(out Vector3 position, out Quaternion rotation)
    {
        var center=screenRenderer.bounds.center;
        var outward=Outward();
        float half=Mathf.Max(screenRenderer.bounds.extents.y,.05f);
        float dist=half/Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad);
        dist=Mathf.Clamp(dist*.92f,.18f,.8f);
        position=center+outward*dist;
        rotation=Quaternion.LookRotation(-outward,Vector3.up);
    }

    Vector3 Outward()
    {
        var center=screenRenderer.bounds.center;
        var body=screenRenderer.bounds;
        var found=false;
        foreach(var renderer in GetComponentsInChildren<Renderer>())
        {
            if(renderer==screenRenderer) continue;
            if(!found) { body=renderer.bounds; found=true; }
            else body.Encapsulate(renderer.bounds);
        }
        var outward=screenRenderer.transform.forward;
        if(found && Vector3.Dot(outward,body.center-center)<0) outward=-outward;
        if(outward.sqrMagnitude<.0001f) outward=transform.forward;
        return outward.normalized;
    }

    void BuildRoomCamera()
    {
        if(view==null || screenRenderer==null) return;
        roomTexture=new RenderTexture(960,540,16){name="Laptop room view"};
        var cameraObject=new GameObject("Laptop front camera");
        cameraObject.transform.SetParent(transform,false);
        roomCamera=cameraObject.AddComponent<Camera>();
        roomCamera.clearFlags=view.clearFlags;
        roomCamera.backgroundColor=view.backgroundColor;
        roomCamera.cullingMask=view.cullingMask & ~(1<<5);
        roomCamera.nearClipPlane=.2f;
        roomCamera.farClipPlane=view.farClipPlane;
        roomCamera.fieldOfView=50;
        roomCamera.depth=-20;
        roomCamera.targetTexture=roomTexture;
        roomCamera.GetUniversalAdditionalCameraData().renderType=CameraRenderType.Base;
        const int hiddenLayer=3;
        foreach(var part in GetComponentsInChildren<Transform>(true)) part.gameObject.layer=hiddenLayer;
        roomCamera.cullingMask&=~(1<<hiddenLayer);
        roomCamera.cullingMask&=~(1<<PreviewLayer);
        PlaceRoomCamera();
    }

    void PlaceRoomCamera()
    {
        roomCamera.transform.localPosition=new Vector3(0.0690003f,.235f,.424002f);
        roomCamera.transform.localRotation=Quaternion.Euler(8.416f,0f,0f);
        roomCamera.transform.localScale=Vector3.one;
    }

    void BuildDesktop()
    {
        EnsureUiSprites();
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(font==null) font=Font.CreateDynamicFontFromOSFont("Arial",16);
        EnsureEventSystem();
        desktopRoot=new GameObject("Laptop desktop");
        var canvas=desktopRoot.AddComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder=300;
        var scaler=desktopRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1600,900);
        desktopRoot.AddComponent<GraphicRaycaster>();

        var background=new GameObject("Wallpaper");
        background.transform.SetParent(desktopRoot.transform,false);
        Stretch(background.AddComponent<RectTransform>());
        if(wallpaper!=null)
        {
            var photo=background.AddComponent<RawImage>();
            photo.texture=wallpaper;
            photo.color=Color.white;
        }
        else
        {
            var fill=background.AddComponent<Image>();
            fill.color=new Color(.07f,.27f,.52f);
        }
        for(int i=0;i<AppNames.Length;i++)
        {
            int index=i;
            var emoji=string.IsNullOrEmpty(AppEmoji[i])?null:Resources.Load<Texture2D>(AppEmoji[i]);
            CreateIcon(desktopRoot.transform,AppNames[i],AppColors[i],emoji,new Vector2(24,-24-i*116),()=>ShowApp(index));
        }
        appWindow=CreateWindow(desktopRoot.transform,"App",new Vector2(0,20),new Vector2(560,340),true,out appTitle,out appBody);
        appWindow.SetActive(false);
        BuildCharacterPicker();
        cameraWindow=CreateWindow(desktopRoot.transform,"Камера · Прямой эфир",new Vector2(-20,-20),new Vector2(500,340),false,out _,out _);
        var cameraRect=cameraWindow.GetComponent<RectTransform>();
        cameraRect.anchorMin=cameraRect.anchorMax=new Vector2(1,1);
        cameraRect.pivot=new Vector2(1,1);
        cameraRect.anchoredPosition=new Vector2(-20,-20);
        var feedObject=new GameObject("Feed");
        feedObject.transform.SetParent(cameraWindow.transform,false);
        var feedRect=feedObject.AddComponent<RectTransform>();
        feedRect.anchorMin=Vector2.zero; feedRect.anchorMax=Vector2.one;
        feedRect.offsetMin=new Vector2(12,38); feedRect.offsetMax=new Vector2(-12,-54);
        var feed=feedObject.AddComponent<RawImage>();
        feed.texture=roomTexture;
        feed.raycastTarget=false;
        var live=Label(cameraWindow.transform,"● LIVE     Встроенная камера",13,new Color(.4f,.9f,.72f),TextAnchor.MiddleLeft);
        live.rectTransform.anchorMin=live.rectTransform.anchorMax=Vector2.zero;
        live.rectTransform.pivot=Vector2.zero;live.rectTransform.anchoredPosition=new Vector2(16,6);live.rectTransform.sizeDelta=new Vector2(460,28);

        CreateIcon(desktopRoot.transform,"Корзина · Выход",new Color(.28f,.58f,.67f),null,Vector2.zero,BeginExit);
        var trash=desktopRoot.transform.Find("Корзина · Выход").GetComponent<RectTransform>();
        trash.anchorMin=trash.anchorMax=new Vector2(1,0);trash.pivot=new Vector2(1,0);trash.anchoredPosition=new Vector2(-24,82);

        BuildTaskbar();
    }

    void BuildTaskbar()
    {
        var bar=ImagePanel(desktopRoot.transform,"Taskbar",new Color(.045f,.07f,.12f,.96f));
        taskbar=bar.gameObject;
        var barRect=bar.rectTransform;
        barRect.anchorMin=new Vector2(0,0); barRect.anchorMax=new Vector2(1,0);
        barRect.pivot=new Vector2(.5f,0);
        barRect.sizeDelta=new Vector2(0,64); barRect.anchoredPosition=Vector2.zero;
        TaskPiece(bar.transform,"Top highlight",new Color(.4f,.65f,.9f,.4f),new Vector2(.5f,1),new Vector2(.5f,1),Vector2.zero,new Vector2(1600,1));

        var start=RoundImage(bar.transform,"Start",new Color(.2f,.43f,.8f),cardSprite,Image.Type.Sliced);
        var startRect=start.rectTransform;
        startRect.anchorMin=startRect.anchorMax=new Vector2(0,.5f);
        startRect.pivot=new Vector2(0,.5f);
        startRect.anchoredPosition=new Vector2(6,0);
        startRect.sizeDelta=new Vector2(84,36);
        var startButton=start.gameObject.AddComponent<Button>();
        startButton.targetGraphic=start;
        startButton.onClick.AddListener(ToggleStart);
        var startLabel=Label(start.transform,"Пуск",18,Color.white,TextAnchor.MiddleCenter);
        Stretch(startLabel.rectTransform);

        var search=TaskPiece(bar.transform,"Search",new Color(.13f,.17f,.24f),new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(98,0),new Vector2(210,36));
        var searchLabel=Label(search,"Поиск",16,new Color(.75f,.78f,.82f),TextAnchor.MiddleLeft);
        Stretch(searchLabel.rectTransform);
        searchLabel.rectTransform.offsetMin=new Vector2(14,0);

        float pinX=322f;
        for(int i=0;i<AppNames.Length;i++)
        {
            var pin=TaskPiece(bar.transform,AppNames[i],AppColors[i],new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(pinX,0),new Vector2(28,28));
            var emoji=string.IsNullOrEmpty(AppEmoji[i])?null:Resources.Load<Texture2D>(AppEmoji[i]);
            DrawAppIcon(pin,i,AppColors[i],28);
            pin.GetComponent<Image>().color=Color.clear;
            pin.GetComponent<Image>().raycastTarget=true;
            int app=i;var pinButton=pin.gameObject.AddComponent<Button>();pinButton.onClick.AddListener(()=>ShowApp(app));
            pinX+=34f;
        }

        float windowX=pinX+12f;
        var cameraSlot=TaskButton(bar.transform,"Camera task",windowX,156,new Color(.35f,.7f,.95f),()=>{cameraWindow.transform.SetAsLastSibling();taskbar.transform.SetAsLastSibling();});
        var cameraBadge=TaskPiece(cameraSlot,"Camera badge",new Color(.22f,.45f,.65f),new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(10,0),new Vector2(28,28));
        var cameraGlyph=Label(cameraBadge,"●",20,Color.white,TextAnchor.MiddleCenter);Stretch(cameraGlyph.rectTransform);
        var cameraLabel=Label(cameraSlot,"Камера",15,Color.white,TextAnchor.MiddleLeft);
        Stretch(cameraLabel.rectTransform);
        cameraLabel.rectTransform.offsetMin=new Vector2(48,0);

        openAppSlot=TaskButton(bar.transform,"Open app",windowX+166,232,new Color(.9f,.75f,.3f),()=>{var active=characterPicker.activeSelf?characterPicker:appWindow;active.SetActive(true);active.transform.SetAsLastSibling();taskbar.transform.SetAsLastSibling();}).gameObject;
        var appBadge=TaskPiece(openAppSlot.transform,"App badge",Color.clear,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(10,0),new Vector2(28,28));
        DrawAppIcon(appBadge,0,AppColors[0],28);
        openAppLabel=Label(openAppSlot.transform,"",15,Color.white,TextAnchor.MiddleLeft);
        Stretch(openAppLabel.rectTransform);
        openAppLabel.rectTransform.offsetMin=new Vector2(48,0);
        openAppLabel.rectTransform.offsetMax=new Vector2(-8,0);
        openAppSlot.SetActive(false);

        var status=Label(bar.transform,"●  Онлайн",13,new Color(.5f,.85f,.72f),TextAnchor.MiddleRight);
        status.rectTransform.anchorMin=status.rectTransform.anchorMax=new Vector2(1,.5f);status.rectTransform.pivot=new Vector2(1,.5f);status.rectTransform.anchoredPosition=new Vector2(-118,0);status.rectTransform.sizeDelta=new Vector2(110,30);
        var clock=TaskPiece(bar.transform,"Clock",new Color(0,0,0,0),new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-8,0),new Vector2(92,40));
        clockLabel=Label(clock,"00:00",15,Color.white,TextAnchor.UpperCenter);
        Stretch(clockLabel.rectTransform);
        clockLabel.rectTransform.offsetMax=new Vector2(0,-4);
        dateLabel=Label(clock,"",12,new Color(.8f,.84f,.88f),TextAnchor.LowerCenter);
        Stretch(dateLabel.rectTransform);
        dateLabel.rectTransform.offsetMin=new Vector2(0,4);
        RefreshClock();

        startMenu=RoundImage(bar.transform,"Start menu",new Color(.07f,.1f,.16f,.98f),cardSprite,Image.Type.Sliced).gameObject;
        var menuShadow=startMenu.AddComponent<Shadow>();menuShadow.effectDistance=new Vector2(3,-5);menuShadow.effectColor=new Color(0,0,0,.4f);
        var menuRect=startMenu.GetComponent<RectTransform>();
        menuRect.anchorMin=menuRect.anchorMax=new Vector2(0,0);
        menuRect.pivot=new Vector2(0,0);
        menuRect.anchoredPosition=new Vector2(6,70);
        menuRect.sizeDelta=new Vector2(270,84);
        var exit=RoundImage(startMenu.transform,"Exit",new Color(.25f,.13f,.19f),cardSprite,Image.Type.Sliced);
        var exitRect=exit.rectTransform;
        exitRect.anchorMin=Vector2.zero; exitRect.anchorMax=Vector2.one;
        exitRect.offsetMin=new Vector2(8,8); exitRect.offsetMax=new Vector2(-8,-8);
        var exitButton=exit.gameObject.AddComponent<Button>();
        exitButton.targetGraphic=exit;
        exitButton.onClick.AddListener(BeginExit);
        var power=RoundImage(exit.transform,"Power badge",new Color(.85f,.32f,.38f),circleSprite,Image.Type.Simple);
        power.raycastTarget=false;power.rectTransform.anchorMin=power.rectTransform.anchorMax=new Vector2(0,.5f);power.rectTransform.anchoredPosition=new Vector2(32,0);power.rectTransform.sizeDelta=new Vector2(36,36);
        var powerLabel=Label(power.transform,"↪",24,Color.white,TextAnchor.MiddleCenter);Stretch(powerLabel.rectTransform);
        var exitLabel=Label(exit.transform,"<b>Выйти</b>\n<size=12><color=#D7B7C1>Вернуться в офис</color></size>",18,Color.white,TextAnchor.MiddleLeft);
        Stretch(exitLabel.rectTransform);
        exitLabel.rectTransform.offsetMin=new Vector2(62,0);
        startMenu.SetActive(false);
    }

    void ToggleStart()
    {
        if(startMenu==null) return;
        startMenu.SetActive(!startMenu.activeSelf);
    }

    void RefreshClock()
    {
        if(clockLabel==null) return;
        var now=System.DateTime.Now;
        clockLabel.text=now.ToString("HH:mm");
        if(dateLabel!=null) dateLabel.text=now.ToString("dd.MM.yyyy");
    }

    RectTransform TaskButton(Transform parent,string name,float x,float width,Color accent,UnityEngine.Events.UnityAction click)
    {
        var image=RoundImage(parent,name,new Color(.13f,.18f,.26f),cardSprite,Image.Type.Sliced);
        var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,.5f);rect.pivot=new Vector2(0,.5f);rect.anchoredPosition=new Vector2(x,0);rect.sizeDelta=new Vector2(width,46);
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(click);
        var colors=button.colors;colors.highlightedColor=new Color(.72f,.84f,1);colors.pressedColor=new Color(.5f,.7f,.95f);button.colors=colors;
        TaskPiece(rect,"Active indicator",accent,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,2),new Vector2(28,3));
        return rect;
    }

    static RectTransform TaskPiece(Transform parent,string name,Color color,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
    {
        var image=ImagePanel(parent,name,color);
        image.raycastTarget=false;
        var rect=image.rectTransform;
        rect.anchorMin=rect.anchorMax=anchor;
        rect.pivot=pivot;
        rect.anchoredPosition=position;
        rect.sizeDelta=size;
        return rect;
    }

    void ShowApp(int index)
    {
        bool character=index==CharacterApp;
        if(appWindow!=null)
        {
            appWindow.SetActive(!character);
            if(!character)
            {
                appTitle.text=AppNames[index];
                appBody.gameObject.SetActive(true);
                appBody.text=AppNotes[index];
                var rect=appWindow.GetComponent<RectTransform>();
                rect.sizeDelta=index==1?new Vector2(680,620):new Vector2(560,340);
                var paper=appWindow.transform.Find("Notepad paper");
                if(paper==null)
                {
                    var sheet=RoundImage(appWindow.transform,"Notepad paper",new Color(.98f,.95f,.87f),cardSprite,Image.Type.Sliced);
                    Stretch(sheet.rectTransform);sheet.rectTransform.offsetMin=new Vector2(12,12);sheet.rectTransform.offsetMax=new Vector2(-12,-56);sheet.raycastTarget=false;
                    sheet.transform.SetSiblingIndex(appBody.transform.GetSiblingIndex());paper=sheet.transform;
                }
                paper.gameObject.SetActive(index==1);
                appBody.fontSize=index==1?18:22;appBody.supportRichText=true;
                appBody.color=index==1?new Color(.19f,.23f,.28f):new Color(.9f,.92f,.94f);
                appBody.rectTransform.offsetMin=new Vector2(index==1?30:18,24);
                appBody.rectTransform.offsetMax=new Vector2(index==1?-30:-18,-76);
                rect.anchoredPosition=new Vector2(0,20);
                appWindow.transform.SetAsLastSibling();
            }
        }
        if(characterPicker!=null)
        {
            characterPicker.SetActive(character);
            if(character)
            {
                characterPicker.transform.SetAsLastSibling();
                ShowPreview();
                StartCoroutine(FocusPlayerName());
            }
        }
        if(cameraWindow!=null) cameraWindow.transform.SetAsLastSibling();
        if(openAppSlot!=null)
        {
            openAppSlot.SetActive(true);
            openAppLabel.text=AppNames[index];
            var badge=openAppSlot.transform.Find("App badge");
            foreach(Transform child in badge)Destroy(child.gameObject);
            DrawAppIcon(badge,index,AppColors[index],28);
        }
        if(taskbar!=null) taskbar.transform.SetAsLastSibling();
    }

    void BuildCharacterPicker()
    {
        characterTexture=new RenderTexture(768,768,16){name="Character preview"};
        previewPivot=new GameObject("Character preview stage").transform;
        previewPivot.position=new Vector3(0f,-80f,0f);
        previewRig=new GameObject("Character preview rig").transform;
        previewRig.position=previewPivot.position;
        var keyObject=new GameObject("Preview light");
        keyObject.transform.SetParent(previewRig,false);
        keyObject.transform.rotation=Quaternion.Euler(32f,-28f,0f);
        var key=keyObject.AddComponent<Light>();
        key.type=LightType.Directional;
        key.intensity=1.15f;
        key.cullingMask=1<<PreviewLayer;
        var fillObject=new GameObject("Preview fill");
        fillObject.transform.SetParent(previewRig,false);
        fillObject.transform.rotation=Quaternion.Euler(12f,155f,0f);
        var fill=fillObject.AddComponent<Light>();
        fill.type=LightType.Directional;
        fill.intensity=.5f;
        fill.cullingMask=1<<PreviewLayer;
        var cameraObject=new GameObject("Character preview camera");
        characterCamera=cameraObject.AddComponent<Camera>();
        characterCamera.clearFlags=CameraClearFlags.SolidColor;
        characterCamera.backgroundColor=new Color(.07f,.08f,.11f);
        characterCamera.cullingMask=1<<PreviewLayer;
        characterCamera.fieldOfView=24f;
        characterCamera.nearClipPlane=.05f;
        characterCamera.farClipPlane=30f;
        characterCamera.targetTexture=characterTexture;
        characterCamera.depth=-30;
        characterCamera.enabled=false;
        characterCamera.GetUniversalAdditionalCameraData().renderType=CameraRenderType.Base;
        FramePreviewCamera();

        EnsureUiSprites();
        var card=RoundImage(desktopRoot.transform,"Character picker",new Color(.075f,.09f,.13f,1f),cardSprite,Image.Type.Sliced);
        characterPicker=card.gameObject;
        var picker=card.rectTransform;
        picker.anchorMin=picker.anchorMax=new Vector2(.5f,.5f);
        picker.pivot=new Vector2(.5f,.5f);
        picker.sizeDelta=new Vector2(840,660);
        picker.anchoredPosition=new Vector2(-160,18);

        var header=Label(characterPicker.transform,"Выбор персонажа",18,new Color(.72f,.76f,.82f),TextAnchor.MiddleLeft);
        var headerRect=header.rectTransform;
        headerRect.anchorMin=new Vector2(0,1); headerRect.anchorMax=new Vector2(1,1);
        headerRect.pivot=new Vector2(.5f,1);
        headerRect.sizeDelta=new Vector2(-108,48);
        headerRect.anchoredPosition=new Vector2(-8,-6);
        var dragSurface=ImagePanel(characterPicker.transform,"Drag title bar",Color.clear);
        var dragRect=dragSurface.rectTransform;dragRect.anchorMin=new Vector2(0,1);dragRect.anchorMax=Vector2.one;dragRect.pivot=new Vector2(.5f,1);dragRect.sizeDelta=new Vector2(-76,52);dragRect.anchoredPosition=new Vector2(-30,0);
        dragSurface.gameObject.AddComponent<LaptopWindowDrag>().target=picker;
        var close=RoundButton(characterPicker.transform,"×",new Color(1f,1f,1f,.1f),new Vector2(40,40),new Vector2(1,1),new Vector2(1,1),ClosePicker,out var closeLabel);
        close.anchoredPosition=new Vector2(-16,-12);
        closeLabel.fontSize=24;

        var stage=RoundImage(characterPicker.transform,"Stage",new Color(.07f,.08f,.11f,1f),cardSprite,Image.Type.Sliced);
        var stageRect=stage.rectTransform;
        stageRect.anchorMin=Vector2.zero; stageRect.anchorMax=Vector2.one;
        stageRect.offsetMin=new Vector2(18,150); stageRect.offsetMax=new Vector2(-18,-60);

        var viewObject=new GameObject("Preview");
        viewObject.transform.SetParent(stage.transform,false);
        var viewRect=viewObject.AddComponent<RectTransform>();
        viewRect.anchorMin=viewRect.anchorMax=new Vector2(1,.5f);viewRect.pivot=new Vector2(1,.5f);
        viewRect.sizeDelta=new Vector2(424,424);viewRect.anchoredPosition=new Vector2(-8,10);
        var characterView=viewObject.AddComponent<RawImage>();
        characterView.texture=characterTexture;
        characterView.raycastTarget=true;
        viewObject.AddComponent<LaptopOrbit>().desktop=this;

        var previous=RoundButton(stage.transform,"‹",new Color(1f,1f,1f,.12f),new Vector2(52,52),new Vector2(0,.5f),new Vector2(0,.5f),()=>StepCharacter(-1),out var previousLabel);
        previous.anchorMin=previous.anchorMax=new Vector2(.73f,0);previous.pivot=new Vector2(.5f,0);
        previous.anchoredPosition=new Vector2(-76,14);
        previousLabel.fontSize=30;
        var next=RoundButton(stage.transform,"›",new Color(1f,1f,1f,.12f),new Vector2(52,52),new Vector2(1,.5f),new Vector2(1,.5f),()=>StepCharacter(1),out var nextLabel);
        next.anchorMin=next.anchorMax=new Vector2(.73f,0);next.pivot=new Vector2(.5f,0);
        next.anchoredPosition=new Vector2(76,14);
        nextLabel.fontSize=30;
        var hint=Label(stage.transform,"Потяните, чтобы повернуть",14,new Color(1f,1f,1f,.42f),TextAnchor.MiddleCenter);
        var hintRect=hint.rectTransform;
        hintRect.anchorMin=new Vector2(0,0); hintRect.anchorMax=new Vector2(1,0);
        hintRect.pivot=new Vector2(.5f,0);
        hintRect.sizeDelta=new Vector2(-360,24);
        hintRect.anchoredPosition=new Vector2(170,72);

        var info=RoundImage(stage.transform,"Character description card",new Color(.11f,.14f,.20f,.97f),cardSprite,Image.Type.Sliced);
        info.raycastTarget=false;var infoRect=info.rectTransform;
        infoRect.anchorMin=new Vector2(0,0);infoRect.anchorMax=new Vector2(0,1);infoRect.pivot=new Vector2(0,.5f);
        infoRect.sizeDelta=new Vector2(312,-40);infoRect.anchoredPosition=new Vector2(20,0);
        characterName=Label(info.transform,"",30,Color.white,TextAnchor.MiddleLeft);
        characterName.fontStyle=FontStyle.Bold;
        var nameRect=characterName.rectTransform;
        nameRect.anchorMin=nameRect.anchorMax=new Vector2(0,1);
        nameRect.pivot=new Vector2(0,1);
        nameRect.sizeDelta=new Vector2(264,42);
        nameRect.anchoredPosition=new Vector2(24,-26);
        characterPose=Label(info.transform,"",16,new Color(.62f,.68f,.76f),TextAnchor.MiddleLeft);
        var poseRect=characterPose.rectTransform;
        poseRect.anchorMin=poseRect.anchorMax=new Vector2(0,1);
        poseRect.pivot=new Vector2(0,1);
        poseRect.sizeDelta=new Vector2(120,24);
        poseRect.anchoredPosition=new Vector2(24,-78);
        pageDots=new Image[6];
        for(int i=0;i<pageDots.Length;i++)
        {
            var dot=RoundImage(stage.transform,"Dot",new Color(1f,1f,1f,.28f),circleSprite,Image.Type.Simple);
            var dotRect=dot.rectTransform;
            dotRect.anchorMin=dotRect.anchorMax=new Vector2(.73f,1);
            dotRect.pivot=new Vector2(0,.5f);
            dotRect.sizeDelta=new Vector2(8,8);
            dotRect.anchoredPosition=new Vector2(-28+i*16,-22);
            pageDots[i]=dot;
        }
        var choose=RoundButton(characterPicker.transform,"Выбрать",new Color(.23f,.47f,.98f),new Vector2(188,48),new Vector2(1,0),new Vector2(1,0),ChooseCharacter,out chooseLabel);
        choose.anchoredPosition=new Vector2(-28,44);
        chooseImage=choose.GetComponent<Image>();
        chooseLabel.fontSize=20;
        characterDescription=Label(info.transform,"",19,new Color(.80f,.84f,.9f),TextAnchor.UpperLeft);
        var descriptionRect=characterDescription.rectTransform;
        descriptionRect.anchorMin=descriptionRect.anchorMax=new Vector2(0,1);descriptionRect.pivot=new Vector2(0,1);
        descriptionRect.anchoredPosition=new Vector2(24,-132);descriptionRect.sizeDelta=new Vector2(264,220);
        characterDescription.lineSpacing=1.25f;
        var field=RoundImage(characterPicker.transform,"Player name required",new Color(.055f,.065f,.09f),cardSprite,Image.Type.Sliced);
        field.rectTransform.anchorMin=field.rectTransform.anchorMax=Vector2.zero;field.rectTransform.pivot=Vector2.zero;
        field.rectTransform.anchoredPosition=new Vector2(28,44);field.rectTransform.sizeDelta=new Vector2(560,48);
        playerNameInput=field.gameObject.AddComponent<InputField>();playerNameInput.targetGraphic=field;
        var inputText=Label(field.transform,"",20,Color.white,TextAnchor.MiddleLeft);Stretch(inputText.rectTransform);
        inputText.rectTransform.offsetMin=new Vector2(14,4);inputText.rectTransform.offsetMax=new Vector2(-14,-4);inputText.supportRichText=false;
        var placeholder=Label(field.transform,"Имя для резюме *",20,new Color(.55f,.61f,.70f),TextAnchor.MiddleLeft);Stretch(placeholder.rectTransform);
        placeholder.rectTransform.offsetMin=new Vector2(14,4);placeholder.rectTransform.offsetMax=new Vector2(-14,-4);
        playerNameInput.textComponent=inputText;playerNameInput.placeholder=placeholder;playerNameInput.characterLimit=32;
        playerNameInput.lineType=InputField.LineType.SingleLine;
        playerNameInput.customCaretColor=true;playerNameInput.caretColor=new Color(.6f,.85f,1f);
        playerNameInput.caretWidth=3;playerNameInput.caretBlinkRate=1.2f;
        playerNameInput.selectionColor=new Color(.25f,.5f,1f,.45f);
        playerNameInput.text=string.IsNullOrWhiteSpace(PlayerName)?FunnyNames[Random.Range(0,FunnyNames.Length)]:PlayerName;
        var editHint=Label(characterPicker.transform,"ИМЯ ДЛЯ РЕЗЮМЕ  ·  нажмите и напишите своё",14,new Color(.70f,.78f,.90f),TextAnchor.MiddleLeft);
        editHint.rectTransform.anchorMin=editHint.rectTransform.anchorMax=Vector2.zero;editHint.rectTransform.pivot=Vector2.zero;
        editHint.rectTransform.anchoredPosition=new Vector2(28,102);editHint.rectTransform.sizeDelta=new Vector2(700,24);
        playerNameInput.onValueChanged.AddListener(_=>{if(nameValidation!=null){nameValidation.text="Можно изменить имя · до 32 символов";nameValidation.color=new Color(.65f,.71f,.8f);}RefreshPickerChrome();});
        nameValidation=Label(characterPicker.transform,"Можно изменить имя · до 32 символов",14,new Color(.65f,.71f,.8f),TextAnchor.MiddleLeft);
        nameValidation.rectTransform.anchorMin=nameValidation.rectTransform.anchorMax=Vector2.zero;nameValidation.rectTransform.pivot=Vector2.zero;
        nameValidation.rectTransform.anchoredPosition=new Vector2(28,18);nameValidation.rectTransform.sizeDelta=new Vector2(760,22);
        characterPicker.SetActive(false);
    }

    System.Collections.IEnumerator FocusPlayerName()
    {
        yield return null;
        if(playerNameInput==null || characterPicker==null || !characterPicker.activeInHierarchy)yield break;
        playerNameInput.ActivateInputField();
        playerNameInput.caretPosition=playerNameInput.text.Length;
        playerNameInput.selectionAnchorPosition=playerNameInput.text.Length;
        playerNameInput.selectionFocusPosition=playerNameInput.text.Length;
    }

    void RestorePlayerProfile()
    {
        if(controller==null || characters==null)return;
        string saved=PlayerPrefs.GetString(CharacterKey, "");
        if(string.IsNullOrWhiteSpace(PlayerName)||string.IsNullOrEmpty(saved))return;
        for(int i=0;i<characters.Length;i++)if(characters[i]!=null&&characters[i].name==saved)
        {characterIndex=i;controller.EquipCharacter(characters[i]);break;}
    }

    void ClosePicker()
    {
        if(characterPicker!=null) characterPicker.SetActive(false);
        if(openAppSlot!=null) openAppSlot.SetActive(false);
    }

    void FramePreviewCamera()
    {
        if(characterCamera==null || previewPivot==null) return;
        float distance=(PreviewFocus*1.34f)/Mathf.Tan(characterCamera.fieldOfView*.5f*Mathf.Deg2Rad);
        var focus=previewPivot.position+Vector3.up*PreviewFocus;
        characterCamera.transform.position=new Vector3(previewPivot.position.x,focus.y,previewPivot.position.z+distance);
        characterCamera.transform.LookAt(focus);
    }

    void RememberPreviewAnchor()
    {
        previewAnchorOffset=Vector3.zero;
        if(previewInstance==null || previewAnimator==null) return;
        var hips=previewAnimator.GetBoneTransform(HumanBodyBones.Hips);
        if(hips==null) return;
        var bounds=OfficeThirdPersonController.SkinnedWorldBounds(previewInstance);
        if(bounds.size.y<.05f) return;
        previewAnchorOffset=previewInstance.transform.InverseTransformVector(bounds.center-hips.position);
    }

    void CenterPreview()
    {
        if(previewInstance==null || previewPivot==null) return;
        var hips=previewAnimator!=null?previewAnimator.GetBoneTransform(HumanBodyBones.Hips):null;
        Vector3 anchor;
        if(hips!=null) anchor=hips.position+previewInstance.transform.TransformVector(previewAnchorOffset);
        else
        {
            var bounds=RendererBounds(previewInstance);
            if(bounds.size.sqrMagnitude<.001f) return;
            anchor=bounds.center;
        }
        var focus=previewPivot.position+Vector3.up*PreviewFocus;
        var delta=focus-anchor;
        if(delta.sqrMagnitude<.000001f) return;
        previewInstance.transform.position+=delta;
    }

    static Bounds RendererBounds(GameObject root)
    {
        var bounds=new Bounds();
        bool first=true;
        foreach(var renderer in root.GetComponentsInChildren<Renderer>())
        {
            if(!renderer.enabled) continue;
            if(first) { bounds=renderer.bounds; first=false; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    void RefreshPickerChrome()
    {
        GameObject prefab=null;
        int count=characters==null?0:characters.Length;
        if(count>0) prefab=characters[(characterIndex%count+count)%count];
        if(characterName!=null) characterName.text=prefab==null?"Нет персонажей":(characterIndex<CharacterNames.Length?CharacterNames[characterIndex]:CharacterTitle(prefab));
        if(characterPose!=null) characterPose.text=prefab==null?"":PreviewCaption[characterIndex%PreviewCaption.Length];
        if(characterDescription!=null)characterDescription.text=prefab!=null&&characterIndex<CharacterDescriptions.Length?CharacterDescriptions[characterIndex]:"";
        bool equipped=prefab!=null && controller!=null && controller.humanoidAnimator!=null && controller.humanoidAnimator.gameObject.name.StartsWith(prefab.name);
        bool profileSaved=equipped && playerNameInput!=null && !string.IsNullOrWhiteSpace(playerNameInput.text) && playerNameInput.text.Trim()==PlayerName;
        if(chooseLabel!=null) chooseLabel.text=profileSaved?"Выбран":equipped?"Сохранить":"Выбрать";
        if(chooseImage!=null) chooseImage.color=profileSaved?new Color(.15f,.46f,.34f):new Color(.23f,.47f,.98f);
        if(pageDots==null) return;
        for(int i=0;i<pageDots.Length;i++)
        {
            if(pageDots[i]==null) continue;
            pageDots[i].gameObject.SetActive(i<count);
            bool current=i==characterIndex;
            pageDots[i].color=current?Color.white:new Color(1f,1f,1f,.28f);
            pageDots[i].rectTransform.localScale=Vector3.one*(current?1.25f:1f);
        }
    }

    void EnsureUiSprites()
    {
        if(cardSprite==null) cardSprite=RoundRectSprite(20);
        if(circleSprite==null) circleSprite=CircleSprite(64);
    }

    Image RoundImage(Transform parent,string name,Color color,Sprite sprite,Image.Type type)
    {
        var image=ImagePanel(parent,name,color);
        image.sprite=sprite;
        image.type=type;
        return image;
    }

    RectTransform RoundButton(Transform parent,string caption,Color color,Vector2 size,Vector2 anchor,Vector2 pivot,UnityEngine.Events.UnityAction click,out Text label)
    {
        bool round=caption.Length<=2;
        var image=RoundImage(parent,caption,color,round?circleSprite:cardSprite,round?Image.Type.Simple:Image.Type.Sliced);
        var rect=image.rectTransform;
        rect.anchorMin=rect.anchorMax=anchor;
        rect.pivot=pivot;
        rect.sizeDelta=size;
        var button=image.gameObject.AddComponent<Button>();
        button.targetGraphic=image;
        var colors=button.colors;
        colors.normalColor=Color.white;
        colors.highlightedColor=new Color(.9f,.94f,1f);
        colors.pressedColor=new Color(.72f,.78f,.88f);
        colors.selectedColor=Color.white;
        colors.fadeDuration=.06f;
        button.colors=colors;
        button.onClick.AddListener(click);
        label=Label(image.transform,caption,18,Color.white,TextAnchor.MiddleCenter);
        Stretch(label.rectTransform);
        return rect;
    }

    static Sprite RoundRectSprite(int radius)
    {
        int size=radius*2+2;
        var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);
        tex.wrapMode=TextureWrapMode.Clamp;
        tex.filterMode=FilterMode.Bilinear;
        var pixels=new Color32[size*size];
        float half=size*.5f;
        float limit=half-radius;
        for(int y=0;y<size;y++)
        for(int x=0;x<size;x++)
        {
            float dx=Mathf.Max(Mathf.Abs(x+.5f-half)-limit,0f);
            float dy=Mathf.Max(Mathf.Abs(y+.5f-half)-limit,0f);
            float edge=Mathf.Sqrt(dx*dx+dy*dy)-radius;
            byte alpha=(byte)(Mathf.Clamp01(.85f-edge)*255f);
            pixels[y*size+x]=new Color32(255,255,255,alpha);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(radius,radius,radius,radius));
    }

    static Sprite CircleSprite(int size)
    {
        var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);
        tex.wrapMode=TextureWrapMode.Clamp;
        tex.filterMode=FilterMode.Bilinear;
        var pixels=new Color32[size*size];
        float radius=size*.5f;
        var center=new Vector2(radius,radius);
        for(int y=0;y<size;y++)
        for(int x=0;x<size;x++)
        {
            float edge=Vector2.Distance(new Vector2(x+.5f,y+.5f),center)-(radius-1f);
            byte alpha=(byte)(Mathf.Clamp01(1f-edge)*255f);
            pixels[y*size+x]=new Color32(255,255,255,alpha);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100);
    }

    void StepCharacter(int direction)
    {
        if(characters==null || characters.Length==0) return;
        characterIndex=(characterIndex+direction)%characters.Length;
        if(characterIndex<0) characterIndex+=characters.Length;
        ShowPreview();
    }

    void ChooseCharacter()
    {
        if(controller==null || characters==null || characters.Length==0) return;
        string entered=playerNameInput==null?"":playerNameInput.text.Trim();
        if(string.IsNullOrWhiteSpace(entered))
        {
            nameValidation.text="Как к вам обращаться? Введите имя, чтобы выбрать персонажа.";
            nameValidation.color=new Color(1,.59f,.47f);playerNameInput.ActivateInputField();return;
        }
        var prefab=characters[characterIndex];
        if(prefab==null)return;
        controller.EquipCharacter(prefab);
        playerNameInput.SetTextWithoutNotify(entered);
        PlayerPrefs.SetString(NameKey,entered);PlayerPrefs.SetString(CharacterKey,prefab.name);PlayerPrefs.Save();
        nameValidation.color=new Color(.58f,.9f,.72f);nameValidation.text="Сохранено: "+entered+" · "+CharacterNames[characterIndex];
        RefreshPickerChrome();
    }

    public void SetPreviewDrag(bool dragging) { previewDrag=dragging; }

    public void OrbitPreview(float deltaX)
    {
        if(previewPivot!=null) previewPivot.Rotate(0f,-deltaX*.45f,0f,Space.World);
    }

    void ShowPreview()
    {
        if(characterCamera!=null) characterCamera.enabled=true;
        if(characters==null || characters.Length==0 || previewPivot==null)
        {
            if(characterName!=null) characterName.text="Нет персонажей";
            return;
        }
        characterIndex=(characterIndex%characters.Length+characters.Length)%characters.Length;
        var prefab=characters[characterIndex];
        if(previewInstance!=null)
        {
            previewInstance.SetActive(false);
            Destroy(previewInstance);
        }
        previewAnimator=null;
        if(prefab==null) return;
        previewInstance=Instantiate(prefab,previewPivot);
        previewInstance.transform.localPosition=Vector3.zero;
        previewInstance.transform.localRotation=Quaternion.identity;
        previewInstance.transform.localScale=Vector3.one;
        previewPivot.rotation=Quaternion.identity;
        var animator=previewInstance.GetComponent<Animator>();
        if(animator!=null)
        {
            animator.SetBool("Grounded",true);
            animator.SetFloat("Speed",0f);
            animator.SetFloat("VerticalSpeed",0f);
            animator.Play("Locomotion",0,0f);
            animator.Update(.2f);
        }
        OfficeThirdPersonController.PlaceHumanoid(previewInstance,previewPivot,PreviewLayer);
        previewAnimator=animator;
        RememberPreviewAnchor();
        DrivePreview();
        if(previewAnimator!=null) previewAnimator.Update(.2f);
        CenterPreview();
        RefreshPickerChrome();
    }

    void DrivePreview()
    {
        bool open=characterPicker!=null && characterPicker.activeInHierarchy;
        if(characterCamera!=null) characterCamera.enabled=open;
        if(!open || previewAnimator==null) return;
        if(!previewDrag && previewPivot!=null) previewPivot.Rotate(0f,28f*Time.deltaTime,0f,Space.World);
        int slot=characterIndex%PreviewState.Length;
        bool jump=slot==3;
        previewAnimator.SetBool("Grounded",PreviewGrounded[slot]);
        previewAnimator.SetFloat("VerticalSpeed",PreviewVertical[slot]);
        previewAnimator.SetFloat("Speed",jump?0f:PreviewSpeed[slot],.2f,Time.deltaTime);
        if(!previewAnimator.IsInTransition(0))
        {
            var state=previewAnimator.GetCurrentAnimatorStateInfo(0);
            if(jump)
            {
                if(!state.IsName("Jump") || state.normalizedTime>.98f)
                    previewAnimator.CrossFade("Jump",.1f,0,0f);
            }
            else if(!state.IsName("Locomotion"))
                previewAnimator.CrossFade("Locomotion",.15f,0);
        }
        CenterPreview();
    }

    static string CharacterTitle(GameObject prefab)
    {
        var value=prefab.name;
        const string suffix="_Textured";
        if(value.EndsWith(suffix)) value=value.Substring(0,value.Length-suffix.Length);
        return value;
    }

    void EnsureEventSystem()
    {
        var system=FindAnyObjectByType<EventSystem>();
        if(system==null)
        {
            var events=new GameObject("Laptop EventSystem");
            system=events.AddComponent<EventSystem>();
        }
        if(system.GetComponent<InputSystemUIInputModule>()==null)
        {
            var legacy=system.GetComponent<StandaloneInputModule>();
            if(legacy!=null) Destroy(legacy);
            system.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }

    void CreateIcon(Transform parent,string label,Color color,Texture2D emoji,Vector2 position,UnityEngine.Events.UnityAction click)
    {
        var button=RoundImage(parent,label,new Color(.045f,.08f,.14f,.32f),cardSprite,Image.Type.Sliced);
        var rect=button.GetComponent<RectTransform>();
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);
        rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=position;
        rect.sizeDelta=new Vector2(112,104);
        var uiButton=button.gameObject.AddComponent<Button>();
        uiButton.targetGraphic=button;
        uiButton.onClick.AddListener(click);
        int iconKind=System.Array.IndexOf(AppNames,label);
        DrawAppIcon(button.transform,iconKind<0?3:iconKind,color,58);
        var colors=uiButton.colors;colors.highlightedColor=new Color(.65f,.85f,1f);colors.pressedColor=new Color(.4f,.65f,.9f);uiButton.colors=colors;
        var text=Label(button.transform,label,14,Color.white,TextAnchor.LowerCenter);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin=new Vector2(4,4);
        text.rectTransform.offsetMax=new Vector2(-4,-66);
        var textShadow=text.gameObject.AddComponent<Shadow>();textShadow.effectColor=new Color(0,0,0,.75f);textShadow.effectDistance=new Vector2(1,-1);
        text.horizontalOverflow=HorizontalWrapMode.Wrap;
        text.raycastTarget=false;
    }

    void DrawAppIcon(Transform parent,int kind,Color accent,float size)
    {
        var tile=RoundImage(parent,"App icon",accent,cardSprite,Image.Type.Sliced);tile.raycastTarget=false;
        var r=tile.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(64,64);r.anchoredPosition=new Vector2(0,size>40?16:0);r.localScale=Vector3.one*(size/64);
        void Box(string name,float x,float y,float w,float h,Color c)
        {var p=ImagePanel(r,name,c);p.raycastTarget=false;p.rectTransform.anchorMin=p.rectTransform.anchorMax=new Vector2(.5f,.5f);p.rectTransform.anchoredPosition=new Vector2(x,y);p.rectTransform.sizeDelta=new Vector2(w,h);}
        var white=new Color(.94f,.97f,1);var dark=new Color(.08f,.18f,.3f);
        if(kind==0)
        {Box("Display frame",0,5,44,30,dark);Box("Glass display",0,6,36,22,new Color(.35f,.82f,1));Box("Screen reflection",-8,11,14,3,white);Box("Stand",0,-14,5,10,white);Box("Base",0,-20,28,4,white);}
        else if(kind==1)
        {Box("Notebook shadow",2,-2,35,44,dark);Box("Paper",0,1,34,44,white);Box("Binding",-12,1,5,44,new Color(.95f,.57f,.23f));for(int i=0;i<4;i++)Box("Ruled line",3,12-i*8,19,2,new Color(.35f,.5f,.65f));}
        else if(kind==2)
        {var head=RoundImage(r,"Portrait",white,circleSprite,Image.Type.Simple);head.raycastTarget=false;head.rectTransform.sizeDelta=new Vector2(21,21);head.rectTransform.anchoredPosition=new Vector2(0,10);Box("Shoulders",0,-14,34,21,white);Box("Shirt",0,-17,14,15,new Color(.23f,.33f,.68f));}
        else
        {Box("Bin body",0,-3,30,33,white);Box("Bin rim",0,16,38,5,white);Box("Handle",0,22,16,5,white);for(int i=0;i<3;i++)Box("Bin groove",-8+i*8,-3,3,22,accent);}
    }

    static void Emoji(Transform parent,Texture2D texture,float pad)
    {
        var icon=new GameObject("Emoji");
        icon.transform.SetParent(parent,false);
        var rect=icon.AddComponent<RectTransform>();
        Stretch(rect);
        rect.offsetMin=new Vector2(pad,pad);
        rect.offsetMax=new Vector2(-pad,-pad);
        var raw=icon.AddComponent<RawImage>();
        raw.texture=texture;
        raw.raycastTarget=false;
    }

    GameObject CreateWindow(Transform parent,string title,Vector2 position,Vector2 size,bool closable,out Text titleText,out Text bodyText)
    {
        var window=RoundImage(parent,title+" window",new Color(.055f,.075f,.12f,.98f),cardSprite,Image.Type.Sliced);
        var shadow=window.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.45f);shadow.effectDistance=new Vector2(5,-8);
        var rect=window.GetComponent<RectTransform>();
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
        rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=position;
        rect.sizeDelta=size;
        var header=RoundImage(window.transform,"Header",new Color(.12f,.17f,.25f),cardSprite,Image.Type.Sliced);
        header.gameObject.AddComponent<LaptopWindowDrag>().target=rect;
        var headerRect=header.GetComponent<RectTransform>();
        headerRect.anchorMin=new Vector2(0,1); headerRect.anchorMax=new Vector2(1,1);
        headerRect.pivot=new Vector2(.5f,1);
        headerRect.sizeDelta=new Vector2(-8,44); headerRect.anchoredPosition=new Vector2(0,-4);
        titleText=Label(header.transform,title,17,Color.white,TextAnchor.MiddleLeft);
        Stretch(titleText.rectTransform);
        titleText.rectTransform.offsetMin=new Vector2(12,0);
        titleText.rectTransform.offsetMax=new Vector2(closable?-92:-12,0);
        bodyText=Label(window.transform,"",22,new Color(.9f,.92f,.94f),TextAnchor.UpperLeft);
        Stretch(bodyText.rectTransform);
        bodyText.rectTransform.offsetMin=new Vector2(18,16);
        bodyText.rectTransform.offsetMax=new Vector2(-18,-64);
        if(closable)
        {
            var close=RoundImage(header.transform,"Close",new Color(.8f,.28f,.32f),cardSprite,Image.Type.Sliced);
            var closeRect=close.GetComponent<RectTransform>();
            closeRect.anchorMin=closeRect.anchorMax=new Vector2(1,.5f);
            closeRect.pivot=new Vector2(1,.5f);
            closeRect.anchoredPosition=new Vector2(-8,0);
            closeRect.sizeDelta=new Vector2(32,30);
            var closeButton=close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic=close;
            var windowObject=window.gameObject;
            closeButton.onClick.AddListener(()=>{ windowObject.SetActive(false); if(openAppSlot!=null && windowObject==appWindow) openAppSlot.SetActive(false); });
            var closeLabel=Label(close.transform,"×",22,Color.white,TextAnchor.MiddleCenter);
            Stretch(closeLabel.rectTransform);
            closeLabel.raycastTarget=false;
        }
        return window.gameObject;
    }

    static Image ImagePanel(Transform parent,string name,Color color)
    {
        var panel=new GameObject(name);
        panel.transform.SetParent(parent,false);
        panel.AddComponent<RectTransform>();
        var image=panel.AddComponent<Image>();
        image.color=color;
        return image;
    }

    Text Label(Transform parent,string value,int size,Color color,TextAnchor anchor)
    {
        var label=new GameObject("Label");
        label.transform.SetParent(parent,false);
        var rect=label.AddComponent<RectTransform>();
        var text=label.AddComponent<Text>();
        text.font=font;
        text.fontSize=size;
        text.color=color;
        text.alignment=anchor;
        text.text=value;
        text.raycastTarget=false;
        rect.sizeDelta=new Vector2(100,30);
        return text;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin=Vector2.zero;
        rect.anchorMax=Vector2.one;
        rect.offsetMin=Vector2.zero;
        rect.offsetMax=Vector2.zero;
    }

    void OnGUI()
    {
        if(!claimsUse) return;
        GUI.Box(new Rect(Screen.width*.5f-110,Screen.height-105,220,30),"E — Использовать");
    }
}

sealed class LaptopOrbit : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public LaptopDesktop desktop;
    public void OnPointerDown(PointerEventData eventData) { if(desktop!=null) desktop.SetPreviewDrag(true); }
    public void OnDrag(PointerEventData eventData) { if(desktop!=null) desktop.OrbitPreview(eventData.delta.x); }
    public void OnPointerUp(PointerEventData eventData) { if(desktop!=null) desktop.SetPreviewDrag(false); }
}
