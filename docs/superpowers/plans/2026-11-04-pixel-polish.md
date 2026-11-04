# Pixel polish implementation plan

**Goal:** Reviewable pixel-art visual upgrade without gameplay changes.

**Architecture:** Cached pixel sprites feed the existing icon and enemy components; scene-owned cosmetic pools bound allocations. UI updates are skipped only when displayed values are unchanged.

**Spec:** ../specs/2026-11-04-pixel-polish-design.md

## Tasks

- [x] Implement all 27 module sprites and shared UI/world integration. Validate sprite identity, size, alpha, point filtering, prototype fallback and ghost behavior.
- [x] Integrate seven enemy silhouettes and pixel status accents; preserve gameplay transforms. Validate every enemy type, burn/chill/sand states and cleanup.
- [x] Pool hit/melt/death and shield visuals with bounded capacity, full state reset and original RNG consumption; leave resource callbacks unchanged.
- [x] Apply coherent pixel UI panel treatment and avoid redundant countdown/ball-count updates.
- [x] Correct both READMEs from actual code, including version, setup, controls, build and verification.
- [x] Compile, run available tests, inspect preview, review diff for gameplay invariants, commit to branch and deliver branch/patch.

## Review focus

Placement ghosts and rotation cues; disabled reload/domain lifecycle; RNG sequence under pool overflow; resource callbacks and timing; bilingual readability at game scale.

## Environment

Fresh isolated checkout; main unchanged. Unity Editor not found, but existing desktop player and matching managed assemblies are available. Baseline Node tests initially blocked by missing sharp dependency; install locally in work tooling before rerun.
