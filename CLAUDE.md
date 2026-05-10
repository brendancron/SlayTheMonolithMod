# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

A Slay the Spire 2 mod built from the BaseLib template. The mod is loaded by STS2 at runtime — it is **not** a standalone application. Output is a `.dll` (C# code + Harmony patches) plus a `.pck` (Godot-packaged assets), both dropped into the game's `mods/SlayTheMonolithMod/` directory.

## Build / publish

- `dotnet build` — compiles the mod and the `CopyToModsFolderOnBuild` MSBuild target copies `SlayTheMonolithMod.dll` and `SlayTheMonolithMod.json` to `$(Sts2Path)/mods/SlayTheMonolithMod/`. Use this for fast code-only iteration.
- `dotnet publish` — same, plus the `GodotPublish` target shells out to the configured Godot binary (`--headless --export-pack BasicExport`) to build the `.pck` containing assets. Required whenever assets under `SlayTheMonolithMod/` change.
- `Sts2DataDir` (location of `sts2.dll` / `0Harmony.dll`) is auto-discovered in `Sts2PathDiscovery.props` via Windows registry and Steam library scan. To override, uncomment `Sts2Path` in `Directory.Build.props` or pass `/p:Sts2Path=...`. Build fails fast with a clear error if the path can't be resolved.
- `GodotPath` in `Directory.Build.props` must point at a Godot 4.5.1 mono executable. **The game won't load a `.pck` exported by a newer Godot** — pin to 4.5.1 (MegaDot build) even after Godot itself releases new versions.
- `Directory.Build.props` is gitignored (it carries machine-specific paths). When onboarding a new machine, recreate it from the template values or check git history.
- There are no tests in this project.

## Architecture

**Two-folder layout, intentional split:**
- `SlayTheMonolithModCode/` — C# source. Compiled into the mod DLL.
- `SlayTheMonolithMod/` — Godot assets (images, materials, shaders, localization JSON, `mod_image.png`). The csproj explicitly `<Compile Remove>`s this folder; it is packed into the `.pck` by Godot's export-pack, not by the C# compiler.

The same name is used for the project, the assembly, the asset folder, the manifest, and the mod ID — they must stay in sync if renamed.

**Entry point.** `MainFile.cs` is decorated with `[ModInitializer(nameof(Initialize))]`; BaseLib discovers this attribute at game startup and calls `Initialize()`, which constructs a Harmony instance and runs `PatchAll()`. There is no `Main`. Add new Harmony patch classes anywhere in the assembly — `PatchAll` finds them by attribute.

**Manifest.** `SlayTheMonolithMod.json` declares `id`, `version`, `has_pck`/`has_dll` flags, and dependencies (currently `BaseLib >= v0.3.0`). The id must match `MainFile.ModId`. Bump `version` for releases.

**Accessing internal STS2 APIs.** `Krafs.Publicizer` (`<Publicize Include="sts2" />`) rewrites the referenced `sts2.dll` so private/protected members are accessible from this assembly. This means autocomplete and direct calls to internals work, but those internals can be renamed/removed by the game devs without warning — patches over publicized members are inherently fragile.

**Cross-platform builds.** `<NoWarn>$(NoWarn);MSB3270</NoWarn>` suppresses the architecture-mismatch warning because mods target MSIL (any CPU) while the local `sts2.dll` is platform-specific. Don't switch the build to a platform-specific target.

**Analyzer.** `Alchyr.Sts2.ModAnalyzers` runs at compile time and reads `SlayTheMonolithMod/localization/**/*.json` (declared as `<AdditionalFiles>`) to validate localization keys against C# usage. Localization JSON belongs under that path or the analyzer won't see it.

## Project docs

Design notes, research, and feasibility findings live under `docs/`. Check there before re-researching anything about BaseLib internals or content-extension patterns. Current contents:

- `docs/baselib-feasibility.md` — what BaseLib supports for adding enemies, events, and biomes (verdict: enemies/events first-class; biomes have no API and require a Harmony spike).
- `docs/custom-music.md` — how custom music works (verdict: FMOD-only audio, but BaseLib's `FmodAudio.PlayFile`/`PlayMusic` streams `.ogg`/`.mp3`/`.wav` without needing a real bank).

## Debugging

- In-game mod load errors and BaseLib migration warnings: `%APPDATA%\SlayTheSpire2\logs\godot.log`.
- To inspect STS2 internal types/method signatures (e.g. when writing a Harmony patch target), decompile the publicized `sts2.dll` that lives under `.godot/mono/temp/obj/.../publicized/` after a build, using a tool like `ilspycmd`. The original game DLL is not publicized — only the build-time copy is.
