using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Patches;

// Vanilla NMainMenu calls NAudioManager.PlayMusic("event:/music/menu_update")
// at NMainMenu.cs:325 when the menu opens. Prefix-redirect that specific event
// path to our custom menu_music event in the slaythemonolithmod bank. Other
// PlayMusic callers (e.g. game-over screen) are untouched.
[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.PlayMusic))]
internal static class MainMenuMusicPatch
{
    private const string VanillaMenuEvent = "event:/music/menu_update";
    private const string ModMenuEvent = "event:/mods/slaythemonolithmod/menu_music";

    [HarmonyPrefix]
    private static void Prefix(ref string music)
    {
        if (music == VanillaMenuEvent)
        {
            music = ModMenuEvent;
        }
    }
}
