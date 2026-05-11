using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Patches;

// Vanilla CreatureCmd:324 plays "event:/temp/sfx/game_over" through
// NAudioManager.PlayMusic when the local player dies. Prefix-redirect that
// specific event path to our custom event in the slaythemonolithmod bank.
// Other PlayMusic callers (main menu, etc.) flow through their own patches.
[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.PlayMusic))]
internal static class DeathMusicPatch
{
    private const string VanillaDeathEvent = "event:/temp/sfx/game_over";
    private const string ModDeathEvent = "event:/mods/slaythemonolithmod/game_over_theme";

    [HarmonyPrefix]
    private static void Prefix(ref string music)
    {
        if (music == VanillaDeathEvent)
        {
            music = ModDeathEvent;
        }
    }
}
