using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

public sealed class Volesters : CustomEncounterModel, ILocalizationProvider
{
    public Volesters() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => act is TheContinent;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Volesters",
        LossText: "Cut down by a hail of shards.");

    public override bool IsWeak => true;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Volester>(),
    };

    // Three Volesters at 5 dmg each = 15 unblocked per turn against ~14-18 HP each.
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Volester>().ToMutable(), null),
            (ModelDb.Monster<Volester>().ToMutable(), null),
            (ModelDb.Monster<Volester>().ToMutable(), null),
        };
}
