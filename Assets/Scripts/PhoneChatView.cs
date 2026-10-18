using System;
using UnityEngine;
using UnityEngine.UIElements;

public enum PhoneCallState { Incoming, Connecting, Speaking, Active, Ended }

/// <summary>Portrait telephone UI; all controls are connected to the existing chat controller.</summary>
public sealed class PhoneChatView
{
    readonly Label trustState;
    readonly Label trustPercent;
    readonly PhoneSignal signal;
    readonly VisualElement portrait;
    readonly Button sound;
    readonly VisualElement callPanel, historyPanel, composerPanel, footerPanel;
    readonly Label callCaption, callerName, callHint;
    readonly Button acceptCall, declineCall;

    public PhoneChatView(VisualElement root, PopupField<string> character, ProgressBar trust,
        Label feedback, Label status, ScrollView history, TextField input, Button send,
        Button reset, Button retry, Action hangUp, Action toggleSound, Action answerCall)
    {
        root.Clear();
        root.AddToClassList("phone-stage");
        root.style.backgroundColor = new Color(.025f, .045f, .05f);
        root.style.paddingLeft = root.style.paddingRight = 12;
        root.style.paddingTop = root.style.paddingBottom = 10;
        root.style.alignItems = Align.Center;
        root.style.justifyContent = Justify.Center;
        root.styleSheets.Add(Resources.Load<StyleSheet>("PhoneChat"));

        var window = new VisualElement { name = "phoneWindow" };
        window.AddToClassList("phone-window");
        root.Add(window);
        var chrome = Row("phone-chrome");
        chrome.Add(new Label("☎  Телефон") { name = "phoneTitle" });
        chrome.Add(new Label("ЛОКАЛЬНАЯ ЛИНИЯ") { name = "lineCaption" });
        var close = new Button(hangUp) { text = "×", tooltip = "Завершить разговор" };
        close.AddToClassList("phone-close"); chrome.Add(close); window.Add(chrome);

        var body = new VisualElement(); body.AddToClassList("phone-body"); window.Add(body);
        var trustCard = new VisualElement(); trustCard.AddToClassList("phone-trust-card");
        trustCard.tooltip = "Цель: успокоить Нину и помочь ей самостоятельно проверить звонок через официальный канал банка.";
        var trustRow = Row("phone-trust-heading");
        trustRow.Add(new Label("ДОВЕРИЕ СОБЕСЕДНИКА"));
        trustState = new Label(); trustState.AddToClassList("phone-trust-state"); trustRow.Add(trustState);
        trustCard.Add(trustRow);
        var trustLine = Row("phone-trust-line");
        trust.style.height = 7; trust.style.marginTop = 0; trust.style.flexGrow = 1;
        trustLine.Add(trust);
        trustPercent = new Label(); trustPercent.AddToClassList("phone-trust-percent"); trustLine.Add(trustPercent);
        trustCard.Add(trustLine); body.Add(trustCard);

        portrait = new VisualElement { name = "phonePortrait" };
        portrait.AddToClassList("phone-portrait");
        character.label = "";
        character.style.marginTop = 0; character.style.minHeight = 26;
        character.style.flexGrow = 0; character.style.alignSelf = Align.Stretch;
        character.AddToClassList("phone-character");
        portrait.Add(character);
        var imageArea = new VisualElement(); imageArea.AddToClassList("phone-image-area");
        imageArea.generateVisualContent += context =>
        {
            var painter = context.painter2D;
            painter.lineWidth = 1;
            painter.strokeColor = new Color(.7f, .95f, .9f, .10f);
            float w = imageArea.contentRect.width, h = imageArea.contentRect.height;
            painter.BeginPath();
            for (float x = 0; x < w; x += 28) { painter.MoveTo(new Vector2(x, 0)); painter.LineTo(new Vector2(x, h)); }
            for (float y = 0; y < h; y += 28) { painter.MoveTo(new Vector2(0, y)); painter.LineTo(new Vector2(w, y)); }
            painter.Stroke();
        };
        var avatar = new Image
        {
            image = Resources.Load<Texture2D>("Characters/NinaPortraitSelected"),
            scaleMode = ScaleMode.ScaleToFit,
            uv = new Rect(0f, 0f, 1f, 1f)
        };
        avatar.AddToClassList("phone-avatar"); imageArea.Add(avatar); portrait.Add(imageArea);
        signal = new PhoneSignal(); portrait.Add(signal); body.Add(portrait);

        var information = new VisualElement(); information.AddToClassList("phone-information");
        status.style.marginTop = status.style.marginBottom = 0;
        status.AddToClassList("phone-status"); information.Add(status);
        feedback.style.marginTop = 2; feedback.AddToClassList("phone-feedback"); information.Add(feedback);
        body.Add(information); body.Add(retry);

        callPanel = new VisualElement { name = "incomingCall" }; callPanel.AddToClassList("phone-incoming");
        callCaption = new Label(); callCaption.AddToClassList("phone-call-caption"); callPanel.Add(callCaption);
        callerName = new Label(); callerName.AddToClassList("phone-caller"); callPanel.Add(callerName);
        callHint = new Label(); callHint.AddToClassList("phone-call-hint"); callPanel.Add(callHint);
        var actions = Row("phone-call-actions");
        acceptCall = new Button(answerCall) { text = "Ответить", name = "answerCall" };
        acceptCall.AddToClassList("phone-answer"); actions.Add(acceptCall);
        declineCall = new Button(hangUp) { text = "Сбросить", name = "declineCall" };
        declineCall.AddToClassList("phone-decline"); actions.Add(declineCall);
        callPanel.Add(actions); body.Add(callPanel);
        historyPanel = history;
        history.AddToClassList("phone-history");
        history.style.backgroundColor = new Color(.015f, .028f, .03f);
        history.style.paddingLeft = history.style.paddingRight = 8;
        history.style.paddingTop = history.style.paddingBottom = 8;
        history.style.minHeight = 64; body.Add(history);
        history.verticalScroller.style.width = 8;
        history.verticalScroller.lowButton.style.display = DisplayStyle.None;
        history.verticalScroller.highButton.style.display = DisplayStyle.None;
        var thumb = history.verticalScroller.Q<VisualElement>("unity-dragger");
        if (thumb != null)
        {
            thumb.style.backgroundColor = new Color(.31f, .48f, .46f);
            thumb.style.width = 6;
            thumb.style.borderTopLeftRadius = thumb.style.borderTopRightRadius = thumb.style.borderBottomLeftRadius = thumb.style.borderBottomRightRadius = 3;
        }
        var composer = Row("phone-composer");
        composerPanel = composer;
        input.style.height = 44; input.style.marginTop = 0; input.style.fontSize = 14;
        input.style.flexGrow = 1; input.AddToClassList("phone-input");
        input.tooltip = "Введите сообщение. Enter — отправить, Shift + Enter — новая строка.";
        composer.Add(input);
        send.text = "→"; send.tooltip = "Отправить сообщение";
        send.AddToClassList("phone-send"); composer.Add(send); body.Add(composer);

        var footer = Row("phone-footer");
        footerPanel = footer;
        reset.text = "Новый"; reset.tooltip = "Начать новый разговор и сбросить доверие";
        reset.AddToClassList("phone-new"); footer.Add(reset);
        var end = new Button(hangUp) { text = "Завершить" }; end.AddToClassList("phone-hangup"); footer.Add(end);
        sound = new Button(toggleSound) { text = "Звук", tooltip = "Включить или выключить озвучку" };
        sound.AddToClassList("phone-sound"); footer.Add(sound); body.Add(footer);
        window.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            // Match the actual image aspect instead of squeezing it into a short banner.
            var texture = avatar.image;
            float aspect = texture != null && texture.height > 0 ? (float)texture.width / texture.height : 4f / 3f;
            float idealHeight = (evt.newRect.width - 18f) / aspect + 44f;
            float availableHeight = Mathf.Min(evt.newRect.height * .45f, evt.newRect.height - 320f);
            portrait.style.height = Mathf.Clamp(idealHeight, 100f, Mathf.Max(100f, availableHeight));
        });
    }

    static VisualElement Row(string className)
    {
        var element = new VisualElement(); element.AddToClassList(className);
        element.style.flexDirection = FlexDirection.Row;
        return element;
    }

    public void SetTrust(int trust, bool complete)
    {
        trustPercent.text = trust + "%";
        trustState.text = complete ? "ДОВЕРЯЕТ" : trust < 25 ? "ТРЕВОГА" : trust < 50 ? "НЕ ДОВЕРЯЕТ" : trust < 75 ? "СОМНЕВАЕТСЯ" : "СЛУШАЕТ";
    }
    public void SetSound(bool enabled)
    {
        sound.text = enabled ? "Звук: вкл" : "Звук: выкл";
        sound.EnableInClassList("phone-muted", !enabled);
    }
    public void SetCallState(PhoneCallState state, string name)
    {
        bool incoming = state == PhoneCallState.Incoming || state == PhoneCallState.Connecting || state == PhoneCallState.Ended;
        callPanel.style.display = incoming ? DisplayStyle.Flex : DisplayStyle.None;
        historyPanel.style.display = incoming ? DisplayStyle.None : DisplayStyle.Flex;
        composerPanel.style.display = state == PhoneCallState.Active ? DisplayStyle.Flex : DisplayStyle.None;
        footerPanel.style.display = incoming ? DisplayStyle.None : DisplayStyle.Flex;
        callerName.text = name;
        callCaption.text = state == PhoneCallState.Ended ? "ЗВОНОК ЗАВЕРШЁН" : state == PhoneCallState.Connecting ? "СОЕДИНЕНИЕ…" : "ВХОДЯЩИЙ ЗВОНОК";
        callHint.text = state == PhoneCallState.Ended ? "Начните новый звонок, когда будете готовы."
            : state == PhoneCallState.Connecting ? "Подключаем персонажа и голос…" : name + " ждёт ответа на звонок.";
        acceptCall.text = state == PhoneCallState.Ended ? "Новый звонок" : "Ответить";
        acceptCall.SetEnabled(state != PhoneCallState.Connecting);
        declineCall.style.display = state == PhoneCallState.Ended ? DisplayStyle.None : DisplayStyle.Flex;
    }
    public void Tick(bool speaking)
    {
        signal.Speaking = speaking;
        signal.MarkDirtyRepaint();
    }

    sealed class PhoneSignal : VisualElement
    {
        public bool Speaking;
        public PhoneSignal()
        {
            AddToClassList("phone-signal");
            generateVisualContent += context =>
            {
                var painter = context.painter2D;
                painter.strokeColor = Speaking ? new Color(.27f, .92f, .54f) : new Color(.19f, .42f, .48f);
                painter.lineWidth = 2;
                float w = contentRect.width, mid = contentRect.height / 2;
                painter.BeginPath();
                for (int i = 0; i < 38; i++)
                {
                    float x = (i + .5f) * w / 38;
                    float height = Speaking ? 2 + 5 * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8 + i * .8f)) : 1;
                    painter.MoveTo(new Vector2(x, mid - height)); painter.LineTo(new Vector2(x, mid + height));
                }
                painter.Stroke();
            };
        }
    }
}
