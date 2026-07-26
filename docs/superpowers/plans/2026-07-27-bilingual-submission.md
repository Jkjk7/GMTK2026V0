# Bilingual Submission Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the jam build English-first with a persistent Simplified Chinese switch and provide matching English and Chinese repository landing pages.

**Architecture:** A dependency-free static `GameLocalization` service owns the current language, persistence, and two-language string selection. Runtime string producers ask the service for the active variant; a bootstrap-created button saves the alternate language and reloads the scene so dynamically constructed UI refreshes consistently.

**Tech Stack:** Unity 6000.5.4f1, C#, UnityEngine.UI, PlayerPrefs, SceneManager, Markdown.

## Global Constraints

- Fresh installs default to English.
- Persist only `English` or `SimplifiedChinese`; invalid saved values fall back to English.
- Do not add or update Unity packages.
- Do not change gameplay values, rules, damage, timing, board logic, or wave logic.
- Translate player-facing runtime strings; exclude comments, debug logs, Inspector tooltips, editor-only text, and internal object names.
- `README.md` is English and `README.zh-CN.md` is Simplified Chinese; each links to the other.
- Never stage the existing `MinerModule.cs`, `Packages/`, or `ProjectSettings/` compatibility/reserialization changes.

---

## File Map

- Create `Assets/Scripts/Core/GameLocalization.cs`: language enum, English fallback, persistence, and `Text`.
- Create `Assets/Editor/LocalizationRegressionChecks.cs`: deterministic localization checks.
- Modify `Assets/Scripts/Game/GameBootstrap.cs`: visible language-switch button and primary static labels.
- Modify `Assets/Scripts/Core/ModuleCatalog.cs`: bilingual module names, categories, and descriptions.
- Modify `Assets/Scripts/Combat/CombatHUD.cs`: bilingual wave and breach status.
- Modify `Assets/Scripts/UI/PrepPhasePanel.cs`: bilingual preparation labels.
- Modify `Assets/Scripts/UI/GoldPanel.cs`: bilingual currency/insufficient labels.
- Modify `Assets/Scripts/UI/ShopController.cs`: bilingual refresh label.
- Modify `Assets/Scripts/UI/ModuleSlotView.cs`: bilingual empty state.
- Modify `Assets/Scripts/UI/ScrapZone.cs`: bilingual scrap prompts.
- Modify `Assets/Scripts/UI/DraftChoiceView.cs`: bilingual draft prompts.
- Modify `Assets/Scripts/UI/RunStatsHud.cs`: bilingual expansion label and stat captions.
- Modify `Assets/Scripts/UI/PlacementController.cs`: bilingual board expansion, move, scrap, and dismantle confirmations.
- Modify `README.md`: English landing page.
- Create `README.zh-CN.md`: Simplified Chinese landing page.

### Task 1: Add localization core and player-facing language switch

**Files:**

- Create: `Assets/Scripts/Core/GameLocalization.cs`
- Create: `Assets/Editor/LocalizationRegressionChecks.cs`
- Modify: `Assets/Scripts/Game/GameBootstrap.cs`

**Interfaces:**

- Produces: `enum GameLanguage { English = 0, SimplifiedChinese = 1 }`
- Produces: `GameLocalization.CurrentLanguage`, `GameLocalization.IsChinese`,
  `GameLocalization.Text(string english, string chinese)`,
  `GameLocalization.SetLanguage(GameLanguage language, bool save = true)`,
  and `GameLocalization.ResetForTests(bool clearPreference = true)`.

- [ ] **Step 1: Write the failing editor checks**

```csharp
GameLocalization.ResetForTests();
Require(GameLocalization.CurrentLanguage == GameLanguage.English, "Fresh fallback must be English.");
Require(GameLocalization.Text("Shop", "商店") == "Shop", "English selection failed.");
GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese, false);
Require(GameLocalization.Text("Shop", "商店") == "商店", "Chinese selection failed.");
GameLocalization.SetLanguage((GameLanguage)99, false);
Require(GameLocalization.CurrentLanguage == GameLanguage.English, "Invalid values must fall back.");
```

- [ ] **Step 2: Run Unity once to verify RED**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath "$PWD" \
  -executeMethod LocalizationRegressionChecks.Run \
  -logFile /tmp/gmtk-localization-red.log
```

Expected: nonzero because `GameLocalization` does not exist.

- [ ] **Step 3: Implement the dependency-free localization service**

```csharp
public enum GameLanguage { English = 0, SimplifiedChinese = 1 }

public static class GameLocalization
{
    const string PreferenceKey = "gmtk.language";
    static bool initialized;
    static GameLanguage currentLanguage;

    public static GameLanguage CurrentLanguage { get { EnsureInitialized(); return currentLanguage; } }
    public static bool IsChinese => CurrentLanguage == GameLanguage.SimplifiedChinese;
    public static string Text(string english, string chinese) => IsChinese ? chinese : english;

    public static void SetLanguage(GameLanguage language, bool save = true)
    {
        currentLanguage = language == GameLanguage.SimplifiedChinese
            ? GameLanguage.SimplifiedChinese : GameLanguage.English;
        initialized = true;
        if (save) { PlayerPrefs.SetInt(PreferenceKey, (int)currentLanguage); PlayerPrefs.Save(); }
    }

