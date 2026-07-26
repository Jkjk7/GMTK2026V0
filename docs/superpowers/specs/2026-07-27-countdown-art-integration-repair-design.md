# Countdown Art Integration Repair Design

## Goal

Make the existing Count down art pass visibly coherent in the running Game view. Replace
prototype squares and oversized color blocks with readable clockwork art while preserving all
gameplay behavior, module footprints, collisions, targeting, damage, economy, and localization.

## Confirmed Direction

Use the complete countdown clock as the dominant background motif. Keep it clearly recognizable
behind the board and combat lane, but reduce its brightness so projectiles, enemies, board states,
and text remain more prominent.

Reuse the generated environment and hourglass assets that are already suitable, generate the
missing module sheets, and establish one runtime mapping used consistently by the shop, hand,
drag preview, placed module, fused module, and placement ghost.

## Root Causes

1. `CountdownRingView` builds only sixty square tick renderers at sorting order `-20`; it never
   loads the generated circular ornament. This produces scattered bars rather than a clock.
2. `GameSkin.LoadOrCreateRuntime` finds no populated `GameSkin` resource. `GetModuleIcon` also
   handles only `Redirector` and `Projectile`, so most UI icons fall back to a gray square.
3. Every placed module creates its original full-size colored prototype body, then
   `ModuleSkinApplicator` adds a small clock face on top. The result is a large color block with
   a decoration rather than a skinned module.
4. `CreateSandClockPanel` explicitly creates two `PrototypeSprites.Square` images and never loads
   `UI/hourglass_frame.png`.
5. Slot and board state colors are mostly opaque fills. They hide the dark-steel and brass
   textures that should provide the visual structure.

## Visual Design

### Countdown Ring

- Load the generated circular ornament into the runtime resource registry.
- Render the ornament above the battle backdrop and below board cells, modules, enemies, and
  projectiles.
- Keep the full circle visible and centered on the combined board/combat composition.
- Overlay exactly sixty dynamic ticks. Unlit ticks use a subtle dark brass; lit ticks use aged
  brass and gold; the final warning segment alternates orange and red.
- Do not create colliders or UI raycast targets.

### Hourglass

- Replace the two-square silhouette with the generated ornate hourglass frame.
- Keep separate top and bottom sand-fill images behind the frame so the remaining-time ratio and
  penalty/gain feedback continue to animate.
- Keep the precise `mm:ss.mmm` label legible above the frame.
- Remove the opaque black block around the hourglass; use only a subtle timer plaque or shadow.

### Module Icons and Placed Modules

- Produce distinct clockwork silhouettes for every sellable `ModuleType`.
- Use one authoritative `ModuleType -> Sprite` lookup for shop cards, hand cards, drag previews,
  tooltips, placement ghosts, placed modules, and fused results.
- Replace the placed module's prototype body sprite with the mapped module sprite.
- Preserve existing functional tinting only as a restrained accent; do not tint the entire icon
  into a flat blue, red, or yellow block.
- Retain level labels, energy bars, targeting lines, rotation, and functional effects.

### Board and Sidebar

- Keep generated dark-steel/brass board cells visible without opaque green or red washes.
- Valid, invalid, hovered, locked, and enchant states use borders, hatching, or low-alpha overlays.
- Replace large flat sidebar/slot fills with the generated panel texture or a dark translucent
  neutral surface.
- Empty slots remain visually quiet; occupied slots emphasize their icon and rarity strip.

## Runtime Architecture

Extend `CountdownArtResources` into the runtime entry point for the hourglass, ring ornament,
panel, and module sprites. It must return safe prototype fallbacks when an asset is missing.

`GameSkin` will expose complete module mappings rather than a two-case switch. Runtime fallback
creation must populate these mappings from `Resources`, so the game does not depend on manually
creating or editing a ScriptableObject before a jam build.

`ModuleSlotView`, placement drag icons, tooltips, and `ModuleSkinApplicator` consume the same
mapping. Individual gameplay module classes remain unchanged except where their body renderer
must stop overwriting an already applied formal skin.

`CountdownRingView` owns the ornament and dynamic tick renderers. `SandClockPanel` continues to
own time-dependent fills and feedback, while `GameBootstrap` only creates and positions the
visual hierarchy.

## Asset Processing

- Generate two isolated, green-background module sheets matching the existing manifest:
  seven core combat modules and ten utility/path modules.
- Process them through `tools/Art/process-countdown-assets.mjs`.
- Inspect each extracted sprite for clipping, merged cells, baked text, and green spill.
- Copy runtime-required outputs under `Assets/Resources/Countdown`.
- Process existing hourglass and ring assets again if visible green fringe remains.

## Testing and Verification

Automated regression checks must prove:

- the ring ornament and hourglass frame load at runtime;
- every defined `ModuleType` resolves to a non-prototype icon;
- shop, hand, drag, and placed-module APIs resolve the same sprite;
- placed modules no longer retain the prototype square body;
- the ring still has exactly sixty ticks and remains below gameplay sorting orders;
- localization, projectile retargeting, enchant seeds, and gameplay constants remain unchanged.

Final visual verification must restart Play mode and capture the Game view at 1920×1080. Check
the empty shop, occupied shop, purchased hand card, placed modules, placement hover states,
hourglass, full-time ring, and warning-time ring. No completion claim is valid from asset-file
existence alone.

## Scope Boundaries

This repair does not redesign gameplay layout, rebalance modules, modify board dimensions, change
enemy spawning, change damage, or replace the battle-lane background. It only corrects art
integration, visual hierarchy, and readability.
