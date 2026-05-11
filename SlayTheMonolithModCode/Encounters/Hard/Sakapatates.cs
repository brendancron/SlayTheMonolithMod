using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Hard-pool regular Sakapatates encounter, modeled on vanilla RubyRaidersNormal
// but with fixed positions: Robust in front, Ranger in the middle, Catapult
// in back. NCombatRoom.PositionEnemies lays enemies out in list order from
// the player side outward, so the order returned here is the order rendered.
public sealed class Sakapatates : CustomEncounterModel, ILocalizationProvider
{
    public Sakapatates() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => act is TheContinent;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Sakapatates",
        LossText: "Trampled by the trio.");

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<RobustSakapatate>(),
        ModelDb.Monster<RangerSakapatate>(),
        ModelDb.Monster<CatapultSakapatate>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<RobustSakapatate>().ToMutable(), null),
            (ModelDb.Monster<RangerSakapatate>().ToMutable(), null),
            (ModelDb.Monster<CatapultSakapatate>().ToMutable(), null),
        };
}
