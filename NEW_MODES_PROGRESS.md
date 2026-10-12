# Boulders, Chaos, and Marathon — completed October 9, 2026

## Feature audit
- [x] **Boulders-Away:** registered modifier and supplied icon; random 5–10 second spawn interval only while active; timer stops at the shared three-boulder capacity.
- [x] **Shared boulder physics:** spawn above the board, gravity, rotating sprite, 3–5 impacts, side/bottom and mutual bounces, 20 base damage per distinct living-monster collision, dead-unit pass-through, stone-only obstacle collisions with one stone hit per contact. Small pixel bursts and monster portrait shake on impact. Boulders remain above tile visuals and fall downward on screen even with Flip The Board.
- [x] **Rocks for Everyone:** appended boss ability enum and registered it in existing boss option pools. Its three rocks are spread across the top. Normal boss selection waits for room for the complete volley; explicit requests queue until room exists.
- [x] **Chaos:** uses the same 14 Standard castles and existing victory/defeat flow. Modifier selection runs on levels 1, 3, 5, 7, 9, 11, and 13. Previously selected modifiers remain active together on subsequent levels, including boss levels. Already-active modifier kinds are excluded from later rolls. Independent effect timers and combined damage/control hooks are retained.
- [x] **Chaos experience:** adds 0.20 to the existing end-of-run XP conversion fraction, after difficulty and final-win bonuses, subject to the existing 100% cap. Round XP calculations otherwise remain Standard's.
- [x] **Marathon gauge:** editable purple liquid gauge mirrors the ultimate gauge on the board's left. Starts empty; applied castle damage fills it. Threshold starts at 500 and grows by 1.18 per level, with continued linear growth after level 60 to prevent exponential overflow. Excess damage carries forward, including multiple level-ups from one large hit.
- [x] **Marathon choices:** pauses gameplay and presents three distinct random buff/debuff pairs; both members of each pair have the same rarity. Select a pair and confirm to apply both through the existing run-modifier path, close the panel, and resume. Matching rarity rolls favor common choices while allowing all available rarities.
- [x] **Marathon bosses:** eight existing bosses in campaign order, including the final survival boss, appear at levels 3, 6, 9, 12, 15, 18, 21, and 24. Displayed boss health and critical sprites follow the inverse gauge percentage. The final boss remains from level 24 onward with infinite health and persistent ability timers; level thresholds and rewards continue normally.
- [x] **Marathon defeat/experience:** existing defeat and end-of-run flow retained. Each completed damage level grants Standard's round XP calculation, including difficulty and party bonuses, then the normal end-of-run XP conversion applies.
- [x] **Mode selection/unlocks:** the existing New Game button in TitleScene now opens the editable mode panel. Close button uses the existing menu button artwork. Chaos and Marathon unlock on reaching Standard's final level; legacy best completed-level records, final-win progress, difficulty unlocks, and existing Standard checkpoint progress are recognized retroactively.
- [x] **Saved runs:** save version 2 records mode, retained Chaos modifiers, and Marathon damage. Existing version 1 saves default to Standard. Resume does not clamp Marathon to the Standard campaign length.
- [x] **Earthquake/bomb VFX:** impact shake added to their shared board VFX entry point, including commander-triggered uses; additive offsets are restored afterward.
- [x] **Cleanup:** boulders clear on round/run termination, modifiers reset on a fresh run, and Marathon UI/pause state is cleaned up when the controller is destroyed.

## Editable assets used at runtime
- `Assets/Prefabs/GameModes/GameModeMenu.prefab`
- `Assets/Prefabs/GameModes/MarathonLevelGauge.prefab`
- `Assets/Prefabs/GameModes/MarathonRewardPanel.prefab`
- `Assets/Prefabs/GameModes/MarathonPurpleLiquid.mat`
- `Assets/Resources/GameModeAssets.asset` — prefab references, supplied boulder sprite, unlock level count, and Marathon threshold tuning.

