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
    static readonly string[] AppNotes={"Этот компьютер.","Заметок пока нет.",""};
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
            CreateIcon(desktopRoot.transform,AppNames[i],AppColors[i],emoji,new Vector2(18,-12-i*85),()=>ShowApp(index));
        }
        appWindow=CreateWindow(desktopRoot.transform,"App",new Vector2(0,20),new Vector2(560,340),true,out appTitle,out appBody);
        appWindow.SetActive(false);
        BuildCharacterPicker();
        cameraWindow=CreateWindow(desktopRoot.transform,"Camera",new Vector2(-20,-20),new Vector2(500,320),false,out _,out _);
        var cameraRect=cameraWindow.GetComponent<RectTransform>();
        cameraRect.anchorMin=cameraRect.anchorMax=new Vector2(1,1);
        cameraRect.pivot=new Vector2(1,1);
        cameraRect.anchoredPosition=new Vector2(-20,-20);
        var feedObject=new GameObject("Feed");
        feedObject.transform.SetParent(cameraWindow.transform,false);
        var feedRect=feedObject.AddComponent<RectTransform>();
        feedRect.anchorMin=Vector2.zero; feedRect.anchorMax=Vector2.one;
        feedRect.offsetMin=new Vector2(10,10); feedRect.offsetMax=new Vector2(-10,-42);
        var feed=feedObject.AddComponent<RawImage>();
        feed.texture=roomTexture;
        feed.raycastTarget=false;

        BuildTaskbar();
    }

    void BuildTaskbar()
    {
        var bar=ImagePanel(desktopRoot.transform,"Taskbar",new Color(.09f,.09f,.1f,.96f));
        taskbar=bar.gameObject;
        var barRect=bar.rectTransform;
        barRect.anchorMin=new Vector2(0,0); barRect.anchorMax=new Vector2(1,0);
        barRect.pivot=new Vector2(.5f,0);
        barRect.sizeDelta=new Vector2(0,48); barRect.anchoredPosition=Vector2.zero;

        var start=ImagePanel(bar.transform,"Start",new Color(.22f,.47f,.86f));
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

        var search=TaskPiece(bar.transform,"Search",new Color(.2f,.2f,.22f),new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(98,0),new Vector2(210,32));
        var searchLabel=Label(search,"Поиск",16,new Color(.75f,.78f,.82f),TextAnchor.MiddleLeft);
        Stretch(searchLabel.rectTransform);
        searchLabel.rectTransform.offsetMin=new Vector2(14,0);

        float pinX=322f;
        for(int i=0;i<AppNames.Length;i++)
        {
            var pin=TaskPiece(bar.transform,AppNames[i],AppColors[i],new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(pinX,0),new Vector2(28,28));
            var emoji=string.IsNullOrEmpty(AppEmoji[i])?null:Resources.Load<Texture2D>(AppEmoji[i]);
            if(emoji!=null) Emoji(pin,emoji,2f);
            else
            {
                var mark=Label(pin,AppNames[i].Substring(0,1),14,Color.white,TextAnchor.MiddleCenter);
                Stretch(mark.rectTransform);
            }
            pinX+=34f;
        }

        float windowX=pinX+12f;
        var cameraSlot=TaskPiece(bar.transform,"Camera task",new Color(.2f,.24f,.3f),new Vector2(0,0),new Vector2(0,0),new Vector2(windowX,0),new Vector2(150,48));
        TaskPiece(cameraSlot,"Mark",new Color(.35f,.7f,.95f),new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(10,0),new Vector2(8,18));
        var cameraLabel=Label(cameraSlot,"Camera",15,Color.white,TextAnchor.MiddleLeft);
        Stretch(cameraLabel.rectTransform);
        cameraLabel.rectTransform.offsetMin=new Vector2(26,0);

        openAppSlot=TaskPiece(bar.transform,"Open app",new Color(.2f,.24f,.3f),new Vector2(0,0),new Vector2(0,0),new Vector2(windowX+156f,0),new Vector2(210,48)).gameObject;
        TaskPiece(openAppSlot.transform,"Mark",new Color(.9f,.75f,.3f),new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(10,0),new Vector2(8,18));
        openAppLabel=Label(openAppSlot.transform,"",15,Color.white,TextAnchor.MiddleLeft);
        Stretch(openAppLabel.rectTransform);
        openAppLabel.rectTransform.offsetMin=new Vector2(26,0);
        openAppLabel.rectTransform.offsetMax=new Vector2(-8,0);
        openAppSlot.SetActive(false);

        for(int i=0;i<3;i++)
            TaskPiece(bar.transform,"Tray icon",new Color(.78f,.8f,.84f),new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-108-i*22,0),new Vector2(16,16));
        var clock=TaskPiece(bar.transform,"Clock",new Color(0,0,0,0),new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-8,0),new Vector2(92,40));
        clockLabel=Label(clock,"00:00",15,Color.white,TextAnchor.UpperCenter);
        Stretch(clockLabel.rectTransform);
        clockLabel.rectTransform.offsetMax=new Vector2(0,-4);
        dateLabel=Label(clock,"",12,new Color(.8f,.84f,.88f),TextAnchor.LowerCenter);
        Stretch(dateLabel.rectTransform);
        dateLabel.rectTransform.offsetMin=new Vector2(0,4);
        RefreshClock();

        startMenu=ImagePanel(bar.transform,"Start menu",new Color(.16f,.17f,.2f,.98f)).gameObject;
        var menuRect=startMenu.GetComponent<RectTransform>();
        menuRect.anchorMin=menuRect.anchorMax=new Vector2(0,0);
        menuRect.pivot=new Vector2(0,0);
        menuRect.anchoredPosition=new Vector2(6,52);
        menuRect.sizeDelta=new Vector2(210,64);
        var exit=ImagePanel(startMenu.transform,"Exit",new Color(.7f,.22f,.2f));
        var exitRect=exit.rectTransform;
        exitRect.anchorMin=Vector2.zero; exitRect.anchorMax=Vector2.one;
        exitRect.offsetMin=new Vector2(8,8); exitRect.offsetMax=new Vector2(-8,-8);
        var exitButton=exit.gameObject.AddComponent<Button>();
        exitButton.targetGraphic=exit;
        exitButton.onClick.AddListener(BeginExit);
        var exitEmoji=Resources.Load<Texture2D>("LaptopEmoji/exit");
        if(exitEmoji!=null)
        {
            var icon=new GameObject("Emoji");
            icon.transform.SetParent(exit.transform,false);
            var iconRect=icon.AddComponent<RectTransform>();
            iconRect.anchorMin=iconRect.anchorMax=new Vector2(0,.5f);
            iconRect.pivot=new Vector2(0,.5f);
            iconRect.anchoredPosition=new Vector2(14,0);
            iconRect.sizeDelta=new Vector2(28,28);
            var raw=icon.AddComponent<RawImage>();
            raw.texture=exitEmoji;
            raw.raycastTarget=false;
        }
        var exitLabel=Label(exit.transform,"Выйти",18,Color.white,exitEmoji!=null?TextAnchor.MiddleLeft:TextAnchor.MiddleCenter);
        Stretch(exitLabel.rectTransform);
        if(exitEmoji!=null) exitLabel.rectTransform.offsetMin=new Vector2(48,0);
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
                rect.sizeDelta=new Vector2(560,340);
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
            }
        }
        if(cameraWindow!=null) cameraWindow.transform.SetAsLastSibling();
        if(openAppSlot!=null)
        {
            openAppSlot.SetActive(true);
            openAppLabel.text=AppNames[index];
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
        var card=RoundImage(desktopRoot.transform,"Character picker",new Color(.11f,.12f,.15f,.98f),cardSprite,Image.Type.Sliced);
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
        var close=RoundButton(characterPicker.transform,"×",new Color(1f,1f,1f,.1f),new Vector2(40,40),new Vector2(1,1),new Vector2(1,1),ClosePicker,out var closeLabel);
        close.anchoredPosition=new Vector2(-16,-12);
        closeLabel.fontSize=24;

        var stage=RoundImage(characterPicker.transform,"Stage",new Color(.07f,.08f,.11f,1f),cardSprite,Image.Type.Sliced);
        var stageRect=stage.rectTransform;
        stageRect.anchorMin=Vector2.zero; stageRect.anchorMax=Vector2.one;
        stageRect.offsetMin=new Vector2(18,118); stageRect.offsetMax=new Vector2(-18,-60);

        var viewObject=new GameObject("Preview");
        viewObject.transform.SetParent(stage.transform,false);
        var viewRect=viewObject.AddComponent<RectTransform>();
        Stretch(viewRect);
        viewRect.offsetMin=new Vector2(64,28); viewRect.offsetMax=new Vector2(-64,-16);
        var fitter=viewObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio=1f;
        var characterView=viewObject.AddComponent<RawImage>();
        characterView.texture=characterTexture;
        characterView.raycastTarget=true;
        viewObject.AddComponent<LaptopOrbit>().desktop=this;

        var previous=RoundButton(stage.transform,"‹",new Color(1f,1f,1f,.12f),new Vector2(52,52),new Vector2(0,.5f),new Vector2(0,.5f),()=>StepCharacter(-1),out var previousLabel);
        previous.anchoredPosition=new Vector2(14,0);
        previousLabel.fontSize=30;
        var next=RoundButton(stage.transform,"›",new Color(1f,1f,1f,.12f),new Vector2(52,52),new Vector2(1,.5f),new Vector2(1,.5f),()=>StepCharacter(1),out var nextLabel);
        next.anchoredPosition=new Vector2(-14,0);
        nextLabel.fontSize=30;
        var hint=Label(stage.transform,"Потяните, чтобы повернуть",14,new Color(1f,1f,1f,.42f),TextAnchor.MiddleCenter);
        var hintRect=hint.rectTransform;
        hintRect.anchorMin=new Vector2(0,0); hintRect.anchorMax=new Vector2(1,0);
        hintRect.pivot=new Vector2(.5f,0);
        hintRect.sizeDelta=new Vector2(-24,24);
        hintRect.anchoredPosition=new Vector2(0,8);

        characterName=Label(characterPicker.transform,"",30,Color.white,TextAnchor.MiddleLeft);
        characterName.fontStyle=FontStyle.Bold;
        var nameRect=characterName.rectTransform;
        nameRect.anchorMin=nameRect.anchorMax=new Vector2(0,0);
        nameRect.pivot=Vector2.zero;
        nameRect.sizeDelta=new Vector2(420,38);
        nameRect.anchoredPosition=new Vector2(28,62);
        characterPose=Label(characterPicker.transform,"",16,new Color(.62f,.68f,.76f),TextAnchor.MiddleLeft);
        var poseRect=characterPose.rectTransform;
        poseRect.anchorMin=poseRect.anchorMax=new Vector2(0,0);
        poseRect.pivot=Vector2.zero;
        poseRect.sizeDelta=new Vector2(120,24);
        poseRect.anchoredPosition=new Vector2(28,34);
        pageDots=new Image[6];
        for(int i=0;i<pageDots.Length;i++)
        {
            var dot=RoundImage(characterPicker.transform,"Dot",new Color(1f,1f,1f,.28f),circleSprite,Image.Type.Simple);
            var dotRect=dot.rectTransform;
            dotRect.anchorMin=dotRect.anchorMax=new Vector2(0,0);
            dotRect.pivot=new Vector2(0,.5f);
            dotRect.sizeDelta=new Vector2(8,8);
            dotRect.anchoredPosition=new Vector2(156+i*16,46);
            pageDots[i]=dot;
        }
        var choose=RoundButton(characterPicker.transform,"Выбрать",new Color(.23f,.47f,.98f),new Vector2(188,48),new Vector2(1,0),new Vector2(1,0),ChooseCharacter,out chooseLabel);
        choose.anchoredPosition=new Vector2(-22,32);
        chooseImage=choose.GetComponent<Image>();
        chooseLabel.fontSize=20;
        characterPicker.SetActive(false);
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
        if(characterName!=null) characterName.text=prefab==null?"Нет персонажей":CharacterTitle(prefab);
        if(characterPose!=null) characterPose.text=prefab==null?"":PreviewCaption[characterIndex%PreviewCaption.Length];
        bool equipped=prefab!=null && controller!=null && controller.humanoidAnimator!=null && controller.humanoidAnimator.gameObject.name.StartsWith(prefab.name);
        if(chooseLabel!=null) chooseLabel.text=equipped?"Выбран":"Выбрать";
        if(chooseImage!=null) chooseImage.color=equipped?new Color(.15f,.46f,.34f):new Color(.23f,.47f,.98f);
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
        var prefab=characters[characterIndex];
        controller.EquipCharacter(prefab);
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
        var button=ImagePanel(parent,label,color);
        var rect=button.GetComponent<RectTransform>();
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);
        rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=position;
        rect.sizeDelta=new Vector2(108,82);
        var uiButton=button.gameObject.AddComponent<Button>();
        uiButton.targetGraphic=button;
        uiButton.onClick.AddListener(click);
        if(emoji!=null)
        {
            var icon=new GameObject("Emoji");
            icon.transform.SetParent(button.transform,false);
            var iconRect=icon.AddComponent<RectTransform>();
            iconRect.anchorMin=iconRect.anchorMax=new Vector2(.5f,1);
            iconRect.pivot=new Vector2(.5f,1);
            iconRect.anchoredPosition=new Vector2(0,-4);
            iconRect.sizeDelta=new Vector2(40,40);
            var raw=icon.AddComponent<RawImage>();
            raw.texture=emoji;
            raw.raycastTarget=false;
        }
        var text=Label(button.transform,label,emoji!=null?13:15,Color.white,TextAnchor.LowerCenter);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin=new Vector2(4,4);
        text.rectTransform.offsetMax=new Vector2(-4,emoji!=null?-44:-4);
        text.horizontalOverflow=HorizontalWrapMode.Wrap;
        text.raycastTarget=false;
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
        var window=ImagePanel(parent,title+" window",new Color(.12f,.14f,.18f,.96f));
        var rect=window.GetComponent<RectTransform>();
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
        rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=position;
        rect.sizeDelta=size;
        var header=ImagePanel(window.transform,"Header",new Color(.2f,.24f,.3f,1));
        var headerRect=header.GetComponent<RectTransform>();
        headerRect.anchorMin=new Vector2(0,1); headerRect.anchorMax=new Vector2(1,1);
        headerRect.pivot=new Vector2(.5f,1);
        headerRect.sizeDelta=new Vector2(0,36); headerRect.anchoredPosition=Vector2.zero;
        titleText=Label(header.transform,title,20,Color.white,TextAnchor.MiddleLeft);
        Stretch(titleText.rectTransform);
        titleText.rectTransform.offsetMin=new Vector2(12,0);
        titleText.rectTransform.offsetMax=new Vector2(closable?-92:-12,0);
        bodyText=Label(window.transform,"",22,new Color(.9f,.92f,.94f),TextAnchor.UpperLeft);
        Stretch(bodyText.rectTransform);
        bodyText.rectTransform.offsetMin=new Vector2(18,16);
        bodyText.rectTransform.offsetMax=new Vector2(-18,-48);
        if(closable)
        {
            var close=ImagePanel(header.transform,"Close",new Color(.7f,.22f,.2f));
            var closeRect=close.GetComponent<RectTransform>();
            closeRect.anchorMin=closeRect.anchorMax=new Vector2(1,.5f);
            closeRect.pivot=new Vector2(1,.5f);
            closeRect.anchoredPosition=new Vector2(-8,0);
            closeRect.sizeDelta=new Vector2(76,26);
            var closeButton=close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic=close;
            var windowObject=window.gameObject;
            closeButton.onClick.AddListener(()=>{ windowObject.SetActive(false); if(openAppSlot!=null && windowObject==appWindow) openAppSlot.SetActive(false); });
            var closeLabel=Label(close.transform,"Закрыть",14,Color.white,TextAnchor.MiddleCenter);
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
