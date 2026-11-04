# Responsive pixel UI implementation plan

**Goal:** Make purchases, card receipts, controls and rewards feel responsive without changing game rules.

**Architecture:** Cached `UiFeedback` decorates controls; `ModalReveal` owns panel/card presentation transforms; views call feedback only around existing successful/denied operations.

**Tech stack:** Unity 6000.4.9f1, uGUI, existing pixel sprite caches, no external tween dependency.

**Spec:** ../specs/2026-11-04-ui-feedback-design.md

- [x] Implement shared control feedback and verify pause-time animation, bounded decorations, reduced motion and exact restoration.
- [x] Integrate purchase/refresh denial and successful hand receipt, preserving transaction/refund rules; add quiet cached UI cues.
- [x] Implement magical reward reveals and quieter modal presentation, verifying interruption and immediate single-use callbacks.
- [x] Apply shared feedback to main menu, settings, tutorial and gameplay buttons.
- [x] Replace the upper-left hourglass with a cached pixel frame/glass/sand presentation, preserving clock values and resource callbacks.
- [x] Compile and run actual-player checks; inspect animations and bilingual UI; obtain independent review.
- [x] Update documentation and prepare the existing branch with fresh Windows/source review deliverables.
