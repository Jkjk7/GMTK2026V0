# Bilingual Submission Design

## Goal

Ship a jam-safe English-first interface with an in-game Simplified Chinese switch, plus matching
English and Chinese repository landing pages, without changing gameplay or Unity package versions.

## Scope

- English is the default language for a fresh install.
- A visible `中文` / `EN` button switches languages and saves the choice in `PlayerPrefs`.
- Switching reloads the active scene so all programmatically constructed UI is refreshed together.
- The main player journey is bilingual: preparation and combat HUD, shop and hand labels, currency,
  countdown/status text, confirmation dialogs, module names, categories, descriptions, tooltips,
  blessing/curse choices, board expansion, scrap controls, and result overlays.
- Debug logs, source comments, Inspector tooltips, editor-only text, and internal object names remain
  unchanged because players do not see them.
- `README.md` becomes the English landing page. `README.zh-CN.md` contains the corresponding
  Simplified Chinese content. Each links to the other.

## Architecture

Create a small static `GameLocalization` service:

- `GameLanguage` enum with `English` and `SimplifiedChinese`.
- `CurrentLanguage`, `IsChinese`, and `SetLanguage`.
- `Text(english, chinese)` for direct strings.
- Persist the selected value under one stable `PlayerPrefs` key.
- Missing/invalid saved values fall back to English.

`GameBootstrap` creates a compact top-right language button after the canvas is available. The
button label advertises the language it will switch to (`中文` while English is active, `EN` while
Chinese is active). Clicking saves the new language and reloads the active scene.

Player-facing string producers call `GameLocalization.Text`. This includes both static labels
created by `GameBootstrap` and dynamic text assembled by controllers or catalogs. Existing numeric
formatting and gameplay values remain unchanged.

## Failure Handling

- If no active scene can be reloaded, the preference is still saved for the next launch.
- Unsupported preference values use English.
- No localization package, external files, fonts, or network access are required.
- Existing Unicode-capable UI font resolution remains the single font path for both languages.

## Verification

- Editor regression asserts English is the fresh-install fallback and both language selections
  return the expected text.
- Search player-facing runtime scripts for remaining Chinese literals and document any intentionally
  excluded editor/debug-only occurrences.
- Run `CountdownArtRegressionChecks.Run` and `GmtkBugfixRegressionChecks.Run` in Unity 6000.5.4f1.
- Confirm `README.md` and `README.zh-CN.md` link to each other and contain no placeholders.

## Non-goals

- Full Unity Localization package migration.
- Locale auto-detection.
- Additional languages.
- Live replacement of every existing `Text` component without scene reload.
- Translation of code comments, debug logging, or Inspector-only content.
