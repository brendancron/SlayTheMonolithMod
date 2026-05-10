using System.Runtime.CompilerServices;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Runs;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Runs;

// Tags a RunState as alt-mode by inspecting its act list at Launch time. Lets
// runtime systems answer "is the player currently in an alt run?" without re-reading
// the config (which the player may have toggled since the run started). The act list
// itself was decided earlier by AltActListPatch, which reads the config directly —
// so this is just a stable per-run mirror of that decision.
public static class AltRunState
{
    private static readonly ConditionalWeakTable<RunState, object> AltRuns = new();
    private static readonly object Marker = new();

    public static void OnRunStarted(RunState state)
    {
        if (state.Acts.Any(a => a is CustomActModel))
        {
            AltRuns.AddOrUpdate(state, Marker);
            MainFile.Logger.Info("Alt-storyline run started");
        }
        else
        {
            AltRuns.Remove(state);
        }
    }

    public static bool IsAlt(RunState? state) =>
        state != null && AltRuns.TryGetValue(state, out _);
}
