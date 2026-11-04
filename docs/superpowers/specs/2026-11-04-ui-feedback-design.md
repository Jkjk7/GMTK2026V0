# Responsive pixel UI

The user requested richer feedback across the existing UI: unaffordable shop cards should shake, dismantled/received cards should light up with a small ding, and upgrade rewards should appear with a magical reveal. Continue on `codex/pixel-art-polish` and retain all game rules.

Use the existing navy/brass pixel artwork with red rejection, gold receipt and violet/cyan reward accents. A shared `UiFeedback` component adds subtle hover/press, brief rejected-action shake, bright receipt star and bounded sparks. It decorates existing RectTransforms and restores their exact pose. Existing purchase, refund, inventory and reward callbacks still execute immediately.

Reward panels get a quick spring reveal with staggered cards and a cached rune/mote layer. Confirmation, settings and result panels use the quieter form of this reveal. Visuals use unscaled time so paused menus animate, respect `PresentationSettings.MotionEnabled`, use the existing particle-quality budget and do not consume gameplay RNG. Decorations never receive raycasts. Optional short UI sounds are synthesized once and shared, with rate limiting.

Verify denied purchases preserve coins/offers/inventory, receipts are not retriggered by hover/affordability refresh, reward callbacks remain single-use, interrupted effects restore layout, and repeated opening/clicking cannot grow visual objects without limit. Inspect actual Unity renderings and deliver updated source/Windows review copies. No frame-rate improvement is promised.
