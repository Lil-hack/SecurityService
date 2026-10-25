# Office team dashboard

The `Office team dashboard` object is mounted on the north teal studio wall at `(18.65, 2.65, 7.40)`, above the collaboration table. Both ExecutiveOffice and MainMenu contain the display. Recreate it with **Tools / Office / Add Team Dashboard**. Shared materials, rounded housing mesh, card sprite and four rendered character portraits are in `Assets/ExecutiveOffice/Dashboard`.

The world-space Canvas uses ordinary depth testing, so its content is hidden by walls and furniture. It has no input raycaster and does not intercept gameplay controls. It refreshes twice a second and immediately after a statistics event. Names are plain text; player markup cannot alter the layout. The four visible leaderboard slots represent actual participants and empty future coop slots, not simulated opponents.

## Data

- Profile: existing `Office.PlayerName` and `Office.CharacterPrefab` preferences. Hiring status: existing `Office.BossHired`. Changes made through the laptop/boss are picked up automatically.
- Statistics: `Office.Statistics.Local.v1` stores lifetime income in dollars, completed objectives, up to 30 local-calendar daily income buckets and idempotent reward IDs. `Office.Statistics.PlayerId` identifies the local participant. Existing saves without reward IDs remain compatible. The existing editor reset (`PlayerPrefs.DeleteAll`) clears these too.
- `OfficeTeamProgress.RecordRevenue(amount)` adds nonnegative income. `RecordCompletedJob(reward)` adds an objective and its reward. `TryRecordReward(id, amount)` atomically saves both the reward ID and income/objective, refusing duplicate IDs. Staff loan dialogues use this method to credit $100/$200/$500/$1000 exactly once per NPC. They save immediately and notify displays. Revenue is cumulative received money, not a spendable balance or loan repayment system.
- `OfficeTeamProgress.SetRemotePlayers(players)` is the future networking boundary. Supply remote `OfficeTeamMember` snapshots on joins, updates and disconnects. Local duplicates, invalid IDs and negative statistics are ignored; repeated remote IDs replace their previous snapshot. Remote data is runtime-only and never replaces the local save. This does not implement multiplayer transport.
- Ranking is descending earned revenue, then completed jobs, then stable player ID. With no income or jobs the leader card waits for the first result. Equal leading results show a shared-place hint. The team total includes all supplied participants; the screen shows the top four.

## Verification

Play checks covered the saved name/character, a persisted 1,500-ruble test income and one job, today's earnings, a three-person sorted roster, duplicate/local participant filtering, a 5,000-ruble team total and live leader display. Negative income was rejected and malformed statistics safely fell back to zero. The original statistics were restored after testing. Matched renders behind the wall verified occlusion. Screenshots are in `.local/DashboardQA`; console and shader checks passed.
