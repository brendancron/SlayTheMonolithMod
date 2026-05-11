using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Hard-pool cultist encounter mirroring vanilla CultistsNormal's shape:
// one of each cultist type. Reaper stacks Ritual fast (3 per Incantation)
// while Great Sword opens with a Ritual buff and trades hp for block.
public sealed class Cultists : CustomEncounterModel, ILocalizationProvider
{
    public Cultists() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => act is TheContinent;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Cultists",
        LossText: "The cultists' rites consumed you.");

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<ReaperCultist>(),
        ModelDb.Monster<GreatSwordCultist>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<ReaperCultist>().ToMutable(), null),
            (ModelDb.Monster<GreatSwordCultist>().ToMutable(), null),
        };
}
