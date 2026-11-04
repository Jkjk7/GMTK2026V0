# TimeSands pixel art polish

The user selected pixel art and requested an isolated branch for review. Improve icons, enemy visuals, cosmetic combat feedback, and documentation while preserving all game rules.

## Approach

Use a cached code-native pixel atlas for all 27 module types, an AI-assisted enemy sprite sheet with seven distinct silhouettes, crisp status accents, and scene-owned bounded cosmetic pools. Retain current layout and readable bilingual typography. This extends the current Unity SpriteRenderer/uGUI architecture without new runtime dependencies. The alternatives were retaining smooth brass art or a brighter cartoon treatment; the user chose pixel art.

## Constraints

- No damage, prices, enemy stats, pathing, wave timing, colliders, targeting, unlocks, or rewards change.
- Preserve global random consumption. Sand and coin visual arrival callbacks carry gameplay state and must remain untouched.
- All module surfaces share icons, including FlameWall and consumables. Keep placement validity and path orientation cues.
- Keep main untouched; deliver on `codex/pixel-art-polish`.
- Use point-filtered sprites and bounded transient effects. Document verification limits honestly.

## Verification

Compile runtime C# against the existing player's Unity assemblies. Run asset processing tests. Verify actual sprite pixels, all enum mappings, visual reset/lifecycle and status rendering. Attempt smoke tests in a disposable player copy; only Unity Editor can prove full asset import/build checks. Include updated bilingual README and reviewable preview.
