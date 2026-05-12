using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Relics;

// Florrie -- Esquie's rock. Functionally identical to vanilla WingedBoots:
// allows 3 free travels across non-adjacent map points before being used up.
// Awarded as the reward branch of the Archnemesis event (both barter and
// combat outcomes); EventRelicPool is the right registration bucket.
[Pool(typeof(EventRelicPool))]
public sealed class Florrie : CustomRelicModel
{
    private const int FreeTravelsTotal = 3;

    public int _timesUsed;

    public override List<(string, string)>? Localization => new RelicLoc(
        Title: "Florrie",
        Description: $"Allows you to travel to any map node up to {FreeTravelsTotal} times per run, regardless of adjacency.",
        Flavor: "Esquie's rock. He'd missed her.");

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool IsUsedUp => TimesUsed >= FreeTravelsTotal;
    public override bool ShowCounter => !IsUsedUp;
    public override int DisplayAmount => FreeTravelsTotal - TimesUsed;

    [SavedProperty]
    public int TimesUsed
    {
        get => _timesUsed;
        set
        {
            AssertMutable();
            _timesUsed = value;
            InvokeDisplayAmountChanged();
            CheckIfUsedUp();
        }
    }

    public override bool IsAllowed(IRunState runState) => runState.Players.Count == 1;

    public override bool ShouldAllowFreeTravel() => !IsUsedUp;

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (IsUsedUp) return Task.CompletedTask;
        if (Owner.RunState.CurrentRoomCount > 1) return Task.CompletedTask;
        if (Owner.RunState is not RunState runState) return Task.CompletedTask;
        if (runState.VisitedMapCoords.Count <= 1) return Task.CompletedTask;

        var visited = runState.VisitedMapCoords;
        var coord = visited[visited.Count - 2];
        var point = runState.Map.GetPoint(coord);
        if (point == null) return Task.CompletedTask;
        var currentPoint = Owner.RunState.CurrentMapPoint;
        if (currentPoint == null) return Task.CompletedTask;
        if (point.Children.Contains(currentPoint)) return Task.CompletedTask;

        TimesUsed++;
        return Task.CompletedTask;
    }

    private void CheckIfUsedUp()
    {
        if (IsUsedUp) Status = RelicStatus.Disabled;
    }
}
