using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using SlayTheMonolithMod.SlayTheMonolithModCode.Runs;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Patches;

// Both new and loaded runs converge on RunManager.Launch (RunManager.cs:480),
// which fires its own RunStarted event after run-state setup is done. Postfix
// here so AltRunState sees a fully-initialized RunState.
[HarmonyPatch(typeof(RunManager), nameof(RunManager.Launch))]
internal static class RunStartHook
{
    [HarmonyPostfix]
    private static void Postfix(RunState __result)
    {
        AltRunState.OnRunStarted(__result);
    }
}
