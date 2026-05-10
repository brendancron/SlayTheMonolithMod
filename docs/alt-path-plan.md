# Alternate-storyline implementation plan

Plan date: 2026-05-10. Branch: `biomes`.

## Goal

Custom monsters/encounters/biomes form a self-contained alternate run. Vanilla STS2 runs see no mod content; alt runs see no vanilla acts. Player toggles between modes via a BaseLib mod-config setting.

## TL;DR

Single source-of-truth flag `IsAltRun` is captured at run-start from a `SimpleModConfig` property and frozen for the life of that `RunState`. `CustomEncounterModel.IsValidForAct` reads the flag (gating mod content out of vanilla runs); a Harmony patch on `ActModel.GetRandomList` reads the flag (swapping the act list to all-custom or all-vanilla).

```
mod config (BaseLib UI)  →  [RunManager run-start hook]  →  per-RunState flag (AltRunState)
                                                                         ↓
                                  encounters' IsValidForAct + GetRandomList patch
```

Promotion path to per-run choice (vs. global toggle) is sketched in Phase 4.

## Phase 0 — Config + flag plumbing (no user-visible change)

**New files:**

- `SlayTheMonolithModCode/Config/StoryConfig.cs` — `BaseLib.Config.SimpleModConfig` subclass.
  ```csharp
  [ConfigSection("Storyline")]
  public static bool AlternateStorylineEnabled { get; set; } = false;
  ```
- `SlayTheMonolithModCode/Runs/AltRunState.cs` — static helper mapping `RunState → bool`. Public API `AltRunState.IsAlt(RunState state)`. Internal `OnRunStarted(RunState state)` writes the snapshot, `OnRunEnded(RunState state)` cleans up. Use a `ConditionalWeakTable<RunState, object>` so we don't leak.
- `SlayTheMonolithMod/localization/eng/mod_config.json` — section title, label, description for the toggle. The csproj already includes `SlayTheMonolithMod/localization/**/*.json` as `<AdditionalFiles>` for the analyzer.

**Modified files:**

- `SlayTheMonolithModCode/MainFile.cs` — call `ModConfigRegistry.Register(ModId, new StoryConfig())` from `Initialize()`. Pattern matches BaseLib's own `BaseLibMain` config registration.

**Harmony patch (1 new):**

- `SlayTheMonolithModCode/Patches/RunStartHook.cs` — postfix on whatever `RunManager` method finishes `RunState` construction. Calls `AltRunState.OnRunStarted(state)`. **Investigate during implementation** which method to target. Candidates:
  - `RunManager.SetUpSavedSinglePlayer` (`RunManager.cs:204`)
  - `RunManager.InitializeSavedRun` (`RunManager.cs:345`)
  - `RunManager.CanonicalizeSave` (`RunManager.cs:385`)
  - The path that runs for a *new* run vs. a *loaded* save — both need the hook.

**Smoke test:** Add a `Log.Info` from `OnRunStarted`. Start a run from main menu and verify the log fires once with the expected flag value. Toggle config, start a second run, verify fires again with new value.

## Phase 1 — Encounter gating (validates the wiring before biome work)

**Modified files (mechanical):**

- All 25 `*Normal.cs` files in `SlayTheMonolithModCode/Encounters/`.
  Replace
  ```csharp
  public override bool IsValidForAct(ActModel act) => act is Overgrowth;
  ```
  with
  ```csharp
  public override bool IsValidForAct(ActModel act) =>
      AltRunState.IsAlt(RunManager.Instance.State) && act is Overgrowth;
  ```
- `scripts/generate_monsters.py` ENC_TPL — bake the gated form so regenerated encounters stay consistent.

**Open question:** does `IsValidForAct` get called at a moment when `RunManager.Instance.State` is the *current* run's state, or some other state? BaseLib's `AddActContent.AddCustomEncounters` postfix runs inside `ActModel.GenerateAllEncounters` enumeration which is cached on `ActModel._allEncounters` at *first access*. If first access is during act-list construction in `RunManager.GenerateRun()`, the timing is fine. If first access can happen pre-run (e.g. main-menu preloading), the flag won't be set yet and `AltRunState.IsAlt` would return false. Verify before relying on this.

