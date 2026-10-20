# Settings and modifier polish — 2026-10-18

## Scope and decisions
- User confirmed recurring floor hazards belong to **Watch Your Step**, not Inversion.
- User confirmed hallucinations spawn **faster** as time passes; larger living ghosts accelerate spawning further.
- Preserve unrelated existing edit to `Tetrabeasts_GameIdea.txt`.
- No AGENTS.md found in workspace/ancestor search. No delegation requested.

## Checklist
- [x] Playlist: flush opaque sticky header; smooth mouse wheel.
- [x] Hallucination: time/size spawn ramp, modest movement/animation growth, expanded visible-art bounce bounds.
- [x] Boulders: louder collision audio (2x local impact gain, respects SFX/master controls).
- [x] Bombardment: 30% max-HP base damage, stationary craters count for rows, generated flaming boulder warning.
- [x] Inversion: 95% alpha radial countdown over full rotating 60% alpha image.
- [x] Flip The Board: generated pixel warning, flipped spawn one row nearer board.
- [x] Watch Your Step: 1–3 empty-cell hazards every 7–10 seconds, respective two-second warning.
- [x] Dr. Feel Good: normal heal cadence, two uncapped stacks per heal, escalating purple border/tint and stack badge.
- [x] Rewrite seven player descriptions; update authoring defaults too.
- [x] Audit/reset Chaos level state; regression coverage. Existing per-level rebuild and boulder Clear already reset ramp state; explicit new-field reset added to EndNewModifierEffects.
- [x] Compile, targeted Unity tests, asset inspection, final diff review.

## Resume notes
Primary logic: `LevelModifierController.NewModifiers.cs`, `Board.LevelModifiers.cs`, `Board.cs`, `LevelModifierController.Chaos.cs`, `Piece.cs`, `VolumePanelUI.Playlist.cs`.
Existing tests: `Assets/Editor/NewLevelModifierTests.cs`, `ModifierFeedbackTests.cs`, `BoulderAndModeTests.cs`, `GameplayPlaylistTests.cs`.
Unity version 6000.2.9f1. No running Unity process found initially.
Both imagegen outputs are in `Assets/Sprites/UI/{BombardmentWarning,FlipBoardWarning}.png`, with sprite metadata and modifier references. Full prompts in `Tools/ModifierWarningArtPrompts.md`.

## Completed validation
- Restricted Unity launch stalled at licensing; cancelled and reran with escalated access to local licensing service (approved).
- First Unity run: 17/17 passed (`Logs/ModifierPolish-tests.xml`).
- Final Unity run after all source changes: **17/17 passed**, zero failures (`Logs/ModifierPolish-tests-final.xml`, `Logs/ModifierPolish-unity-final.log`).
- Target suites: NewLevelModifierTests, ModifierFeedbackTests, BoulderAndModeTests, GameplayPlaylistTests.
- Compilation succeeded. Tests include normal healing cadence, uncapped stacks, debuff pooling, empty-cell warning/revalidation, layered inversion, bombardment survival, crater-only and crater/stone cascade termination, hallucination ramp/reset, retained Chaos effects, audio controls, paused wheel easing and cancellation.
- Playlist renders in `Logs/Playlist-*.png` inspected. Corrected sliced duplicate section text during transitions; final screenshot verified flush opaque heading.
- Both generated warning sprites are 1254x1254 with transparent corners. Unity tests verify sprite references, point filtering, alpha handling and disabled mipmaps.
- Existing Chaos BeginLevel/RebuildChaosEffects resets older timers/contagion/overgrowth state; round cleanup resets boulder elapsed time. New growth/warning fields now explicitly clear on end/reset.
- Removed unrelated Unity TimeManager serialization migration after tests. User's pre-existing `Tetrabeasts_GameIdea.txt` change remains untouched.
- `git diff --check` passed (only line-ending notices).

## Playtest follow-up
The October 18 implementation passed its tests. Subjective sound balance, ghost pacing and VFX intensity have not been assessed during a full manual gameplay run. The October 20 requests below supersede the earlier bombardment survival behavior.

## Follow-up — 2026-10-20 (complete)
- [x] Dr. Feel Good: incoming healing reduced by existing stack count percent, before adding new stacks. Healing bottoms at zero at 100 stacks; damage stacks remain uncapped. Applies to ordinary and global restoration.
- [x] Flip: 5–20-second schedule; actual castle damage after five seconds starts the two-second warning immediately. Repeated hits do not restart warnings. Timers reset after the flip.
- [x] Inversion: foreground and backdrop rotate together while foreground wipes.
- [x] Bombardment: removes units without TileDied/reserve-loss events. Craters remain separate from attack-blocking obstacles.
- [x] Ghosts: random board locations/directions; six-second baseline spawning, 1.5-second minimum under time/size ramp; stronger size speed (.24); silhouette bounce scale .7.
- [x] Ghost combination count: starts at one, caps at ten; reaching ten permanently stops merging and unlocks canvas-wide movement with pulsing red tint. Projectile hits halve count (round up, minimum one), spawn a replacement, and continue to castle. Per-projectile IDs avoid repeat hits; short merge cooldown avoids immediately undoing a split.
- [x] Boulder collision damage: 8 + 15–20% max HP (previously 5 + 10–15%).
- [x] Update relevant player descriptions and serialized tuning.
- [x] Compile and run revised regression suites; inspect failures and final diff.

Expanded `NewLevelModifierTests` covers healing, actual castle-damage triggering/invulnerability, Chaos event routing, synchronized rotation, bombardment death events, swept enemy/player collision through craters, ghost combination/escape/shrinking, varied spawn regions, and a real projectile continuing to damage the castle. Boulder expectations updated in two suites. Prior user changes to the modifier database and `Tetrabeasts_GameIdea.txt` remain untouched.

Validation: final Unity run **17/17 passed, zero failures**, exit code 0 (`Logs/ModifierFollowup-final.xml`, `Logs/ModifierFollowup-final.log`). Earlier test-only failures were corrected by constructing the coroutine fixture after play-mode domain reload and disabling cosmetic floating damage text in the inactive-controller fixture. Runtime code compiled successfully. `git diff --check` passed. Restored the unrelated TimeManager serialization migration from the pre-test copy. No implementation tasks remain; subjective pacing/VFX/damage balance awaits a manual gameplay session.
