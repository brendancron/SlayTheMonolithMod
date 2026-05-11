using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Patches;

// Takes over playback of the act music event when a CustomActModel run is
// active so we can pause + resume it at the same timeline position across
// boss/elite combats. Vanilla's NRunMusicController only knows how to
// RESTART the act event from 0 after a CustomBgm interruption — fine for
// vanilla bosses which use FMOD parameter automation to fade themselves out,
// but for our standalone-event bosses the act music ends up repeating its
// intro every time the player finishes an elite/boss fight.
//
// Strategy: postfix UpdateMusic. Let vanilla do bank loading + ambience
// setup, then stop the proxy's tracked event and replace it with our own
// FmodAudio.CreateEventInstance handle. FMOD EventInstance exposes
// set_timeline_position / get_timeline_position so we can save/restore
// position around interruptions.
//
// Only takes over when the run's current act is a CustomActModel. Vanilla
// acts (Overgrowth/Hive/Glory/etc.) are left untouched.
//
// Class-level [HarmonyPatch] is required for PatchAll to discover this class —
// the method-level attributes alone aren't enough.
[HarmonyPatch]
internal static class ContinuousActMusicPatch
{
    private static GodotObject? _actInstance;
    private static string? _activeEventPath;
    private static long _pausedPositionMs;

    [HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.UpdateMusic))]
    [HarmonyPostfix]
    private static void TakeOverActMusic(NRunMusicController __instance)
    {
        var act = __instance._runState?.Act;
        if (act is not CustomActModel) return;
        if (act.BgMusicOptions == null || act.BgMusicOptions.Length == 0) return;

        // Stop the proxy-tracked event vanilla just started, so we don't
        // get duplicate playback.
        StopProxyMusic(__instance);

        EnsureInstance(act.BgMusicOptions[0]);
    }

    [HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.PlayCustomMusic))]
    [HarmonyPrefix]
    private static void OnCustomMusicStart(string customMusic)
    {
        if (_actInstance == null)
        {
            MainFile.Logger.Info($"[ActMusic] PlayCustomMusic({customMusic}) but no _actInstance to pause");
            return;
        }

        var pos = _actInstance.Call("get_timeline_position");
        _pausedPositionMs = pos.AsInt64();
        _actInstance.Call("set_paused", Variant.From(true));
        MainFile.Logger.Info($"[ActMusic] paused at {_pausedPositionMs}ms for {customMusic}");
    }

    [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.EndCombatInternal))]
    [HarmonyPostfix]
    private static void OnCombatEnd(CombatManager __instance)
    {
        var encounter = __instance._state?.Encounter;
        if (encounter is not CustomEncounterModel)
        {
            MainFile.Logger.Info($"[ActMusic] OnCombatEnd: encounter is {encounter?.GetType().Name ?? "null"} (not Custom) — skip");
            return;
        }
        if (string.IsNullOrEmpty(encounter.CustomBgm))
        {
            MainFile.Logger.Info($"[ActMusic] OnCombatEnd: {encounter.GetType().Name} has no CustomBgm — skip");
            return;
        }
        if (_actInstance == null)
        {
            MainFile.Logger.Warn($"[ActMusic] OnCombatEnd: _actInstance is null, can't resume map music");
            return;
        }

        var ctrl = NRunMusicController.Instance;
        if (ctrl != null) StopProxyMusic(ctrl);

        _actInstance.Call("set_timeline_position", Variant.From(_pausedPositionMs));
        _actInstance.Call("set_paused", Variant.From(false));
        MainFile.Logger.Info($"[ActMusic] resumed at {_pausedPositionMs}ms after {encounter.GetType().Name}");
    }

    [HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.StopMusic))]
    [HarmonyPostfix]
    private static void OnStopMusic()
    {
        ReleaseInstance();
    }

    private static void EnsureInstance(string eventPath)
    {
        if (_actInstance != null && _activeEventPath == eventPath)
        {
            // Already playing this event; just unpause if necessary.
            _actInstance.Call("set_paused", Variant.From(false));
            return;
        }
        ReleaseInstance();
        _activeEventPath = eventPath;
        _actInstance = FmodAudio.CreateEventInstance(eventPath);
        if (_actInstance != null)
        {
            _actInstance.Call("start");
            MainFile.Logger.Info($"Continuous act music started: {eventPath}");
        }
        else
        {
            MainFile.Logger.Warn($"FmodAudio.CreateEventInstance returned null for {eventPath}");
        }
    }

    private static void ReleaseInstance()
    {
        if (_actInstance != null)
        {
            _actInstance.Call("stop", Variant.From(0)); // FMOD_STUDIO_STOP_ALLOWFADEOUT
            _actInstance.Call("release");
            _actInstance = null;
        }
        _activeEventPath = null;
        _pausedPositionMs = 0;
    }

    private static void StopProxyMusic(NRunMusicController controller)
    {
        var proxy = controller._proxy;
        if (proxy != null)
        {
            ((GodotObject)proxy).Call("stop_music", Array.Empty<Variant>());
        }
    }
}
