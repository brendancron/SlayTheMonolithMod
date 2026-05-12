using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Hard-pool variant of the Volester swarm. Four Volesters at 9-12 HP each
// with 3 stacks of Flying (50% damage reduction) on combat start, throwing
// 4-6 each per turn. IsWeak defaults to false here, so TheContinent's hard
// rotation picks this one up.
public sealed class VolestersHard : CustomEncounterModel, ILocalizationProvider
{
    public VolestersHard() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => act is TheContinent;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Volester Swarm",
        LossText: "Buried under a storm of shards.");

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Volester>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Volester>().ToMutable(), null),
            (ModelDb.Monster<Volester>().ToMutable(), null),
            (ModelDb.Monster<Volester>().ToMutable(), null),
            (ModelDb.Monster<Volester>().ToMutable(), null),
        };
}
