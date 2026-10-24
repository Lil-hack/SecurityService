using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Desktop prototype: owns one local LiteRT-LM CPU worker per chat.</summary>
public sealed class GemmaTestChat : MonoBehaviour
{
    [SerializeField] string modelPath = "Models/gemma-4-E2B-it.litertlm";
    [SerializeField] string pythonPath = ".local/gemma-runtime/Scripts/python.exe";
    [SerializeField] bool characterChatMode;

    [Serializable] sealed class Character
    {
        public string id, name, promptPath, voiceProfilePath, openingText;
    }
    [Serializable] sealed class CharacterCatalog { public Character[] characters; }
    Character[] characters;
    Character selectedCharacter;
    PopupField<string> characterChoice;
    Button retryButton;
    string CharacterName => selectedCharacter == null ? "Нина Петровна" : selectedCharacter.name;

    [Serializable] sealed class Command
    {
        public string command, text, state;
        public int trust = -1;
        public bool mission_complete;
    }
    [Serializable] sealed class Reply
    {
        public string kind, text;
        public string[] events;
        public bool valid, new_information;
    }
    readonly CharacterTrustMission trustMission = new CharacterTrustMission();
    ProgressBar trustBar;
    Label trustFeedback, missionGoal;
    string pendingPlayerMessage;
    bool awaitingEvaluation;
    PhoneChatView phoneView;
    PhoneCallState callState = PhoneCallState.Incoming;
    bool openingStarted;
    bool openingContextQueued;
    AudioClip preparedOpening;
    string OpeningText => string.IsNullOrWhiteSpace(selectedCharacter?.openingText)
        ? "Алло? Это Нина Петровна. Ой, милок, мне тревожное сообщение пришло!" : selectedCharacter.openingText;

    readonly ConcurrentQueue<Action> pending = new ConcurrentQueue<Action>();
    Process worker;
    PanelSettings panel;
    TextField modelInput;
    TextField messageInput;
    Label subtitle;
    Label status;
    Label answer;
    ScrollView history;
    Button loadButton, sendButton, resetButton, stopButton;
    GemmaSpeech speech;
    Toggle speechEnabled;
    Toggle telephoneEffect;
    Slider telephoneStrength, voicePitch;
    Slider speechVolume;
    PopupField<string> voiceChoice;
    Button previewVoice;
    readonly List<string> voicePaths = new List<string>();
    readonly List<string> voiceLabels = new List<string>();
    Label speechStatus;
    Button repeatSpeech;
    bool ready, busy;
    bool previousRunInBackground;
    int session;
    string diagnostics = "";
    string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

    void OnEnable()
    {
        phoneView = null;
        callState = PhoneCallState.Incoming; openingStarted = false;
        try { NinaVoiceProfile.Load(Root); }
        catch (Exception error) { UnityEngine.Debug.LogWarning("Не удалось загрузить голос Нины: " + error.Message); }
        previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        panel = ScriptableObject.CreateInstance<PanelSettings>();
        panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panel.referenceResolution = new Vector2Int(1100, 800);
        panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("GemmaTheme");
        var document = GetComponent<UIDocument>();
        if (document == null) document = gameObject.AddComponent<UIDocument>();
        document.panelSettings = panel;
        document.enabled = true;
        document.rootVisualElement.Clear();
        BuildUI(document.rootVisualElement);
        speech = GetComponent<GemmaSpeech>();
        if (speech == null) speech = gameObject.AddComponent<GemmaSpeech>();
        speech.enabled = true;
        speech.Volume = speechVolume.value / 100f;
        speech.TelephoneEnabled = telephoneEffect.value;
        speech.TelephoneStrength = telephoneStrength.value / 100f;
        speech.PitchSemitones = voicePitch.value;
        ApplyVoiceCharacter();
        speech.StatusChanged += OnSpeechStatus;
        speech.Initialize(Resolve(pythonPath), Root, voicePaths[voiceLabels.IndexOf(voiceChoice.value)]);
        if (characterChatMode)
        {
            try { ApplySelectedCharacterVoice(); LoadModel(); }
            catch (Exception error) { Fail(error.Message); }
        }
    }

