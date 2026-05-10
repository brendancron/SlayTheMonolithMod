# Custom music feasibility

Research date: 2026-05-09. BaseLib version surveyed: 3.0.0.

## TL;DR

STS2 is **FMOD-only** for music — there is no `AudioStreamPlayer` codepath in the shipped audio system. Game install confirms this: `fmod.dll`, `fmodstudio.dll`, and `libGodotFmod.windows.template_release.x86_64.dll` sit alongside the `.exe`, with all banks (`.bank` files) packed inside `SlayTheSpire2.pck` at `res://banks/desktop/*.bank`. The only `AudioStream*` references in the decompile are in `MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager`, a dev-only manager not used for shipped music.

**The escape hatch is excellent.** BaseLib 3.0.0 ships `BaseLib.Utils.FmodAudio` (~1100 lines) — a documented wrapper around the game's FMOD server that includes `PlayFile(absolutePath, ...)` and `PlayMusic(absolutePath, ...)`, which stream arbitrary `.ogg`/`.wav`/`.mp3` files **through FMOD with no bank required**. This is enough to ship custom music for a Clair Obscur mod without ever opening FMOD Studio.

`FmodAudio` carries an `[Obsolete]` warning ("not guaranteed to continue to exist in its current state") — treat it as a moving target across BaseLib versions, but absolutely usable today. Real precedent exists: the **Spire Radio** mod (Nexus Mods #131) lets users drop `.mp3` files into a folder and plays them by room type, confirming the file-based path works in production.

If you want true per-act adaptive music with FMOD's parameter-driven layering (the way base-game acts swap between `Init`/`Enemy`/`Merchant`/`Rest`/`Treasure`/`Elite` etc. via the `Progress` parameter), you need to author a real `.bank` file in FMOD Studio. That's a serious commitment but a well-trodden path — see the FMOD Workflow section.

## Music slots

| Slot | API surface | Type | Mod-friendly? | Recommended approach |
|------|-------------|------|---------------|----------------------|
| **Per-act background music** | `ActModel.BgMusicOptions` (`abstract string[]`) + `ActModel.MusicBankPaths` (`abstract string[]`) | FMOD event paths + bank `res://` paths | Only if you're already implementing a custom `ActModel` (not first-class — see `baselib-feasibility.md`). The bank gets loaded by `NRunMusicController.LoadActBank`, which calls `_proxy.load_act_banks` on the FMOD GDScript proxy. | Real `.bank` authored in FMOD Studio. Ship under `res://banks/desktop/<modid>_act.bank` or load from disk via `FmodAudio.LoadBank(absPath)` at mod init, then reference `event:/mods/<modid>/...` paths from `BgMusicOptions`. |
| **Per-encounter music (combat / elite / boss)** | `EncounterModel.CustomBgm` (`virtual string => ""`) | FMOD event path | **Yes — first-class.** `CombatManager.StartCombatInternal` checks `Encounter.HasBgm` and calls `NRunMusicController.PlayCustomMusic(Encounter.CustomBgm)`. | Two routes: (a) author an `event:/mods/<modid>/...` event in a custom bank, return that path; (b) **simpler** — return `""` from `CustomBgm`, Harmony-postfix `CombatManager.StartCombatInternal` to call `FmodAudio.PlayFile(absPath)` on a streamed `.ogg`. Route (b) sidesteps the proxy and gives up the game's automatic post-combat BGM resume. |
| **Per-encounter ambience** | `EncounterModel.AmbientSfx` (`virtual string => ""`) | FMOD event path | Same as `CustomBgm`. Read in `NRunMusicController.UpdateAmbience`. | Author a looping FMOD event in a custom bank. |
| **Per-event music** | **None.** `BaseLib.Abstracts.CustomEventModel` exposes only `CustomInitialPortraitPath`, `CustomBackgroundScenePath`, `CustomVfxPath`. Events inherit their act's ambience. | n/a | n/a (no API) | Call `FmodAudio.PlayFile(...)` or `PlayEvent(...)` from your event option's `Func<Task> onChosen`. Stop with the returned `GodotObject` handle when the event closes. |
| **One-shot SFX (any time)** | `BaseLib.Utils.FmodAudio.PlayEvent` / `PlayFile` (with cooldowns and pools) | event path or absolute file path | **Yes — fully first-class.** Used internally by `CustomMonsterModel`'s SFX flow (`CustomAttackSfx` etc.). | `FmodAudio.PlayEvent("event:/sfx/heal")` for vanilla events; `FmodAudio.PlayFile(absPath)` for custom files. |
| **Sound replacement (hijack a vanilla event)** | `FmodAudio.RegisterReplacement` / `RegisterFileReplacement` / `RegisterEventReplacement` | Harmony-patches `NAudioManager.PlayOneShot` once, then dispatches | **Yes — clean, multi-mod safe.** | `FmodAudio.RegisterFileReplacement("event:/sfx/heal", absPath)` redirects any `PlayOneShot` of that event. **Caveat**: hooks `PlayOneShot`, not the `update_music` path used by act/encounter BGM, so it does NOT reskin music — only event-style one-shots. |

## FMOD authoring workflow (when you need real banks)

For true adaptive layering with the game's `Progress` parameter (`MusicProgressTrack` enum: `Init`/`Enemy`/`Merchant`/`Rest`/`Treasure`/`Elite`/`CombatEnd`/`Elite2`/`MerchantEnd`):

1. Install **FMOD Studio 2.03.x** (the version BaseLib's `FmodAudio` doc-comments target, also the version corroborated by Spire Radio).
2. Clone <https://github.com/BAKAOLC/STS2_FModProject_Minimal> and open `STS2.fspro`. This is a reverse-engineered minimal project preserving the master bank and mixer routes.
3. **Critical rule from the template README**: never modify `Banks/Master`. Always create your own uniquely-named bank and put events under it. Modifying Master will silence the game.
4. Author events under e.g. `event:/mods/clairobscur/act1_main`. Route them through `bus:/master/music` (the existing music bus) in `Window → Mixer Routing` so user volume sliders apply.
5. `File → Build` produces `Master.bank`, `Master.strings.bank`, and `<YourBank>.bank` in `Build/desktop/`. Ship **only your bank** + `GUIDs.txt` (do NOT ship Master).
6. At mod init: `FmodAudio.LoadBank(Path.Combine(modDir, "audio/clairobscur.bank"))`. Then reference `event:/mods/clairobscur/act1_main` from `CustomBgm` or `BgMusicOptions`.
7. **Licensing**: FMOD is free for indie projects under $200k/yr revenue. Mods themselves don't need a separate license, but content using FMOD events technically falls under FMOD's EULA. Standard hobby-mod risk profile.

## Godot-native escape hatch (the realistic path for hobby mods)

There is no real Godot-native music path — `NRunMusicController._proxy` calls `update_music` / `play_music` / `load_act_banks` on a GDScript proxy that wraps FMOD. The runtime has no `AudioStreamPlayer.Stream = load("res://music.ogg")` codepath for game music.

**The practical equivalent**: `FmodAudio.PlayMusic(string absolutePath, float volume = 1f, float pitch = 1f)` streams an `.ogg`/`.wav`/`.mp3` from disk through FMOD with no bank. Returns an `FmodSound` GodotObject handle exposing `play()`/`stop()`/`set_volume()`/`set_pitch()`/`is_playing()`/`set_paused()`/`release()`.

Sketch for boss/encounter music via Harmony postfix on `CombatManager.StartCombatInternal`:

```csharp
// Pseudocode — actual hook point: postfix CombatManager.StartCombatInternal
// or check the encounter type at NRunMusicController.PlayCustomMusic.
if (combat.Encounter is MyClairObscurBoss && _myMusicHandle == null) {
    NRunMusicController.Instance?.StopMusic();           // silence the act bank
    _myMusicHandle = FmodAudio.PlayMusic(
        Path.Combine(modDir, "music/clea_battle.ogg"), volume: 0.8f);
}
// On combat end / leave room:
_myMusicHandle?.Call("stop");
_myMusicHandle?.Call("release");
_myMusicHandle = null;
NRunMusicController.Instance?.UpdateMusic();             // restore act bgm
```

The `FmodAudio.RegisterFileReplacement` registry is **not** a substitute — it only fires for events routed through `NAudioManager.PlayOneShot`, and music goes through `_proxy.Call("update_music", ...)` which never hits that codepath.

## Existing precedent

- **Spire Radio** (<https://www.nexusmods.com/slaythespire2/mods/131>) — public BaseLib-dependent mod that lets users drag-and-drop `.mp3` files keyed by room type. Strong signal that `FmodAudio.PlayFile`/`PlayMusic` works in practice.
- **STS2-RitsuLib** (<https://github.com/BAKAOLC/STS2-RitsuLib>) — second-tier framework with much richer FMOD support than BaseLib: 40+ files in `Audio/` including `AudioAdaptiveMusicDirector`, `FmodStudioServer`, `FmodStudioStreamingFiles`, `IFmodMusicPlayback`. README states it does not conflict with BaseLib. **If you decide to invest seriously in music, layering RitsuLib alongside BaseLib is likely a better foundation than wrestling with `FmodAudio` directly.**
- **BaseLib wiki** (<https://alchyr.github.io/BaseLib-Wiki/>) has no audio/music page. The `FmodAudio` helper is documented only via XML doc comments inside the package.
- `spencerqfox/sts2-custom-mods` and `jiegec/STS2FirstMod` — neither contains audio assets or FMOD code. No biome-music precedent in the lightweight public mods.

## Pointers

**BaseLib internals** (decompile via `ilspycmd` against `~\.nuget\packages\alchyr.sts2.baselib\3.0.0\lib\net9.0\BaseLib.dll`):

- `BaseLib.Utils.FmodAudio` — the entire BaseLib audio surface. Methods: `PlayEvent`, `PlayEventByGuid`, `CreateEventInstance`, `PlayFile`, `PreloadFile`, `PreloadMusic`, `PlayMusic`, `CreateSoundInstance`, `LoadBank`/`UnloadBank`, `RegisterReplacement`/`RegisterFileReplacement`/`RegisterEventReplacement`, `CreatePool`/`PlayPool`, `StartSnapshot`/`StopSnapshot`, `GetBus`/`SetBusVolume`/`SetBusMute`/`SetBusPaused`, `SetGlobalParameter`/`SetGlobalParameterByLabel`, `EventExists`/`BusExists`, `MuteAll`/`UnmuteAll`/`PauseAll`/`UnpauseAll`, `SetDspBufferSize`, `GetPerformanceData`, `UnloadAll`.
- XML doc reference: `~\.nuget\packages\alchyr.sts2.baselib\3.0.0\lib\net9.0\BaseLib.xml` — search for `FmodAudio`.

**Game internals** (decompile via the technique in `memory/reference_sts2_decompile.md`):

- `MegaCrit.Sts2.Core.Nodes.Audio.NRunMusicController` — game music orchestration. Key methods: `UpdateMusic` (reads `_runState.Act.BgMusicOptions`/`MusicBankPaths`, calls `LoadActBank` then `update_music`), `PlayCustomMusic` (used by combat), `LoadActBank`, `UpdateAmbience`. Also defines the `MusicProgressTrack` enum.
- `MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager` — global audio facade. `PlayOneShot`, `PlayMusic`, `StopMusic`, `SetBgmVol`. All forward to a `_audioNode` proxy via `Call(...)`.
- `MegaCrit.Sts2.Core.Audio.FmodSfx` — string constants with the vanilla event-path scheme (e.g. `"event:/sfx/heal"`).
- `MegaCrit.Sts2.Core.Models.ActModel` — `BgMusicOptions` / `MusicBankPaths` / `AmbientSfx` abstract members.
- `MegaCrit.Sts2.Core.Models.EncounterModel` — `CustomBgm` / `HasBgm` / `AmbientSfx` / `HasAmbientSfx` virtuals.
- `MegaCrit.Sts2.Core.Models.Acts.Overgrowth` — canonical example: `BgMusicOptions = ["event:/music/act1_a1_v1", "event:/music/act1_a2_v2"]`, `MusicBankPaths = ["res://banks/desktop/act1_a1.bank", "res://banks/desktop/act1_a2.bank"]`, `AmbientSfx = "event:/sfx/ambience/act1_ambience"`.
- `MegaCrit.Sts2.Core.Models.Encounters.WaterfallGiantBoss` — canonical `CustomBgm = "event:/music/act1_b_boss_waterfall_giant"`.
- `MegaCrit.Sts2.Core.Combat.CombatManager.StartCombatInternal` — the Harmony hook point for encounter-music override (checks `Encounter.HasBgm`, calls `NRunMusicController.PlayCustomMusic`).

**Web:**

- BaseLib repo: <https://github.com/Alchyr/BaseLib-StS2>
- BaseLib wiki: <https://alchyr.github.io/BaseLib-Wiki/> (no audio page)
- Spire Radio: <https://www.nexusmods.com/slaythespire2/mods/131>
- RitsuLib: <https://github.com/BAKAOLC/STS2-RitsuLib>
- FMOD Studio project template: <https://github.com/BAKAOLC/STS2_FModProject_Minimal>

## Unverified

- Whether `FmodAudio.LoadBank` correctly resolves bank cross-references that point at the game's master bank (i.e. whether a custom bank built with the RitsuLib template works at runtime when only `<YourBank>.bank` is shipped). The template README implies yes; no public mod ships a verifiable example.
- Whether `BgMusicOptions` accepts non-FMOD strings (very unlikely — `_proxy.Call("update_music", path)` is implemented in GDScript that almost certainly funnels into FMOD's `play_event_by_path`).
- Whether stopping `_proxy`'s music via `NRunMusicController.StopMusic()` and then playing a parallel `FmodAudio.PlayMusic(file)` causes mixer/bus conflicts — likely fine (both go through FMOD) but untested.
