# Field guide and animated combat implementation plan

**Goal:** Optional illustrated onboarding and lively, bounded pixel combat presentation.

**Spec:** ../specs/2026-11-04-guide-and-spectacle-design.md

**Architecture:** Tutorial modal reuses uGUI and sprite atlases. PresentationSettings stores only visual preferences. Scene-owned burst effects and child-only animators extend existing render components.

- [x] Add PresentationSettings and verify validation, visual budget levels and preference isolation.
- [x] Add bilingual Field Guide, menu/settings entries and modal input guards; test pause/close lifecycle.
- [x] Add shared bounded particle bursts and cosmetic projectile hooks; test reuse, expiry, RNG and quality levels.
- [x] Add enemy and emitter child animations; test freeze/reduced-motion and unchanged simulation roots.
- [x] Compile, run real-player tests, inspect bilingual tutorial and animated combat previews; review gameplay invariants.
- [x] Update both READMEs and prepare the existing review branch with refreshed source/Windows preview deliverables.

Review focus: closing a tutorial over an already paused settings menu; same-frame ESC input leakage; scene unload while paused; high particle density and disabled preferences; root transforms and resource callbacks.