**Smoke test:**
1. Flag OFF, start vanilla Overgrowth run, walk the map — no mod monsters should appear (try ~5 runs to be statistically safe; current pool injects 25 mod encounters into a pool of ~30 vanilla).
2. Flag ON, start run, walk the map — mod monsters should appear.
3. With current pre-Phase-2 state, *both* mod monsters and vanilla monsters appear in alt-mode (we haven't suppressed vanilla yet). That's expected; Phase 2's `GetRandomList` swap fixes it.

## Phase 2 — First custom act

Per `docs/baselib-feasibility.md` § Biomes/acts: BaseLib 3.1.2's `CustomActModel` does the heavy lifting. Subclass it, override the abstract members listed in that doc, ship the asset bundle.

**New files:**

- `SlayTheMonolithModCode/Acts/Act1Mod.cs` — `CustomActModel(actNumber: 1)`. Override every abstract member from `ActModel.cs` (~12 of them — see feasibility doc for the full list). For audio/SFX/music, point at vanilla Overgrowth's FMOD paths to skip authoring custom audio for the MVP.
- `SlayTheMonolithMod/scenes/backgrounds/<id>/<id>_background.tscn` — placeholder background scene.
- `SlayTheMonolithMod/scenes/rest_site/<id>_rest_site.tscn` — placeholder rest site.
- `SlayTheMonolithMod/images/packed/map/map_bgs/<id>/{map_top,map_middle,map_bottom}_<id>.png` — three map BG layers.

**Modified files:**

- All 25 `*Normal.cs` encounters: re-point `IsValidForAct` from `Overgrowth` to `Act1Mod` (still gated by `AltRunState.IsAlt`). With one act, all 25 still ride the same biome — we'll spread them across acts in Phase 3.
- `scripts/generate_monsters.py` ENC_TPL — update to default to `Act1Mod` and the gate.

**Harmony patch (1 new):**

- `SlayTheMonolithModCode/Patches/AltActListPatch.cs` — postfix on `ActModel.GetRandomList`. Logic:
  - If `AltRunState.IsAlt(RunManager.Instance.State)`: return `[Act1Mod, Act2Mod, Act3Mod]` (placeholders for 2/3 until they exist).
  - Else: filter the list to exclude any `CustomActModel`.
  - Use `[HarmonyPriority(Priority.Low)]` so we run *after* BaseLib's `ActModelGetRandomListPatch.cs:22` which adds custom acts. Our patch has the final word.
  - **Bypass BaseLib's Underdocks-discovery short-circuit when alt-run is on** (per feasibility doc gotcha). Alt runs should always get alt acts regardless of `UnlockState.IsEpochRevealed<UnderdocksEpoch>()`.

**Smoke test:**
1. Flag ON → start run → first act should be `Act1Mod`, only mod monsters appear.
2. Flag OFF → start run → first act is Overgrowth/Underdocks/Hive/Glory (vanilla weights), no mod content.

## Phase 3 — Acts 2 and 3

Repeat Phase 2 twice. Adds `Act2Mod` and `Act3Mod` plus their asset bundles. Spread the 25 encounters across acts (current 25 → maybe 8/8/9 across acts 1/2/3 — tune by difficulty).

The generator script grows a `ActAssignment` column on the table.

## Phase 4 — Optional polish: per-run modifier UI

Replace (or supplement) the global config toggle with a `ModifierModel` subclass that appears in the run-setup screen alongside vanilla modifiers. Lets a player do one vanilla and one alt run with the mod loaded.

Costs:
- New `AlternateStorylineModifier : ModifierModel`.
- Harmony patch to inject into `ModelDb.GoodModifiers` and/or `ModelDb.BadModifiers` (both hard-coded arrays — see `ModelDb.cs:225, 238`).
- Patch the modifier-picker UI if it doesn't auto-pick up reflected `ModifierModel` subtypes.
- Adjust `AltRunState.OnRunStarted` to read from `runState.ActiveModifiers` instead of from the static config property.

Defer until Phases 0–3 are working.

## Risks / unverified

- **Run-start hook timing.** Need to confirm a single `RunManager` method covers both fresh runs and reloaded saves. Worst case we need two patches.
- **`IsValidForAct` evaluation timing.** If BaseLib caches the encounter list before `AltRunState.OnRunStarted` runs, the gate will see a stale flag. We may need to invalidate `ActModel._allEncounters` after run-start, or move the snapshot earlier in the RunManager flow.
- **`GetRandomList` ordering against BaseLib's patch.** `[HarmonyPriority(Priority.Low)]` should suffice but hasn't been tested. Fallback: transpile rather than postfix.
- **Multiplayer.** `ModelIdSerializationCache.Hash` already differs when mod content registers (multiplayer hosts must use the same mods). Custom acts don't make this worse, but worth surfacing in the mod manifest description.
- **Save migration.** Players with an in-progress vanilla run who enable the flag should not have that run switch to alt mid-stream. The Phase-0 "snapshot at run-start" design handles this, but explicit testing is needed: load a vanilla save, toggle config, continue the run, confirm no mod content appears.
- **`UnlockState.IsEpochRevealed<UnderdocksEpoch>()` bypass.** Our `GetRandomList` patch must be unconditional in alt-mode, even on a fresh save where Underdocks isn't revealed. Verify against BaseLib `ActModelGetRandomListPatch.cs:22`.

## Pointers

- BaseLib config: `BaseLib.Config.SimpleModConfig`, `ModConfigRegistry.Register`, attribute set in `BaseLib.Config/*Attribute.cs` (decompile path).
- BaseLib's own example: `BaseLib.Config/BaseLibConfig.cs` — full reference for the attribute syntax (sections, sliders, hidden fields, buttons).
- Run state: `MegaCrit.Sts2.Core.Runs.RunManager`, `RunState`. Active modifiers live on `RunState.ActiveModifiers` (verify field name during implementation).
- Vanilla modifiers (for Phase 4 reference): `MegaCrit.Sts2.Core.Models.Modifiers/DeadlyEvents.cs` is a clean minimal example.
- Hard-coded modifier registry: `ModelDb.cs:225` (`GoodModifiers`), `ModelDb.cs:238` (`BadModifiers`).
- BaseLib act-list patch: `Baselib.Patches.Content/ActModelGetRandomListPatch.cs` — read fully before writing our override patch.

## Recommended order

1. Phase 0 (config + flag, no behavior change). Commit.
2. Phase 1 (encounter gating). Smoke-test ON/OFF. Commit.
3. Phase 2 (first custom act with placeholder assets). Smoke-test alt run lands in custom act 1. Commit.
4. Phases 3 and 4 once 2 is solid.

Each phase ends with a commit on the `biomes` branch so we can roll back cleanly if a phase reveals an architectural problem.
