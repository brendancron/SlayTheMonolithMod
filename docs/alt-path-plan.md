# Alternate-storyline implementation plan

Plan date: 2026-05-10 (revised). Branch: `biomes`.

## Goal

Custom monsters/encounters/biomes form a self-contained alternate run. Vanilla STS2 runs see no mod content; alt runs see no vanilla acts. Player toggles between modes via a BaseLib mod-config setting.

## TL;DR

Single source-of-truth flag `IsAltRun` is captured at run-start from a `SimpleModConfig` property and frozen for the life of that `RunState`. **Only the act-list selection reads this flag.** Encounters bind to specific acts via `IsValidForAct(act => act is OurAct1)` — they never read the flag. This keeps the encounter pool stable (no cache invalidation needed) and matches how vanilla `Overgrowth`/`Hive`/`Glory` encounters bind to their own acts.

```
mod config (BaseLib UI) → [RunManager run-start hook] → per-RunState flag (AltRunState)
                                                                  ↓
                                  GetRandomList patch (act-list selection only)

encounters → IsValidForAct(act is OurAct1)  ← stable, no flag, no cache thrash
```

## Phase 0 — Config + flag plumbing — DONE

What landed:
- `SlayTheMonolithModCode/Config/StoryConfig.cs` — `SimpleModConfig` with `AlternateStorylineEnabled` bool.
- `SlayTheMonolithModCode/Runs/AltRunState.cs` — `ConditionalWeakTable<RunState, object>` flag-holder. Public API `AltRunState.IsAlt(RunState)`.
- `SlayTheMonolithModCode/Patches/RunStartHook.cs` — Harmony postfix on `RunManager.Launch` (`RunManager.cs:480`); converges new + loaded run paths.
- `SlayTheMonolithMod/localization/eng/settings_ui.json` — `mod_title`, section title, and `.hover.desc` for the toggle.
- `SlayTheMonolithModCode/MainFile.cs` — `ModConfigRegistry.Register(ModId, new StoryConfig())` in `Initialize()`.

Verified in log (`%APPDATA%/SlayTheSpire2/logs/godot.log`):
- `[INFO] [BaseLib] Setting up SimpleModConfig SlayTheMonolithMod.SlayTheMonolithModCode.Config.StoryConfig`
- `[INFO] [SlayTheMonolithMod] Alt-storyline run started` fires once per run after the act preload, exactly when `RunManager.Launch` runs.

## Phase 1 — (collapsed)

Originally planned as "gate encounters' `IsValidForAct` on the alt flag." Reverted: that approach conflated act-binding with mode-selection, would have required cache invalidation against `ModelDb.Preload`'s baked encounter list, and left encounters with knowledge they don't need. The work folds into Phase 2.

## Phase 2 — Custom acts + encounter rebinding

**New files:**

- `SlayTheMonolithModCode/Acts/Act1Mod.cs` — `CustomActModel(actNumber: 1)`. Per `docs/baselib-feasibility.md` § Biomes/acts, override the abstract members from `ActModel` (~12 of them — see that doc for the full list). For audio/SFX/music in MVP, point at vanilla Overgrowth's FMOD paths to skip authoring custom audio.
- Asset stubs:
  - `SlayTheMonolithMod/scenes/backgrounds/<id>/<id>_background.tscn`
  - `SlayTheMonolithMod/scenes/rest_site/<id>_rest_site.tscn`
  - `SlayTheMonolithMod/images/packed/map/map_bgs/<id>/{map_top,map_middle,map_bottom}_<id>.png`

**Modified files:**

