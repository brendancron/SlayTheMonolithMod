# BaseLib content-extension feasibility (enemies, events, biomes)

Research date: 2026-05-10. BaseLib version surveyed: 3.1.2 (against `Alchyr.Sts2.BaseLib`). The biomes section was rewritten for 3.1.2 — the rest of the doc dates to 3.0.0 and reflects APIs that did not change between versions.

## TL;DR

- **Enemies + events**: first-class. `CustomMonsterModel`, `CustomEncounterModel`, `CustomEventModel` self-register in their constructors and BaseLib's `AddActContent` Harmony patch injects them into the matching act's pools at runtime. Real examples exist in public mods.
- **Biomes/acts**: first-class as of BaseLib 3.1.2. `BaseLib.Abstracts.CustomActModel` self-registers via `CustomContentDictionary.AddAct`, and `ActModelGetRandomListPatch` slots custom acts into the run-act list opposite the vanilla acts of the same `ActNumber`. Default chest visuals, background scene, ancients, and map-point counts are all provided — you mainly write colors, music banks, ambient SFX, encounter/event rosters, and four background-image paths.

A "Clair Obscur alternate path" mod is therefore feasible in two stages with much-reduced risk in stage 2: (1) drop-in enemies/events that ride the existing acts; (2) a stub `CustomActModel` to validate that a modded act actually carries a run to completion, before committing real biome assets.

## Enemies

**API.** `BaseLib.Abstracts.CustomMonsterModel : MonsterModel, ICustomModel, ISceneConversions`. Override `Id` (from `AbstractModel`) and the standard `MonsterModel` ability/intent/HP machinery; point at assets via `CustomVisualPath` (`.tscn`), `CustomAttackSfx` / `CustomCastSfx` / `CustomDeathSfx`, and `SetupAnimationState(...)` if your spine animation lacks any of the five standard states (Idle/Hit/Attack/Cast/Dead).

**Pool model.** Monsters are *not* added to a global pool — they are pulled only from inside `EncounterModel.GenerateMonsters()`. Registration is implicit: a monster used in zero encounters is dead code.

**Encounters as the pool unit.** `CustomEncounterModel(RoomType roomType, bool autoAdd = true)` registers itself in `CustomContentDictionary.CustomEncounters` from its constructor. At game init, `BaseLib.Patches.Content.AddActContent` Harmony-postfixes every `ActModel` subtype's `GenerateAllEncounters()` and yields any registered encounter whose `IsValidForAct(act)` returns true. Implement e.g. `public override bool IsValidForAct(ActModel act) => act.ActNumber() == 1;` to drop into act 1. `RoomType.Monster | Elite | Boss` decides which slot it competes for. Override `CustomScenePath` to point at a 1920x1080 `Control` scene with named `Marker2D` children for enemy positions; `GenerateMonsters()` returns `(MonsterModel.ToMutable(), markerName)` tuples. Boss encounters additionally need `CustomRunHistoryIconPath` / `CustomRunHistoryIconOutlinePath`.