    void BuildUI(VisualElement root)
    {
        root.styleSheets.Add(Resources.Load<StyleSheet>("GemmaChat"));
        root.style.flexGrow = 1;
        root.style.backgroundColor = new Color(.035f, .05f, .085f);
        root.style.paddingLeft = root.style.paddingRight = 36;
        root.style.paddingTop = root.style.paddingBottom = 28;
        root.style.color = new Color(.90f, .94f, 1f);
        var title = new Label("Gemma / Локальный чат");
        title.style.fontSize = 30;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        root.Add(title);
        subtitle = new Label("GEMMA 4 E2B IT    •    LiteRT-LM    •    CPU");
        subtitle.style.color = new Color(.45f, .78f, .95f);
        subtitle.style.marginBottom = 18;
        root.Add(subtitle);
        modelInput = new TextField("Файл модели (.litertlm)") { value = modelPath };
        modelInput.style.marginBottom = 8;
        root.Add(modelInput);
        var toolbar = new VisualElement();
        toolbar.style.flexDirection = FlexDirection.Row;
        toolbar.style.flexShrink = 0;
        toolbar.style.minHeight = 40;
        loadButton = MakeButton("Загрузить модель", LoadModel);
        resetButton = MakeButton("Новый чат", ResetChat);
        stopButton = MakeButton("Остановить / выгрузить", StopModel);
        toolbar.Add(loadButton); toolbar.Add(resetButton); toolbar.Add(stopButton);
        root.Add(toolbar);
        var voiceBar = new VisualElement();
        voiceBar.style.flexDirection = FlexDirection.Row;
        voiceBar.style.flexShrink = 0;
        voiceBar.style.minHeight = 40;
        voiceBar.style.marginTop = 8;
        voiceBar.style.alignItems = Align.Center;
        speechEnabled = new Toggle("Озвучивать ответы") { value = true };
        speechEnabled.RegisterValueChangedCallback(evt => { if (!evt.newValue && speech != null) speech.Cancel(); });
        voiceBar.Add(speechEnabled);
        repeatSpeech = MakeButton("Повторить голосом", () =>
        {
            speechEnabled.value = true;
            if (speech != null && answer != null) speech.Speak(answer.text);
        });
        repeatSpeech.SetEnabled(false);
        voiceBar.Add(repeatSpeech);
        voiceBar.Add(MakeButton("Остановить голос", () => { if (speech != null) speech.Cancel(); }));
        speechVolume = new Slider("Громкость, %", 0f, 100f)
        {
            value = Mathf.Clamp01(PlayerPrefs.GetFloat("GemmaChat.SpeechVolume", 1f)) * 100f,
            showInputField = true
        };
        speechVolume.style.flexGrow = 1;
        speechVolume.style.minWidth = 240;
        speechVolume.style.maxWidth = 380;
        speechVolume.style.marginLeft = 12;
        speechVolume.labelElement.style.minWidth = 100;
        speechVolume.labelElement.style.width = 100;
        speechVolume.RegisterValueChangedCallback(evt =>
        {
            float volume = evt.newValue / 100f;
            if (speech != null) speech.Volume = volume;
            PlayerPrefs.SetFloat("GemmaChat.SpeechVolume", volume);
        });
        voiceBar.Add(speechVolume);
        root.Add(voiceBar);
        BuildVoiceChoice(root);
        speechStatus = new Label("Загрузка голоса…");
        speechStatus.style.whiteSpace = WhiteSpace.Normal;
        speechStatus.style.color = new Color(.45f, .78f, .95f);
        root.Add(speechStatus);
        status = new Label("Выберите файл модели и нажмите «Загрузить модель».");
        status.style.whiteSpace = WhiteSpace.Normal;
        status.style.marginTop = status.style.marginBottom = 12;
        root.Add(status);
        history = new ScrollView();
        history.style.flexGrow = 1;
        history.style.backgroundColor = new Color(.06f, .08f, .125f);
        history.style.paddingLeft = history.style.paddingRight = 18;
        history.style.paddingTop = history.style.paddingBottom = 12;
        root.Add(history);
        if (!characterChatMode) AddMessage("Чат", "Модель работает локально. После загрузки напишите сообщение ниже.");
        messageInput = new TextField { multiline = true, maxLength = 2000 };
        messageInput.style.height = 88;
        messageInput.style.marginTop = 14;
        messageInput.style.fontSize = 18;
        messageInput.RegisterCallback<KeyDownEvent>(evt =>
        {
            if ((evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && !evt.shiftKey)
            {
                SendMessage(); evt.StopPropagation();
            }
        }, TrickleDown.TrickleDown);
        root.Add(messageInput);
        var footer = new VisualElement();
        footer.style.flexDirection = FlexDirection.Row;
        footer.style.justifyContent = Justify.SpaceBetween;
        footer.Add(new Label("Enter — отправить    ·    Shift + Enter — новая строка"));
        sendButton = MakeButton("Отправить →", SendMessage);
        footer.Add(sendButton); root.Add(footer);
        UpdateControls();
        if (characterChatMode) BuildCharacterUI(root);
    }

    void BuildCharacterUI(VisualElement root)
    {
        // Reuse the chat and worker controls; the tuning UI stays in GemmaChat.
        root.Clear();
        root.Add(new Label("Разговор с персонажем") { style = { fontSize = 28, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 16 } });
        try
        {
            var catalog = JsonUtility.FromJson<CharacterCatalog>(File.ReadAllText(Resolve("Tools/Gemma/Prompts/characters.json")));
            characters = catalog.characters;
            if (characters == null || characters.Length == 0) throw new InvalidDataException("Нет настроенных персонажей");
        }
        catch (Exception error)
        {
            UnityEngine.Debug.LogWarning("Каталог персонажей: " + error.Message);
            characters = new[] { new Character { id = "nina", name = "Нина Петровна", promptPath = "Tools/Gemma/Prompts/nina_petrovna.txt", voiceProfilePath = "Tools/Gemma/Prompts/nina_voice.json" } };
        }
        var names = new List<string>();
        foreach (var character in characters) names.Add(character.name);
        int selected = Array.FindIndex(characters, c => c.id == PlayerPrefs.GetString("CharacterChat.Selected", "nina"));
        if (selected < 0) selected = 0;
        selectedCharacter = characters[selected];
        characterChoice = new PopupField<string>("Персонаж", names, selected) { name = "characterChoice" };
        characterChoice.style.flexGrow = 1;
        characterChoice.style.alignSelf = Align.Center;
        characterChoice.RegisterValueChangedCallback(evt => SelectCharacter(characters[names.IndexOf(evt.newValue)]));
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.flexShrink = 0;
        header.Add(characterChoice);
        header.Add(resetButton);
        root.Add(header);
        trustBar = new ProgressBar { name = "trustBar", lowValue = 0, highValue = 100 };
        trustBar.style.height = 24;
        trustBar.style.flexShrink = 0;
        trustBar.style.marginTop = 10;
        root.Add(trustBar);
        missionGoal = new Label("Цель: успокоить Нину и помочь ей самостоятельно проверить ситуацию через официальный канал банка.");
        missionGoal.style.whiteSpace = WhiteSpace.Normal;
        missionGoal.style.marginTop = 6;
        root.Add(missionGoal);
        trustFeedback = new Label { name = "trustFeedback" };
        trustFeedback.style.whiteSpace = WhiteSpace.Normal;
        trustFeedback.style.marginTop = 4;
        root.Add(trustFeedback);
        UpdateTrustUI();
        status.text = "Подключаю персонажа…";
        root.Add(status);
        retryButton = MakeButton("Повторить подключение", () =>
        {
            try
            {
                ApplySelectedCharacterVoice();
                if (!speech.Ready) speech.SelectVoice(speech.VoicePath);
                LoadModel();
            }
            catch (Exception error) { Fail(error.Message); }
        });
        retryButton.style.display = DisplayStyle.None;
        root.Add(retryButton);
        history.Clear(); history.name = "conversation";
        root.Add(history);
        messageInput.name = "messageInput";
        root.Add(messageInput);
        var footer = new VisualElement();
        footer.style.flexDirection = FlexDirection.Row;
        footer.style.justifyContent = Justify.SpaceBetween;
        footer.style.flexShrink = 0;
        footer.Add(new Label("Enter — отправить · Shift + Enter — новая строка"));
        footer.Add(sendButton);
        root.Add(footer);
        phoneView = new PhoneChatView(root, characterChoice, trustBar, trustFeedback, status,
            history, messageInput, sendButton, resetButton, retryButton,
            () => { StopModel(); status.text = "Разговор завершён"; },
            () => { speechEnabled.value = !speechEnabled.value; phoneView?.SetSound(speechEnabled.value); },
            AnswerIncomingCall);
        phoneView.SetSound(speechEnabled.value);
        phoneView.SetCallState(callState, CharacterName);
        UpdateTrustUI();
    }

    void ApplySelectedCharacterVoice()
    {
        if (selectedCharacter == null) return;
        var profile = JsonUtility.FromJson<NinaVoiceProfile>(File.ReadAllText(Resolve(selectedCharacter.voiceProfilePath)));
        if (profile == null || string.IsNullOrWhiteSpace(profile.voicePath)) throw new InvalidDataException("Не задан голос персонажа");
        preparedOpening = string.IsNullOrWhiteSpace(profile.openingAudio) ? null : Resources.Load<AudioClip>(profile.openingAudio);
        if (preparedOpening != null) preparedOpening.LoadAudioData();
        speech.Cancel();
        speech.TelephoneEnabled = profile.telephone;
        speech.TelephoneStrength = profile.telephoneStrength / 100f;
        speech.PitchSemitones = profile.pitchSemitones;
        speech.SpeechRate = profile.ratePercent / 100f;
        speech.PauseSeconds = profile.pauseMilliseconds / 1000f;
        speech.Tremble = profile.tremblePercent / 100f;
        speech.VolumeVariation = profile.variationPercent / 100f;
        speech.Rasp = profile.raspPercent / 100f;
        if (speech.VoicePath != profile.voicePath) speech.SelectVoice(profile.voicePath);
    }

    void SelectCharacter(Character character)
    {
        selectedCharacter = character;
        PlayerPrefs.SetString("CharacterChat.Selected", character.id);
        try
        {
            ApplySelectedCharacterVoice();
            if (!ready) { LoadModel(); return; }
            ResetTrust();
            PrepareIncomingCall();
            history.Clear(); answer = null; busy = true;
            status.text = "Подключаю персонажа…";
            WriteCommand("character", File.ReadAllText(Resolve(character.promptPath)));
            UpdateControls();
        }
        catch (Exception error) { Fail(error.Message); }
    }

    void BuildVoiceChoice(VisualElement root)
    {
        voicePaths.Clear(); voiceLabels.Clear();
        string voicesRoot = Resolve("Models/Piper");
        if (Directory.Exists(voicesRoot))
        {
            string[] models = Directory.GetFiles(voicesRoot, "*.onnx", SearchOption.AllDirectories);
            Array.Sort(models, StringComparer.OrdinalIgnoreCase);
            foreach (string model in models)
            {
                if (!File.Exists(model + ".json")) continue;
                string relative = Path.GetRelativePath(Root, model).Replace('\\', '/');
                string name = Path.GetFileNameWithoutExtension(model);
                string label = name == "ru_RU-irina-medium" ? "Irina — исходный русский голос"
                    : (name.StartsWith("female", StringComparison.Ordinal) ? "Женский — "
                    : name.StartsWith("male", StringComparison.Ordinal) ? "Мужской — " : "") + name;
                if (voiceLabels.Contains(label)) label += " (" + relative + ")";
                voicePaths.Add(relative); voiceLabels.Add(label);
            }
        }
        if (voicePaths.Count == 0)
        {
            voicePaths.Add("Models/Piper/ru_RU-irina-medium.onnx");
            voiceLabels.Add("Irina — голос не установлен");
        }
        string selected = PlayerPrefs.GetString("GemmaChat.PiperVoice", "Models/Piper/ru_RU-irina-medium.onnx");
        int index = voicePaths.FindIndex(path => string.Equals(path, selected, StringComparison.OrdinalIgnoreCase));
        if (index < 0) index = 0;
        voiceChoice = new PopupField<string>("Голос", voiceLabels, index);
        voiceChoice.style.flexShrink = 0;
        voiceChoice.style.minHeight = 30;
        voiceChoice.style.marginTop = 6;
        voiceChoice.RegisterValueChangedCallback(evt =>
        {
            string path = voicePaths[voiceLabels.IndexOf(evt.newValue)];
            PlayerPrefs.SetString("GemmaChat.PiperVoice", path);
            if (speech != null) speech.SelectVoice(path);
        });
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.flexShrink = 0;
        row.style.minHeight = 40;
        voiceChoice.style.flexGrow = 1;
        row.Add(voiceChoice);
        previewVoice = MakeButton("Проба голоса", PreviewSelectedVoice);
        row.Add(previewVoice);
        telephoneEffect = new Toggle("Телефон")
        {
            value = PlayerPrefs.GetInt("GemmaChat.TelephoneEffect", 1) != 0,
            tooltip = "Телефонное звучание: диапазон голоса 300–3400 Гц"
        };
        telephoneEffect.style.alignSelf = Align.Center;
        telephoneEffect.style.marginLeft = 12;
        telephoneEffect.RegisterValueChangedCallback(evt =>
        {
            PlayerPrefs.SetInt("GemmaChat.TelephoneEffect", evt.newValue ? 1 : 0);
            if (speech != null) speech.TelephoneEnabled = evt.newValue;
        });
        row.Add(telephoneEffect);
        root.Add(row);
        var effectsRow = new VisualElement();
        effectsRow.style.flexDirection = FlexDirection.Row;
        effectsRow.style.flexShrink = 0;
        effectsRow.style.minHeight = 36;
        effectsRow.style.alignItems = Align.Center;
        telephoneStrength = new Slider("Сила телефона, %", 0f, 100f)
        {
            value = Mathf.Clamp01(PlayerPrefs.GetFloat("GemmaChat.TelephoneStrength", .5f)) * 100f,
            showInputField = true,
            tooltip = "0 — без фильтра; 50 — обычный телефон; 100 — узкий диапазон"
        };
        telephoneStrength.style.flexGrow = 1;
        telephoneStrength.style.minWidth = 300;
        telephoneStrength.RegisterValueChangedCallback(evt =>
        {
            float strength = evt.newValue / 100f;
            PlayerPrefs.SetFloat("GemmaChat.TelephoneStrength", strength);
            if (speech != null) speech.TelephoneStrength = strength;
        });
        effectsRow.Add(telephoneStrength);
        voicePitch = new Slider("Тон, полутоны", -12f, 7f)
        {
            value = Mathf.Clamp(PlayerPrefs.GetFloat("GemmaChat.PitchSemitones", 0f), -12f, 7f),
            showInputField = true,
            tooltip = "0 — исходный голос; отрицательные значения понижают тон, положительные повышают. Темп компенсируется для следующих фраз."
        };
        voicePitch.style.flexGrow = 1;
        voicePitch.style.minWidth = 300;
        voicePitch.style.marginLeft = 16;
        voicePitch.RegisterValueChangedCallback(evt =>
        {
            PlayerPrefs.SetFloat("GemmaChat.PitchSemitones", evt.newValue);
            if (speech != null) speech.PitchSemitones = evt.newValue;
        });
        effectsRow.Add(voicePitch);
        root.Add(effectsRow);
        var characterRow = new VisualElement();
        characterRow.style.flexDirection = FlexDirection.Row;
        characterRow.style.flexWrap = Wrap.Wrap;
        characterRow.style.flexShrink = 0;
        AddVoiceSetting(characterRow, "Темп, %", "Rate", 65f, 130f, 91f,
            "100 — обычный темп. Применяется к следующим синтезируемым фразам.");
        AddVoiceSetting(characterRow, "Паузы, мс", "Pause", 0f, 500f, 150f,
            "Дополнительные паузы между предложениями и после междометий. Для следующих фраз.");
        AddVoiceSetting(characterRow, "Дрожание, %", "Tremble", 0f, 100f, 20f,
            "Лёгкие плавные колебания высоты голоса.");
        AddVoiceSetting(characterRow, "Неровность, %", "Variation", 0f, 100f, 20f,
            "Мягкие изменения громкости, не превышающие выбранную общую громкость.");
        AddVoiceSetting(characterRow, "Хрипотца, %", "Rasp", 0f, 100f, 12f,
            "Лёгкое искажение для шероховатого тембра.");
        root.Add(characterRow);
        root.Add(MakeButton("Сохранить голос Нины", SaveNinaVoice));
    }

    void SaveNinaVoice()
    {
        try
        {
            new NinaVoiceProfile
            {
                voicePath = voicePaths[voiceLabels.IndexOf(voiceChoice.value)],
                telephone = telephoneEffect.value, telephoneStrength = telephoneStrength.value,
                pitchSemitones = voicePitch.value,
                ratePercent = PlayerPrefs.GetFloat("GemmaChat.Character.Rate", 88f),
                pauseMilliseconds = PlayerPrefs.GetFloat("GemmaChat.Character.Pause", 180f),
                tremblePercent = PlayerPrefs.GetFloat("GemmaChat.Character.Tremble", 18f),
                variationPercent = PlayerPrefs.GetFloat("GemmaChat.Character.Variation", 22f),
                raspPercent = PlayerPrefs.GetFloat("GemmaChat.Character.Rasp", 10f)
            }.Save(Root);
            PlayerPrefs.Save();
            speechStatus.text = "Голос Нины сохранён";
        }
        catch (Exception error) { speechStatus.text = "Не удалось сохранить голос Нины: " + error.Message; }
    }

    void AddVoiceSetting(VisualElement row, string label, string key, float min, float max, float initial, string hint)
    {
        string pref = "GemmaChat.Character." + key;
        var slider = new Slider(label, min, max)
        {
            name = "voice" + key, showInputField = true, tooltip = hint,
            value = Mathf.Clamp(PlayerPrefs.GetFloat(pref, initial), min, max)
        };
        slider.style.width = 290;
        slider.style.flexGrow = 1;
        slider.style.marginRight = 12;
        slider.style.minHeight = 32;
        slider.RegisterValueChangedCallback(evt =>
        {
            PlayerPrefs.SetFloat(pref, evt.newValue);
            ApplyVoiceCharacter();
        });
        row.Add(slider);
    }

    void ApplyVoiceCharacter()
    {
        if (speech == null) return;
        speech.SpeechRate = Mathf.Clamp(PlayerPrefs.GetFloat("GemmaChat.Character.Rate", 91f), 65f, 130f) / 100f;
        speech.PauseSeconds = Mathf.Clamp(PlayerPrefs.GetFloat("GemmaChat.Character.Pause", 150f), 0f, 500f) / 1000f;
        speech.Tremble = Mathf.Clamp01(PlayerPrefs.GetFloat("GemmaChat.Character.Tremble", 20f) / 100f);
        speech.VolumeVariation = Mathf.Clamp01(PlayerPrefs.GetFloat("GemmaChat.Character.Variation", 20f) / 100f);
        speech.Rasp = Mathf.Clamp01(PlayerPrefs.GetFloat("GemmaChat.Character.Rasp", 12f) / 100f);
    }

    void PreviewSelectedVoice()
    {
        if (speech != null && speech.Ready && !busy)
            speech.Speak("Здравствуйте. Это проверка голоса. Вы хорошо меня слышите?");
    }

    static Button MakeButton(string text, Action action)
    {
        var button = new Button(action) { text = text };
        button.style.height = 36;
        button.style.paddingLeft = button.style.paddingRight = 14;
        return button;
    }

    Label AddMessage(string who, string text)
    {
        var item = new VisualElement();
        item.style.marginBottom = 18;
        var author = new Label(who);
        author.style.color = new Color(.45f, .78f, .95f);
        author.style.unityFontStyleAndWeight = FontStyle.Bold;
        item.Add(author);
        var content = new Label(text);
        content.style.whiteSpace = WhiteSpace.Normal;
        content.style.fontSize = 18;
        content.enableRichText = false;
        if (characterChatMode)
        {
            bool player = who == "Вы";
            item.style.marginBottom = 8;
            item.style.maxWidth = Length.Percent(92);
            item.style.alignSelf = player ? Align.FlexEnd : Align.FlexStart;
            item.style.backgroundColor = player ? new Color(.08f, .20f, .16f) : new Color(.08f, .19f, .31f);
            item.style.paddingLeft = item.style.paddingRight = 8;
            item.style.paddingTop = item.style.paddingBottom = 7;
            item.style.borderTopLeftRadius = item.style.borderTopRightRadius = item.style.borderBottomLeftRadius = item.style.borderBottomRightRadius = 5;
            author.style.fontSize = 11;
            author.style.alignSelf = Align.FlexStart;
            author.style.backgroundColor = player ? new Color(.12f, .38f, .27f) : new Color(.12f, .38f, .61f);
            author.style.color = new Color(.82f, .94f, 1f);
            author.style.paddingLeft = author.style.paddingRight = 4;
            author.style.marginBottom = 4;
            content.style.fontSize = 14;
            content.style.color = player ? new Color(.75f, .91f, .82f) : new Color(.59f, .78f, .97f);
        }
        item.Add(content); history.Add(item);
        var targetHistory = history;
        targetHistory.schedule.Execute(() =>
        {
            if (targetHistory.Contains(item)) targetHistory.ScrollTo(item);
        });
        return content;
    }

    string Resolve(string path) => Path.IsPathRooted(path) ? path : Path.Combine(Root, path);

    void LoadModel()
    {
        StopWorker();
        ResetTrust();
        PrepareIncomingCall();
        string weights = GemmaModelFiles.Resolve(modelInput.value.Trim().Trim('"'), Root);
        string python = Resolve(pythonPath);
        if (!File.Exists(weights)) { Fail("Файл модели не найден. Укажите полный путь к .litertlm."); return; }
        if (!weights.EndsWith(".litertlm", StringComparison.OrdinalIgnoreCase))
        { Fail("Для этого чата нужен файл .litertlm."); return; }
        if (!File.Exists(python)) { Fail("Среда LiteRT-LM не установлена. См. Tools/Gemma/README.md."); return; }
        int current = session;
        diagnostics = "";
        try
        {
            worker = new Process { StartInfo = new ProcessStartInfo
            {
                FileName = python,
                Arguments = "-u \"" + Resolve("Tools/Gemma/worker.py") + "\" --model \"" + weights + "\""
                    + (selectedCharacter == null ? "" : " --system-prompt \"" + Resolve(selectedCharacter.promptPath) + "\""),
                WorkingDirectory = Root,
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
            }, EnableRaisingEvents = true };
            worker.StartInfo.EnvironmentVariables["PYTHONUTF8"] = "1";
            worker.OutputDataReceived += (_, evt) =>
            {
                if (!string.IsNullOrEmpty(evt.Data))
                    pending.Enqueue(() => { if (current == session) Receive(evt.Data); });
            };
            worker.ErrorDataReceived += (_, evt) =>
            {
                if (!string.IsNullOrEmpty(evt.Data)) pending.Enqueue(() =>
                {
                    if (current == session) diagnostics = (diagnostics + "\n" + evt.Data);
                    if (diagnostics.Length > 3000) diagnostics = diagnostics.Substring(diagnostics.Length - 3000);
                });
            };
            worker.Exited += (_, __) => pending.Enqueue(() =>
            {
                if (current == session) Fail("Процесс модели завершён. " + diagnostics);
            });
            worker.Start(); worker.BeginOutputReadLine(); worker.BeginErrorReadLine();
            history.Clear(); answer = null;
            busy = true;
            status.text = characterChatMode ? "Подключаю " + CharacterName + "…" : "Загружаю модель на CPU… Первый запуск может занять некоторое время.";
            UpdateControls();
        }
        catch (Exception error) { StopWorker(); Fail(error.Message); }
    }

    void Receive(string json)
    {
        Reply reply;
        try { reply = JsonUtility.FromJson<Reply>(json); }
        catch (ArgumentException) { return; }
        if (reply == null) return;
        switch (reply.kind)
        {
            case "evaluation":
                if (!awaitingEvaluation) return;
                awaitingEvaluation = false;
                trustMission.Apply(pendingPlayerMessage, reply.events, reply.valid, reply.new_information);
                UpdateTrustUI();
                status.text = CharacterName + " отвечает…";
                WriteCommand("send", pendingPlayerMessage);
                pendingPlayerMessage = null;
                break;
            case "ready": ready = true; busy = false; status.text = characterChatMode ? "Входящий звонок · " + CharacterName : reply.text; break;
            case "opening_seeded": busy = false; break;
            case "token":
                if (answer != null)
                {
                    answer.text += reply.text; history.ScrollTo(answer);
                    if (speechEnabled.value && speech != null) speech.AppendText(reply.text);
                }
                break;
            case "done":
                busy = false; status.text = characterChatMode && callState == PhoneCallState.Speaking ? CharacterName + " говорит…" : characterChatMode
                    ? trustMission.Complete ? "Миссия выполнена. Начните новый разговор, чтобы сыграть снова." : "Можно отправить следующее сообщение"
                    : "Готово · CPU · локально";
                if (speech != null) speech.EndStream();
                break;
            case "reset": busy = false; status.text = characterChatMode ? "Входящий звонок · " + CharacterName : "История модели очищена."; break;
            case "fatal": StopWorker(); Fail(reply.text); return;
        }
        UpdateControls();
    }

    void SendMessage()
    {
        if (!ready || busy || (characterChatMode && (callState != PhoneCallState.Active || trustMission.Complete || speech == null || !speech.Ready)) || string.IsNullOrWhiteSpace(messageInput.value)) return;
        if (speech != null)
        {
            if (speechEnabled.value) speech.BeginStream();
            else speech.Cancel();
        }
        string prompt = messageInput.value.Trim();
        AddMessage("Вы", prompt);
        answer = AddMessage(CharacterName, "");
        messageInput.value = ""; busy = true;
        status.text = characterChatMode ? "Оцениваю реплику…" : "Gemma отвечает…";
        if (characterChatMode)
        {
            pendingPlayerMessage = prompt; awaitingEvaluation = true;
            WriteCommand("evaluate", prompt);
        }
        else WriteCommand("send", prompt);
        UpdateControls();
    }

    void ResetChat()
    {
        if (!ready || busy) return;
        if (speech != null) speech.Cancel();
        ResetTrust();
        PrepareIncomingCall();
        history.Clear(); answer = null; busy = true;
        status.text = "Очищаю историю…";
        WriteCommand("reset"); UpdateControls();
    }

    void WriteCommand(string command, string text = "")
    {
        try
        {
            // Mono's redirected StreamWriter can default to the Windows code page.
            // Write UTF-8 bytes explicitly so Cyrillic survives the process boundary.
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Command
            {
                command = command, text = text,
                trust = characterChatMode && (command == "send" || command == "start") ? trustMission.Trust : -1,
                state = trustMission.Stage, mission_complete = trustMission.Complete
            }) + "\n");
            worker.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
            worker.StandardInput.BaseStream.Flush();
        }
        catch (Exception error) { StopWorker(); Fail(error.Message); }
    }

    void Fail(string message) { ready = false; busy = false; status.text = "Ошибка: " + message; UpdateControls(); }
    void UpdateControls()
    {
        sendButton.SetEnabled(ready && !busy && (!characterChatMode || (callState == PhoneCallState.Active && !trustMission.Complete && speech != null && speech.Ready))); resetButton.SetEnabled(ready && !busy);
        messageInput.SetEnabled(!characterChatMode || (callState == PhoneCallState.Active && !trustMission.Complete));
        if (characterChoice != null) characterChoice.SetEnabled(!busy && callState != PhoneCallState.Connecting && callState != PhoneCallState.Speaking);
        if (retryButton != null) retryButton.style.display = !ready && !busy && callState != PhoneCallState.Ended ? DisplayStyle.Flex : DisplayStyle.None;
        loadButton.SetEnabled(!busy && !ready); modelInput.SetEnabled(!busy && !ready);
        stopButton.SetEnabled(worker != null);
        if (repeatSpeech != null) repeatSpeech.SetEnabled(!busy && answer != null && !string.IsNullOrWhiteSpace(answer.text) && speech != null && speech.Ready);
        if (voiceChoice != null) voiceChoice.SetEnabled(!busy);
        if (previewVoice != null) previewVoice.SetEnabled(!busy && speech != null && speech.Ready);
    }
    void OnSpeechStatus(string value)
    {
        speechStatus.text = value;
        if (characterChatMode && speech != null && !speech.Ready && !busy)
            status.text = "Голос персонажа: " + value;
        UpdateControls();
    }
    void StopModel()
    {
        StopWorker();
        callState = PhoneCallState.Ended; openingStarted = false;
        phoneView?.SetCallState(callState, CharacterName);
        status.text = characterChatMode ? "Звонок завершён" : "Модель выгружена. Можно загрузить снова.";
        UpdateControls();
    }
    void StopWorker()
    {
        awaitingEvaluation = false; pendingPlayerMessage = null;
        if (speech != null) speech.Cancel();
        ++session; ready = busy = false;
        if (worker == null) return;
        try { if (!worker.HasExited) worker.Kill(); }
        catch (InvalidOperationException) { }
        finally { worker.Dispose(); worker = null; }
    }
    void Update()
    {
        while (pending.TryDequeue(out var action)) action();
        if (characterChatMode) TickCall();
        phoneView?.Tick(speech != null && speech.Playing);
    }
    void PrepareIncomingCall()
    {
        if (!characterChatMode) return;
        callState = PhoneCallState.Incoming; openingStarted = false; openingContextQueued = false;
        phoneView?.SetCallState(callState, CharacterName);
        messageInput.value = "";
    }
    void AnswerIncomingCall()
    {
        if (callState == PhoneCallState.Ended)
        {
            try { ApplySelectedCharacterVoice(); LoadModel(); }
            catch (Exception error) { Fail(error.Message); }
            return;
        }
        if (callState != PhoneCallState.Incoming) return;
        callState = PhoneCallState.Connecting;
        phoneView.SetCallState(callState, CharacterName);
        status.text = "Соединяем с " + CharacterName + "…";
        StartPreparedOpening();
        UpdateControls();
    }
    void StartPreparedOpening()
    {
        if (openingStarted || speech == null) return;
        openingStarted = true; openingContextQueued = false;
        callState = PhoneCallState.Speaking;
        speechEnabled.value = true; phoneView.SetSound(true);
        history.Clear(); answer = AddMessage(CharacterName, OpeningText);
        if (preparedOpening != null) speech.PlayPrepared(preparedOpening, OpeningText);
        else speech.Speak(OpeningText);
        phoneView.SetCallState(callState, CharacterName);
        status.text = CharacterName + " говорит…";
    }
    void TickCall()
    {
        if (callState == PhoneCallState.Speaking && ready && !busy && openingStarted && !openingContextQueued)
        {
            openingContextQueued = true; busy = true;
            WriteCommand("seed_opening", OpeningText);
            UpdateControls();
        }
        if (callState == PhoneCallState.Speaking && openingStarted && openingContextQueued && ready && !busy && speech != null && speech.Ready && !speech.HasPendingSpeech)
        {
            callState = PhoneCallState.Active;
            phoneView.SetCallState(callState, CharacterName);
            status.text = "Ваш ход — ответьте " + CharacterName;
            UpdateControls(); messageInput.Focus();
        }
    }
    void ResetTrust()
    {
        trustMission.Reset(); awaitingEvaluation = false; pendingPlayerMessage = null;
        UpdateTrustUI();
    }
    void UpdateTrustUI()
    {
        if (trustBar == null) return;
        trustBar.value = trustMission.Trust;
        trustBar.title = "Доверие: " + trustMission.Trust + "%";
        phoneView?.SetTrust(trustMission.Trust, trustMission.Complete);
        trustFeedback.text = trustMission.Feedback;
        trustFeedback.style.color = trustMission.Complete ? new Color(.45f, .9f, .6f) : new Color(.65f, .8f, .9f);
        missionGoal.text = trustMission.Complete ? "Миссия выполнена — выбрана независимая проверка через банк."
            : "Цель: успокоить Нину и помочь ей самостоятельно проверить ситуацию через официальный канал банка.";
    }
    void OnDisable()
    {
        StopWorker();
        Application.runInBackground = previousRunInBackground;
        PlayerPrefs.Save();
        if (speech != null) { speech.StatusChanged -= OnSpeechStatus; speech.enabled = false; speech = null; }
        var document = GetComponent<UIDocument>();
        if (document != null) { document.enabled = false; document.panelSettings = null; }
        if (panel != null) Destroy(panel);
    }
}