- All 25 `*Normal.cs` encounters: `IsValidForAct(ActModel act) => act is Act1Mod;` (no flag check, no `RunManager.Instance.State` lookup — encounters don't care about the run mode).
- `scripts/generate_monsters.py` ENC_TPL: same change baked in for regenerations.

**Harmony patch:**

- `SlayTheMonolithModCode/Patches/AltActListPatch.cs` — postfix on `ActModel.GetRandomList`. Logic:
  - If `AltRunState.IsAlt(RunManager.Instance.State)`: return `[Act1Mod, Act2Mod, Act3Mod]` (placeholders for 2/3 until they exist; intermediate state can have alt-mode-on still using vanilla acts 2/3).
  - Else: filter the list to exclude any `CustomActModel`. Custom acts never roll in vanilla runs.
  - Use `[HarmonyPriority(Priority.Low)]` so we run *after* BaseLib's `ActModelGetRandomListPatch` and have the final word.
  - **Bypass BaseLib's Underdocks-discovery short-circuit when alt-run is on** (per feasibility doc): alt runs always get alt acts regardless of `UnlockState.IsEpochRevealed<UnderdocksEpoch>()`.

**Smoke tests:**

1. Flag ON → start run → first act is `Act1Mod`. Only mod monsters appear (because vanilla encounters don't `IsValidForAct(Act1Mod)`).
2. Flag OFF → start run → first act is one of Overgrowth/Underdocks (vanilla weights), no mod content (because mod encounters don't `IsValidForAct(Overgrowth)` anymore).
3. `fight SLAYTHEMONOLITHMOD-LANCELIER_NORMAL` console command works in both modes (it bypasses `IsValidForAct`).

## Phase 3 — Acts 2 and 3

Add `Act2Mod` and `Act3Mod` plus their asset bundles. Spread the 25 encounters across acts via the generator script (table grows an `Act` column → 8/8/9 across acts 1/2/3 or whatever pacing you want). `GetRandomList` patch already returns `[Act1Mod, Act2Mod, Act3Mod]` — Phase 3 just fills in the placeholders.

## Phase 4 (optional polish)

Promote the config flag to a proper run-setup `ModifierModel`. Lets a player do one vanilla and one alt run with the mod loaded. Requires patching `ModelDb.GoodModifiers`/`BadModifiers` (currently hard-coded — see `ModelDb.cs:225, 238`) to inject our modifier into the picker.

Defer until Phases 0–3 are working.

## Risks / unverified

- **`GetRandomList` ordering against BaseLib's patch.** `[HarmonyPriority(Priority.Low)]` should put us last but hasn't been tested. Fallback: transpile rather than postfix.
- **Multiplayer.** `ModelIdSerializationCache.Hash` already differs when mod content registers (multiplayer hosts must use the same mods). Custom acts don't make this worse, but worth a one-line note in the mod manifest description.
- **Save migration.** Players with an in-progress vanilla run who enable the flag should not have that run switch to alt mid-stream. Phase 0's "snapshot at run-start" handles this — confirmed via the log: the postfix fires only on `RunManager.Launch`, which runs once per fresh run-creation or save-load, not on map-room transitions.
- **`UnlockState.IsEpochRevealed<UnderdocksEpoch>()` bypass.** Our `GetRandomList` patch must be unconditional in alt-mode, even on a fresh save where Underdocks isn't revealed. Verify against BaseLib `ActModelGetRandomListPatch.cs:22`.
- **`ModelDb.Preload` cache** (resolved). It does cache encounter lists at startup, but because encounters bind to specific acts (`act is Act1Mod`) the cache is correct for the lifetime of the process — only act-list selection varies, and that's read fresh from `GetRandomList` per run.

## Pointers

- BaseLib config: `BaseLib.Config.SimpleModConfig`, `ModConfigRegistry.Register`, attribute set in `BaseLib.Config/*Attribute.cs`.
- BaseLib's own example: `BaseLib.Config/BaseLibConfig.cs` — full reference for the attribute syntax.
- Run-start hook: `RunManager.Launch` (`RunManager.cs:480`) — converges new + loaded run paths via the `RunStarted` event.
- BaseLib act-list patch: `Baselib.Patches.Content/ActModelGetRandomListPatch.cs` — read fully before writing our override patch.
- Hard-coded modifier registry (Phase 4 reference): `ModelDb.cs:225` (`GoodModifiers`), `ModelDb.cs:238` (`BadModifiers`).
- Vanilla modifier example: `MegaCrit.Sts2.Core.Models.Modifiers/DeadlyEvents.cs`.

## Recommended order

1. Phase 0 (config + flag, no behavior change). **DONE**, committed.
2. Phase 2 (first custom act + re-point all encounters). One commit at the end of the phase.
3. Phase 3 (acts 2 and 3).
4. Phase 4 if/when needed.
