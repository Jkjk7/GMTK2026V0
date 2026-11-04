# Field guide and animated pixel combat

The user requested beginner tutorial options, a more polished presentation, explosive particle effects and animated characters. Continue the review branch `codex/pixel-art-polish`, retaining all prior pixel art and game rules.

## Design

An optional bilingual Field Guide opens from the main menu and in-game settings. It contains illustrated basics, a controls reference and build tips. Progress and navigation are explicit; the player can close and reopen it. In combat it pauses while open, blocks gameplay input, and restores the previous pause state exactly. It does not change progression, grant resources or automatically interrupt a new run.

Combat adds layered pixel sparks, debris and rings with element colors. A bounded reusable pool and local deterministic variation avoid per-hit object churn and global gameplay randomness. Existing sand/coin arrival callbacks stay unchanged. Cinematic effects are the default; balanced/reduced modes lower visual particle counts only.

Enemy types receive different child-only walk/hover/stomp motions, hit recoil and status/cast feedback. New emitter animation is child-only. Existing root simulation transforms remain untouched. Reduced motion suppresses added locomotion decoration while retaining essential feedback.

## Verification

Compile against the shipped Unity assemblies and exercise real-engine player regression checks. Verify guide pause restoration, overlapping settings, keyboard/modal blocking, both languages, particle pool overflow/reuse/expiry/RNG, frozen enemies and unchanged roots. Inspect rendered UI and a captured motion sequence. No Unity Editor import/build claims without running the Editor.
