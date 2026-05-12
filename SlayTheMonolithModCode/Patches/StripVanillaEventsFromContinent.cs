using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Patches;

// Strips vanilla shared events from TheContinent's generated event pool.
//
// Why: ActModel.GenerateRooms composes the pool as
//   AllEvents.Concat(ModelDb.AllSharedEvents)
// where AllSharedEvents is the 18 hardcoded vanilla "shared" events (plus
// any mod events with empty Acts, courtesy of BaseLib's CustomSharedEvents
// transpiler). For TheContinent we only want mod events -- vanilla flavor
// is jarring in the alt arc. There's no act-level opt-out, so we postfix
// GenerateRooms and prune _rooms.events by mod-id prefix.
//
// The prefix-filter keeps anything starting with our slug ("slaythemonolithmod-")
// and drops everything else. BaseLib's PrefixIdPatch guarantees our custom
// events carry the prefix; vanilla events don't.
[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
internal static class StripVanillaEventsFromContinent
{
    // ModelId.Entry is uppercase (e.g. "SLAYTHEMONOLITHMOD-EXPEDITION_JOURNAL"),
    // so the prefix must match case. A lowercase prefix here previously stripped
    // every event including our own, leaving _rooms.events empty -- which then
    // saved as a missing event_ids field and crashed RoomSet.FromSave on Continue.
    private const string ModSlug = "SLAYTHEMONOLITHMOD-";

    [HarmonyPostfix]
    private static void RemoveVanillaShared(ActModel __instance)
    {
        if (__instance is not TheContinent) return;
        __instance._rooms.events.RemoveAll(e => !e.Id.Entry.StartsWith(ModSlug, StringComparison.Ordinal));
    }
}
