using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Patches;

// Vanilla BurrowedPower.AfterBlockBroken hard-codes `monster is Tunneler` and
// silently no-ops for any other monster wearing the power. Chalier mirrors
// Tunneler so its shield-break must drive the same chain:
//   GetStunned -> CreatureCmd.Stun(StillDizzyMove, "BITE_MOVE") -> Remove power
//
// We prefix the vanilla method, detect Chalier, swap __result for our own Task,
// and skip the original. For vanilla Tunneler the prefix returns true and the
// original runs unchanged.
[HarmonyPatch(typeof(BurrowedPower), nameof(BurrowedPower.AfterBlockBroken))]
internal static class ChalierBurrowedStunPatch
{
    [HarmonyPrefix]
    private static bool RouteChalierBlockBreak(BurrowedPower __instance, Creature creature, ref Task __result)
    {
        if (creature != __instance.Owner) return true;
        if (__instance.Owner.Monster is not Chalier chalier) return true;

        __result = HandleChalier(__instance, chalier);
        return false;
    }

    private static async Task HandleChalier(BurrowedPower power, Chalier chalier)
    {
        await chalier.GetStunned();
        await CreatureCmd.Stun(power.Owner, chalier.StillDizzyMove, "BITE_MOVE");
        await PowerCmd.Remove<BurrowedPower>(power.Owner);
    }
}
