# Marathon and leaderboard revision — October 12, 2026

- [x] Restore legacy local scores to Standard, merging with newer Standard scores without repeated duplicates; retain recovery copy.
- [x] Use the original Steam leaderboard for Standard, preserving its score and reset history.
- [x] Sunset-purple/orange/gold dropdown styling; restore a large centered local leaderboard title with dropdown to its right.
- [x] Gold, bold, shadowed mode tooltip titles; attach GameModeDescriptions to GameModeMenu.prefab for editable text.
- [x] Repeat the Marathon castle/boss cycle indefinitely. Keep absolute level, damage requirement, gravity, and enemy-damage progression increasing across cycles.
- [x] Restore normal level-completion reinforcements, capped by reserve capacity and respecting modifiers. Float '+X Reinforcements' after reward confirmation.
- [x] Frame opaque, aspect-preserved reward icons; darker blue/red titles; frame the whole reward panel.
- [x] Correct vertical special-gauge bubble scale while retaining runtime fill colors and full-gauge flashing.
- [x] Parent Marathon gauge beside the special slider so both share HUD visibility and panel ordering.
- [x] Unity compilation and automated behavior checks: 21/21 tests passed, including migration idempotency, castle cycle wrap, reinforcement cap/disabled cases, and shared HUD hierarchy.
- [x] Rendered layout verification and final audit: reward panel, local leaderboard, locked Marathon tooltip, and gameplay gauges inspected. Final focused UI regression run passed 3/3 after the last layout changes.

Editable descriptions: GameModeDescriptions on Assets/Prefabs/GameModes/GameModeMenu.prefab.
Editable reward colors: MarathonRewardColors on Assets/Prefabs/GameModes/MarathonRewardPanel.prefab.
Marathon's damage targets keep the existing growth setting (18% by default), including the old final boss. The level display continues counting upward when the castle sequence repeats. Enemy damage now gains 3% of its base per level; this is editable on GameController.

Live Steam uploads are not part of validation. Standard uses Tetrabeasts_GlobalScore; Chaos and Marathon retain their separate boards. Existing four-week board reset suffixes are unchanged.

## Special gauge fill correction — October 13, 2026

- [x] Replace the special gauge's built-in rounded slider sprite with the same opaque SpritePixel fill used by Marathon. The rounded sprite stretched its transparent edges along the tube, leaving visible gaps.
- [x] Update the gauge authoring helper to retain the corrected sprite. Preserve the existing red tint, liquid material, bubbles, slosh and full-charge flashing.
- [x] Verify the replacement texture is fully opaque and the gameplay scene inherits the prefab sprite without an override.
- [x] Unity compilation/render completed successfully; inspected the gameplay preview at 65% charge and confirmed the liquid fills the tube to its edges and bottom. Preview: Logs/MarathonGameplayLayout.png.

## Boulder collision revision — October 13, 2026

- [x] Impact audio for monster/stone hits and boulder-to-boulder collisions using the existing ThudRing clip with lower pitch, through the normal SFX mixer. Clip is editable in Assets/Resources/GameModeAssets.asset (Boulder Impact Sfx).
- [x] Maximum four active boulders; modifier spawn timer pauses at capacity. Boss volley remains three spaced boulders.
- [x] Each boulder starts with 4–7 damaging hits. Only actual monster HP or destructible stone durability loss consumes one. Sustained contact, immune hits, side walls, and other boulders do not consume hits.
- [x] Remove bottom-wall bounce; retain side-wall, monster, stone, and boulder collisions. Destroy after the last damaging hit or when the entire sprite falls below the screen.
- [x] Update modifier description. Focused Unity regression suite passed 4/4, including collision counts, immunity, cap, pause, bottom passage/off-screen cleanup, and boss volley.
