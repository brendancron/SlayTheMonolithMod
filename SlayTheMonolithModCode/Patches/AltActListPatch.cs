using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using SlayTheMonolithMod.SlayTheMonolithModCode.Config;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Patches;

// Postfix on ActModel.GetRandomList that overrides the act list based on the alt-run
// config flag. Reads StoryConfig directly (not AltRunState) because GetRandomList is
// called from StartRunLobby.cs:414 *before* RunManager.Launch fires the RunStarted
// event, so the per-RunState flag isn't set yet.
//
// HarmonyPriority.Low so we run AFTER BaseLib's ActModelGetRandomListPatch — its
// [2,1,1] weighting may have inserted a CustomActModel into a slot, which we then
// either confirm (alt mode) or strip (vanilla mode).
[HarmonyPatch(typeof(ActModel), nameof(ActModel.GetRandomList))]
[HarmonyPriority(Priority.Low)]
internal static class AltActListPatch
{
    [HarmonyPostfix]
    public static IEnumerable<ActModel> Postfix(IEnumerable<ActModel> __result)
    {
        var list = __result.ToList();
        var defaults = ActModel.GetDefaultList();
        var altMode = StoryConfig.AlternateStorylineEnabled;

        for (int i = 0; i < list.Count; i++)
        {
            if (altMode)
            {
                var custom = CustomContentDictionary.CustomActs
                    .FirstOrDefault(a => a.ActNumber == i + 1);
                if (custom != null)
                {
                    list[i] = custom;
                }
            }
            else if (list[i] is CustomActModel)
            {
                list[i] = i < defaults.Count ? defaults[i] : list[i];
            }
        }

        return list;
    }
}
