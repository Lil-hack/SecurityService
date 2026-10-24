# Собеседование с директором в ExecutiveOffice

На NPC с OfficeBossNpc установлен OfficeBossInterview. Подойдите на расстояние до 3,2 м и нажмите E или кнопку общения. Прямая видимость обязательна. Во время беседы движение и боевые действия игрока заблокированы; Esc всегда возвращает управление. Босс поворачивается к игроку, его обычное расписание временно приостанавливается.

Приветствие и ответы озвучивает существующий GemmaSpeech/Piper с malecommoner.onnx (мужской русский голос, без телефонного фильтра). Gemma запускается локально через существующий worker.py. Command=interview вызывает hiring_evaluator.py с настройками роли и реплик из Inspector; Prompts/office_boss.txt остаётся для старых CLI-вызовов без этих настроек. Банковский сценарий не изменён. Модель оценивает конкретность и пользу ответа. Некорректный JSON не приводит к приёму.

При accepted=true Unity произносит «Вы приняты! Добро пожаловать в команду», ждёт окончания речи, закрывает чат и возвращает управление. Результат сохраняется в PlayerPrefs Office.BossHired. Неубедительный ответ оставляет разговор открытым. При сбое модели доступны повтор подключения и выход. Процессы освобождаются при закрытии/выключении компонента.

Проверки:
- Tools/Gemma/check_office_interview.py — настоящая Gemma: приветствие, полезный навык, попытка подменить решение.
- Tools/Office/Test Boss Gemma Interview — Unity Play: блокировка, разворот, озвучка, реальная оценка, завершение и возврат управления. Использует и затем восстанавливает тестовое расположение игрока и сохранённый результат найма.
- Tools/Office/Enable Boss Gemma Interview — установка компонента в сцену без пересборки офиса.

Зависимости: существующие Models/gemma-4-E2B-it.litertlm, .local/gemma-runtime и Models/Piper/Mantella-RU-Piper-1.0. Пути настраиваются в компоненте OfficeBossInterview.


## JEV → Gemma

Оценка собеседования сначала отправляется в TypeSafe `jev-latest` (Noul `hire`).
Контракт: https://docs.typesafe.ai/api/system-one ; смысл вероятностей: https://docs.typesafe.ai/primitives/noul .
Вероятность >= 0.7 означает приём, <= 0.3 — просьбу уточнить ответ. Промежуточные значения передаются Gemma. Это начальные пороги для игрового сценария, а не гарантия правильности уверенного решения.
Ошибки HTTP/сети, таймаут 10 с, не-JSON, неверная схема, bool вместо числа, NaN/Infinity и значения вне диапазона тоже ведут к Gemma. При отказе обоих оценщиков приём не подтверждается; игрок может повторить ответ.
JEV возвращает решение, текст реплики формируется кодом; при резервном пути Gemma оценивает и формулирует уточнение. Голос Piper и управление беседой не изменены.
Ключ берётся из TYPESAFE_API_KEY либо .local/typesafe.json (поле api_key). Локальный файл исключён из Git, не попадает в Unity-сцену/ассеты. В API отправляются только ответ и до четырёх предыдущих реплик собеседования.
Проверка маршрутизации: .local/gemma-runtime/Scripts/python.exe Tools/Gemma/test_jev_evaluator.py .
Навык проекта установлен: .agents/skills/typesafe-ai/SKILL.md .

## Gemma response generation
JEV owns the hiring verdict; Gemma generates a contextual Russian reply using that fixed decision. A mismatched verdict from Gemma is rejected. Unity displays and speaks the generated text, including acceptance replies. A generation failure preserves the valid JEV verdict and uses a fallback line. If JEV fails, Gemma still performs both judgement and response generation. The initial greeting remains predefined.


## Preloaded voice and streamed responses
Piper is initialized at scene Start by OfficeBossInterview.PrepareVoice and stays resident after chat closes. Scene/component shutdown still frees the process. Its startup cost is moved to the time before approaching the boss.
After the fixed JEV verdict, generate_reply uses a compact speech-only Gemma prompt. Complete sentences are emitted as interview_sentence JSON-lines immediately, even inside redirected diagnostic output. Unity feeds them to GemmaSpeech.BeginStream/AppendText/EndStream. A single chat bubble expands at playback callbacks; the final interview_result owns hiring persistence and conversation completion. The Gemma fallback evaluator remains unchanged in authority and runs only if JEV is invalid/unavailable/uncertain.
Full rerun and comparison: OfficePreview/BossDialogueStreamingComparison.md. Voice/text first-response latency was reduced in the measured run; gaps between streamed sentences remain possible if Gemma produces the next sentence too late.
# Editable NPC prompts

Select `boss` in ExecutiveOffice, then `OfficeBossInterview` in the Inspector.
The multiline public fields `BossRolePrompt`, `ReplyInstructions`,
`AcceptanceInstructions`, `RejectionInstructions` and `GemmaEvaluationRules`
configure Gemma's role, response style and fallback evaluation. `Greeting`,
`HiredGreeting`, `AcceptedFallbackReply` and `RejectedFallbackReply` configure
the visible greetings and safe fallback lines.

The Unity component sends the current values as `prompts` on every `interview`
JSON command. Changes in Play Mode take effect on the next submitted message;
greetings apply when opening the next conversation. Edit outside Play Mode and
save the scene to retain the settings. Unity starts the worker with `--interview`,
so it does not load the legacy role file for this workflow.

The Inspector section «Оценка ответа (Jev)» exposes `JevQuestion`,
`JevInstructions`, `JevAcceptCriteria` and `JevRejectCriteria`. These strings travel
with the same per-message prompts object and populate Jev request state,
instructions and true/false criteria. Defaults match the original request exactly.
Jev remains the primary evaluator with its existing uncertainty gates; the
editable Gemma evaluation rules are used only on fallback. The hiring verdict
and JSON/text transport requirements remain controlled by code. Legacy CLI callers
without prompt settings can still use `Prompts/office_boss.txt`.
