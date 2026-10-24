# Main menu

Open `Assets/Scenes/MainMenu.unity` and enter Play. This is the first scene in the global scene list and the Windows Balanced Release profile. Single player loads `ExecutiveOffice`; co-op is a disabled preview. The backdrop is a separate office scene with an idle player character, office NPCs, patrol and typing. Its chat/laptop workers are removed, so the menu does not launch the AI backend.

Settings persist in PlayerPrefs under `Settings.*`: master and speech volume, mouse sensitivity, fullscreen and graphics quality. Fullscreen applies to standalone players. Speech volume multiplies the existing character voice settings.

The model is `gemma-4-E2B-it.litertlm` (2,588,147,712 bytes). The URL, expected length and SHA-256 are in `Assets/Scripts/GemmaModelFiles.cs`. In the Editor it lives in the project's `Models` folder. Standalone downloads go into `Application.persistentDataPath/Models`; an existing model in `Models` next to the executable can also be used. Boss and Gemma chat workers resolve the same location.

Missing or damaged files download automatically. `.part` files resume with HTTP Range, remain unplayable, and are checked for the LITERTLM header, exact size and SHA-256 before promotion. Pausing or losing connectivity preserves the partial file; Continue retries. HTTP error pages and bad checksums cannot unlock single player. A `.verified.json` receipt avoids hashing the entire model again while its size and modification timestamp remain unchanged. Delete that receipt to force a full checksum check.

## Integration checks

1. Run `python AgentScripts/model_download_fixture.py` from the project root (Python with `cryptography`). It serves a 4 MiB fixture on HTTPS loopback port 18766.
2. Play MainMenu, then run **Tools / Office / Validate Main Menu Downloads**.
3. Read `.local/MenuQA/Results.txt`. Stop the fixture with Ctrl+C.

The fixture's short-lived self-signed certificate is checked by its SHA-256 fingerprint, only for the explicitly injected Editor loopback test request. Production requests use normal certificate verification. No global HTTP or certificate policy is changed.

Verified scenarios: existing model, new download, partial Range resume, pause/resume, HTTP 404 lockout, invalid checksum, repair of a corrupt existing file, and a server ignoring Range. The UI/programmatic single-player gate, settings callbacks/persistence and actual MainMenu → ExecutiveOffice transition were also checked in Play mode. The real URL was checked with a small ranged GET; the existing full project model passed SHA-256 validation, so it was not downloaded again.
