# Marathon, rewards, and mode leaderboards — October 12, 2026

Later user-requested revisions are tracked in MARATHON_REVISION_PROGRESS.md. In particular, legacy scores now migrate into Standard and Standard uses the original Steam board; the archive-only policy below is superseded.

## Implementation checklist
- [x] Hide Marathon UI on game over, and prevent recreation during end-of-run XP transfer.
- [x] Match Marathon gauge runtime width and height to the special gauge.
- [x] Carry gravity across levels with an 8% reduction, continuous accelerating ramp, and a logarithmically increasing cap (3 cells/sec initially; growth coefficient 2). Save/restore Marathon gravity and elapsed ramp time.
- [x] Refresh board presentation from CastleData when Marathon advances; clear HP-frame sprite overrides.
- [x] Shared increase/decrease slosh on special, enemy health, and reserve gauges; consistent liquid shader on special gauge.
- [x] Reward card fade/focus, separate titles/descriptions, black text shadows, matching background/button artwork, and selected flame animation (runs while paused, fits the card without covering text).
- [x] Editable MarathonRewardColors component wired to the runtime reward prefab.
- [x] Leaderboard mode dropdown, separate local scores and Steam boards, safe asynchronous mode switching. Mode tables included in Steam Cloud snapshots.
- [x] Unity compilation, behavior tests, visual verification, and final audit. Complete suite: 19/19 passed with graphics enabled; final focused UI rerun: 3/3 passed, including pointer hover/exit fading; the initial headless presentation test required a graphics device to refresh video backgrounds.

Legacy mixed scores have no mode tag. Preserve the old local/Steam tables; do not guess their mode when creating separate mode-specific tables. No live score uploads are part of validation.

## Editable locations
- `Assets/Prefabs/GameModes/MarathonRewardPanel.prefab`: MarathonRewardColors on the root controls rarity backgrounds/labels, buff/debuff title and description colors, section backgrounds, and inactive-card opacity.
- `Assets/Prefabs/GameModes/MarathonLevelGauge.prefab`: editable Marathon gauge artwork/layout.
- GameController in GameplayScene: Marathon Gravity settings control initial cap, diminishing cap growth, level-up reduction, and ramp acceleration.
- `Assets/Prefabs/SpecialGauge_UI.prefab` and GameplayScene gauge fills: MarathonLiquidSlosh exposes wave height, speed, and damping.
- `Assets/Resources/Prefabs/Panels/SteamLeaderboard_Panel.prefab` and `Assets/Prefabs/Panels/HighScore_Panel.prefab`: mode dropdowns and layout.

## Verification and remaining manual checks
- Automated: game-over hide/restart, persistent gravity and diminishing cap growth, horizontal/vertical gain/loss waves, scene gauge references/materials, castle board/border/HP-frame changes, independent score persistence, and paused reward selection/cursor behavior.
- Visually checked rendered rewards (including flame border and text), expanded leaderboard dropdown, and the full gameplay layout with matching-width left/right gauges. Previews are in `Logs/`.
- No requested implementation remains outstanding. Live Steam upload/download across all three boards still needs a Steam-enabled game session; it was unavailable in the editor test environment. New board names are `Tetrabeasts_StandardScore`, `Tetrabeasts_ChaosScore`, and `Tetrabeasts_MarathonScore`, with the existing four-week reset suffix retained. Old mixed scores remain archived under the old keys/board names.
- Gravity defaults are implemented and tested; extended playtesting can tune their difficulty curve in the Inspector.