**Asset paths** (default conventions if you don't override):

- `res://scenes/creature_visuals/<modid>-<class_name>.tscn` — monster visuals
- `res://scenes/encounters/<modid>-<encounter_name>.tscn` — encounter scene
- `res://scenes/backgrounds/<modid>-<encounter_name>/...` — background

**Static visuals are supported.** The whole spine pipeline is null-gated: `NCreatureVisuals._Ready` only builds a `MegaSprite` when the body node's class is `SpineSprite` (`NCreatureVisuals.cs:113, 151-159`); `NCreature._spineAnimator` is nullable; every animation trigger is `_spineAnimator?.SetTrigger(...)` (`NCreature.cs:711, 831`); death-animation polling is gated on `HasSpineAnimation` (`NCombatUi.cs:277`). So a `.tscn` with a plain `Sprite2D` as `%Visuals` works — the cost is no hit/attack/cast/death animation, the creature just pops out on death.

**Required scene structure** (from `NCreatureVisuals._Ready` lines 144-150). Mark each as Godot "Access as Unique Name":

- `%Visuals` (`Node2D` — typically `Sprite2D` for static or `SpineSprite` for animated) — required
- `%Bounds` (`Control`) — required, click/target hitbox
- `%IntentPos` (`Marker2D`) — required, intent icon anchor
- `%CenterPos` (`Marker2D`) — required, VFX spawn point
- `%OrbPos` (`Marker2D`) — optional, falls back to `IntentPos`
- `%TalkPos` (`Marker2D`) — optional
- `%PhobiaModeVisuals` (`Node2D`) — optional, alternate body for phobia accessibility mode

**Other gotchas.** The publicized `MonsterModel` / `EncounterModel` base classes have many required overrides (`AllPossibleMonsters`, `Tags`, etc.); the canonical reference is the game's own `MegaCrit.Sts2.Core.Models.Encounters.SlimesNormal`. For animated monsters, spine `.skel` + atlas + png assets must be Godot-imported by the 4.5.1 MegaDot build.

## Events

**API.** `BaseLib.Abstracts.CustomEventModel : EventModel, ICustomModel, ILocalizationProvider`. Same self-registration pattern (constructor calls `CustomContentDictionary.AddEvent`).

**Targeting.** `public virtual ActModel[] Acts => ...`: return the acts the event belongs to (e.g. `[ModelDb.Act<Overgrowth>(), ModelDb.Act<Hive>()]`), or return `Array.Empty<ActModel>()` to make it a "shared" event injected into every act's pool via the `CustomSharedEvents` transpiler patch.

**Option helpers.** `Option(Func<Task> onChosen, ...)` wires option labels to localization keys at `<eventid>.pages.<key>.options.<methodname>` automatically via `StringHelper.Slugify(method.Name)`.

**Asset paths.** `CustomInitialPortraitPath`, `CustomBackgroundScenePath`, `CustomVfxPath` are all `res://...` paths into your `.pck`. Localization JSON lives under `SlayTheMonolithMod/localization/**/*.json` (already wired up as `<AdditionalFiles>` in the csproj for the analyzer).

**Gotchas.** Events that involve combat must be flagged shared (`IsShared`) for multiplayer compatibility. The page-keyed lookup is brittle to method renames — refactoring an option's method name silently breaks its localization lookup.

## Biomes / acts

**API.** `BaseLib.Abstracts.CustomActModel : ActModel, ICustomModel, ISceneConversions` (`CustomActModel.cs:23`). Constructor takes `(int actNumber, bool autoAdd = true)` and pushes itself onto `CustomContentDictionary.CustomActs` (`CustomActModel.cs:303-310`, `CustomContentDictionary.cs:114-120`). `actNumber = -1` opts out of natural spawning — the act type still exists in `ModelDb.Acts` and can be referenced explicitly, but no run will roll into it. The game's `ActModel.GetDefaultList`/`GetRandomList` are unchanged; BaseLib injects custom acts via a postfix on `GetRandomList` (see "How custom acts enter the run" below).

**What you must override.** Concrete abstract members from `ActModel.cs` (vanilla) and `CustomActModel.cs` (BaseLib defaults you can keep), with one-line example values from `Overgrowth.cs`:

- `MapTraveledColor` / `MapUntraveledColor` / `MapBgColor` (`Color`) — map UI palette. Default override returns Hive's act-2/3 palette; replace it. e.g. `new Color("28231D")` (`Overgrowth.cs:61-65`).
- `BgMusicOptions` (`string[]`) — FMOD event names, e.g. `"event:/music/act1_a1_v1"` (`Overgrowth.cs:49`). Default returns the act-3 events.
- `MusicBankPaths` (`string[]`) — FMOD bank `res://` paths to load, e.g. `"res://banks/desktop/act1_a1.bank"` (`Overgrowth.cs:51`). Default returns the act-3 banks.
- `AmbientSfx` (`string`) — FMOD ambience event, e.g. `"event:/sfx/ambience/act1_ambience"` (`Overgrowth.cs:53`). Default is act-3.
- `BaseNumberOfRooms` (`int`, `protected`) — `CustomActModel` returns 15/14/13 by `ActNumber` and falls back to 15 for non-1/2/3 numbers (`CustomActModel.cs:213-219`); usually no override needed.
- `ChestSpineSkinNameNormal` / `ChestSpineSkinNameStroke` (`string`) — spine skin keys on the shared treasure-chest skeleton, e.g. `"act1"` / `"act1_stroke"` (`Overgrowth.cs:57-59`). `CustomActModel` defaults to `"act3"` / `"act3_stroke"`.
- `ChestOpenSfx` (`string`) — FMOD event, e.g. `"event:/sfx/ui/treasure/treasure_act1"` (`Overgrowth.cs:17`). Default is act-3.
- `BossDiscoveryOrder` (`IEnumerable<EncounterModel>`) — fixed order in which bosses first appear; `CustomActModel` defaults to `Array.Empty<EncounterModel>()` (`CustomActModel.cs:208`), which means the first boss is fully random.
- `AllAncients` (`IEnumerable<AncientEventModel>`) — `CustomActModel` defaults to the vanilla act-1/2/3 ancients keyed off `ActNumber` and throws for any other number (`CustomActModel.cs:196-202`). Override if you want different ancients or a non-1/2/3 act number; the docstring says custom ancients should declare themselves through `CustomAncientModel.IsValidForAct(ActModel)` rather than being added here.
- `AllEvents` (`IEnumerable<EventModel>`) — vanilla events the act ships with. Modded events still come through `AddActContent` (see below); this property is for vanilla event references, e.g. `[ModelDb.Event<AromaOfChaos>(), ...]` (`Overgrowth.cs:28-43`). Returning an empty array is fine if you only want modded events.
- `GenerateAllEncounters()` (`IEnumerable<EncounterModel>`) — vanilla encounters. Same story: modded encounters auto-bind, so an empty array is fine if every encounter is modded.
- `GetUnlockedAncients(UnlockState)` — `CustomActModel` defaults to `AllAncients.ToList()` (i.e. everything unlocked) (`CustomActModel.cs:315-318`).
- `ApplyActDiscoveryOrderModifications(UnlockState)` — `CustomActModel` defaults to a no-op; only Overgrowth uses this for the very first run's tutorial layout (`CustomActModel.cs:324-326`, `Overgrowth.cs:106-123`).
- `GetMapPointTypes(Rng)` — `CustomActModel` provides a vanilla-equivalent default keyed by `ActNumber` (`CustomActModel.cs:333-354`).
- `CustomMapTopBgPath` / `CustomMapMidBgPath` / `CustomMapBotBgPath` (`string`, `protected abstract`) — three `res://` paths to your map-screen PNGs (`CustomActModel.cs:226-230`). Required, no defaults. The vanilla convention `res://images/packed/map/map_bgs/<id>/map_top|middle|bottom_<id>.png` (`ActModel.cs:51-61`) is no longer enforced — point these wherever you like.
- `CustomRestSiteBackgroundPath` (`string`, `protected abstract`) — `res://` path to a rest-site `.tscn` (`CustomActModel.cs:232`). Required.

**Optional virtual overrides:**

- `CustomBackgroundScenePath` — defaults to `"res://BaseLib/scenes/dynamic_background.tscn"`, which is BaseLib's `NDynamicCombatBackground`-driven scene that builds layers at runtime from `BackgroundAssets.BgLayers` (`CustomActModel.cs:224`, `NDynamicCombatBackground.cs`). Keep the default unless you need a hand-authored combat-background scene.
- `CustomGenerateBackgroundAssets(Rng)` — defaults to `new BackgroundAssets("glory", rng)` (`CustomActModel.cs:372-377`), i.e. your act will reuse Glory's bg layer assets until you replace this. The supplied `BaseLib.Utils.CustomBackgroundAssets` class has three constructors: directory-scan (looks for `_fg_` and `_bg_<n>_*.tscn` files in a `res://` directory and builds randomized layer sets), fixed list, and weighted random per layer (`CustomBackgroundAssets.cs:42-103`).
- `CustomChestScene` — `res://` path to a custom treasure-chest visual scene whose root extends `NCustomTreasureRoomChest` (`CustomActModel.cs:240`, `NCustomTreasureRoomChest.cs`). `null` falls back to the vanilla chest using `ChestSpineResourcePath` + `ChestSpineSkinName*`.
- `ChestSpineResourcePath` — vanilla default is `res://animations/backgrounds/treasure_room/chest_room_act_<id>_skel_data.tres` (`ActModel.cs:125`); `CustomActModel` overrides this to the act-3 skel (`CustomActModel.cs:182`). If you don't override it again or set `CustomChestScene`, your act's chest will use the act-3 spine resource with the act-3 skin.
- `CreateMap` is intercepted via `CustomCreateMap(RunState, bool)`, defaulting to `null` to use the standard map (`CustomActModel.cs:362-365`).
- Localization: implement `ILocalizationProvider` and return an `ActLoc(title, ...extraLoc)` (`ActLoc.cs`). Title shows up in the map UI; extras are key/value pairs scoped to your act's locale block. `CustomActModel` does not implement `ILocalizationProvider` itself — add it on your subclass if you have an `ActLoc` to return.

**Asset bundle.** Keyed off whatever paths you return from the `Custom*Path` overrides — there is no longer a hard `FilePathIdentifier`-based convention. The minimum is:

- 3 map BG PNGs (`CustomMapTopBgPath` / `CustomMapMidBgPath` / `CustomMapBotBgPath`). PNGs must be Godot-imported by the 4.5.1 MegaDot build to produce `.ctex` resources, since vanilla `ActModel.MapTopBg` etc. still go through `PreloadManager.Cache.GetCompressedTexture2D` (`ActModel.cs:53, 57, 61`).
- 1 rest-site `.tscn` (`CustomRestSiteBackgroundPath`).
- FMOD banks for `MusicBankPaths`, plus matching event paths in `BgMusicOptions` and `AmbientSfx`. Note `BaseLib.Audio.FmodAudio` can stream `.ogg`/`.mp3`/`.wav` instead of needing real banks (see `docs/custom-music.md`); if you go that route, leave `MusicBankPaths = []` and trigger playback yourself rather than via the act's auto-load.
- Background combat layers: either keep `CustomBackgroundScenePath`'s default (BaseLib dynamic) and supply per-layer `.tscn`s through `CustomGenerateBackgroundAssets` + `CustomBackgroundAssets`, or override `CustomBackgroundScenePath` to a hand-authored `.tscn`. With the default `CustomGenerateBackgroundAssets` you'll see Glory's background layers — fine for prototyping.
- Treasure chest: nothing required if you accept the act-3 spine fallback. For your own visuals, either ship a `chest_room_act_<id>_skel_data.tres` and override `ChestSpineResourcePath` + `ChestSpineSkinName*`, or override `CustomChestScene` with a `.tscn` whose root extends `NCustomTreasureRoomChest` and skip the spine pipeline.

**How custom acts enter the run.** `BaseLib.Patches.Content.ActModelGetRandomListPatch` postfixes the static `ActModel.GetRandomList` (`ActModelGetRandomListPatch.cs:14-51`). For each slot i in [0,1,2], it builds a candidate pool of `array[i]` null entries (weights `2,1,1`) plus every `CustomActModel` whose `ActNumber == i + 1`, then `rng.NextItem` picks one; non-null replaces the vanilla act in that slot. So:

- A `CustomActModel(actNumber: 1)` competes with `Overgrowth`/`Underdocks` at 1/3 probability per matching custom act vs. 2/3 for the vanilla pair (i.e. the slot starts with two null weights covering the vanilla pair).
- A `CustomActModel(actNumber: 2)` or `(3)` is 50/50 against Hive or Glory respectively (1 null vs. 1 custom) — assuming you only register one for that slot.
- The patch is short-circuited for non-multiplayer first-Underdocks runs (`flag && flag2`, lines 20-25), so a fresh save's first run will not roll a custom act-1 until Underdocks has been discovered. This matches the vanilla flow and isn't bypassable without a separate patch.

`ModelDbCustomActsPatch` postfixes the `ModelDb.Acts` getter to append every `CustomContentDictionary.CustomActs` entry (`ModelDbCustomActsPatch.cs:9-21`), so `ModelDb.Act<MyAct>()` and the run-history compendium can resolve them.

**How encounters/events bind to a custom act.** `AddActContent.Patch` walks `ReflectionHelper.GetSubtypes<ActModel>().Chain(ReflectionHelper.GetSubtypesInMods<ActModel>())` and Harmony-postfixes every declared `GenerateAllEncounters()` and `AllEvents` getter (`AddActContent.cs:22-43`). Your `CustomActModel` subclass declares its own `GenerateAllEncounters()` (since `ActModel.GenerateAllEncounters` is abstract — even returning `Array.Empty<EncounterModel>()` counts as a declaration) so the postfix runs and yields any `CustomEncounterModel` whose `IsValidForAct(act)` returns true (`AddActContent.cs:45-58`). For events, `AllEvents` is also abstract on `ActModel`, your override declares it, and the postfix yields any `CustomEventModel` whose `Acts[]` contains your act by `Id` (`AddActContent.cs:61-74`). Shared events (`Acts.Length == 0`) go through a separate path (the `CustomSharedEvents` transpiler patch documented in the Events section), so the act-bound `AddActContent` loop only handles `ActCustomEvents`.

**Gotchas.**

- `BaseLib.Extensions.ActModelExtensions.ActNumber()` was updated to read `ActNumber` directly off `CustomActModel` (`ActModelExtensions.cs:17-21`), so the old `-1` trap for downstream BaseLib code is gone. It still returns `-1` for any non-vanilla, non-`CustomActModel` `ActModel` subtype — so a `CustomActModel(actNumber: -1)` will be reported as `-1` by this extension. Code that branches on act number (`CustomAncientModel.IsValidForAct` defaults included) will treat that as "unknown act."
- `CustomActModel`'s default `AllAncients` throws for any `ActNumber` outside 1/2/3 (`CustomActModel.cs:201`). If you pick a 4+ act number for a "post-Glory" biome, override `AllAncients` (typically to `Array.Empty<AncientEventModel>()` or your own list).
- `CustomActModel`'s defaults for `BgMusicOptions`, `MusicBankPaths`, `AmbientSfx`, `ChestSpineResourcePath`, `ChestSpineSkinName*`, `ChestOpenSfx`, and `CustomGenerateBackgroundAssets` all silently fall back to act-3 / Glory assets. A stub act will compile, run, and look/sound exactly like Glory until you replace each. Useful for the smoke test, embarrassing if shipped.
- Map background PNGs still flow through `PreloadManager.Cache.GetCompressedTexture2D` (`ActModel.cs:53, 57, 61`). Plain PNGs in the `.pck` without the Godot import step won't resolve.
- `SerializableActModel` keys on `Id` (`SerializableActModel.cs:10-12, 32`), so renaming a custom act breaks save and multiplayer-packet compatibility.
- Multiplayer: the act is serialized by `Id`, so the host and all clients must have the same custom-act mod loaded with the same id. Asset hashing on join is out of scope of what was decompiled here — assume mismatched assets between peers is undefined behavior. **Unverified:** whether BaseLib does any version/asset-hash validation for custom acts in the multiplayer handshake.

**Minimal "hello biome" recipe.** Files to create for a stub act that compiles, gets injected, and runs (using `Overgrowth`-equivalent values where defaults are unsuitable):

1. **`SlayTheMonolithModCode/Acts/MyAct.cs`** — `public sealed class MyAct : CustomActModel { public MyAct() : base(actNumber: 1) {} }` plus overrides for the four required `Custom*Path` properties and the three `Map*Color`s. Override `BgMusicOptions = []` and `MusicBankPaths = []` if not shipping FMOD banks (combine with BaseLib `FmodAudio` for music). Return `Array.Empty<EncounterModel>()` from `GenerateAllEncounters` and `Array.Empty<EventModel>()` from `AllEvents` — modded encounters/events bind via the `AddActContent` patch.
2. **Localization JSON** — `SlayTheMonolithMod/localization/en/acts.json` with at least an entry under your act's `Id.Entry` containing `title`. The analyzer will flag missing keys.
3. **3 map PNGs** — under `SlayTheMonolithMod/images/packed/map/map_bgs/<myact_id>/`, named `map_top|middle|bottom_<myact_id>.png` (matching the vanilla convention is convenient but not required since you specify the paths). Must be opened/saved in the Godot editor at least once so the import side-files exist.
4. **Rest-site scene** — `SlayTheMonolithMod/scenes/rest_site/<myact_id>_rest_site.tscn`. Easiest: duplicate `res://scenes/rest_site/overgrowth_rest_site.tscn` from the vanilla decomp and rename.
5. **Register via `MainFile`** — instantiate `new MyAct();` once in `Initialize()` (or anywhere before `ModelDb.InitIds`'s postfix runs); the constructor handles registration. Confirm in `godot.log` that `BaseLibMain` logs your act under "Patching act types for custom encounters and events".

You should now see your act roll into act-1 slots about 1/3 of the time. The combat-background, music, ancients, treasure chest, and bosses will look/sound like Glory until you replace those overrides — the run will still complete.

## Recommended ordering

1. **Build vocabulary first** — ship 2–3 Clair Obscur enemies + 1 event into vanilla acts. Validates the spine/scene/localization pipeline against real-game runs. Free with BaseLib.
2. **Stub one mod biome** — a `CustomActModel(actNumber: 1)` subclass with the bare-minimum overrides from the recipe above and Glory's defaults for everything else. Question to answer: does a run rolled into the modded act complete cleanly through map gen, combat backgrounds, ancients, boss, and reward flow. Cheap with 3.1.2 (no Harmony work, ~50 lines + four placeholder assets).
3. **Only then** — commit to a 3-biome arc, wire up route-selection (note that with one custom act per slot, the vanilla-vs-modded roll is 2/3 vs. 1/3 for slot 0 and 50/50 for slots 1 and 2 — multi-biome alt-paths within a single slot need a separate transpile of `ActModelGetRandomListPatch.AdjustResult` or a different injection point), and produce real assets.

## Pointers

**BaseLib internals** (decompile via `ilspycmd` against `~\.nuget\packages\alchyr.sts2.baselib\3.1.2\lib\net9.0\BaseLib.dll`):

- `BaseLib.Abstracts.CustomActModel`, `CustomMonsterModel`, `CustomEncounterModel`, `CustomEventModel`, `CustomAncientModel` — the five content base classes.
- `BaseLib.Patches.Content.AddActContent` — encounters/events auto-inject into any `ActModel` subtype, including modded ones.
- `BaseLib.Patches.Content.ActModelGetRandomListPatch` — postfix that splices `CustomActs` into `ActModel.GetRandomList` per `ActNumber`.
- `BaseLib.Patches.Content.ModelDbCustomActsPatch` — postfix that appends `CustomActs` onto `ModelDb.Acts`.
- `BaseLib.Patches.Content.CustomContentDictionary` — the in-memory registry; constructors call `AddAct` / `AddEncounter` / `AddEvent` / `AddAncient`.
- `BaseLib.Extensions.ActModelExtensions` — `ActNumber()` extension; reads `CustomActModel.ActNumber` directly in 3.1.2 (no longer a `-1` trap for modded acts).
- `BaseLib.Utils.CustomBackgroundAssets` — three constructors for building `BackgroundAssets` from custom layer paths.
- `BaseLib.BaseLibScenes.Acts.NDynamicCombatBackground` / `NCustomTreasureRoomChest` — base scenes the dynamic background and custom chest pipelines hook against.
- XML doc reference: `~\.nuget\packages\alchyr.sts2.baselib\3.1.2\lib\net9.0\BaseLib.xml`.

**Game internals** (decompile via the technique in `memory/reference_sts2_decompile.md`):

- `MegaCrit.Sts2.Core.Models.ActModel` — `GetDefaultList`/`GetRandomList` hard-coded act roster.
- `MegaCrit.Sts2.Core.Models.Acts.Overgrowth` — canonical full `ActModel` implementation to model a custom one on.
- `MegaCrit.Sts2.Core.Models.Encounters.SlimesNormal` — canonical `EncounterModel`.
- `MegaCrit.Sts2.Core.Models.Events.AromaOfChaos` — canonical `EventModel`.

**Web:**

- BaseLib Wiki: <https://alchyr.github.io/BaseLib-Wiki/>
  - `CustomEncounterModel`: <https://alchyr.github.io/BaseLib-Wiki/docs/models/custom-encounter.html>
  - `CustomEventModel`: <https://alchyr.github.io/BaseLib-Wiki/docs/models/custom-event.html>
  - **Unverified:** whether the wiki has been updated with a `CustomActModel` page since 3.1.2's release; check before re-deriving anything from the decomp.
- Mod template: <https://github.com/Alchyr/ModTemplate-StS2> · wiki: <https://github.com/Alchyr/ModTemplate-StS2/wiki/Setup>
- BaseLib source: <https://github.com/Alchyr/BaseLib-StS2>
- Public BaseLib mods (drop-in content references):
  - <https://github.com/spencerqfox/sts2-custom-mods> — FogOfWar, FrozenHand, DoorRemaker.
  - <https://github.com/jiegec/STS2FirstMod> — minimal Harmony-only example.

**Unverified.**

- Whether any public BaseLib 3.1.2 mod actually ships a `CustomActModel` end-to-end. The API exists and the smoke test is cheap; nothing here was tested against an in-game run.
- Whether multiplayer asset/version handshake validates custom-act content between peers, beyond the `Id`-keyed `SerializableActModel` packet. Assume mismatch is undefined behavior until proven otherwise.
- Whether `CustomGenerateBackgroundAssets`'s default `new BackgroundAssets("glory", rng)` works without Glory's `.pck` assets being on disk — the constructor presumably loads from `res://scenes/backgrounds/glory/...`, which is in the vanilla game `.pck` so this should "just work," but it has not been verified that BaseLib doesn't override the lookup.