    public static void ResetForTests(bool clearPreference = true)
    {
        if (clearPreference) PlayerPrefs.DeleteKey(PreferenceKey);
        initialized = false;
        currentLanguage = GameLanguage.English;
    }
}
```

`EnsureInitialized` reads `PreferenceKey`, accepts only enum values `0` and `1`, and otherwise
selects English.

- [ ] **Step 4: Add the bootstrap language button**

Create a top-right UI button after the canvas exists. Its label is `中文` in English mode and `EN`
in Chinese mode. On click, call `SetLanguage` with the alternate language and reload
`SceneManager.GetActiveScene().buildIndex` when the index is nonnegative; otherwise reload by
scene name when nonempty.

- [ ] **Step 5: Run localization checks to verify GREEN**

Run the Step 2 command. Expected log: `[Localization Regression] PASS`.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Core/GameLocalization.cs Assets/Scripts/Core/GameLocalization.cs.meta \
  Assets/Editor/LocalizationRegressionChecks.cs Assets/Editor/LocalizationRegressionChecks.cs.meta \
  Assets/Scripts/Game/GameBootstrap.cs
git commit -m "feat: add English-first language switch"
```

### Task 2: Localize the main player journey

**Files:**

- Modify: `Assets/Scripts/Game/GameBootstrap.cs`
- Modify: `Assets/Scripts/Core/ModuleCatalog.cs`
- Modify: `Assets/Scripts/Combat/CombatHUD.cs`
- Modify: `Assets/Scripts/UI/PrepPhasePanel.cs`
- Modify: `Assets/Scripts/UI/GoldPanel.cs`
- Modify: `Assets/Scripts/UI/ShopController.cs`
- Modify: `Assets/Scripts/UI/ModuleSlotView.cs`
- Modify: `Assets/Scripts/UI/ScrapZone.cs`
- Modify: `Assets/Scripts/UI/DraftChoiceView.cs`
- Modify: `Assets/Scripts/UI/RunStatsHud.cs`
- Modify: `Assets/Scripts/UI/PlacementController.cs`

**Interfaces:**

- Consumes: `GameLocalization.Text(string english, string chinese)`.
- Produces: the same existing UI method signatures and gameplay behavior, with language-selected copy.

- [ ] **Step 1: Convert the primary static and dynamic UI strings**

Wrap each included player-facing literal directly:

```csharp
title.text = GameLocalization.Text("Shop", "商店");
statusText.text = GameLocalization.Text(
    $"Wave {wave}/{totalWaves} | Enemies {enemies} | Damage {damage}",
    $"波次 {wave}/{totalWaves} | 敌人 {enemies} | 伤害 {damage}");
```

Use the following stable English terms: `Modules`, `Shop`, `Hand`, `Gold`, `Refresh`, `Ready`,
`Preparation`, `Wave`, `Enemies`, `Damage`, `Empty`, `Scrap`, `Confirm`, `Cancel`, `Expand Board`,
`Insufficient Gold`, `View Upgrades`, `Hide Upgrades`.

- [ ] **Step 2: Add English module catalog variants**

For all 17 `ModuleType` values, return English names and categories when English is active and
retain the existing Chinese strings otherwise. Translate functional descriptions literally and
briefly; preserve every interpolated gameplay number and value expression.

- [ ] **Step 3: Scan for visible Chinese literals**

Run:

```bash
rg -n '"[^"\n]*[\p{Han}][^"\n]*"' Assets/Scripts/Game Assets/Scripts/Core \
  Assets/Scripts/Combat Assets/Scripts/UI --glob '*.cs'
```

Every remaining result must be a debug log, Inspector tooltip, code comment, editor-only check, or
a Chinese argument passed to `GameLocalization.Text`.

- [ ] **Step 4: Run both Unity regression entry points**

Run `LocalizationRegressionChecks.Run`, `CountdownArtRegressionChecks.Run`, and
`GmtkBugfixRegressionChecks.Run`. Expected: all three logs contain `PASS` and all commands exit 0.

- [ ] **Step 5: Commit**

Stage only the listed runtime files and commit:

```bash
git commit -m "feat: localize core player interface"
```

### Task 3: Publish bilingual repository landing pages

**Files:**

- Modify: `README.md`
- Create: `README.zh-CN.md`

**Interfaces:**

- Produces: English project overview at the repository root and equivalent Simplified Chinese copy.

- [ ] **Step 1: Write the English README**

Include: language link, `Sand Circuit: The Last Grain`, Count Down premise, 17-module circuit/tower
defense loop, controls (`Mouse`, `R`, `Space`), Unity version, run/build guidance, current art/VFX
features, GitHub source context, and generative-AI disclosure.

- [ ] **Step 2: Write the Chinese README**

Mirror the English section order and facts in natural Simplified Chinese. Link back to
`README.md` in the first paragraph.

- [ ] **Step 3: Verify documentation consistency**

```bash
rg -n "README.zh-CN.md" README.md
rg -n "README.md" README.zh-CN.md
```

Expected: reciprocal links found; manually confirm both files contain finished copy with no
unfinished markers.

- [ ] **Step 4: Commit**

```bash
git add README.md README.zh-CN.md
git commit -m "docs: add English and Chinese game guides"
```