The liquid shader retains the castle health bar's bubble/noise shading and textures, with a Canvas-compatible vertex transform. The original castle shader/material are unchanged. The prefab authoring command only creates missing assets, preserving future prefab edits.

## Verification completed
- Unity runtime and Editor script compilation succeeded.
- Full Edit Mode regression suite: **10 passed, 0 failed** (`Logs/ModeRegressionTests.xml`). This includes the existing modifier, Hallucination animation, Mao/Quixel timing, and 15-buff achievement tests.
- Final focused mode suite after the audit fixes: **4 passed, 0 failed** (`Logs/BoulderAndModeFinalTests.xml`).
- Tests cover boulder target filtering, collision damage/debounce, capacity, pause, mutual bounces, destruction, boss volley spacing, Chaos effect composition/reset/reapplication, XP bonus, matching-rarity rewards, finite growing thresholds, Marathon carryover/pause/confirmation gating, boss ordering/final persistence, and save serialization compatibility.
- Rendered and inspected the mode menu, reward panel, purple liquid gauge, and gauge placement in the actual gameplay scene. Previews: `Logs/GameModeMenu.png`, `Logs/MarathonRewardPanel.png`, `Logs/MarathonLevelGauge.png`, `Logs/MarathonGameplayLayout.png`.
- `git diff --check` passed.

## Remaining playtesting
No requested feature is intentionally left unimplemented. A full player-driven campaign/Marathon playthrough and balance tuning are still recommended; automated tests and UI renders do not establish long-run gameplay balance or cover every possible Chaos modifier combination. Tests avoid overwriting the user's real saved run or permanent monster progression.

User changes to `Tetrabeasts_GameIdea.txt` are preserved. No commits were made.

## UI polish — completed October 11, 2026
- [x] Runtime reward binding: separate buff/debuff icons and blue/red text, rarity colors, selection outline, and “Choose a Modifier” title.
- [x] Marathon reward pause participates in cursor restoration and keeps the cursor unlocked while choosing.
- [x] Mode tooltip behavior supports locked buttons and includes the requested unlock note.
- [x] Gauge surface wave effect responds to damage gains and settles; gauge supports a horizontal placement offset and centered sideways damage text.
- [x] Saved reward prefab: separate background icons, blue buff/red debuff text, white/green/blue/purple/gold rarity colors, and a yellow selection outline that preserves the rarity color.
- [x] Saved mode prefab: green mode buttons, red close button, yellow hover/focus, darker pressed colors, grey disabled states, mode-name-only labels, and right-side descriptions/unlock notes. Existing hover/click clips are assigned to every new button; locked modes still play informational hover feedback.
- [x] Saved gauge prefab: SpecialGauge_UI border and glass sprites, mirrored border, opaque dark background, centered sideways damage text, no duplicate level label. Narrowed the gauge to fit between the left controls and board, and moved it 28 canvas units right of its original mirrored position.
- [x] Unity compilation and final visual previews completed. Inspected `Logs/GameModeLockedTooltip.png`, `Logs/MarathonRewardPanel.png`, `Logs/MarathonGaugeSlosh.png`, and `Logs/MarathonGameplayLayout.png`.
- [x] Focused UI suite: 3 passed, 0 failed (`Logs/MarathonUIFocusedTests.xml`). Tests cover paused pointer selection/confirmation, icon clearing on reuse, rarity preservation, locked-mode tooltips/click blocking, cursor focus/deferred-callback guards, actual wave geometry/bounds, existing artwork, and hover sound assignments.
- [x] Final full regression suite: **13 passed, 0 failed** (`Logs/MarathonUIRegressionTests.xml`). Changed assets/scripts pass `git diff --check`; incidental TimeManager serialization was restored. User edits to `Tetrabeasts_GameIdea.txt` remain untouched.

All requested October 11 UI changes are implemented. The three existing prefabs under `Assets/Prefabs/GameModes` remain the runtime assets to edit. No commits were made.
