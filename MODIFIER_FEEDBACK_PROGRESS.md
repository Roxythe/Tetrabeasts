# Modifier feedback changes — October 13, 2026

- [x] Boulder impact cue replaced with a normalized, punchier retro crunch; full per-cue gain through AudioManager's Master/SFX-controlled one-shot pool.
- [x] Modifier waves spawn 1–3 separated boulders, capped at four active; capacity still pauses the countdown. Boss volleys remain three.
- [x] Spawn interval accelerates from 5–10 seconds toward 2–4 seconds across three minutes of active level time; resets between levels.
- [x] Boulder damage is 10–15% of the monster's buffed runtime max HP plus 5 flat damage, before existing damage modifiers. Stone durability and damaging-bounce rules are unchanged.
- [x] Larger, brighter gray pixel sprays fan out from actual contact positions with gravity and fade.
- [x] Distinct retro ghost spawn, merge and destruction audio. Merges add a purple/white pixel burst and stepped squash/stretch growth pulse.
- [x] Repeated ImpactShake triggers accumulate with a bounded maximum. Rapid castle damage volleys get an additional bounded intensity boost.
- [x] Bombardment impact sound, small board shake, pixel burst, and short shaking/fading unit echoes while destroyed units are removed immediately from gameplay.
- [x] Inversion runs for 8 seconds then rests for 12 seconds, as confirmed by the user. Activation sound and a matching radial countdown sprite use the same layer as special-block backgrounds; effects reset at level end.
- [x] Flip The Board plays its cue and shakes the board after each warned rotation; resetting the level clears shake offsets before restoring the board transform.
- [x] Seven original synthesized WAV cues use the existing audio manager and live volume controls. Source: Tools/GenerateModifierFeedbackAudio.py.
- [x] Unity behavior/volume regression suite: 24/24 passed; final focused rerun after particle contrast and short-interval shake refinement passed 9/9. Checks include wave separation/cap, damage scaling, inversion timing/wipe/cleanup, ghost merge pulse, bombardment echoes, and live Master/SFX slider muting.
- [x] Visual preview and final cleanup audit: full/half inversion radial wipe and outlined gray boulder spray inspected in the gameplay scene; all new Unity assets have metadata. Preview logs/images are in Logs/ModifierFeedbackPreviews.log, Logs/InversionActivePreview.png, Logs/InversionHalfPreview.png, and Logs/BoulderImpactPreview.png.

## Editable assets

October 14 follow-up: the inversion sprite rotates counterclockwise once over each active countdown, pauses with gameplay, and resets its orientation on the next activation.

- Boulder sound: Assets/Resources/GameModeAssets.asset → Boulder Impact Sfx.
- Other modifier sounds, inversion timing and sprite: corresponding assets in Assets/ScriptableObjects/LevelModifier/.
- New sounds: Assets/Audio/LevelModifier/{BoulderImpact,GhostSpawn,GhostMerge,GhostDestroyed,BombardmentImpact,InversionActivate,BoardFlip}.wav.
- Inversion sprite: Assets/Sprites/UI/InversionBoardVFX.png (point-filtered, transparent).

## Inversion sprite provenance

Generated with the built-in image-generation tool using the imagegen skill, then copied into the project. Prompt:

“Use case: stylized-concept. Asset type: transparent pixel-art game VFX sprite for reversed controls, centered board background overlay in a retro dark fantasy puzzle game. Square composition: two bold opposing curved arrows forming a circular reversal sigil, left-right arrows unmistakable, purple and cyan magic with gold highlights and dark pixel outlines, a few square sparks around the ring. Authentic chunky pixel art with hard square edges, limited palette, no text, no letters, no environment, no frame, no soft airbrush. The complete sigil has ample transparent padding and a transparent empty center. Designed to remain readable when displayed behind a game grid and erased by a clockwise radial countdown wipe.”
