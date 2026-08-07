# TimeSands

[简体中文](README.zh-CN.md)

> Route time into weapons before the final grain falls.

**TimeSands** is a circuit-building tower-defense game created for GMTK Game
Jam 2026 and its theme, **Count Down**. Time is both your health and the resource that powers your
clockwork defense workshop.

## How to Play

Place modules on the board and route energy balls through machines to charge your defenses.
Enemies approach from the right; letting one reach the hourglass removes precious time. Build an
efficient circuit, combine status effects, and survive 25 escalating waves before the last grain
falls.

The current jam build includes:

- 17 attack, routing, economy, enchantment, and support modules
- Buying, placing, rotating, moving, merging, upgrading, and scrapping
- Fire, chill, Melt, black-hole, bomb, and sand-buff combat interactions
- Roguelike module drafts, emitter upgrades, blessings, and bindings
- A 60-tick world-space countdown ring with warning states
- Clockwork module and enemy skins with burn, chill, sand, hit, death, and Melt effects
- English interface by default with an in-game Simplified Chinese switch

## Controls

- **Mouse:** buy, select, drag, place, move, merge, and scrap modules
- **R:** rotate the selected or carried module
- **Space** or **Ready:** finish preparation and begin the wave
- **F:** refresh the shop
- **Right click** or **X:** dismantle a placed module
- **中文 / EN:** switch language; the choice is saved for future launches

## Run the Project

The submission branch is tested with **Unity 6000.5.4f1**.

1. Clone [Jkjk7/GMTK2026V0](https://github.com/Jkjk7/GMTK2026V0).
2. Open the repository root through Unity Hub.
3. Open the game scene in `Assets/Scenes`, then press Play. The runtime bootstrap can also assemble
   the prototype when no bootstrap object is present.

Data and Graphs see in Docs

## Repository Layout

```text
Assets/Scripts/                  Gameplay and UI code
Assets/Art/Generated/Countdown/ Generated and processed countdown artwork
Assets/Editor/                   Batch-mode regression checks
Tools/Art/                       Deterministic PNG processing pipeline
Docs/                            Data and Graphs
```

## Team

Created for GMTK Game Jam 2026 by **Team 强者🥛**.

## Generative AI Disclosure

Generative AI tools assisted with portions of the visual development, code, documentation, and
submission text. The team selected, edited, integrated, and tested the final content.

## License and Use

This repository is published for game-jam demonstration and learning. Do not reuse its assets or
code commercially without permission from the team.
