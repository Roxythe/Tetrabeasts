# New level modifiers — implementation progress

Updated October 5, 2026.

## Implemented

- Dr. Feel Good: +1 healer range, configurable faster healing (default twice as often), instance-owned stacking damage (default 0.25 per stack each second), reversible foreground tint, death/pool/round cleanup. Full-health restoration also applies a stack when it restores health.
- Watch Your Step: randomly fills 33% of cells below the top five rows with distinct floor effects/stone obstacles; prevents environment overlap while active.
- Flip The Board: two-second warning before the first flip, then alternating orientation every 20–30 seconds with warnings. Rotates the shared board transform so spawning, gravity, ghosts, and settling rise on screen. Screen-relative horizontal movement is preserved. Original transform restored at round end.
- Inversion: swaps movement/rotation actions after reading current bindings; never writes keyboard, controller, or Steam bindings. Removed at round end.
- Bombardment: warns at 13 seconds, strikes at 15, destroys monsters and environment in a 3×3 region below the top five rows. Fixed crater occupancy blocks active pieces and settling, does not complete rows or block projectiles. Expires after 30 active seconds; maximum two craters. Crater sprite renders below grid and board special VFX, with a procedural placeholder when no custom sprite is assigned.
- Hallucination: one at round start, then every ten active seconds; configurable velocity and size, inward 195–345° spawn direction, reflected wall travel, size-based consuming with random ties, 0.5-second consume sprite, projectile-triggered death frames and immediate larger replacement/reset timer. Supplied placeholder linked; editable animation and consume sprite fields are on the modifier asset. Size is capped to 85% of the smaller board dimension to keep bounces possible.
- Six ScriptableObject assets and supplied icons added to the shared selection database. Existing serialized enum values preserved.
- All timers suspend with gameplay; transient modifier state is removed on victory, defeat, restart, reset, and controller teardown.

## Verification

- C# MSBuild compilation passed with the two new partial source files included.
- Unity 6000.2.9f1 compilation passed. Both regression tests in `Assets/Editor/NewLevelModifierTests.cs` passed (2 passed, 0 failed); report saved to `Logs/NewLevelModifierTests.xml` (ignored build output).
- Runtime checks cover damage scaling, extra healing range/frequency, restoration/death, pooling and settling, 33% non-overlapping placement/top-row exclusion, flip warning/reset, inversion lifecycle, crater damage/blocking/projectile transparency/expiry, hallucination bounce/merge/projectile replacement, and paused timer/UI ordering.
- `git diff --check` passed. Existing font asset edits were present before this work.
- Manual full-game visual review remains pending, including customized bindings and presentation after resize. Automated checks run headlessly and do not replace a visual playtest.

To rerun in Unity Test Runner, select EditMode and `NewLevelModifierTests`. Command-line equivalent: Unity Editor with `-batchmode -nographics -projectPath <project> -runTests -testPlatform EditMode -testFilter NewLevelModifierTests -testResults <project>/Logs/NewLevelModifierTests.xml`.

## Art/tuning still available to customize

- Assign final crater/warning artwork to the Bombardment asset and side warning artwork to Flip The Board if desired; runtime fallbacks are provided.
- Hallucination's living animation uses the 19 new frames in numeric order with `UIImagePingPongAnimator`, at 12 FPS in ping-pong mode. The round clock controls playback so pauses, consume flashes, and death animation do not fight over the sprite. Death/consume artwork remains independently configurable.
- Tune healing interval/damage, hallucination velocity/size growth, and flip intervals in the six modifier assets.

## October 5 animation follow-up

- Mao and Quixel special playback set to 1.25× speed (about 4.01 seconds instead of 5.02 seconds for each character clip).
- Their popup exit and audio fade overlap the end of playback; the final sprite renders once, without the previous post-animation hold. Other commanders retain their existing popup finish mode.
- Added `AnimationPlaybackTests` for ordered ping-pong frames and real special-popup timing/final frames at time scale zero. Extended modifier tests to check hallucination animation freezes while paused.
- Gameplay and editor/test assemblies compile successfully with MSBuild; `git diff --check` passes.
- Runtime test execution and visual review for this follow-up are pending because the project is currently open in the user's Unity editor.
