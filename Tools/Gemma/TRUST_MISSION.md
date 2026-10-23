# Nina's bank safety mission

The CharacterChat scene starts each conversation at 50 trust. Unity owns score,
turns, rewarded categories, repeat limits, and victory (`CharacterTrustMission`).
Gemma uses a separate short-lived conversation on the same loaded engine to label
events. Judge JSON is validated; unknown/malformed output awards no points, and
the character still answers. Judge text is never displayed or sent to Piper.

Only one positive category is rewarded per turn:

| Event | Points | Rewards per conversation |
|---|---:|---:|
| Independent bank verification via card/app official contact | +10 | 1 |
| Seek help from a relative | +10 | 1 |
| Explain protection of codes/passwords/card data | +10 | 1 |
| Clear explanation of the situation or answer to Nina's question | +5 | 3 |
| Calm support | +3 | 2 |
| Request secrets, transfer money, or isolate from family | −20 | Every occurrence |
| Pressure/threats/rushing | −10 | Every occurrence |
| Contradiction/ignoring a question | −5 | Every occurrence |
| Irrelevant message | −3 | Every occurrence |

Negative events take precedence over positive ones. Exact normalized duplicates
cannot earn points. Similar recent player messages are detected by text similarity;
category reward caps also bound gains from paraphrases. This is not a guarantee
of perfect semantic repeat detection or perfect model classification.

Trust is capped at 90 until an independent bank check has been proposed, at least
five player turns have occurred, and a positive rewarded turn brings trust to 90.
Unity then awards completion and sets trust to 100. Politeness alone cannot win.
After victory the last spoken reply finishes and sending is disabled; New Chat
resets trust, rewards, history, and speech. Character switching also resets state.

The selected trust stage is passed to the character before generating the spoken
reply. Voice/persona safety rules remain in effect at all trust levels. Evaluation
adds latency before response generation; TTS still streams completed phrases.

Checks: `AgentScripts/CheckTrustMission.cs` covers deterministic rules;
`Tools/Gemma/check_trust_evaluator.py` runs actual model classification;
`AgentScripts/CheckTrustGame.cs` covers UI, real scoring, speech, victory, and reset.
