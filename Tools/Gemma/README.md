# Gemma test chat

Open `Assets/Scenes/GemmaChat.unity`, enter Play Mode, and click **Загрузить модель**.
Gemma 4 expects `Models/gemma-4-E2B-it.litertlm`; an absolute file path also works.
Send with Enter; Shift+Enter inserts a newline. **Новый чат** clears both the UI
and model context. **Остановить / выгрузить** kills the owned worker, including an
in-progress generation. Loading again starts a fresh conversation.

## Runtime

This is a **Windows Unity Editor / desktop prototype**, not a mobile native plugin.
Unity owns a Python subprocess and exchanges UTF-8 JSON lines over stdin/stdout.
Inference uses Google's LiteRT-LM API 0.18.0, CPU backend, four threads, 4096 context
tokens, and at most 512 output tokens per turn. Thinking is disabled for this test.
No HTTP server or API key is used. After installation and downloading the weights,
inference runs offline. The UI remains responsive during load and generation.

Replies are also spoken by the local Piper worker. See `Tools/Piper/README.md`
for voice installation, controls, verification and source/license information.

The original `Packages/com.google.gemma.cpp/Plugins/x64/gemma.dll` was already in
the project and is left untouched. gemma.cpp's `.sbs` interface cannot load
`.litertlm` files. This chat uses LiteRT-LM instead of the gemma.cpp Unity plugin.

## Reinstall locally

Use Python 3.10+ (tested here with Python 3.12 on Windows x64), with a recent pip:

```powershell
./Tools/Gemma/setup.ps1 -Python C:/path/to/python.exe
C:/path/to/python.exe Tools/Gemma/download_model.py
```

The downloader requires Python 3.11+ and saves the portable model from
`litert-community/gemma-4-E2B-it-litert-lm`. The standard
`gemma-4-E2B-it.litertlm` bundle is about 2.59 GB on disk. Text-only inference
does not initialize vision or audio backends; lower working RAM usage does not
mean the file on disk is 0.8 GB. Actual memory use and speed depend on the machine.
The download records the source revision and verifies the upstream LFS SHA-256.
Existing weights with a different hash are never overwritten automatically.

`.local/` (runtime/cache) and `Models/` (weights/source metadata) are git-ignored.
Do not commit weights or the Python environment. For distributing a player,
package a supported runtime and model location explicitly; this prototype resolves
Tools and .local relative to the parent of Application.dataPath.

## Sources

- https://developers.google.com/edge/litert-lm/python
- https://huggingface.co/litert-community/gemma-4-E2B-it-litert-lm
- https://github.com/google/gemma-unity-plugin

## Nina Petrovna persona

Edit `Tools/Gemma/Prompts/nina_petrovna.txt` to change the character instructions.
The worker reads this UTF-8 file when the model loads. After editing, unload and
reload the model. Gemma 4 receives the persona through its native system message, without a
seeded model reply. Resetting creates a new conversation with that system message.

This is a defensive bank-call scenario: Nina checks the caller, can consult her
son, never shares banking secrets or transfers to a caller's "safe account", and
can independently contact the bank via an official number. Prompt instructions
are not a guarantee that the model will always follow the role or answer correctly.
# Character chat scene

Open `Assets/Scenes/CharacterChat.unity` and enter Play Mode. Gemma 4 and the
selected character's Piper voice load automatically. The interface contains a
character selector, new-conversation button, message history, and composer.
The scene opens on an incoming-call screen with Answer and Decline. Answer plays
the configured opening text and a preloaded WAV immediately, even while Gemma
loads. No model inference or live TTS is needed for the opening. The fixed line
is added to the model context silently. Player input appears after the opening
speech finishes and model/voice resources are ready.
Decline/hangup cancels the connection and pending speech. New starts a fresh
incoming call. The opening line is not a player turn and does not change trust.
Opening text is in `characters.json`; each voice profile selects its cached audio
resource. Rebuild those WAV assets with `Tools/Piper/build_opening.py` after
changing the opening text or synthesis settings.
Completed response phrases are spoken while generation continues. Sending is
enabled once both the model and voice are ready.

`Tools/Gemma/Prompts/characters.json` registers character names, system prompt
files, and saved voice profiles. Nina Petrovna is currently the only configured
character. Character switching clears the conversation and queued speech,
applies that character's role and voice, and reuses the loaded Gemma engine.
The full tuning interface remains in `Assets/Scenes/GemmaChat.unity`.

CharacterChat includes a bank safety trust mission, starting at 50%. Each player
message is classified before the spoken reply; Unity applies fixed score rules.
See `Tools/Gemma/TRUST_MISSION.md` for reward limits, completion conditions and checks.

