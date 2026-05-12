using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

public sealed class GlaiseNormal : CustomEncounterModel, ILocalizationProvider
{
    public GlaiseNormal() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => act is TheContinent;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Glaise",
        LossText: "The Glaise tore through you.");

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Glaise>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Glaise>().ToMutable(), null),
        };
}
