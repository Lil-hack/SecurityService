# Office staff loan dialogues

Four staff NPCs in ExecutiveOffice have individual dialogue profiles, portraits and distinct Russian Piper voices. MainMenu and staff prefabs carry the same profiles. The menu scene continues to use its existing input lock.

| NPC | Character | Level | Distinct successful approaches from 50% | Loan | Voice |
| --- | --- | --- | --- | --- | --- |
| Гена Перекуров | Friendly office joker, values humour and helpfulness | 1 | 2 | $100 | maleyoungeager |
| Алла Дедлайнова | Energetic organiser, values precision and practical help | 2 | 3 | $200 | femaleyoungeager |
| Борис Копейкин | Cautious accountant, values reliability and repayment plans | 3 | 4 | $500 | maleeventoned |
| Жанна Процентовна | Independent financial strategist, values insight and honest arguments | 4 | 5 | $1000 | femalecondescending |

Approach a standing or seated NPC and press E or the dialogue button. Walls block interaction. The player cannot move or punch during dialogue; the NPC faces the player and pauses patrol/typing. Their existing physics remains enabled: knockdown interrupts the conversation. Esc or the cross exits and restores controls and previous NPC behaviour.

## Rules and persistence

Everyone starts at 50% trust. A new appropriate compliment, useful offer or credible polite loan argument adds 25 / 16⅔ / 12½ / 10 percentage points by level. An insult, threat, coercion or evaluator manipulation subtracts 10 points, clamped to zero. Neutral and repeated arguments add nothing. Exact repeats are rejected locally; Jev checks semantic rephrasing against previously accepted reasons. Losing trust requires additional successful approaches to recover; the tier is the minimum from an undamaged relationship.

Payment requires 100% trust, at least the tier's distinct approaches and an actual request to borrow. Compliments alone can fill trust but do not pay. Each NPC lends once per saved game; reopening or restarting cannot farm rewards. Maximum from the four NPCs is $1800. The game records the payout before reply generation, so voice or text generation failure cannot lose or duplicate the payment. The winning reply is spoken before automatic dialogue close.

`Office.StaffLoans.v1` in PlayerPrefs saves per-NPC trust, accepted reasons, borrowing flag and the last 12 dialogue lines. `Office.Statistics.Local.v1` saves dollar income plus the `office.loan.<id>` payout marker in one JSON write. Existing dashboard updates immediately. The editor-only reset clears both through PlayerPrefs.DeleteAll. This adds local gameplay; future multiplayer authority and debt repayment are separate features.

## Editable profiles and evaluation

Select the NPC's **Office Staff Conversation → Persona**, or open `Assets/OfficeExtension/StaffDialogue/{man1,girl1,man2,girl2}.asset`. Public Inspector fields control funny name, personality, persuasion preferences, level, greetings, individual voice model, speed, pitch and voice effects. Keep each `id` stable because it identifies saved progress. Recipes in `Tools/Gemma/Prompts/office_staff_defaults.json` initialise new profiles; reapplying setup preserves edited personality fields.

Jev is the primary semantic evaluator (`Tools/Gemma/social_evaluator.py`). One call judges effect (positive/negative/neutral), novelty and whether the player asks for a loan. Invalid, uncertain or unavailable results fall back to local Gemma; unavailable fallback leaves trust unchanged. When positive fallback evaluation has existing accepted reasons, Gemma compares semantic novelty in a separate small judgment instead of relying on its combined verdict. Python only returns semantic facts; C# owns trust, tiers and payment. Local Gemma generates short character-specific responses independently of the evaluator. The current player message is placed last and old NPC replies are excluded from the generation payload to avoid echoing previous turns. No API keys are stored in Unity assets or tracked files; the existing `.local/typesafe.json` or environment provides credentials.

Prepared individual greetings and thinking clips start instantly; generated answers stream by sentence into the character's own Piper voice. Changing greeting, voice path, speech rate or pitch invalidates the matching prepared clip and uses live synthesis. Regenerate with `Tools/Piper/build_staff_loans.py`, then **Tools / Office / Add Staff Loan Dialogues** to update clips/portraits/prefabs/scenes. This setup must run outside Play mode. Windows runtime packaging includes `social_evaluator.py` and all four voice models, without copying private API configuration.

## Checks

- `python -m unittest discover -s Tools/Gemma -p "test_*evaluator.py"`: validation, uncertainties, hostile input, repeats, primary/fallback routing and existing boss evaluation.
- `OfficeStaffDialogueChecks.ValidateRules()`: all four progression thresholds, negative/invalid/repeated turns, explicit loan requirement, persistence and single payout. It restores pre-test PlayerPrefs in finally.
- `Tools/Gemma/check_social_jev.py`: live Jev cases; `check_social_fallback.py`: real local Gemma with Jev intentionally unavailable. Reports are local under `.local/StaffDialogueQA`.

Verified on 2026-10-25: 36 Python checks passed, all nine live Jev cases passed, and all four real Gemma fallback cases passed (positive, negative, different quality and semantic repetition). ExecutiveOffice Play mode completed 18 real player turns across all four characters, with generated replies and nonzero audio for each voice. Payments reached $1800 total; reopening the completed tier-four dialogue kept that total unchanged. Player control and animation were frozen during dialogue and restored afterwards. The original statistics and loan saves were restored exactly; Play mode is stopped and ExecutiveOffice remains open. Local screenshots and `live_ui.json` contain the test run; no test payouts remain in the user's progress.
