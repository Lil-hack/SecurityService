# Piper speech for Nina Petrovna

Chat responses are spoken as Gemma streams text. Completed phrases are queued
for synthesis while the rest of the reply is still being generated. Punctuation
is confirmed by the following token to avoid cutting ellipses or decimal numbers;
long sentences are split at word boundaries, and the final tail is flushed on done.
Piper processes phrases serially and Unity plays their audio in order, without
repeating the entire reply when generation ends. There is a short buffering and
synthesis delay, rather than speech on every individual character.
The Unity UI provides an enable toggle, repeat-last-reply button and stop button.
The voice volume slider ranges from 0 to 100 percent and changes the AudioSource
gain immediately, including during playback. Its value is saved between sessions.
The **Телефон** toggle applies live high-pass (300 Hz) and low-pass (3400 Hz)
filters to speech. It is enabled initially and saved between sessions. Bypass
restores the full voice bandwidth; volume and synthesis speed are unchanged.
The telephone strength slider ranges from 0 (bypass) through 50 (300–3400 Hz)
to 100 (650–2200 Hz). The pitch slider ranges from −12 to +7 semitones using
AudioSource.pitch. Piper length_scale compensates this playback rate for newly
synthesized phrases, so the speech rate control remains separate. Both settings apply immediately,
survive voice switching, and are saved between sessions.
Character sliders provide speech rate (65–130%, initially 91%), added pauses
(0–500 ms, initially 150 ms), subtle pitch tremble (initially 20%), gentle volume
variation (initially 20%), and mild distortion/rasp (initially 12%). All persist.
Rate and pauses affect newly synthesized phrases; already buffered audio keeps
its timing. Pauses follow sentences and existing interjections, without inventing
words. Tremble, volume variation, and rasp apply live and can each be set to zero.
These effects stylize the selected voice rather than reproduce a specific age.
CharacterChat uses the cartoon preset in `Tools/Gemma/Prompts/nina_voice_cartoon.json`:
pitch +3.5 semitones, 110% rate, 80 ms pauses, and telephone strength 25%.
The grandmother preset remains in `nina_voice.json` and is inactive.
New questions, reset, model unloading and leaving Play Mode stop the old audio.
Stale chunks are discarded by request id, and Unity releases generated AudioClips.
Speech failures are shown separately and do not erase the chat response.
If AudioSource is playing but nothing is audible, disable **Mute Audio** in the
Unity Game window and check the system output volume. The chat does not override
the editor's mute setting on every frame.
While the chat is active it runs in the background, so switching away from the
Game window does not freeze model replies or speech; the previous setting is
restored when the chat is disabled.

The persistent local worker loads the selected voice once (Irina initially uses
`Models/Piper/ru_RU-irina-medium.onnx`),
uses the CPU, and sends PCM16 sentence chunks over stdio to Unity AudioSource.
A listener is created only if the scene has none. No audio files or external
services are needed during normal chat. This uses the same Windows desktop
Python environment as Gemma; mobile/player packaging requires separate work.

Voice: Russian Irina Medium, mono 22,050 Hz, 63,201,294-byte ONNX plus JSON config.
Speaking speed is slightly reduced (`length_scale=1.1`). This is a stock synthetic
female voice; the language model's mood and age are not a voice-cloning model.

## Installed Mantella RU voices

The supplied `Mantella-RU-Piper-1.0 (1).zip` is extracted under
`Models/Piper/Mantella-RU-Piper-1.0`, preserving its README and SKSE folder tree.
This project uses only the ONNX/config pairs; Skyrim/SKSE/Mantella itself is not
required. There are 10 female and 12 male models (~1.4 GB total), plus the original
Irina voice. The archive's README describes a noncommercial Skyrim-derived pack
and the source datasets/rights; preserve it when using these models.

The chat scans Models/Piper recursively for ONNX files with matching .onnx.json.
Select **Голос**, wait for it to load, then click **Проба голоса** to audition
without a Gemma request. Selection persists between sessions. Switching stops
old audio and loads only the selected voice, reusing the same AudioSource and
volume. It does not keep all 23 models in RAM. Switching during text generation
is disabled; repeat the last reply to hear it in the newly selected voice.

`check_pack.py` checks actual synthesis for all 22 models, saves WAV previews under
`.local/piper-mantella-previews` and records compatibility-check.json beside the pack.

## Install / verify

Install `Tools/Gemma/requirements.txt` with the existing project setup script,
then run:

```powershell
.local/gemma-runtime/Scripts/python.exe Tools/Piper/download_voice.py
.local/gemma-runtime/Scripts/python.exe Tools/Piper/check_voice.py
```

The downloader pins the source revision and verifies model size and SHA-256.
Models/Piper is ignored by Git. The check performs real synthesis, verifies
non-silent PCM and writes `.local/piper-check.wav` for listening.

Runtime Piper TTS 1.8.0 is GPL-3.0; the upstream Irina MODEL_CARD lists its dataset
license as Unknown. Keep that uncertainty in mind before distributing the voice
with a commercial build; attribution/source details are in VOICE_MODEL_CARD.md
and Models/Piper/MODEL_CARD. No licensing assumptions are made by this prototype.

Sources:
- https://github.com/OHF-Voice/piper1-gpl/blob/main/docs/API_PYTHON.md
- https://huggingface.co/rhasspy/piper-voices/tree/main/ru/ru_RU/irina/medium
